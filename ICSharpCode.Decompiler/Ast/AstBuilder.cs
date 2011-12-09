// Copyright (c) 2011 AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.Ast.Transforms;
using ICSharpCode.Decompiler.ILAst;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.Utils;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace ICSharpCode.Decompiler.Ast
{
	using Ast = ICSharpCode.NRefactory.CSharp;
	using VarianceModifier = ICSharpCode.NRefactory.TypeSystem.VarianceModifier;
	
	[Flags]
	public enum ConvertTypeOptions
	{
		None = 0,
		IncludeNamespace = 1,
		IncludeTypeParameterDefinitions = 2,
		DoNotUsePrimitiveTypeNames = 4
	}
	
	public class AstBuilder
	{
		DecompilerContext context;
		CompilationUnit astCompileUnit = new CompilationUnit();
		Dictionary<string, NamespaceDeclaration> astNamespaces = new Dictionary<string, NamespaceDeclaration>();
		bool transformationsHaveRun;
		
		public AstBuilder(DecompilerContext context)
		{
			if (context == null)
				throw new ArgumentNullException("context");
			this.context = context;
			this.DecompileMethodBodies = true;
		}
		
		public static bool MemberIsHidden(MemberReference member, DecompilerSettings settings)
		{
			MethodDefinition method = member as MethodDefinition;
			if (method != null) {
				if (method.IsGetter || method.IsSetter || method.IsAddOn || method.IsRemoveOn)
					return true;
				if (settings.AnonymousMethods && method.Name.StartsWith("<", StringComparison.Ordinal) && method.IsCompilerGenerated())
					return true;
			}
			TypeDefinition type = member as TypeDefinition;
			if (type != null && type.DeclaringType != null) {
				if (settings.AnonymousMethods && type.Name.StartsWith("<>c__DisplayClass", StringComparison.Ordinal) && type.IsCompilerGenerated())
					return true;
				if (settings.YieldReturn && YieldReturnDecompiler.IsCompilerGeneratorEnumerator(type))
					return true;
			} else if (type != null && type.IsCompilerGenerated()) {
				if (type.Name.StartsWith("<PrivateImplementationDetails>", StringComparison.Ordinal))
					return true;
				if (type.IsAnonymousType())
					return true;
			}
			FieldDefinition field = member as FieldDefinition;
			if (field != null && field.IsCompilerGenerated()) {
				if (settings.AnonymousMethods && field.Name.StartsWith("CS$<>", StringComparison.Ordinal))
					return true;
				if (settings.AutomaticProperties && field.Name.StartsWith("<", StringComparison.Ordinal) && field.Name.EndsWith("BackingField", StringComparison.Ordinal))
					return true;
			}
			// event-fields are not [CompilerGenerated]
			if (field != null && settings.AutomaticEvents && field.DeclaringType.Events.Any(ev => ev.Name == field.Name))
				return true;
			return false;
		}
		
		/// <summary>
		/// Runs the C# transformations on the compilation unit.
		/// </summary>
		public void RunTransformations()
		{
			RunTransformations(null);
		}
		
		public void RunTransformations(Predicate<IAstTransform> transformAbortCondition)
		{
			TransformationPipeline.RunTransformationsUntil(astCompileUnit, transformAbortCondition, context);
			transformationsHaveRun = true;
		}
		
		/// <summary>
		/// Gets the abstract source tree.
		/// </summary>
		public CompilationUnit CompilationUnit {
			get { return astCompileUnit; }
		}
		
		/// <summary>
		/// Generates C# code from the abstract source tree.
		/// </summary>
		/// <remarks>This method adds ParenthesizedExpressions into the AST, and will run transformations if <see cref="RunTransformations"/> was not called explicitly</remarks>
		public void GenerateCode(ITextOutput output)
		{
			if (!transformationsHaveRun)
				RunTransformations();
			
			astCompileUnit.AcceptVisitor(new InsertParenthesesVisitor { InsertParenthesesForReadability = true }, null);
			var outputFormatter = new TextOutputFormatter(output);
			var formattingPolicy = new CSharpFormattingOptions();
			// disable whitespace in front of parentheses:
			formattingPolicy.SpaceBeforeMethodCallParentheses = false;
			formattingPolicy.SpaceBeforeMethodDeclarationParentheses = false;
			formattingPolicy.SpaceBeforeConstructorDeclarationParentheses = false;
			formattingPolicy.SpaceBeforeDelegateDeclarationParentheses = false;
			astCompileUnit.AcceptVisitor(new CSharpOutputVisitor(outputFormatter, formattingPolicy), null);
		}
		
		public void AddAssembly(AssemblyDefinition assemblyDefinition, bool onlyAssemblyLevel = false)
		{
			if (assemblyDefinition.Name.Version != null) {
				astCompileUnit.AddChild(
					new AttributeSection {
						AttributeTarget = "assembly",
						Attributes = {
							new NRefactory.CSharp.Attribute {
								Type = new SimpleType("AssemblyVersion")
									.WithAnnotation(new TypeReference(
										"System.Reflection", "AssemblyVersionAttribute",
										assemblyDefinition.MainModule, assemblyDefinition.MainModule.TypeSystem.Corlib)),
								Arguments = {
									new PrimitiveExpression(assemblyDefinition.Name.Version.ToString())
								}
							}
						}
					}, AttributedNode.AttributeRole);
			}
			
			ConvertCustomAttributes(astCompileUnit, assemblyDefinition, "assembly");
			ConvertSecurityAttributes(astCompileUnit, assemblyDefinition, "assembly");
			ConvertCustomAttributes(astCompileUnit, assemblyDefinition.MainModule, "module");
			AddTypeForwarderAttributes(astCompileUnit, assemblyDefinition.MainModule, "assembly");
			
			if (!onlyAssemblyLevel) {
				foreach (TypeDefinition typeDef in assemblyDefinition.MainModule.Types) {
					// Skip the <Module> class
					if (typeDef.Name == "<Module>") continue;
					// Skip any hidden types
					if (AstBuilder.MemberIsHidden(typeDef, context.Settings))
						continue;

					AddType(typeDef);
				}
			}
		}
		
		void AddTypeForwarderAttributes(CompilationUnit astCompileUnit, ModuleDefinition module, string target)
		{
			if (!module.HasExportedTypes)
				return;
			foreach (ExportedType type in module.ExportedTypes) {
				if (type.IsForwarder) {
					var forwardedType = CreateTypeOfExpression(new TypeReference(type.Namespace, type.Name, module, type.Scope));
					astCompileUnit.AddChild(
						new AttributeSection {
							AttributeTarget = target,
							Attributes = {
								new NRefactory.CSharp.Attribute {
									Type = new SimpleType("TypeForwardedTo")
										.WithAnnotation(new TypeReference(
											"System.Runtime.CompilerServices", "TypeForwardedToAttribute",
											module, module.TypeSystem.Corlib)),
									Arguments = { forwardedType }
								}
							}
						}, AttributedNode.AttributeRole);
				}
			}
		}
		
		NamespaceDeclaration GetCodeNamespace(string name)
		{
			if (string.IsNullOrEmpty(name)) {
				return null;
			}
			if (astNamespaces.ContainsKey(name)) {
				return astNamespaces[name];
			} else {
				// Create the namespace
				NamespaceDeclaration astNamespace = new NamespaceDeclaration { Name = name };
				astCompileUnit.AddChild(astNamespace, CompilationUnit.MemberRole);
				astNamespaces[name] = astNamespace;
				return astNamespace;
			}
		}
		
		public void AddType(TypeDefinition typeDef)
		{
			var astType = CreateType(typeDef);
			NamespaceDeclaration astNS = GetCodeNamespace(typeDef.Namespace);
			if (astNS != null) {
				astNS.AddChild(astType, NamespaceDeclaration.MemberRole);
			} else {
				astCompileUnit.AddChild(astType, CompilationUnit.MemberRole);
			}
		}
		
		public void AddMethod(MethodDefinition method)
		{
			AstNode node = method.IsConstructor ? (AstNode)CreateConstructor(method) : CreateMethod(method);
			astCompileUnit.AddChild(node, CompilationUnit.MemberRole);
		}

		public void AddProperty(PropertyDefinition property)
		{
			astCompileUnit.AddChild(CreateProperty(property), CompilationUnit.MemberRole);
		}
		
		public void AddField(FieldDefinition field)
		{
			astCompileUnit.AddChild(CreateField(field), CompilationUnit.MemberRole);
		}
		
		public void AddEvent(EventDefinition ev)
		{
			astCompileUnit.AddChild(CreateEvent(ev), CompilationUnit.MemberRole);
		}
		
		/// <summary>
		/// Creates the AST for a type definition.
		/// </summary>
		/// <param name="typeDef"></param>
		/// <returns>TypeDeclaration or DelegateDeclaration.</returns>
		public AttributedNode CreateType(TypeDefinition typeDef)
		{
			// create type
			TypeDefinition oldCurrentType = context.CurrentType;
			context.CurrentType = typeDef;
			TypeDeclaration astType = new TypeDeclaration();
			ConvertAttributes(astType, typeDef);
			astType.AddAnnotation(typeDef);
			astType.Modifiers = ConvertModifiers(typeDef);
			astType.Name = CleanName(typeDef.Name);
			
			if (typeDef.IsEnum) {  // NB: Enum is value type
				astType.ClassType = ClassType.Enum;
				astType.Modifiers &= ~Modifiers.Sealed;
			} else if (typeDef.IsValueType) {
				astType.ClassType = ClassType.Struct;
				astType.Modifiers &= ~Modifiers.Sealed;
			} else if (typeDef.IsInterface) {
				astType.ClassType = ClassType.Interface;
				astType.Modifiers &= ~Modifiers.Abstract;
			} else {
				astType.ClassType = ClassType.Class;
			}
			
			IEnumerable<GenericParameter> genericParameters = typeDef.GenericParameters;
			if (typeDef.DeclaringType != null && typeDef.DeclaringType.HasGenericParameters)
				genericParameters = genericParameters.Skip(typeDef.DeclaringType.GenericParameters.Count);
			astType.TypeParameters.AddRange(MakeTypeParameters(genericParameters));
			astType.Constraints.AddRange(MakeConstraints(genericParameters));
			
			AttributedNode result = astType;
			if (typeDef.IsEnum) {
				long expectedEnumMemberValue = 0;
				bool forcePrintingInitializers = IsFlagsEnum(typeDef);
				foreach (FieldDefinition field in typeDef.Fields) {
					if (field.IsRuntimeSpecialName) {
						// the value__ field
						if (field.FieldType != typeDef.Module.TypeSystem.Int32) {
							astType.AddChild(ConvertType(field.FieldType), TypeDeclaration.BaseTypeRole);
						}
					} else {
						EnumMemberDeclaration enumMember = new EnumMemberDeclaration();
						enumMember.AddAnnotation(field);
						enumMember.Name = CleanName(field.Name);
						long memberValue = (long)CSharpPrimitiveCast.Cast(TypeCode.Int64, field.Constant, false);
						if (forcePrintingInitializers || memberValue != expectedEnumMemberValue) {
							enumMember.AddChild(new PrimitiveExpression(field.Constant), EnumMemberDeclaration.InitializerRole);
						}
						expectedEnumMemberValue = memberValue + 1;
						astType.AddChild(enumMember, TypeDeclaration.MemberRole);
					}
				}
			} else if (typeDef.BaseType != null && typeDef.BaseType.FullName == "System.MulticastDelegate") {
				DelegateDeclaration dd = new DelegateDeclaration();
				dd.Modifiers = astType.Modifiers & ~Modifiers.Sealed;
				dd.Name = astType.Name;
				dd.AddAnnotation(typeDef);
				astType.Attributes.MoveTo(dd.Attributes);
				astType.TypeParameters.MoveTo(dd.TypeParameters);
				astType.Constraints.MoveTo(dd.Constraints);
				foreach (var m in typeDef.Methods) {
					if (m.Name == "Invoke") {
						dd.ReturnType = ConvertType(m.ReturnType, m.MethodReturnType);
						dd.Parameters.AddRange(MakeParameters(m));
						ConvertAttributes(dd, m.MethodReturnType, m.Module);
					}
				}
				result = dd;
			} else {
				// Base type
				if (typeDef.BaseType != null && !typeDef.IsValueType && typeDef.BaseType.FullName != "System.Object") {
					astType.AddChild(ConvertType(typeDef.BaseType), TypeDeclaration.BaseTypeRole);
				}
				foreach (var i in typeDef.Interfaces)
					astType.AddChild(ConvertType(i), TypeDeclaration.BaseTypeRole);
				
				AddTypeMembers(astType, typeDef);

				if (astType.Members.OfType<IndexerDeclaration>().Any(idx => idx.PrivateImplementationType.IsNull)) {
					// Remove the [DefaultMember] attribute if the class contains indexers
					foreach (AttributeSection section in astType.Attributes) {
						foreach (Ast.Attribute attr in section.Attributes) {
							TypeReference tr = attr.Type.Annotation<TypeReference>();
							if (tr != null && tr.Name == "DefaultMemberAttribute" && tr.Namespace == "System.Reflection") {
								attr.Remove();
							}
						}
						if (section.Attributes.Count == 0)
							section.Remove();
					}
				}
			}

			context.CurrentType = oldCurrentType;
			return result;
		}

		internal static string CleanName(string name)
		{
			int pos = name.LastIndexOf('`');
			if (pos >= 0)
				name = name.Substring(0, pos);
			pos = name.LastIndexOf('.');
			if (pos >= 0)
				name = name.Substring(pos + 1);
			return name;
		}

		#region Create TypeOf Expression
		/// <summary>
		/// Creates a typeof-expression for the specified type.
		/// </summary>
		public static TypeOfExpression CreateTypeOfExpression(TypeReference type)
		{
			return new TypeOfExpression(AddEmptyTypeArgumentsForUnboundGenerics(ConvertType(type)));
		}
		
		static AstType AddEmptyTypeArgumentsForUnboundGenerics(AstType type)
		{
			TypeReference typeRef = type.Annotation<TypeReference>();
			if (typeRef == null)
				return type;
			TypeDefinition typeDef = typeRef.Resolve(); // need to resolve to figure out the number of type parameters
			if (typeDef == null || !typeDef.HasGenericParameters)
				return type;
			SimpleType sType = type as SimpleType;
			MemberType mType = type as MemberType;
			if (sType != null) {
				while (typeDef.GenericParameters.Count > sType.TypeArguments.Count) {
					sType.TypeArguments.Add(new SimpleType(""));
				}
			}
			
			if (mType != null) {
				AddEmptyTypeArgumentsForUnboundGenerics(mType.Target);
				
				int outerTypeParamCount = typeDef.DeclaringType == null ? 0 : typeDef.DeclaringType.GenericParameters.Count;
				
				while (typeDef.GenericParameters.Count - outerTypeParamCount > mType.TypeArguments.Count) {
					mType.TypeArguments.Add(new SimpleType(""));
				}
			}
			
			return type;
		}
		#endregion
		
		#region Convert Type Reference
		/// <summary>
		/// Converts a type reference.
		/// </summary>
		/// <param name="type">The Cecil type reference that should be converted into
		/// a type system type reference.</param>
		/// <param name="typeAttributes">Attributes associated with the Cecil type reference.
		/// This is used to support the 'dynamic' type.</param>
		public static AstType ConvertType(TypeReference type, ICustomAttributeProvider typeAttributes = null, ConvertTypeOptions options = ConvertTypeOptions.None)
		{
			int typeIndex = 0;
			return ConvertType(type, typeAttributes, ref typeIndex, options);
		}
		
		static AstType ConvertType(TypeReference type, ICustomAttributeProvider typeAttributes, ref int typeIndex, ConvertTypeOptions options)
		{
			while (type is OptionalModifierType || type is RequiredModifierType) {
				type = ((TypeSpecification)type).ElementType;
			}
			if (type == null) {
				return AstType.Null;
			}
			
			if (type is Mono.Cecil.ByReferenceType) {
				typeIndex++;
				// by reference type cannot be represented in C#; so we'll represent it as a pointer instead
				return ConvertType((type as Mono.Cecil.ByReferenceType).ElementType, typeAttributes, ref typeIndex, options)
					.MakePointerType();
			} else if (type is Mono.Cecil.PointerType) {
				typeIndex++;
				return ConvertType((type as Mono.Cecil.PointerType).ElementType, typeAttributes, ref typeIndex, options)
					.MakePointerType();
			} else if (type is Mono.Cecil.ArrayType) {
				typeIndex++;
				return ConvertType((type as Mono.Cecil.ArrayType).ElementType, typeAttributes, ref typeIndex, options)
					.MakeArrayType((type as Mono.Cecil.ArrayType).Rank);
			} else if (type is GenericInstanceType) {
				GenericInstanceType gType = (GenericInstanceType)type;
				if (gType.ElementType.Namespace == "System" && gType.ElementType.Name == "Nullable`1" && gType.GenericArguments.Count == 1) {
					typeIndex++;
					return new ComposedType {
						BaseType = ConvertType(gType.GenericArguments[0], typeAttributes, ref typeIndex, options),
						HasNullableSpecifier = true
					};
				}
				AstType baseType = ConvertType(gType.ElementType, typeAttributes, ref typeIndex, options & ~ConvertTypeOptions.IncludeTypeParameterDefinitions);
				List<AstType> typeArguments = new List<AstType>();
				foreach (var typeArgument in gType.GenericArguments) {
					typeIndex++;
					typeArguments.Add(ConvertType(typeArgument, typeAttributes, ref typeIndex, options));
				}
				ApplyTypeArgumentsTo(baseType, typeArguments);
				return baseType;
			} else if (type is GenericParameter) {
				return new SimpleType(type.Name);
			} else if (type.IsNested) {
				AstType typeRef = ConvertType(type.DeclaringType, typeAttributes, ref typeIndex, options & ~ConvertTypeOptions.IncludeTypeParameterDefinitions);
				string namepart = ICSharpCode.NRefactory.TypeSystem.ReflectionHelper.SplitTypeParameterCountFromReflectionName(type.Name);
				MemberType memberType = new MemberType { Target = typeRef, MemberName = namepart };
				memberType.AddAnnotation(type);
				if ((options & ConvertTypeOptions.IncludeTypeParameterDefinitions) == ConvertTypeOptions.IncludeTypeParameterDefinitions) {
					AddTypeParameterDefininitionsTo(type, memberType);
				}
				return memberType;
			} else {
				string ns = type.Namespace ?? string.Empty;
				string name = type.Name;
				if (name == null)
					throw new InvalidOperationException("type.Name returned null. Type: " + type.ToString());
				
				if (name == "Object" && ns == "System" && HasDynamicAttribute(typeAttributes, typeIndex)) {
					return new PrimitiveType("dynamic");
				} else {
					if (ns == "System") {
						if ((options & ConvertTypeOptions.DoNotUsePrimitiveTypeNames)
						    != ConvertTypeOptions.DoNotUsePrimitiveTypeNames) {
							switch (name) {
								case "SByte":
									return new PrimitiveType("sbyte");
								case "Int16":
									return new PrimitiveType("short");
								case "Int32":
									return new PrimitiveType("int");
								case "Int64":
									return new PrimitiveType("long");
								case "Byte":
									return new PrimitiveType("byte");
								case "UInt16":
									return new PrimitiveType("ushort");
								case "UInt32":
									return new PrimitiveType("uint");
								case "UInt64":
									return new PrimitiveType("ulong");
								case "String":
									return new PrimitiveType("string");
								case "Single":
									return new PrimitiveType("float");
								case "Double":
									return new PrimitiveType("double");
								case "Decimal":
									return new PrimitiveType("decimal");
								case "Char":
									return new PrimitiveType("char");
								case "Boolean":
									return new PrimitiveType("bool");
								case "Void":
									return new PrimitiveType("void");
								case "Object":
									return new PrimitiveType("object");
							}
						}
					}
					
					name = ICSharpCode.NRefactory.TypeSystem.ReflectionHelper.SplitTypeParameterCountFromReflectionName(name);
					
					AstType astType;
					if ((options & ConvertTypeOptions.IncludeNamespace) == ConvertTypeOptions.IncludeNamespace && ns.Length > 0) {
						string[] parts = ns.Split('.');
						AstType nsType = new SimpleType(parts[0]);
						for (int i = 1; i < parts.Length; i++) {
							nsType = new MemberType { Target = nsType, MemberName = parts[i] };
						}
						astType = new MemberType { Target = nsType, MemberName = name };
					} else {
						astType = new SimpleType(name);
					}
					astType.AddAnnotation(type);
					
					if ((options & ConvertTypeOptions.IncludeTypeParameterDefinitions) == ConvertTypeOptions.IncludeTypeParameterDefinitions) {
						AddTypeParameterDefininitionsTo(type, astType);
					}
					return astType;
				}
			}
		}
		
		static void AddTypeParameterDefininitionsTo(TypeReference type, AstType astType)
		{
			if (type.HasGenericParameters) {
				List<AstType> typeArguments = new List<AstType>();
				foreach (GenericParameter gp in type.GenericParameters) {
					typeArguments.Add(new SimpleType(gp.Name));
				}
				ApplyTypeArgumentsTo(astType, typeArguments);
			}
		}
		
		static void ApplyTypeArgumentsTo(AstType baseType, List<AstType> typeArguments)
		{
			SimpleType st = baseType as SimpleType;
			if (st != null) {
				st.TypeArguments.AddRange(typeArguments);
			}
			MemberType mt = baseType as MemberType;
			if (mt != null) {
				TypeReference type = mt.Annotation<TypeReference>();
				if (type != null) {
					int typeParameterCount;
					ICSharpCode.NRefactory.TypeSystem.ReflectionHelper.SplitTypeParameterCountFromReflectionName(type.Name, out typeParameterCount);
					if (typeParameterCount > typeArguments.Count)
						typeParameterCount = typeArguments.Count;
					mt.TypeArguments.AddRange(typeArguments.GetRange(typeArguments.Count - typeParameterCount, typeParameterCount));
					typeArguments.RemoveRange(typeArguments.Count - typeParameterCount, typeParameterCount);
					if (typeArguments.Count > 0)
						ApplyTypeArgumentsTo(mt.Target, typeArguments);
				} else {
					mt.TypeArguments.AddRange(typeArguments);
				}
			}
		}
		
		const string DynamicAttributeFullName = "System.Runtime.CompilerServices.DynamicAttribute";
		
		static bool HasDynamicAttribute(ICustomAttributeProvider attributeProvider, int typeIndex)
		{
			if (attributeProvider == null || !attributeProvider.HasCustomAttributes)
				return false;
			foreach (CustomAttribute a in attributeProvider.CustomAttributes) {
				if (a.Constructor.DeclaringType.FullName == DynamicAttributeFullName) {
					if (a.ConstructorArguments.Count == 1) {
						CustomAttributeArgument[] values = a.ConstructorArguments[0].Value as CustomAttributeArgument[];
						if (values != null && typeIndex < values.Length && values[typeIndex].Value is bool)
							return (bool)values[typeIndex].Value;
					}
					return true;
				}
			}
			return false;
		}
		#endregion
		
		#region ConvertModifiers
		Modifiers ConvertModifiers(TypeDefinition typeDef)
		{
			Modifiers modifiers = Modifiers.None;
			if (typeDef.IsNestedPrivate)
				modifiers |= Modifiers.Private;
			else if (typeDef.IsNestedAssembly || typeDef.IsNestedFamilyAndAssembly || typeDef.IsNotPublic)
				modifiers |= Modifiers.Internal;
			else if (typeDef.IsNestedFamily)
				modifiers |= Modifiers.Protected;
			else if (typeDef.IsNestedFamilyOrAssembly)
				modifiers |= Modifiers.Protected | Modifiers.Internal;
			else if (typeDef.IsPublic || typeDef.IsNestedPublic)
				modifiers |= Modifiers.Public;
			
			if (typeDef.IsAbstract && typeDef.IsSealed)
				modifiers |= Modifiers.Static;
			else if (typeDef.IsAbstract)
				modifiers |= Modifiers.Abstract;
			else if (typeDef.IsSealed)
				modifiers |= Modifiers.Sealed;
			
			return modifiers;
		}
		
		Modifiers ConvertModifiers(FieldDefinition fieldDef)
		{
			Modifiers modifiers = Modifiers.None;
			if (fieldDef.IsPrivate)
				modifiers |= Modifiers.Private;
			else if (fieldDef.IsAssembly || fieldDef.IsFamilyAndAssembly)
				modifiers |= Modifiers.Internal;
			else if (fieldDef.IsFamily)
				modifiers |= Modifiers.Protected;
			else if (fieldDef.IsFamilyOrAssembly)
				modifiers |= Modifiers.Protected | Modifiers.Internal;
			else if (fieldDef.IsPublic)
				modifiers |= Modifiers.Public;
			
			if (fieldDef.IsLiteral) {
				modifiers |= Modifiers.Const;
			} else {
				if (fieldDef.IsStatic)
					modifiers |= Modifiers.Static;
				
				if (fieldDef.IsInitOnly)
					modifiers |= Modifiers.Readonly;
			}
			
			RequiredModifierType modreq = fieldDef.FieldType as RequiredModifierType;
			if (modreq != null && modreq.ModifierType.FullName == typeof(IsVolatile).FullName)
				modifiers |= Modifiers.Volatile;
			
			return modifiers;
		}
		
		Modifiers ConvertModifiers(MethodDefinition methodDef)
		{
			if (methodDef == null)
				return Modifiers.None;
			Modifiers modifiers = Modifiers.None;
			if (methodDef.IsPrivate)
				modifiers |= Modifiers.Private;
			else if (methodDef.IsAssembly || methodDef.IsFamilyAndAssembly)
				modifiers |= Modifiers.Internal;
			else if (methodDef.IsFamily)
				modifiers |= Modifiers.Protected;
			else if (methodDef.IsFamilyOrAssembly)
				modifiers |= Modifiers.Protected | Modifiers.Internal;
			else if (methodDef.IsPublic)
				modifiers |= Modifiers.Public;
			
			if (methodDef.IsStatic)
				modifiers |= Modifiers.Static;
			
			if (methodDef.IsAbstract) {
				modifiers |= Modifiers.Abstract;
				if (!methodDef.IsNewSlot)
					modifiers |= Modifiers.Override;
			} else if (methodDef.IsFinal) {
				if (!methodDef.IsNewSlot) {
					modifiers |= Modifiers.Sealed | Modifiers.Override;
				}
			} else if (methodDef.IsVirtual) {
				if (methodDef.IsNewSlot)
					modifiers |= Modifiers.Virtual;
				else
					modifiers |= Modifiers.Override;
			}
			if (!methodDef.HasBody && !methodDef.IsAbstract)
				modifiers |= Modifiers.Extern;
			
			return modifiers;
		}

		#endregion
		
		void AddTypeMembers(TypeDeclaration astType, TypeDefinition typeDef)
		{
			// Nested types
			foreach (TypeDefinition nestedTypeDef in typeDef.NestedTypes) {
				if (MemberIsHidden(nestedTypeDef, context.Settings))
					continue;
				var nestedType = CreateType(nestedTypeDef);
				SetNewModifier(nestedType);
				astType.AddChild(nestedType, TypeDeclaration.MemberRole);
			}
			
			// Add fields
			foreach(FieldDefinition fieldDef in typeDef.Fields) {
				if (MemberIsHidden(fieldDef, context.Settings)) continue;
				astType.AddChild(CreateField(fieldDef), TypeDeclaration.MemberRole);
			}
			
			// Add events
			foreach(EventDefinition eventDef in typeDef.Events) {
				astType.AddChild(CreateEvent(eventDef), TypeDeclaration.MemberRole);
			}

			// Add properties
			foreach(PropertyDefinition propDef in typeDef.Properties) {
				astType.Members.Add(CreateProperty(propDef));
			}
			
			// Add methods
			foreach(MethodDefinition methodDef in typeDef.Methods) {
				if (MemberIsHidden(methodDef, context.Settings)) continue;
				
				if (methodDef.IsConstructor)
					astType.Members.Add(CreateConstructor(methodDef));
				else
					astType.Members.Add(CreateMethod(methodDef));
			}
		}

		AttributedNode CreateMethod(MethodDefinition methodDef)
		{
			MethodDeclaration astMethod = new MethodDeclaration();
			astMethod.AddAnnotation(methodDef);
			astMethod.ReturnType = ConvertType(methodDef.ReturnType, methodDef.MethodReturnType);
			astMethod.Name = CleanName(methodDef.Name);
			astMethod.TypeParameters.AddRange(MakeTypeParameters(methodDef.GenericParameters));
			astMethod.Parameters.AddRange(MakeParameters(methodDef));
			// constraints for override and explicit interface implementation methods are inherited from the base method, so they cannot be specified directly
			if (!methodDef.IsVirtual || (methodDef.IsNewSlot && !methodDef.IsPrivate)) astMethod.Constraints.AddRange(MakeConstraints(methodDef.GenericParameters));
			if (!methodDef.DeclaringType.IsInterface) {
				if (IsExplicitInterfaceImplementation(methodDef)) {
					astMethod.PrivateImplementationType = ConvertType(methodDef.Overrides.First().DeclaringType);
				} else {
					astMethod.Modifiers = ConvertModifiers(methodDef);
					if (methodDef.IsVirtual == methodDef.IsNewSlot)
						SetNewModifier(astMethod);
				}
				astMethod.Body = CreateMethodBody(methodDef, astMethod.Parameters);
			}
			ConvertAttributes(astMethod, methodDef);
			if (methodDef.HasCustomAttributes && astMethod.Parameters.Count > 0) {
				foreach (CustomAttribute ca in methodDef.CustomAttributes) {
					if (ca.AttributeType.Name == "ExtensionAttribute" && ca.AttributeType.Namespace == "System.Runtime.CompilerServices") {
						astMethod.Parameters.First().ParameterModifier = ParameterModifier.This;
					}
				}
			}
			
			// Convert MethodDeclaration to OperatorDeclaration if possible
			if (methodDef.IsSpecialName && !methodDef.HasGenericParameters) {
				OperatorType? opType = OperatorDeclaration.GetOperatorType(methodDef.Name);
				if (opType.HasValue) {
					OperatorDeclaration op = new OperatorDeclaration();
					op.CopyAnnotationsFrom(astMethod);
					op.ReturnType = astMethod.ReturnType.Detach();
					op.OperatorType = opType.Value;
					op.Modifiers = astMethod.Modifiers;
					astMethod.Parameters.MoveTo(op.Parameters);
					astMethod.Attributes.MoveTo(op.Attributes);
					op.Body = astMethod.Body.Detach();
					return op;
				}
			}
			return astMethod;
		}
		
		bool IsExplicitInterfaceImplementation(MethodDefinition methodDef)
		{
			return methodDef.HasOverrides && methodDef.IsPrivate;
		}

		IEnumerable<TypeParameterDeclaration> MakeTypeParameters(IEnumerable<GenericParameter> genericParameters)
		{
			foreach (var gp in genericParameters) {
				TypeParameterDeclaration tp = new TypeParameterDeclaration();
				tp.Name = CleanName(gp.Name);
				if (gp.IsContravariant)
					tp.Variance = VarianceModifier.Contravariant;
				else if (gp.IsCovariant)
					tp.Variance = VarianceModifier.Covariant;
				ConvertCustomAttributes(tp, gp);
				yield return tp;
			}
		}
		
		IEnumerable<Constraint> MakeConstraints(IEnumerable<GenericParameter> genericParameters)
		{
			foreach (var gp in genericParameters) {
				Constraint c = new Constraint();
				c.TypeParameter = new SimpleType(CleanName(gp.Name));
				// class/struct must be first
				if (gp.HasReferenceTypeConstraint)
					c.BaseTypes.Add(new PrimitiveType("class"));
				if (gp.HasNotNullableValueTypeConstraint)
					c.BaseTypes.Add(new PrimitiveType("struct"));
				
				foreach (var constraintType in gp.Constraints) {
					if (gp.HasNotNullableValueTypeConstraint && constraintType.FullName == "System.ValueType")
						continue;
					c.BaseTypes.Add(ConvertType(constraintType));
				}
				
				if (gp.HasDefaultConstructorConstraint && !gp.HasNotNullableValueTypeConstraint)
					c.BaseTypes.Add(new PrimitiveType("new")); // new() must be last
				if (c.BaseTypes.Any())
					yield return c;
			}
		}
		
		ConstructorDeclaration CreateConstructor(MethodDefinition methodDef)
		{
			ConstructorDeclaration astMethod = new ConstructorDeclaration();
			astMethod.AddAnnotation(methodDef);
			astMethod.Modifiers = ConvertModifiers(methodDef);
			if (methodDef.IsStatic) {
				// don't show visibility for static ctors
				astMethod.Modifiers &= ~Modifiers.VisibilityMask;
			}
			astMethod.Name = CleanName(methodDef.DeclaringType.Name);
			astMethod.Parameters.AddRange(MakeParameters(methodDef));
			astMethod.Body = CreateMethodBody(methodDef, astMethod.Parameters);
			ConvertAttributes(astMethod, methodDef);
			if (methodDef.IsStatic && methodDef.DeclaringType.IsBeforeFieldInit && !astMethod.Body.IsNull) {
				astMethod.Body.InsertChildAfter(null, new Comment(" Note: this type is marked as 'beforefieldinit'."), AstNode.Roles.Comment);
			}
			return astMethod;
		}

		Modifiers FixUpVisibility(Modifiers m)
		{
			Modifiers v = m & Modifiers.VisibilityMask;
			// If any of the modifiers is public, use that
			if ((v & Modifiers.Public) == Modifiers.Public)
				return Modifiers.Public | (m & ~Modifiers.VisibilityMask);
			// If both modifiers are private, no need to fix anything
			if (v == Modifiers.Private)
				return m;
			// Otherwise, use the other modifiers (internal and/or protected)
			return m & ~Modifiers.Private;
		}

		MemberDeclaration CreateProperty(PropertyDefinition propDef)
		{
			PropertyDeclaration astProp = new PropertyDeclaration();
			astProp.AddAnnotation(propDef);
			var accessor = propDef.GetMethod ?? propDef.SetMethod;
			Modifiers getterModifiers = Modifiers.None;
			Modifiers setterModifiers = Modifiers.None;
			if (IsExplicitInterfaceImplementation(accessor)) {
				astProp.PrivateImplementationType = ConvertType(accessor.Overrides.First().DeclaringType);
			} else if (!propDef.DeclaringType.IsInterface) {
				getterModifiers = ConvertModifiers(propDef.GetMethod);
				setterModifiers = ConvertModifiers(propDef.SetMethod);
				astProp.Modifiers = FixUpVisibility(getterModifiers | setterModifiers);
				try {
					if (accessor.IsVirtual && !accessor.IsNewSlot && (propDef.GetMethod == null || propDef.SetMethod == null)) {
						foreach (var basePropDef in TypesHierarchyHelpers.FindBaseProperties(propDef)) {
							if (basePropDef.GetMethod != null && basePropDef.SetMethod != null) {
								var propVisibilityModifiers = ConvertModifiers(basePropDef.GetMethod) | ConvertModifiers(basePropDef.SetMethod);
								astProp.Modifiers = FixUpVisibility((astProp.Modifiers & ~Modifiers.VisibilityMask) | (propVisibilityModifiers & Modifiers.VisibilityMask));
								break;
							} else if ((basePropDef.GetMethod ?? basePropDef.SetMethod).IsNewSlot) {
								break;
							}
						}
					}
				} catch (ReferenceResolvingException) {
					// TODO: add some kind of notification (a comment?) about possible problems with decompiled code due to unresolved references.
				}
			}
			astProp.Name = CleanName(propDef.Name);
			astProp.ReturnType = ConvertType(propDef.PropertyType, propDef);
			
			if (propDef.GetMethod != null) {
				astProp.Getter = new Accessor();
				astProp.Getter.Body = CreateMethodBody(propDef.GetMethod);
				astProp.Getter.AddAnnotation(propDef.GetMethod);
				ConvertAttributes(astProp.Getter, propDef.GetMethod);
				
				if ((getterModifiers & Modifiers.VisibilityMask) != (astProp.Modifiers & Modifiers.VisibilityMask))
					astProp.Getter.Modifiers = getterModifiers & Modifiers.VisibilityMask;
			}
			if (propDef.SetMethod != null) {
				astProp.Setter = new Accessor();
				astProp.Setter.Body = CreateMethodBody(propDef.SetMethod);
				astProp.Setter.AddAnnotation(propDef.SetMethod);
				ConvertAttributes(astProp.Setter, propDef.SetMethod);
				ParameterDefinition lastParam = propDef.SetMethod.Parameters.LastOrDefault();
				if (lastParam != null) {
					ConvertCustomAttributes(astProp.Setter, lastParam, "param");
					if (lastParam.HasMarshalInfo) {
						astProp.Setter.Attributes.Add(new AttributeSection(ConvertMarshalInfo(lastParam, propDef.Module)) { AttributeTarget = "param" });
					}
				}
				
				if ((setterModifiers & Modifiers.VisibilityMask) != (astProp.Modifiers & Modifiers.VisibilityMask))
					astProp.Setter.Modifiers = setterModifiers & Modifiers.VisibilityMask;
			}
			ConvertCustomAttributes(astProp, propDef);

			MemberDeclaration member = astProp;
			if(propDef.IsIndexer())
				member = ConvertPropertyToIndexer(astProp, propDef);
			if(!accessor.HasOverrides && !accessor.DeclaringType.IsInterface)
				if (accessor.IsVirtual == accessor.IsNewSlot)
					SetNewModifier(member);
			return member;
		}

		IndexerDeclaration ConvertPropertyToIndexer(PropertyDeclaration astProp, PropertyDefinition propDef)
		{
			var astIndexer = new IndexerDeclaration();
			astIndexer.Name = astProp.Name;
			astIndexer.CopyAnnotationsFrom(astProp);
			astProp.Attributes.MoveTo(astIndexer.Attributes);
			astIndexer.Modifiers = astProp.Modifiers;
			astIndexer.PrivateImplementationType = astProp.PrivateImplementationType.Detach();
			astIndexer.ReturnType = astProp.ReturnType.Detach();
			astIndexer.Getter = astProp.Getter.Detach();
			astIndexer.Setter = astProp.Setter.Detach();
			astIndexer.Parameters.AddRange(MakeParameters(propDef.Parameters));
			return astIndexer;
		}
		
		AttributedNode CreateEvent(EventDefinition eventDef)
		{
			if (eventDef.AddMethod != null && eventDef.AddMethod.IsAbstract) {
				// An abstract event cannot be custom
				EventDeclaration astEvent = new EventDeclaration();
				ConvertCustomAttributes(astEvent, eventDef);
				astEvent.AddAnnotation(eventDef);
				astEvent.Variables.Add(new VariableInitializer(CleanName(eventDef.Name)));
				astEvent.ReturnType = ConvertType(eventDef.EventType, eventDef);
				if (!eventDef.DeclaringType.IsInterface)
					astEvent.Modifiers = ConvertModifiers(eventDef.AddMethod);
				return astEvent;
			} else {
				CustomEventDeclaration astEvent = new CustomEventDeclaration();
				ConvertCustomAttributes(astEvent, eventDef);
				astEvent.AddAnnotation(eventDef);
				astEvent.Name = CleanName(eventDef.Name);
				astEvent.ReturnType = ConvertType(eventDef.EventType, eventDef);
				if (eventDef.AddMethod == null || !IsExplicitInterfaceImplementation(eventDef.AddMethod))
					astEvent.Modifiers = ConvertModifiers(eventDef.AddMethod);
				else
					astEvent.PrivateImplementationType = ConvertType(eventDef.AddMethod.Overrides.First().DeclaringType);
				
				if (eventDef.AddMethod != null) {
					astEvent.AddAccessor = new Accessor {
						Body = CreateMethodBody(eventDef.AddMethod)
					}.WithAnnotation(eventDef.AddMethod);
					ConvertAttributes(astEvent.AddAccessor, eventDef.AddMethod);
				}
				if (eventDef.RemoveMethod != null) {
					astEvent.RemoveAccessor = new Accessor {
						Body = CreateMethodBody(eventDef.RemoveMethod)
					}.WithAnnotation(eventDef.RemoveMethod);
					ConvertAttributes(astEvent.RemoveAccessor, eventDef.RemoveMethod);
				}
				MethodDefinition accessor = eventDef.AddMethod ?? eventDef.RemoveMethod;
				if (accessor.IsVirtual == accessor.IsNewSlot) {
					SetNewModifier(astEvent);
				}
				return astEvent;
			}
		}
		
		public bool DecompileMethodBodies { get; set; }
		
		BlockStatement CreateMethodBody(MethodDefinition method, IEnumerable<ParameterDeclaration> parameters = null)
		{
			if (DecompileMethodBodies)
				return AstMethodBodyBuilder.CreateMethodBody(method, context, parameters);
			else
				return null;
		}

		FieldDeclaration CreateField(FieldDefinition fieldDef)
		{
			FieldDeclaration astField = new FieldDeclaration();
			astField.AddAnnotation(fieldDef);
			VariableInitializer initializer = new VariableInitializer(CleanName(fieldDef.Name));
			astField.AddChild(initializer, FieldDeclaration.Roles.Variable);
			astField.ReturnType = ConvertType(fieldDef.FieldType, fieldDef);
			astField.Modifiers = ConvertModifiers(fieldDef);
			if (fieldDef.HasConstant) {
				initializer.Initializer = CreateExpressionForConstant(fieldDef.Constant, fieldDef.FieldType, fieldDef.DeclaringType.IsEnum);
			}
			ConvertAttributes(astField, fieldDef);
			SetNewModifier(astField);
			return astField;
		}
		
		static Expression CreateExpressionForConstant(object constant, TypeReference type, bool isEnumMemberDeclaration = false)
		{
			if (constant == null) {
				if (type.IsValueType && !(type.Namespace == "System" && type.Name == "Nullable`1"))
					return new DefaultValueExpression(ConvertType(type));
				else
					return new NullReferenceExpression();
			} else {
				TypeCode c = Type.GetTypeCode(constant.GetType());
				if (c >= TypeCode.SByte && c <= TypeCode.UInt64 && !isEnumMemberDeclaration) {
					return MakePrimitive((long)CSharpPrimitiveCast.Cast(TypeCode.Int64, constant, false), type);
				} else {
					return new PrimitiveExpression(constant);
				}
			}
		}
		
		public static IEnumerable<ParameterDeclaration> MakeParameters(MethodDefinition method, bool isLambda = false)
		{
			var parameters = MakeParameters(method.Parameters, isLambda);
			if (method.CallingConvention == MethodCallingConvention.VarArg) {
				return parameters.Concat(new[] { new ParameterDeclaration { Type = new PrimitiveType("__arglist") } });
			} else {
				return parameters;
			}
		}
		
		public static IEnumerable<ParameterDeclaration> MakeParameters(IEnumerable<ParameterDefinition> paramCol, bool isLambda = false)
		{
			foreach(ParameterDefinition paramDef in paramCol) {
				ParameterDeclaration astParam = new ParameterDeclaration();
				astParam.AddAnnotation(paramDef);
				if (!(isLambda && paramDef.ParameterType.ContainsAnonymousType()))
					astParam.Type = ConvertType(paramDef.ParameterType, paramDef);
				astParam.Name = paramDef.Name;
				
				if (paramDef.ParameterType is ByReferenceType) {
					astParam.ParameterModifier = (!paramDef.IsIn && paramDef.IsOut) ? ParameterModifier.Out : ParameterModifier.Ref;
					ComposedType ct = astParam.Type as ComposedType;
					if (ct != null && ct.PointerRank > 0)
						ct.PointerRank--;
				}
				
				if (paramDef.HasCustomAttributes) {
					foreach (CustomAttribute ca in paramDef.CustomAttributes) {
						if (ca.AttributeType.Name == "ParamArrayAttribute" && ca.AttributeType.Namespace == "System")
							astParam.ParameterModifier = ParameterModifier.Params;
					}
				}
				if (paramDef.IsOptional) {
					astParam.DefaultExpression = CreateExpressionForConstant(paramDef.Constant, paramDef.ParameterType);
				}
				
				ConvertCustomAttributes(astParam, paramDef);
				ModuleDefinition module = ((MethodDefinition)paramDef.Method).Module;
				if (paramDef.HasMarshalInfo) {
					astParam.Attributes.Add(new AttributeSection(ConvertMarshalInfo(paramDef, module)));
				}
				if (astParam.ParameterModifier != ParameterModifier.Out) {
					if (paramDef.IsIn)
						astParam.Attributes.Add(new AttributeSection(CreateNonCustomAttribute(typeof(InAttribute), module)));
					if (paramDef.IsOut)
						astParam.Attributes.Add(new AttributeSection(CreateNonCustomAttribute(typeof(OutAttribute), module)));
				}
				yield return astParam;
			}
		}
		
		#region ConvertAttributes
		void ConvertAttributes(AttributedNode attributedNode, TypeDefinition typeDefinition)
		{
			ConvertCustomAttributes(attributedNode, typeDefinition);
			ConvertSecurityAttributes(attributedNode, typeDefinition);
			
			// Handle the non-custom attributes:
			#region SerializableAttribute
			if (typeDefinition.IsSerializable)
				attributedNode.Attributes.Add(new AttributeSection(CreateNonCustomAttribute(typeof(SerializableAttribute))));
			#endregion
			
			#region ComImportAttribute
			if (typeDefinition.IsImport)
				attributedNode.Attributes.Add(new AttributeSection(CreateNonCustomAttribute(typeof(ComImportAttribute))));
			#endregion
			
			#region StructLayoutAttribute
			LayoutKind layoutKind = LayoutKind.Auto;
			switch (typeDefinition.Attributes & TypeAttributes.LayoutMask) {
				case TypeAttributes.SequentialLayout:
					layoutKind = LayoutKind.Sequential;
					break;
				case TypeAttributes.ExplicitLayout:
					layoutKind = LayoutKind.Explicit;
					break;
			}
			CharSet charSet = CharSet.None;
			switch (typeDefinition.Attributes & TypeAttributes.StringFormatMask) {
				case TypeAttributes.AnsiClass:
					charSet = CharSet.Ansi;
					break;
				case TypeAttributes.AutoClass:
					charSet = CharSet.Auto;
					break;
				case TypeAttributes.UnicodeClass:
					charSet = CharSet.Unicode;
					break;
			}
			LayoutKind defaultLayoutKind = (typeDefinition.IsValueType && !typeDefinition.IsEnum) ? LayoutKind.Sequential: LayoutKind.Auto;
			if (layoutKind != defaultLayoutKind || charSet != CharSet.Ansi || typeDefinition.PackingSize > 0 || typeDefinition.ClassSize > 0) {
				var structLayout = CreateNonCustomAttribute(typeof(StructLayoutAttribute));
				structLayout.Arguments.Add(new IdentifierExpression("LayoutKind").Member(layoutKind.ToString()));
				if (charSet != CharSet.Ansi) {
					structLayout.AddNamedArgument("CharSet", new IdentifierExpression("CharSet").Member(charSet.ToString()));
				}
				if (typeDefinition.PackingSize > 0) {
					structLayout.AddNamedArgument("Pack", new PrimitiveExpression((int)typeDefinition.PackingSize));
				}
				if (typeDefinition.ClassSize > 0) {
					structLayout.AddNamedArgument("Size", new PrimitiveExpression((int)typeDefinition.ClassSize));
				}
				attributedNode.Attributes.Add(new AttributeSection(structLayout));
			}
			#endregion
		}
		
		void ConvertAttributes(AttributedNode attributedNode, MethodDefinition methodDefinition)
		{
			ConvertCustomAttributes(attributedNode, methodDefinition);
			ConvertSecurityAttributes(attributedNode, methodDefinition);
			
			MethodImplAttributes implAttributes = methodDefinition.ImplAttributes & ~MethodImplAttributes.CodeTypeMask;
			
			#region DllImportAttribute
			if (methodDefinition.HasPInvokeInfo && methodDefinition.PInvokeInfo != null) {
				PInvokeInfo info = methodDefinition.PInvokeInfo;
				Ast.Attribute dllImport = CreateNonCustomAttribute(typeof(DllImportAttribute));
				dllImport.Arguments.Add(new PrimitiveExpression(info.Module.Name));
				
				if (info.IsBestFitDisabled)
					dllImport.AddNamedArgument("BestFitMapping", new PrimitiveExpression(false));
				if (info.IsBestFitEnabled)
					dllImport.AddNamedArgument("BestFitMapping", new PrimitiveExpression(true));
				
				CallingConvention callingConvention;
				switch (info.Attributes & PInvokeAttributes.CallConvMask) {
					case PInvokeAttributes.CallConvCdecl:
						callingConvention = CallingConvention.Cdecl;
						break;
					case PInvokeAttributes.CallConvFastcall:
						callingConvention = CallingConvention.FastCall;
						break;
					case PInvokeAttributes.CallConvStdCall:
						callingConvention = CallingConvention.StdCall;
						break;
					case PInvokeAttributes.CallConvThiscall:
						callingConvention = CallingConvention.ThisCall;
						break;
					case PInvokeAttributes.CallConvWinapi:
						callingConvention = CallingConvention.Winapi;
						break;
					default:
						throw new NotSupportedException("unknown calling convention");
				}
				if (callingConvention != CallingConvention.Winapi)
					dllImport.AddNamedArgument("CallingConvention", new IdentifierExpression("CallingConvention").Member(callingConvention.ToString()));
				
				CharSet charSet = CharSet.None;
				switch (info.Attributes & PInvokeAttributes.CharSetMask) {
					case PInvokeAttributes.CharSetAnsi:
						charSet = CharSet.Ansi;
						break;
					case PInvokeAttributes.CharSetAuto:
						charSet = CharSet.Auto;
						break;
					case PInvokeAttributes.CharSetUnicode:
						charSet = CharSet.Unicode;
						break;
				}
				if (charSet != CharSet.None)
					dllImport.AddNamedArgument("CharSet", new IdentifierExpression("CharSet").Member(charSet.ToString()));
				
				if (!string.IsNullOrEmpty(info.EntryPoint) && info.EntryPoint != methodDefinition.Name)
					dllImport.AddNamedArgument("EntryPoint", new PrimitiveExpression(info.EntryPoint));
				
				if (info.IsNoMangle)
					dllImport.AddNamedArgument("ExactSpelling", new PrimitiveExpression(true));
				
				if ((implAttributes & MethodImplAttributes.PreserveSig) == MethodImplAttributes.PreserveSig)
					implAttributes &= ~MethodImplAttributes.PreserveSig;
				else
					dllImport.AddNamedArgument("PreserveSig", new PrimitiveExpression(false));
				
				if (info.SupportsLastError)
					dllImport.AddNamedArgument("SetLastError", new PrimitiveExpression(true));
				
				if (info.IsThrowOnUnmappableCharDisabled)
					dllImport.AddNamedArgument("ThrowOnUnmappableChar", new PrimitiveExpression(false));
				if (info.IsThrowOnUnmappableCharEnabled)
					dllImport.AddNamedArgument("ThrowOnUnmappableChar", new PrimitiveExpression(true));
				
				attributedNode.Attributes.Add(new AttributeSection(dllImport));
			}
			#endregion
			
			#region PreserveSigAttribute
			if (implAttributes == MethodImplAttributes.PreserveSig) {
				attributedNode.Attributes.Add(new AttributeSection(CreateNonCustomAttribute(typeof(PreserveSigAttribute))));
				implAttributes = 0;
			}
			#endregion
			
			#region MethodImplAttribute
			if (implAttributes != 0) {
				Ast.Attribute methodImpl = CreateNonCustomAttribute(typeof(MethodImplAttribute));
				TypeReference methodImplOptions = new TypeReference(
					"System.Runtime.CompilerServices", "MethodImplOptions",
					methodDefinition.Module, methodDefinition.Module.TypeSystem.Corlib);
				methodImpl.Arguments.Add(MakePrimitive((long)implAttributes, methodImplOptions));
				attributedNode.Attributes.Add(new AttributeSection(methodImpl));
			}
			#endregion
			
			ConvertAttributes(attributedNode, methodDefinition.MethodReturnType, methodDefinition.Module);
		}
		
		void ConvertAttributes(AttributedNode attributedNode, MethodReturnType methodReturnType, ModuleDefinition module)
		{
			ConvertCustomAttributes(attributedNode, methodReturnType, "return");
			if (methodReturnType.HasMarshalInfo) {
				var marshalInfo = ConvertMarshalInfo(methodReturnType, module);
				attributedNode.Attributes.Add(new AttributeSection(marshalInfo) { AttributeTarget = "return" });
			}
		}
		
		internal static void ConvertAttributes(AttributedNode attributedNode, FieldDefinition fieldDefinition, string attributeTarget = null)
		{
			ConvertCustomAttributes(attributedNode, fieldDefinition);
			
			#region FieldOffsetAttribute
			if (fieldDefinition.HasLayoutInfo) {
				Ast.Attribute fieldOffset = CreateNonCustomAttribute(typeof(FieldOffsetAttribute), fieldDefinition.Module);
				fieldOffset.Arguments.Add(new PrimitiveExpression(fieldDefinition.Offset));
				attributedNode.Attributes.Add(new AttributeSection(fieldOffset) { AttributeTarget = attributeTarget });
			}
			#endregion
			
			#region NonSerializedAttribute
			if (fieldDefinition.IsNotSerialized) {
				Ast.Attribute nonSerialized = CreateNonCustomAttribute(typeof(NonSerializedAttribute), fieldDefinition.Module);
				attributedNode.Attributes.Add(new AttributeSection(nonSerialized) { AttributeTarget = attributeTarget });
			}
			#endregion
			
			if (fieldDefinition.HasMarshalInfo) {
				attributedNode.Attributes.Add(new AttributeSection(ConvertMarshalInfo(fieldDefinition, fieldDefinition.Module))  { AttributeTarget = attributeTarget });
			}
		}
		
		#region MarshalAsAttribute (ConvertMarshalInfo)
		static Ast.Attribute ConvertMarshalInfo(IMarshalInfoProvider marshalInfoProvider, ModuleDefinition module)
		{
			MarshalInfo marshalInfo = marshalInfoProvider.MarshalInfo;
			Ast.Attribute attr = CreateNonCustomAttribute(typeof(MarshalAsAttribute), module);
			var unmanagedType = new TypeReference("System.Runtime.InteropServices", "UnmanagedType", module, module.TypeSystem.Corlib);
			attr.Arguments.Add(MakePrimitive((int)marshalInfo.NativeType, unmanagedType));
			
			FixedArrayMarshalInfo fami = marshalInfo as FixedArrayMarshalInfo;
			if (fami != null) {
				attr.AddNamedArgument("SizeConst", new PrimitiveExpression(fami.Size));
				if (fami.ElementType != NativeType.None)
					attr.AddNamedArgument("ArraySubType", MakePrimitive((int)fami.ElementType, unmanagedType));
			}
			SafeArrayMarshalInfo sami = marshalInfo as SafeArrayMarshalInfo;
			if (sami != null && sami.ElementType != VariantType.None) {
				var varEnum = new TypeReference("System.Runtime.InteropServices", "VarEnum", module, module.TypeSystem.Corlib);
				attr.AddNamedArgument("SafeArraySubType", MakePrimitive((int)sami.ElementType, varEnum));
			}
			ArrayMarshalInfo ami = marshalInfo as ArrayMarshalInfo;
			if (ami != null) {
				if (ami.ElementType != NativeType.Max)
					attr.AddNamedArgument("ArraySubType", MakePrimitive((int)ami.ElementType, unmanagedType));
				if (ami.Size >= 0)
					attr.AddNamedArgument("SizeConst", new PrimitiveExpression(ami.Size));
				if (ami.SizeParameterMultiplier != 0 && ami.SizeParameterIndex >= 0)
					attr.AddNamedArgument("SizeParamIndex", new PrimitiveExpression(ami.SizeParameterIndex));
			}
			CustomMarshalInfo cmi = marshalInfo as CustomMarshalInfo;
			if (cmi != null) {
				attr.AddNamedArgument("MarshalType", new PrimitiveExpression(cmi.ManagedType.FullName));
				if (!string.IsNullOrEmpty(cmi.Cookie))
					attr.AddNamedArgument("MarshalCookie", new PrimitiveExpression(cmi.Cookie));
			}
			FixedSysStringMarshalInfo fssmi = marshalInfo as FixedSysStringMarshalInfo;
			if (fssmi != null) {
				attr.AddNamedArgument("SizeConst", new PrimitiveExpression(fssmi.Size));
			}
			return attr;
		}
		#endregion
		
		Ast.Attribute CreateNonCustomAttribute(Type attributeType)
		{
			return CreateNonCustomAttribute(attributeType, context.CurrentType != null ? context.CurrentType.Module : null);
		}
		
		static Ast.Attribute CreateNonCustomAttribute(Type attributeType, ModuleDefinition module)
		{
			Debug.Assert(attributeType.Name.EndsWith("Attribute", StringComparison.Ordinal));
			Ast.Attribute attr = new Ast.Attribute();
			attr.Type = new SimpleType(attributeType.Name.Substring(0, attributeType.Name.Length - "Attribute".Length));
			if (module != null) {
				attr.Type.AddAnnotation(new TypeReference(attributeType.Namespace, attributeType.Name, module, module.TypeSystem.Corlib));
			}
			return attr;
		}
		
		static void ConvertCustomAttributes(AstNode attributedNode, ICustomAttributeProvider customAttributeProvider, string attributeTarget = null)
		{
			if (customAttributeProvider.HasCustomAttributes) {
				var attributes = new List<ICSharpCode.NRefactory.CSharp.Attribute>();
				foreach (var customAttribute in customAttributeProvider.CustomAttributes.OrderBy(a => a.AttributeType.FullName)) {
					if (customAttribute.AttributeType.Name == "ExtensionAttribute" && customAttribute.AttributeType.Namespace == "System.Runtime.CompilerServices") {
						// don't show the ExtensionAttribute (it's converted to the 'this' modifier)
						continue;
					}
					if (customAttribute.AttributeType.Name == "ParamArrayAttribute" && customAttribute.AttributeType.Namespace == "System") {
						// don't show the ParamArrayAttribute (it's converted to the 'params' modifier)
						continue;
					}
					
					var attribute = new ICSharpCode.NRefactory.CSharp.Attribute();
					attribute.AddAnnotation(customAttribute);
					attribute.Type = ConvertType(customAttribute.AttributeType);
					attributes.Add(attribute);
					
					SimpleType st = attribute.Type as SimpleType;
					if (st != null && st.Identifier.EndsWith("Attribute", StringComparison.Ordinal)) {
						st.Identifier = st.Identifier.Substring(0, st.Identifier.Length - "Attribute".Length);
					}

					if(customAttribute.HasConstructorArguments) {
						foreach (var parameter in customAttribute.ConstructorArguments) {
							Expression parameterValue = ConvertArgumentValue(parameter);
							attribute.Arguments.Add(parameterValue);
						}
					}
					if (customAttribute.HasProperties) {
						TypeDefinition resolvedAttributeType = customAttribute.AttributeType.Resolve();
						foreach (var propertyNamedArg in customAttribute.Properties) {
							var propertyReference = resolvedAttributeType != null ? resolvedAttributeType.Properties.FirstOrDefault(pr => pr.Name == propertyNamedArg.Name) : null;
							var propertyName = new IdentifierExpression(propertyNamedArg.Name).WithAnnotation(propertyReference);
							var argumentValue = ConvertArgumentValue(propertyNamedArg.Argument);
							attribute.Arguments.Add(new AssignmentExpression(propertyName, argumentValue));
						}
					}

					if (customAttribute.HasFields) {
						TypeDefinition resolvedAttributeType = customAttribute.AttributeType.Resolve();
						foreach (var fieldNamedArg in customAttribute.Fields) {
							var fieldReference = resolvedAttributeType != null ? resolvedAttributeType.Fields.FirstOrDefault(f => f.Name == fieldNamedArg.Name) : null;
							var fieldName = new IdentifierExpression(fieldNamedArg.Name).WithAnnotation(fieldReference);
							var argumentValue = ConvertArgumentValue(fieldNamedArg.Argument);
							attribute.Arguments.Add(new AssignmentExpression(fieldName, argumentValue));
						}
					}
				}

				if (attributeTarget == "module" || attributeTarget == "assembly") {
					// use separate section for each attribute
					foreach (var attribute in attributes) {
						var section = new AttributeSection();
						section.AttributeTarget = attributeTarget;
						section.Attributes.Add(attribute);
						attributedNode.AddChild(section, AttributedNode.AttributeRole);
					}
				} else if (attributes.Count > 0) {
					// use single section for all attributes
					var section = new AttributeSection();
					section.AttributeTarget = attributeTarget;
					section.Attributes.AddRange(attributes);
					attributedNode.AddChild(section, AttributedNode.AttributeRole);
				}
			}
		}
		
		static void ConvertSecurityAttributes(AstNode attributedNode, ISecurityDeclarationProvider secDeclProvider, string attributeTarget = null)
		{
			if (!secDeclProvider.HasSecurityDeclarations)
				return;
			var attributes = new List<ICSharpCode.NRefactory.CSharp.Attribute>();
			foreach (var secDecl in secDeclProvider.SecurityDeclarations.OrderBy(d => d.Action)) {
				foreach (var secAttribute in secDecl.SecurityAttributes.OrderBy(a => a.AttributeType.FullName)) {
					var attribute = new ICSharpCode.NRefactory.CSharp.Attribute();
					attribute.AddAnnotation(secAttribute);
					attribute.Type = ConvertType(secAttribute.AttributeType);
					attributes.Add(attribute);
					
					SimpleType st = attribute.Type as SimpleType;
					if (st != null && st.Identifier.EndsWith("Attribute", StringComparison.Ordinal)) {
						st.Identifier = st.Identifier.Substring(0, st.Identifier.Length - "Attribute".Length);
					}
					
					var module = secAttribute.AttributeType.Module;
					var securityActionType = new TypeReference("System.Security.Permissions", "SecurityAction", module, module.TypeSystem.Corlib);
					attribute.Arguments.Add(MakePrimitive((int)secDecl.Action, securityActionType));
					
					if (secAttribute.HasProperties) {
						TypeDefinition resolvedAttributeType = secAttribute.AttributeType.Resolve();
						foreach (var propertyNamedArg in secAttribute.Properties) {
							var propertyReference = resolvedAttributeType != null ? resolvedAttributeType.Properties.FirstOrDefault(pr => pr.Name == propertyNamedArg.Name) : null;
							var propertyName = new IdentifierExpression(propertyNamedArg.Name).WithAnnotation(propertyReference);
							var argumentValue = ConvertArgumentValue(propertyNamedArg.Argument);
							attribute.Arguments.Add(new AssignmentExpression(propertyName, argumentValue));
						}
					}

					if (secAttribute.HasFields) {
						TypeDefinition resolvedAttributeType = secAttribute.AttributeType.Resolve();
						foreach (var fieldNamedArg in secAttribute.Fields) {
							var fieldReference = resolvedAttributeType != null ? resolvedAttributeType.Fields.FirstOrDefault(f => f.Name == fieldNamedArg.Name) : null;
							var fieldName = new IdentifierExpression(fieldNamedArg.Name).WithAnnotation(fieldReference);
							var argumentValue = ConvertArgumentValue(fieldNamedArg.Argument);
							attribute.Arguments.Add(new AssignmentExpression(fieldName, argumentValue));
						}
					}
				}
			}
			if (attributeTarget == "module" || attributeTarget == "assembly") {
				// use separate section for each attribute
				foreach (var attribute in attributes) {
					var section = new AttributeSection();
					section.AttributeTarget = attributeTarget;
					section.Attributes.Add(attribute);
					attributedNode.AddChild(section, AttributedNode.AttributeRole);
				}
			} else if (attributes.Count > 0) {
				// use single section for all attributes
				var section = new AttributeSection();
				section.AttributeTarget = attributeTarget;
				section.Attributes.AddRange(attributes);
				attributedNode.AddChild(section, AttributedNode.AttributeRole);
			}
		}
		
		private static Expression ConvertArgumentValue(CustomAttributeArgument argument)
		{
			if (argument.Value is CustomAttributeArgument[]) {
				ArrayInitializerExpression arrayInit = new ArrayInitializerExpression();
				foreach (CustomAttributeArgument element in (CustomAttributeArgument[])argument.Value) {
					arrayInit.Elements.Add(ConvertArgumentValue(element));
				}
				ArrayType arrayType = argument.Type as ArrayType;
				return new ArrayCreateExpression {
					Type = ConvertType(arrayType != null ? arrayType.ElementType : argument.Type),
					AdditionalArraySpecifiers = { new ArraySpecifier() },
					Initializer = arrayInit
				};
			} else if (argument.Value is CustomAttributeArgument) {
				// occurs with boxed arguments
				return ConvertArgumentValue((CustomAttributeArgument)argument.Value);
			}
			var type = argument.Type.Resolve();
			if (type != null && type.IsEnum) {
				return MakePrimitive(Convert.ToInt64(argument.Value), type);
			} else if (argument.Value is TypeReference) {
				return CreateTypeOfExpression((TypeReference)argument.Value);
			} else {
				return new PrimitiveExpression(argument.Value);
			}
		}
		#endregion

		internal static Expression MakePrimitive(long val, TypeReference type)
		{
			if (TypeAnalysis.IsBoolean(type) && val == 0)
				return new Ast.PrimitiveExpression(false);
			else if (TypeAnalysis.IsBoolean(type) && val == 1)
				return new Ast.PrimitiveExpression(true);
			else if (val == 0 && type is PointerType)
				return new Ast.NullReferenceExpression();
			if (type != null)
			{ // cannot rely on type.IsValueType, it's not set for typerefs (but is set for typespecs)
				TypeDefinition enumDefinition = type.Resolve();
				if (enumDefinition != null && enumDefinition.IsEnum) {
					TypeCode enumBaseTypeCode = TypeCode.Int32;
					foreach (FieldDefinition field in enumDefinition.Fields) {
						if (field.IsStatic && object.Equals(CSharpPrimitiveCast.Cast(TypeCode.Int64, field.Constant, false), val))
							return ConvertType(type).Member(field.Name).WithAnnotation(field);
						else if (!field.IsStatic && field.IsRuntimeSpecialName)
							enumBaseTypeCode = TypeAnalysis.GetTypeCode(field.FieldType); // use primitive type of the enum
					}
					if (IsFlagsEnum(enumDefinition)) {
						long enumValue = val;
						Expression expr = null;
						long negatedEnumValue = ~val;
						// limit negatedEnumValue to the appropriate range
						switch (enumBaseTypeCode) {
							case TypeCode.Byte:
							case TypeCode.SByte:
								negatedEnumValue &= byte.MaxValue;
								break;
							case TypeCode.Int16:
							case TypeCode.UInt16:
								negatedEnumValue &= ushort.MaxValue;
								break;
							case TypeCode.Int32:
							case TypeCode.UInt32:
								negatedEnumValue &= uint.MaxValue;
								break;
						}
						Expression negatedExpr = null;
						foreach (FieldDefinition field in enumDefinition.Fields.Where(fld => fld.IsStatic)) {
							long fieldValue = (long)CSharpPrimitiveCast.Cast(TypeCode.Int64, field.Constant, false);
							if (fieldValue == 0)
								continue;	// skip None enum value

							if ((fieldValue & enumValue) == fieldValue) {
								var fieldExpression = ConvertType(type).Member(field.Name).WithAnnotation(field);
								if (expr == null)
									expr = fieldExpression;
								else
									expr = new BinaryOperatorExpression(expr, BinaryOperatorType.BitwiseOr, fieldExpression);

								enumValue &= ~fieldValue;
							}
							if ((fieldValue & negatedEnumValue) == fieldValue) {
								var fieldExpression = ConvertType(type).Member(field.Name).WithAnnotation(field);
								if (negatedExpr == null)
									negatedExpr = fieldExpression;
								else
									negatedExpr = new BinaryOperatorExpression(negatedExpr, BinaryOperatorType.BitwiseOr, fieldExpression);

								negatedEnumValue &= ~fieldValue;
							}
						}
						if (enumValue == 0 && expr != null) {
							if (!(negatedEnumValue == 0 && negatedExpr != null && negatedExpr.Descendants.Count() < expr.Descendants.Count())) {
								return expr;
							}
						}
						if (negatedEnumValue == 0 && negatedExpr != null) {
							return new UnaryOperatorExpression(UnaryOperatorType.BitNot, negatedExpr);
						}
					}
					return new Ast.PrimitiveExpression(CSharpPrimitiveCast.Cast(enumBaseTypeCode, val, false)).CastTo(ConvertType(type));
				}
			}
			TypeCode code = TypeAnalysis.GetTypeCode(type);
			if (code == TypeCode.Object || code == TypeCode.Empty)
				code = TypeCode.Int32;
			return new Ast.PrimitiveExpression(CSharpPrimitiveCast.Cast(code, val, false));
		}

		static bool IsFlagsEnum(TypeDefinition type)
		{
			if (!type.HasCustomAttributes)
				return false;

			return type.CustomAttributes.Any(attr => attr.AttributeType.FullName == "System.FlagsAttribute");
		}

		/// <summary>
		/// Sets new modifier if the member hides some other member from a base type.
		/// </summary>
		/// <param name="member">The node of the member which new modifier state should be determined.</param>
		static void SetNewModifier(AttributedNode member)
		{
			try {
				bool addNewModifier = false;
				if (member is IndexerDeclaration) {
					var propertyDef = member.Annotation<PropertyDefinition>();
					var baseProperties =
						TypesHierarchyHelpers.FindBaseProperties(propertyDef);
					addNewModifier = baseProperties.Any();
				} else
					addNewModifier = HidesBaseMember(member);

				if (addNewModifier)
					member.Modifiers |= Modifiers.New;
			}
			catch (ReferenceResolvingException) {
				// TODO: add some kind of notification (a comment?) about possible problems with decompiled code due to unresolved references.
			}
		}

		private static bool HidesBaseMember(AttributedNode member)
		{
			var memberDefinition = member.Annotation<IMemberDefinition>();
			bool addNewModifier = false;
			var methodDefinition = memberDefinition as MethodDefinition;
			if (methodDefinition != null) {
				addNewModifier = HidesByName(memberDefinition, includeBaseMethods: false);
				if (!addNewModifier)
					addNewModifier = TypesHierarchyHelpers.FindBaseMethods(methodDefinition).Any();
			} else
				addNewModifier = HidesByName(memberDefinition, includeBaseMethods: true);
			return addNewModifier;
		}

		/// <summary>
		/// Determines whether any base class member has the same name as the given member.
		/// </summary>
		/// <param name="member">The derived type's member.</param>
		/// <param name="includeBaseMethods">true if names of methods declared in base types should also be checked.</param>
		/// <returns>true if any base member has the same name as given member, otherwise false.</returns>
		static bool HidesByName(IMemberDefinition member, bool includeBaseMethods)
		{
			Debug.Assert(!(member is PropertyDefinition) || !((PropertyDefinition)member).IsIndexer());

			if (member.DeclaringType.BaseType != null) {
				var baseTypeRef = member.DeclaringType.BaseType;
				while (baseTypeRef != null) {
					var baseType = baseTypeRef.ResolveOrThrow();
					if (baseType.HasProperties && AnyIsHiddenBy(baseType.Properties, member, m => !m.IsIndexer()))
						return true;
					if (baseType.HasEvents && AnyIsHiddenBy(baseType.Events, member))
						return true;
					if (baseType.HasFields && AnyIsHiddenBy(baseType.Fields, member))
						return true;
					if (includeBaseMethods && baseType.HasMethods
					    && AnyIsHiddenBy(baseType.Methods, member, m => !m.IsSpecialName))
						return true;
					if (baseType.HasNestedTypes && AnyIsHiddenBy(baseType.NestedTypes, member))
						return true;
					baseTypeRef = baseType.BaseType;
				}
			}
			return false;
		}

		static bool AnyIsHiddenBy<T>(IEnumerable<T> members, IMemberDefinition derived, Predicate<T> condition = null)
			where T : IMemberDefinition
		{
			return members.Any(m => m.Name == derived.Name
			                   && (condition == null || condition(m))
			                   && TypesHierarchyHelpers.IsVisibleFromDerived(m, derived.DeclaringType));
		}
	}
}
