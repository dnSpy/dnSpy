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
using dnlib.DotNet;
using dnlib.DotNet.Emit;

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
		SyntaxTree syntaxTree = new SyntaxTree();
		Dictionary<string, NamespaceDeclaration> astNamespaces = new Dictionary<string, NamespaceDeclaration>();
		bool transformationsHaveRun;
		
		public AstBuilder(DecompilerContext context)
		{
			if (context == null)
				throw new ArgumentNullException("context");
			this.context = context;
			this.DecompileMethodBodies = true;
		}
		
		public static bool MemberIsHidden(IMemberRef member, DecompilerSettings settings)
		{
			MethodDef method = member as MethodDef;
			if (method != null) {
				if (method.HasSemantics())
					return true;
				if (settings.AnonymousMethods && method.HasGeneratedName() && method.IsCompilerGenerated())
					return true;
			}

			TypeDef type = member as TypeDef;
			if (type != null) {
				if (type.DeclaringType != null) {
					if (settings.AnonymousMethods && IsClosureType(type))
						return true;
					if (settings.YieldReturn && YieldReturnDecompiler.IsCompilerGeneratorEnumerator(type))
						return true;
					if (settings.AsyncAwait && AsyncDecompiler.IsCompilerGeneratedStateMachine(type))
						return true;
				} else if (type.IsCompilerGenerated()) {
					if (type.Name.StartsWith("<PrivateImplementationDetails>", StringComparison.Ordinal))
						return true;
					if (type.IsAnonymousType())
						return true;
				}
			}
			
			FieldDef field = member as FieldDef;
			if (field != null) {
				if (field.IsCompilerGenerated()) {
					if (settings.AnonymousMethods && IsAnonymousMethodCacheField(field))
						return true;
					if (settings.AutomaticProperties && IsAutomaticPropertyBackingField(field))
						return true;
					if (settings.SwitchStatementOnString && IsSwitchOnStringCache(field))
						return true;
				}
				// event-fields are not [CompilerGenerated]
				if (settings.AutomaticEvents && field.DeclaringType.Events.Any(ev => ev.Name == field.Name))
					return true;
			}
			
			return false;
		}

		static bool IsSwitchOnStringCache(FieldDef field)
		{
			return field.Name.StartsWith("<>f__switch", StringComparison.Ordinal);
		}

		static bool IsAutomaticPropertyBackingField(FieldDef field)
		{
			return field.HasGeneratedName() && field.Name.EndsWith("BackingField", StringComparison.Ordinal);
		}

		static bool IsAnonymousMethodCacheField(FieldDef field)
		{
			return field.Name.StartsWith("CS$<>", StringComparison.Ordinal) || field.Name.StartsWith("<>f__am", StringComparison.Ordinal);
		}

		static bool IsClosureType(TypeDef type)
		{
			return type.HasGeneratedName() && type.IsCompilerGenerated() && (type.Name.Contains("DisplayClass") || type.Name.Contains("AnonStorey"));
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
			TransformationPipeline.RunTransformationsUntil(syntaxTree, transformAbortCondition, context);
			transformationsHaveRun = true;
		}
		
		/// <summary>
		/// Gets the abstract source tree.
		/// </summary>
		public SyntaxTree SyntaxTree {
			get { return syntaxTree; }
		}
		
		/// <summary>
		/// Generates C# code from the abstract source tree.
		/// </summary>
		/// <remarks>This method adds ParenthesizedExpressions into the AST, and will run transformations if <see cref="RunTransformations"/> was not called explicitly</remarks>
		public void GenerateCode(ITextOutput output)
		{
			if (!transformationsHaveRun)
				RunTransformations();
			
			syntaxTree.AcceptVisitor(new InsertParenthesesVisitor { InsertParenthesesForReadability = true });
			var outputFormatter = new TextOutputFormatter(output) { FoldBraces = context.Settings.FoldBraces };
			var formattingPolicy = context.Settings.CSharpFormattingOptions;
			syntaxTree.AcceptVisitor(new CSharpOutputVisitor(outputFormatter, formattingPolicy));
		}
		
		public void AddAssembly(AssemblyDef assemblyDefinition, bool onlyAssemblyLevel = false)
		{
			AddAssembly(assemblyDefinition.ManifestModule, onlyAssemblyLevel);
		}
		
		public void AddAssembly(ModuleDef moduleDefinition, bool onlyAssemblyLevel = false)
		{
			if (moduleDefinition.Assembly != null && moduleDefinition.Assembly.Version != null) {
				syntaxTree.AddChild(
					new AttributeSection {
						AttributeTarget = "assembly",
						Attributes = {
							new NRefactory.CSharp.Attribute {
								Type = new SimpleType("AssemblyVersion")
									.WithAnnotation(moduleDefinition.CorLibTypes.GetTypeRef(
										"System.Reflection", "AssemblyVersionAttribute")),
								Arguments = {
									new PrimitiveExpression(moduleDefinition.Assembly.Version.ToString())
								}
							}
						}
					}, EntityDeclaration.AttributeRole);
			}
			
			if (moduleDefinition.Assembly != null) {
				ConvertCustomAttributes(syntaxTree, moduleDefinition.Assembly, "assembly");
				ConvertSecurityAttributes(syntaxTree, moduleDefinition.Assembly, "assembly");
			}
			ConvertCustomAttributes(syntaxTree, moduleDefinition, "module");
			AddTypeForwarderAttributes(syntaxTree, moduleDefinition, "assembly");
			
			if (!onlyAssemblyLevel) {
				foreach (TypeDef typeDef in moduleDefinition.Types) {
					// Skip the <Module> class
					if (typeDef.IsGlobalModuleType) continue;
					// Skip any hidden types
					if (AstBuilder.MemberIsHidden(typeDef, context.Settings))
						continue;

					AddType(typeDef);
				}
			}
		}
		
		void AddTypeForwarderAttributes(SyntaxTree astCompileUnit, ModuleDef module, string target)
		{
			if (!module.HasExportedTypes)
				return;
			foreach (ExportedType type in module.ExportedTypes) {
				if (type.IsForwarder) {
					var forwardedType = CreateTypeOfExpression(null, null, new TypeRefUser(module, type.TypeNamespace, type.TypeName));
					astCompileUnit.AddChild(
						new AttributeSection {
							AttributeTarget = target,
							Attributes = {
								new NRefactory.CSharp.Attribute {
									Type = new SimpleType("TypeForwardedTo")
										.WithAnnotation(module.CorLibTypes.GetTypeRef(
											"System.Runtime.CompilerServices", "TypeForwardedToAttribute")),
									Arguments = { forwardedType }
								}
							}
						}, EntityDeclaration.AttributeRole);
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
				syntaxTree.Members.Add(astNamespace);
				astNamespaces[name] = astNamespace;
				return astNamespace;
			}
		}
		
		public void AddType(TypeDef typeDef)
		{
			var astType = CreateType(typeDef);
			NamespaceDeclaration astNS = GetCodeNamespace(typeDef.Namespace);
			if (astNS != null) {
				astNS.Members.Add(astType);
			} else {
				syntaxTree.Members.Add(astType);
			}
		}
		
		public void AddMethod(MethodDef method)
		{
			AstNode node = method.IsConstructor ? (AstNode)CreateConstructor(method) : CreateMethod(method);
			syntaxTree.Members.Add(node);
		}

		public void AddProperty(PropertyDef property)
		{
			syntaxTree.Members.Add(CreateProperty(property));
		}
		
		public void AddField(FieldDef field)
		{
			syntaxTree.Members.Add(CreateField(field));
		}
		
		public void AddEvent(EventDef ev)
		{
			syntaxTree.Members.Add(CreateEvent(ev));
		}
		
		/// <summary>
		/// Creates the AST for a type definition.
		/// </summary>
		/// <param name="typeDef"></param>
		/// <returns>TypeDeclaration or DelegateDeclaration.</returns>
		public EntityDeclaration CreateType(TypeDef typeDef)
		{
			// create type
			TypeDef oldCurrentType = context.CurrentType;
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
			
			IEnumerable<GenericParam> genericParameters = typeDef.GenericParameters;
			if (typeDef.DeclaringType != null && typeDef.DeclaringType.HasGenericParameters)
				genericParameters = genericParameters.Skip(typeDef.DeclaringType.GenericParameters.Count);
			astType.TypeParameters.AddRange(MakeTypeParameters(genericParameters));
			astType.Constraints.AddRange(MakeConstraints(typeDef, null, genericParameters));
			
			EntityDeclaration result = astType;
			if (typeDef.IsEnum) {
				long expectedEnumMemberValue = 0;
				bool forcePrintingInitializers = IsFlagsEnum(typeDef);
				foreach (FieldDef field in typeDef.Fields) {
					if (!field.IsStatic) {
						// the value__ field
						if (field.FieldType != typeDef.Module.CorLibTypes.Int32) {
							astType.AddChild(ConvertType(typeDef, null, field.FieldType), Roles.BaseType);
						}
					} else {
						EnumMemberDeclaration enumMember = new EnumMemberDeclaration();
						enumMember.AddAnnotation(field);
						enumMember.Name = CleanName(field.Name);
						long memberValue = (long)CSharpPrimitiveCast.Cast(TypeCode.Int64, field.Constant.Value, false);
						if (forcePrintingInitializers || memberValue != expectedEnumMemberValue) {
							enumMember.AddChild(new PrimitiveExpression(field.Constant.Value), EnumMemberDeclaration.InitializerRole);
						}
						expectedEnumMemberValue = memberValue + 1;
						astType.AddChild(enumMember, Roles.TypeMemberRole);
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
						dd.ReturnType = ConvertType(typeDef, m, m.ReturnType, m.Parameters.ReturnParameter.ParamDef);
						dd.Parameters.AddRange(MakeParameters(m));
						ConvertAttributes(dd, m.Parameters.ReturnParameter, m.Module);
					}
				}
				result = dd;
			} else {
				// Base type
				if (typeDef.BaseType != null && !typeDef.IsValueType && typeDef.BaseType.FullName != "System.Object") {
					astType.AddChild(ConvertType(typeDef, null, typeDef.BaseType), Roles.BaseType);
				}
				foreach (var i in typeDef.Interfaces)
					astType.AddChild(ConvertType(typeDef, null, i.Interface), Roles.BaseType);
				
				AddTypeMembers(astType, typeDef);

				if (astType.Members.OfType<IndexerDeclaration>().Any(idx => idx.PrivateImplementationType.IsNull)) {
					// Remove the [DefaultMember] attribute if the class contains indexers
					foreach (AttributeSection section in astType.Attributes) {
						foreach (Ast.Attribute attr in section.Attributes) {
							ITypeDefOrRef tr = attr.Type.Annotation<ITypeDefOrRef>();
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
		public static TypeOfExpression CreateTypeOfExpression(TypeDef typeContext, MethodDef methodContext, ITypeDefOrRef type)
		{
			return new TypeOfExpression(AddEmptyTypeArgumentsForUnboundGenerics(ConvertType(typeContext, methodContext, type)));
		}
		
		static AstType AddEmptyTypeArgumentsForUnboundGenerics(AstType type)
		{
			ITypeDefOrRef typeRef = type.Annotation<ITypeDefOrRef>();
			if (typeRef == null)
				return type;
			TypeDef typeDef = typeRef.ResolveTypeDef(); // need to resolve to figure out the number of type parameters
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
		public static AstType ConvertType(TypeDef typeContext, MethodDef methodContext, ITypeDefOrRef type, IHasCustomAttribute typeAttributes = null, ConvertTypeOptions options = ConvertTypeOptions.None)
		{
			int typeIndex = 0;
			var typeParams = typeContext == null ? (IList<GenericParam>)new List<GenericParam>() : typeContext.GenericParameters;
			var methodParams = methodContext == null ? (IList<GenericParam>)new List<GenericParam>() : methodContext.GenericParameters;
			return ConvertType(typeParams, methodParams, type, typeAttributes, ref typeIndex, options);
		}

		/// <summary>
		/// Converts a type reference.
		/// </summary>
		/// <param name="type">The Cecil type reference that should be converted into
		/// a type system type reference.</param>
		/// <param name="typeAttributes">Attributes associated with the Cecil type reference.
		/// This is used to support the 'dynamic' type.</param>
		public static AstType ConvertType(TypeDef typeContext, MethodDef methodContext, TypeSig type, IHasCustomAttribute typeAttributes = null, ConvertTypeOptions options = ConvertTypeOptions.None)
		{
			int typeIndex = 0;
			var typeParams = typeContext == null ? (IList<GenericParam>)new List<GenericParam>() : typeContext.GenericParameters;
			var methodParams = methodContext == null ? (IList<GenericParam>)new List<GenericParam>() : methodContext.GenericParameters;
			return ConvertType(typeParams, methodParams, type, typeAttributes, ref typeIndex, options);
		}

		static AstType ConvertType(IList<GenericParam> typeContext, IList<GenericParam> methodContext, TypeSig type, IHasCustomAttribute typeAttributes, ref int typeIndex, ConvertTypeOptions options)
		{
			type = type.RemoveModifiers();
			if (type == null) {
				return AstType.Null;
			}
			
			if (type is ByRefSig) {
				typeIndex++;
				// by reference type cannot be represented in C#; so we'll represent it as a pointer instead
				return ConvertType(typeContext, methodContext, (type as ByRefSig).Next, typeAttributes, ref typeIndex, options)
					.MakePointerType();
			} else if (type is PtrSig) {
				typeIndex++;
				return ConvertType(typeContext, methodContext, (type as PtrSig).Next, typeAttributes, ref typeIndex, options)
					.MakePointerType();
			} else if (type is ArraySig) {
				typeIndex++;
				return ConvertType(typeContext, methodContext, (type as ArraySig).Next, typeAttributes, ref typeIndex, options)
					.MakeArrayType((int)(type as ArraySig).Rank);
			} else if (type is SZArraySig) {
				typeIndex++;
				return ConvertType(typeContext, methodContext, (type as SZArraySig).Next, typeAttributes, ref typeIndex, options)
					.MakeArrayType(1);
			} else if (type is GenericInstSig) {
				GenericInstSig gType = (GenericInstSig)type;
				if (gType.GenericType.Namespace == "System" && gType.GenericType.TypeName == "Nullable`1" && gType.GenericArguments.Count == 1) {
					typeIndex++;
					return new ComposedType {
						BaseType = ConvertType(typeContext, methodContext, gType.GenericArguments[0], typeAttributes, ref typeIndex, options),
						HasNullableSpecifier = true
					};
				}
				AstType baseType = ConvertType(typeContext, methodContext, gType.GenericType.TypeDefOrRef, typeAttributes, ref typeIndex, options & ~ConvertTypeOptions.IncludeTypeParameterDefinitions);
				List<AstType> typeArguments = new List<AstType>();
				foreach (var typeArgument in gType.GenericArguments) {
					typeIndex++;
					typeArguments.Add(ConvertType(typeContext, methodContext, typeArgument, typeAttributes, ref typeIndex, options));
				}
				ApplyTypeArgumentsTo(baseType, typeArguments);
				return baseType;
			} else if (type is GenericSig) {

				GenericSig genericSig = (GenericSig)type;
				int num = (int)genericSig.Number;
				if (genericSig is IGenericParam)
					return new SimpleType(((IGenericParam)genericSig).GenericParameter.Name);
				else if (genericSig is GenericVar)
					return new SimpleType(typeContext[num].Name);
				else if (genericSig is GenericMVar)
					return new SimpleType(methodContext[num].Name);
				throw new NotSupportedException();

			} else if (type is TypeDefOrRefSig) {
				return ConvertType(typeContext, methodContext, ((TypeDefOrRefSig)type).TypeDefOrRef, typeAttributes, ref typeIndex, options);
			} else
				throw new NotSupportedException();
		}

		static AstType ConvertType(IList<GenericParam> typeContext, IList<GenericParam> methodContext, ITypeDefOrRef type, IHasCustomAttribute typeAttributes, ref int typeIndex, ConvertTypeOptions options)
		{
			if (type == null)
				return null;

			if (type is TypeSpec)
				return ConvertType(typeContext, methodContext, ((TypeSpec)type).TypeSig, typeAttributes, ref typeIndex, options);

			var declType = type.GetDeclaringType();
			if (declType != null)
			{
				AstType typeRef = ConvertType(typeContext, methodContext, declType, typeAttributes, ref typeIndex, options & ~ConvertTypeOptions.IncludeTypeParameterDefinitions);
				string namepart = ICSharpCode.NRefactory.TypeSystem.ReflectionHelper.SplitTypeParameterCountFromReflectionName(type.Name);
				MemberType memberType = new MemberType { Target = typeRef, MemberName = namepart };
				memberType.AddAnnotation(type);
				if ((options & ConvertTypeOptions.IncludeTypeParameterDefinitions) == ConvertTypeOptions.IncludeTypeParameterDefinitions) {
					AddTypeParameterDefininitionsTo(type, memberType);
				}
				return memberType;
			}
			else {
				string ns = type.Namespace ?? string.Empty;
				string name = type.Name;
				if (name == null)
					throw new InvalidOperationException("type.Name returned null. Type: " + type.ToString());

				if (name == "Object" && ns == "System" && HasDynamicAttribute(typeAttributes, typeIndex)) {
					return new PrimitiveType("dynamic");
				}
				else {
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
					}
					else {
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
		
		static void AddTypeParameterDefininitionsTo(ITypeDefOrRef type, AstType astType)
		{
			TypeDef typeDef = type.ResolveTypeDefThrow();
			if (typeDef.HasGenericParameters) {
				List<AstType> typeArguments = new List<AstType>();
				foreach (GenericParam gp in typeDef.GenericParameters) {
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
				ITypeDefOrRef type = mt.Annotation<ITypeDefOrRef>();
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
		
		static bool HasDynamicAttribute(IHasCustomAttribute attributeProvider, int typeIndex)
		{
			if (attributeProvider == null || !attributeProvider.HasCustomAttributes)
				return false;
			foreach (CustomAttribute a in attributeProvider.CustomAttributes) {
				if (((IMethod)a.Constructor).DeclaringType.FullName == DynamicAttributeFullName) {
					if (a.ConstructorArguments.Count == 1) {
						IList<CAArgument> values = a.ConstructorArguments[0].Value as IList<CAArgument>;
						if (values != null && typeIndex < values.Count && values[typeIndex].Value is bool)
							return (bool)values[typeIndex].Value;
					}
					return true;
				}
			}
			return false;
		}
		#endregion
		
		#region ConvertModifiers
		Modifiers ConvertModifiers(TypeDef typeDef)
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
		
		Modifiers ConvertModifiers(FieldDef fieldDef)
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
			
			CModReqdSig modreq = fieldDef.FieldType as CModReqdSig;
			if (modreq != null && modreq.Modifier.FullName == typeof(IsVolatile).FullName)
				modifiers |= Modifiers.Volatile;
			
			return modifiers;
		}
		
		Modifiers ConvertModifiers(MethodDef methodDef)
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
		
		void AddTypeMembers(TypeDeclaration astType, TypeDef typeDef)
		{
			// Nested types
			foreach (TypeDef nestedTypeDef in typeDef.NestedTypes) {
				if (MemberIsHidden(nestedTypeDef, context.Settings))
					continue;
				var nestedType = CreateType(nestedTypeDef);
				SetNewModifier(nestedType);
				astType.AddChild(nestedType, Roles.TypeMemberRole);
			}
			
			// Add fields
			foreach(FieldDef fieldDef in typeDef.Fields) {
				if (MemberIsHidden(fieldDef, context.Settings)) continue;
				astType.AddChild(CreateField(fieldDef), Roles.TypeMemberRole);
			}
			
			// Add events
			foreach(EventDef eventDef in typeDef.Events) {
				if (eventDef.AddMethod == null && eventDef.RemoveMethod == null)
					continue;
				astType.AddChild(CreateEvent(eventDef), Roles.TypeMemberRole);
			}

			// Add properties
			foreach(PropertyDef propDef in typeDef.Properties) {
				if (propDef.GetMethod == null && propDef.SetMethod == null)
					continue;
				astType.Members.Add(CreateProperty(propDef));
			}
			
			// Add methods
			foreach(MethodDef methodDef in typeDef.Methods) {
				if (MemberIsHidden(methodDef, context.Settings)) continue;
				
				if (methodDef.IsConstructor)
					astType.Members.Add(CreateConstructor(methodDef));
				else
					astType.Members.Add(CreateMethod(methodDef));
			}
		}

		EntityDeclaration CreateMethod(MethodDef methodDef)
		{
			MethodDeclaration astMethod = new MethodDeclaration();
			astMethod.AddAnnotation(methodDef);
			astMethod.ReturnType = ConvertType(methodDef.DeclaringType, methodDef, methodDef.ReturnType, methodDef.Parameters.ReturnParameter.ParamDef);
			astMethod.Name = CleanName(methodDef.Name);
			astMethod.TypeParameters.AddRange(MakeTypeParameters(methodDef.GenericParameters));
			astMethod.Parameters.AddRange(MakeParameters(methodDef));
			// constraints for override and explicit interface implementation methods are inherited from the base method, so they cannot be specified directly
			if (!methodDef.IsVirtual || (methodDef.IsNewSlot && !methodDef.IsPrivate)) astMethod.Constraints.AddRange(MakeConstraints(methodDef.DeclaringType, methodDef, methodDef.GenericParameters));
			if (!methodDef.DeclaringType.IsInterface) {
				if (IsExplicitInterfaceImplementation(methodDef)) {
					astMethod.PrivateImplementationType = ConvertType(methodDef.DeclaringType, methodDef, methodDef.Overrides.First().MethodDeclaration.DeclaringType);
				} else {
					astMethod.Modifiers = ConvertModifiers(methodDef);
					if (methodDef.IsVirtual == methodDef.IsNewSlot)
						SetNewModifier(astMethod);
				}
				astMethod.Body = CreateMethodBody(methodDef, astMethod.Parameters);
				if (context.CurrentMethodIsAsync) {
					astMethod.Modifiers |= Modifiers.Async;
					context.CurrentMethodIsAsync = false;
				}
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
		
		bool IsExplicitInterfaceImplementation(MethodDef methodDef)
		{
			return methodDef.HasOverrides && methodDef.IsPrivate;
		}

		IEnumerable<TypeParameterDeclaration> MakeTypeParameters(IEnumerable<GenericParam> genericParameters)
		{
			foreach (var gp in genericParameters) {
				TypeParameterDeclaration tp = new TypeParameterDeclaration();
				tp.Name = CleanName(gp.Name);
				if ((gp.Flags & GenericParamAttributes.Contravariant) != 0)
					tp.Variance = VarianceModifier.Contravariant;
				else if ((gp.Flags & GenericParamAttributes.Covariant) != 0)
					tp.Variance = VarianceModifier.Covariant;
				ConvertCustomAttributes(tp, gp);
				yield return tp;
			}
		}
		
		IEnumerable<Constraint> MakeConstraints(TypeDef typeContext, MethodDef methodContext, IEnumerable<GenericParam> genericParameters)
		{
			foreach (var gp in genericParameters) {
				Constraint c = new Constraint();
				c.TypeParameter = new SimpleType(CleanName(gp.Name));
				// class/struct must be first
				if ((gp.Flags & GenericParamAttributes.ReferenceTypeConstraint) != 0)
					c.BaseTypes.Add(new PrimitiveType("class"));
				if ((gp.Flags & GenericParamAttributes.NotNullableValueTypeConstraint) != 0)
					c.BaseTypes.Add(new PrimitiveType("struct"));
				
				foreach (var constraintType in gp.GenericParamConstraints) {
					if ((gp.Flags & GenericParamAttributes.NotNullableValueTypeConstraint) != 0 && 
						constraintType.Constraint.FullName == "System.ValueType")
						continue;
					c.BaseTypes.Add(ConvertType(typeContext, methodContext, constraintType.Constraint));
				}

				if ((gp.Flags & GenericParamAttributes.DefaultConstructorConstraint) != 0 && (gp.Flags & GenericParamAttributes.NotNullableValueTypeConstraint) == 0)
					c.BaseTypes.Add(new PrimitiveType("new")); // new() must be last
				if (c.BaseTypes.Any())
					yield return c;
			}
		}
		
		ConstructorDeclaration CreateConstructor(MethodDef methodDef)
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
				astMethod.Body.InsertChildAfter(null, new Comment(" Note: this type is marked as 'beforefieldinit'."), Roles.Comment);
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

		EntityDeclaration CreateProperty(PropertyDef propDef)
		{
			PropertyDeclaration astProp = new PropertyDeclaration();
			astProp.AddAnnotation(propDef);
			var accessor = propDef.GetMethod ?? propDef.SetMethod;
			Modifiers getterModifiers = Modifiers.None;
			Modifiers setterModifiers = Modifiers.None;
			if (IsExplicitInterfaceImplementation(accessor)) {
				astProp.PrivateImplementationType = ConvertType(propDef.DeclaringType, null, accessor.Overrides.First().MethodDeclaration.DeclaringType);
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
			astProp.ReturnType = ConvertType(propDef.DeclaringType, null, propDef.PropertySig.RetType, propDef);
			
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
				Parameter lastParam = propDef.SetMethod.Parameters.LastOrDefault();
				if (lastParam != null) {
					ConvertCustomAttributes(astProp.Setter, lastParam.ParamDef, "param");
					if (lastParam.HasParamDef && lastParam.ParamDef.HasMarshalType) {
						astProp.Setter.Attributes.Add(new AttributeSection(ConvertMarshalInfo(lastParam.ParamDef, propDef.Module)) { AttributeTarget = "param" });
					}
				}
				
				if ((setterModifiers & Modifiers.VisibilityMask) != (astProp.Modifiers & Modifiers.VisibilityMask))
					astProp.Setter.Modifiers = setterModifiers & Modifiers.VisibilityMask;
			}
			ConvertCustomAttributes(astProp, propDef);

			EntityDeclaration member = astProp;
			if(propDef.IsIndexer())
				member = ConvertPropertyToIndexer(astProp, propDef);
			if(!accessor.HasOverrides && !accessor.DeclaringType.IsInterface)
				if (accessor.IsVirtual == accessor.IsNewSlot)
					SetNewModifier(member);
			return member;
		}

		IndexerDeclaration ConvertPropertyToIndexer(PropertyDeclaration astProp, PropertyDef propDef)
		{
			var astIndexer = new IndexerDeclaration();
			astIndexer.CopyAnnotationsFrom(astProp);
			astProp.Attributes.MoveTo(astIndexer.Attributes);
			astIndexer.Modifiers = astProp.Modifiers;
			astIndexer.PrivateImplementationType = astProp.PrivateImplementationType.Detach();
			astIndexer.ReturnType = astProp.ReturnType.Detach();
			astIndexer.Getter = astProp.Getter.Detach();
			astIndexer.Setter = astProp.Setter.Detach();
			astIndexer.Parameters.AddRange(MakeParameters(propDef.DeclaringType, null, propDef.GetParameters().ToList()));
			return astIndexer;
		}
		
		EntityDeclaration CreateEvent(EventDef eventDef)
		{
			if (eventDef.AddMethod != null && eventDef.AddMethod.IsAbstract) {
				// An abstract event cannot be custom
				EventDeclaration astEvent = new EventDeclaration();
				ConvertCustomAttributes(astEvent, eventDef);
				astEvent.AddAnnotation(eventDef);
				astEvent.Variables.Add(new VariableInitializer(CleanName(eventDef.Name)));
				astEvent.ReturnType = ConvertType(eventDef.DeclaringType, null, eventDef.EventType, eventDef);
				if (!eventDef.DeclaringType.IsInterface)
					astEvent.Modifiers = ConvertModifiers(eventDef.AddMethod);
				return astEvent;
			} else {
				CustomEventDeclaration astEvent = new CustomEventDeclaration();
				ConvertCustomAttributes(astEvent, eventDef);
				astEvent.AddAnnotation(eventDef);
				astEvent.Name = CleanName(eventDef.Name);
				astEvent.ReturnType = ConvertType(eventDef.DeclaringType, null, eventDef.EventType, eventDef);
				if (eventDef.AddMethod == null || !IsExplicitInterfaceImplementation(eventDef.AddMethod))
					astEvent.Modifiers = ConvertModifiers(eventDef.AddMethod);
				else
					astEvent.PrivateImplementationType = ConvertType(eventDef.DeclaringType, null, eventDef.AddMethod.Overrides.First().MethodDeclaration.DeclaringType);
				
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
				MethodDef accessor = eventDef.AddMethod ?? eventDef.RemoveMethod;
				if (accessor.IsVirtual == accessor.IsNewSlot) {
					SetNewModifier(astEvent);
				}
				return astEvent;
			}
		}
		
		public bool DecompileMethodBodies { get; set; }
		
		BlockStatement CreateMethodBody(MethodDef method, IEnumerable<ParameterDeclaration> parameters = null)
		{
			if (DecompileMethodBodies)
				return AstMethodBodyBuilder.CreateMethodBody(method, context, parameters);
			else
				return null;
		}

		FieldDeclaration CreateField(FieldDef fieldDef)
		{
			FieldDeclaration astField = new FieldDeclaration();
			astField.AddAnnotation(fieldDef);
			VariableInitializer initializer = new VariableInitializer(CleanName(fieldDef.Name));
			astField.AddChild(initializer, Roles.Variable);
			astField.ReturnType = ConvertType(fieldDef.DeclaringType, null, fieldDef.FieldType, fieldDef);
			astField.Modifiers = ConvertModifiers(fieldDef);
			if (fieldDef.HasConstant) {
				initializer.Initializer = CreateExpressionForConstant(fieldDef.DeclaringType, null, fieldDef.Constant.Value, fieldDef.FieldType, fieldDef.DeclaringType.IsEnum);
			}
			ConvertAttributes(astField, fieldDef);
			SetNewModifier(astField);
			return astField;
		}
		
		static Expression CreateExpressionForConstant(TypeDef typeContext, MethodDef methodContext, object constant, TypeSig type, bool isEnumMemberDeclaration = false)
		{
			if (constant == null) {
				if (type.IsValueType && !(type.Namespace == "System" && type.TypeName == "Nullable`1"))
					return new DefaultValueExpression(ConvertType(typeContext, methodContext, type));
				else
					return new NullReferenceExpression();
			} else {
				TypeCode c = Type.GetTypeCode(constant.GetType());
				if (c >= TypeCode.SByte && c <= TypeCode.UInt64 && !isEnumMemberDeclaration) {
					return MakePrimitive((long)CSharpPrimitiveCast.Cast(TypeCode.Int64, constant, false), type.ToTypeDefOrRef());
				} else {
					return new PrimitiveExpression(constant);
				}
			}
		}
		
		public static IEnumerable<ParameterDeclaration> MakeParameters(MethodDef method, bool isLambda = false)
		{
			var parameters = MakeParameters(method.DeclaringType, method, method.Parameters, isLambda);
			if (method.CallingConvention == dnlib.DotNet.CallingConvention.VarArg) {
				return parameters.Concat(new[] { new ParameterDeclaration { Type = new PrimitiveType("__arglist") } });
			} else {
				return parameters;
			}
		}
		
		public static IEnumerable<ParameterDeclaration> MakeParameters(TypeDef typeContext, MethodDef methodContext, IEnumerable<Parameter> paramCol, bool isLambda = false)
		{
			foreach (Parameter paramDef in paramCol) {
				if (paramDef.IsHiddenThisParameter)
					continue;

				ParameterDeclaration astParam = new ParameterDeclaration();
				astParam.AddAnnotation(paramDef);
				if (!(isLambda && paramDef.Type.ContainsAnonymousType()))
					astParam.Type = ConvertType(typeContext, methodContext, paramDef.Type, paramDef.ParamDef);
				astParam.Name = paramDef.Name;
				
				if (paramDef.Type is ByRefSig) {
					astParam.ParameterModifier = (paramDef.HasParamDef && !paramDef.ParamDef.IsIn && paramDef.ParamDef.IsOut) ? ParameterModifier.Out : ParameterModifier.Ref;
					ComposedType ct = astParam.Type as ComposedType;
					if (ct != null && ct.PointerRank > 0)
						ct.PointerRank--;
				}
				
				if (paramDef.HasParamDef && paramDef.ParamDef.HasCustomAttributes) {
					foreach (CustomAttribute ca in paramDef.ParamDef.CustomAttributes) {
						if (ca.AttributeType.Name == "ParamArrayAttribute" && ca.AttributeType.Namespace == "System")
							astParam.ParameterModifier = ParameterModifier.Params;
					}
				}
				if (paramDef.HasParamDef && paramDef.ParamDef.IsOptional) {
					astParam.DefaultExpression = CreateExpressionForConstant(typeContext, methodContext, paramDef.ParamDef.Constant.Value, paramDef.Type);
				}
				
				ConvertCustomAttributes(astParam, paramDef.ParamDef);
				ModuleDef module = paramDef.Method.Module;
				if (paramDef.HasParamDef && paramDef.ParamDef.HasMarshalType) {
					astParam.Attributes.Add(new AttributeSection(ConvertMarshalInfo(paramDef.ParamDef, module)));
				}
				if (paramDef.HasParamDef && astParam.ParameterModifier != ParameterModifier.Out) {
					if (paramDef.ParamDef.IsIn)
						astParam.Attributes.Add(new AttributeSection(CreateNonCustomAttribute(typeof(InAttribute), module)));
					if (paramDef.ParamDef.IsOut)
						astParam.Attributes.Add(new AttributeSection(CreateNonCustomAttribute(typeof(OutAttribute), module)));
				}
				yield return astParam;
			}
		}
		
		#region ConvertAttributes
		void ConvertAttributes(EntityDeclaration attributedNode, TypeDef typeDef)
		{
			ConvertCustomAttributes(attributedNode, typeDef);
			ConvertSecurityAttributes(attributedNode, typeDef);
			
			// Handle the non-custom attributes:
			#region SerializableAttribute
			if (typeDef.IsSerializable)
				attributedNode.Attributes.Add(new AttributeSection(CreateNonCustomAttribute(typeof(SerializableAttribute))));
			#endregion
			
			#region ComImportAttribute
			if (typeDef.IsImport)
				attributedNode.Attributes.Add(new AttributeSection(CreateNonCustomAttribute(typeof(ComImportAttribute))));
			#endregion
			
			#region StructLayoutAttribute
			LayoutKind layoutKind = LayoutKind.Auto;
			switch (typeDef.Attributes & TypeAttributes.LayoutMask) {
				case TypeAttributes.SequentialLayout:
					layoutKind = LayoutKind.Sequential;
					break;
				case TypeAttributes.ExplicitLayout:
					layoutKind = LayoutKind.Explicit;
					break;
			}
			CharSet charSet = CharSet.None;
			switch (typeDef.Attributes & TypeAttributes.StringFormatMask) {
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
			LayoutKind defaultLayoutKind = (typeDef.IsValueType && !typeDef.IsEnum) ? LayoutKind.Sequential : LayoutKind.Auto;
			if (layoutKind != defaultLayoutKind || charSet != CharSet.Ansi || typeDef.HasClassLayout) {
				var structLayout = CreateNonCustomAttribute(typeof(StructLayoutAttribute));
				structLayout.Arguments.Add(new IdentifierExpression("LayoutKind").Member(layoutKind.ToString()));
				if (charSet != CharSet.Ansi) {
					structLayout.AddNamedArgument("CharSet", new IdentifierExpression("CharSet").Member(charSet.ToString()));
				}
				if (typeDef.PackingSize != ushort.MaxValue) {
					structLayout.AddNamedArgument("Pack", new PrimitiveExpression((int)typeDef.PackingSize));
				}
				if (typeDef.ClassSize != uint.MaxValue) {
					structLayout.AddNamedArgument("Size", new PrimitiveExpression((int)typeDef.ClassSize));
				}
				attributedNode.Attributes.Add(new AttributeSection(structLayout));
			}
			#endregion
		}
		
		void ConvertAttributes(EntityDeclaration attributedNode, MethodDef methodDef)
		{
			ConvertCustomAttributes(attributedNode, methodDef);
			ConvertSecurityAttributes(attributedNode, methodDef);
			
			MethodImplAttributes implAttributes = methodDef.ImplAttributes & ~MethodImplAttributes.CodeTypeMask;
			
			#region DllImportAttribute
			if (methodDef.HasImplMap) {
				ImplMap impl = methodDef.ImplMap;
				Ast.Attribute dllImport = CreateNonCustomAttribute(typeof(DllImportAttribute));
				dllImport.Arguments.Add(new PrimitiveExpression(impl.Module.Name.String));
				
				if (impl.IsBestFitDisabled)
					dllImport.AddNamedArgument("BestFitMapping", new PrimitiveExpression(false));
				if (impl.IsBestFitEnabled)
					dllImport.AddNamedArgument("BestFitMapping", new PrimitiveExpression(true));
				
				System.Runtime.InteropServices.CallingConvention callingConvention;
				switch (impl.Attributes & PInvokeAttributes.CallConvMask) {
					case PInvokeAttributes.CallConvCdecl:
						callingConvention = System.Runtime.InteropServices.CallingConvention.Cdecl;
						break;
					case PInvokeAttributes.CallConvFastcall:
						callingConvention = System.Runtime.InteropServices.CallingConvention.FastCall;
						break;
					case PInvokeAttributes.CallConvStdcall:
						callingConvention = System.Runtime.InteropServices.CallingConvention.StdCall;
						break;
					case PInvokeAttributes.CallConvThiscall:
						callingConvention = System.Runtime.InteropServices.CallingConvention.ThisCall;
						break;
					case PInvokeAttributes.CallConvWinapi:
						callingConvention = System.Runtime.InteropServices.CallingConvention.Winapi;
						break;
					default:
						throw new NotSupportedException("unknown calling convention");
				}
				if (callingConvention != System.Runtime.InteropServices.CallingConvention.Winapi)
					dllImport.AddNamedArgument("CallingConvention", new IdentifierExpression("CallingConvention").Member(callingConvention.ToString()));
				
				CharSet charSet = CharSet.None;
				switch (impl.Attributes & PInvokeAttributes.CharSetMask) {
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
				
				if (!string.IsNullOrEmpty(impl.Name) && impl.Name != methodDef.Name)
					dllImport.AddNamedArgument("EntryPoint", new PrimitiveExpression(impl.Name.String));
				
				if (impl.IsNoMangle)
					dllImport.AddNamedArgument("ExactSpelling", new PrimitiveExpression(true));
				
				if ((implAttributes & MethodImplAttributes.PreserveSig) == MethodImplAttributes.PreserveSig)
					implAttributes &= ~MethodImplAttributes.PreserveSig;
				else
					dllImport.AddNamedArgument("PreserveSig", new PrimitiveExpression(false));

				if (impl.SupportsLastError)
					dllImport.AddNamedArgument("SetLastError", new PrimitiveExpression(true));

				if (impl.IsThrowOnUnmappableCharDisabled)
					dllImport.AddNamedArgument("ThrowOnUnmappableChar", new PrimitiveExpression(false));
				if (impl.IsThrowOnUnmappableCharEnabled)
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
				TypeRef methodImplOptions = methodDef.Module.CorLibTypes.GetTypeRef(
					"System.Runtime.CompilerServices", "MethodImplOptions");
				methodImpl.Arguments.Add(MakePrimitive((long)implAttributes, methodImplOptions));
				attributedNode.Attributes.Add(new AttributeSection(methodImpl));
			}
			#endregion
			
			ConvertAttributes(attributedNode, methodDef.Parameters.ReturnParameter, methodDef.Module);
		}
		
		void ConvertAttributes(EntityDeclaration attributedNode, Parameter methodReturnType, ModuleDef module)
		{
			ConvertCustomAttributes(attributedNode, methodReturnType.ParamDef, "return");
			if (methodReturnType.HasParamDef && methodReturnType.ParamDef.HasMarshalType) {
				var marshalInfo = ConvertMarshalInfo(methodReturnType.ParamDef, module);
				attributedNode.Attributes.Add(new AttributeSection(marshalInfo) { AttributeTarget = "return" });
			}
		}
		
		internal static void ConvertAttributes(EntityDeclaration attributedNode, FieldDef fieldDef, string attributeTarget = null)
		{
			ConvertCustomAttributes(attributedNode, fieldDef);
			
			#region FieldOffsetAttribute
			if (fieldDef.HasLayoutInfo) {
				Ast.Attribute fieldOffset = CreateNonCustomAttribute(typeof(FieldOffsetAttribute), fieldDef.Module);
				fieldOffset.Arguments.Add(new PrimitiveExpression(fieldDef.FieldOffset));
				attributedNode.Attributes.Add(new AttributeSection(fieldOffset) { AttributeTarget = attributeTarget });
			}
			#endregion
			
			#region NonSerializedAttribute
			if (fieldDef.IsNotSerialized) {
				Ast.Attribute nonSerialized = CreateNonCustomAttribute(typeof(NonSerializedAttribute), fieldDef.Module);
				attributedNode.Attributes.Add(new AttributeSection(nonSerialized) { AttributeTarget = attributeTarget });
			}
			#endregion
			
			if (fieldDef.HasMarshalType) {
				attributedNode.Attributes.Add(new AttributeSection(ConvertMarshalInfo(fieldDef, fieldDef.Module))  { AttributeTarget = attributeTarget });
			}
		}
		
		#region MarshalAsAttribute (ConvertMarshalInfo)
		static Ast.Attribute ConvertMarshalInfo(IHasFieldMarshal fieldMarshal, ModuleDef module)
		{
			//MarshalInfo marshalInfo = marshalInfoProvider.MarshalInfo;
			Ast.Attribute attr = CreateNonCustomAttribute(typeof(MarshalAsAttribute), module);
			/*var unmanagedType = new TypeReference("System.Runtime.InteropServices", "UnmanagedType", module, module.TypeSystem.Corlib);
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
			}*/
			return attr;
		}
		#endregion
		
		Ast.Attribute CreateNonCustomAttribute(Type attributeType)
		{
			return CreateNonCustomAttribute(attributeType, context.CurrentType != null ? context.CurrentType.Module : null);
		}
		
		static Ast.Attribute CreateNonCustomAttribute(Type attributeType, ModuleDef module)
		{
			Debug.Assert(attributeType.Name.EndsWith("Attribute", StringComparison.Ordinal));
			Ast.Attribute attr = new Ast.Attribute();
			attr.Type = new SimpleType(attributeType.Name.Substring(0, attributeType.Name.Length - "Attribute".Length));
			if (module != null) {
				attr.Type.AddAnnotation(module.CorLibTypes.GetTypeRef(attributeType.Namespace, attributeType.Name));
			}
			return attr;
		}
		
		static void ConvertCustomAttributes(AstNode attributedNode, IHasCustomAttribute customAttributeProvider, string attributeTarget = null)
		{
			EntityDeclaration entityDecl = attributedNode as EntityDeclaration;
			if (customAttributeProvider != null && customAttributeProvider.HasCustomAttributes) {
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
					// if the method is async, remove [DebuggerStepThrough] and [Async
					if (entityDecl != null && entityDecl.HasModifier(Modifiers.Async)) {
						if (customAttribute.AttributeType.Name == "DebuggerStepThroughAttribute" && customAttribute.AttributeType.Namespace == "System.Diagnostics") {
							continue;
						}
						if (customAttribute.AttributeType.Name == "AsyncStateMachineAttribute" && customAttribute.AttributeType.Namespace == "System.Runtime.CompilerServices") {
							continue;
						}
					}
					
					var attribute = new ICSharpCode.NRefactory.CSharp.Attribute();
					attribute.AddAnnotation(customAttribute);
					attribute.Type = ConvertType(null, null, customAttribute.AttributeType);
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

					if (customAttribute.HasNamedArguments) {
						TypeDef resolvedAttributeType = customAttribute.AttributeType.ResolveTypeDef();
						foreach (var propertyNamedArg in customAttribute.Properties) {
							var propertyReference = resolvedAttributeType != null ? resolvedAttributeType.Properties.FirstOrDefault(pr => pr.Name == propertyNamedArg.Name) : null;
							var propertyName = new IdentifierExpression(propertyNamedArg.Name).WithAnnotation(propertyReference);
							var argumentValue = ConvertArgumentValue(propertyNamedArg.Argument);
							attribute.Arguments.Add(new AssignmentExpression(propertyName, argumentValue));
						}

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
						attributedNode.AddChild(section, EntityDeclaration.AttributeRole);
					}
				} else if (attributes.Count > 0) {
					// use single section for all attributes
					var section = new AttributeSection();
					section.AttributeTarget = attributeTarget;
					section.Attributes.AddRange(attributes);
					attributedNode.AddChild(section, EntityDeclaration.AttributeRole);
				}
			}
		}
		
		static void ConvertSecurityAttributes(AstNode attributedNode, IHasDeclSecurity secDeclProvider, string attributeTarget = null)
		{
			if (secDeclProvider.DeclSecurities.Count == 0)
				return;
			/*var attributes = new List<ICSharpCode.NRefactory.CSharp.Attribute>();
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
					attributedNode.AddChild(section, EntityDeclaration.AttributeRole);
				}
			} else if (attributes.Count > 0) {
				// use single section for all attributes
				var section = new AttributeSection();
				section.AttributeTarget = attributeTarget;
				section.Attributes.AddRange(attributes);
				attributedNode.AddChild(section, EntityDeclaration.AttributeRole);
			}
			*/
		}
		
		private static Expression ConvertArgumentValue(CAArgument argument)
		{
			if (argument.Value is IList<CAArgument>) {
				ArrayInitializerExpression arrayInit = new ArrayInitializerExpression();
				foreach (CAArgument element in (IList<CAArgument>)argument.Value) {
					arrayInit.Elements.Add(ConvertArgumentValue(element));
				}
				SZArraySig arrayType = argument.Type as SZArraySig;
				return new ArrayCreateExpression {
					Type = ConvertType(null, null, arrayType != null ? arrayType.Next : argument.Type),
					AdditionalArraySpecifiers = { new ArraySpecifier() },
					Initializer = arrayInit
				};
			} else if (argument.Value is CAArgument) {
				// occurs with boxed arguments
				return ConvertArgumentValue((CAArgument)argument.Value);
			}
			var type = argument.Type.Resolve();
			if (type != null && type.IsEnum) {
				return MakePrimitive(Convert.ToInt64(argument.Value), type);
			} else if (argument.Value is TypeSig) {
				return CreateTypeOfExpression(null, null, ((TypeSig)argument.Value).ToTypeDefOrRef());
			} else if (argument.Value is UTF8String) {
				return new PrimitiveExpression(((UTF8String)argument.Value).String);
			} else {
				return new PrimitiveExpression(argument.Value);
			}
		}
		#endregion

		internal static Expression MakePrimitive(long val, ITypeDefOrRef type)
		{
			if (val == 0 && type.IsCorlibType("System", "Boolean"))
				return new Ast.PrimitiveExpression(false);
			else if (val == 1 && type.IsCorlibType("System", "Boolean"))
				return new Ast.PrimitiveExpression(true);
			else if (val == 0 && type.TryGetPtrSig() != null)
				return new Ast.NullReferenceExpression();
			if (type != null)
			{ // cannot rely on type.IsValueType, it's not set for typerefs (but is set for typespecs)
				TypeDef enumDefinition = type.ResolveTypeDef();
				if (enumDefinition != null && enumDefinition.IsEnum) {
					TypeCode enumBaseTypeCode = TypeCode.Int32;
					foreach (FieldDef field in enumDefinition.Fields) {
						if (field.IsStatic && object.Equals(CSharpPrimitiveCast.Cast(TypeCode.Int64, field.Constant.Value, false), val))
							return ConvertType(null, null, type).Member(field.Name).WithAnnotation(field);
						else if (!field.IsStatic)
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
						foreach (FieldDef field in enumDefinition.Fields.Where(fld => fld.IsStatic)) {
							long fieldValue = (long)CSharpPrimitiveCast.Cast(TypeCode.Int64, field.Constant.Value, false);
							if (fieldValue == 0)
								continue;	// skip None enum value

							if ((fieldValue & enumValue) == fieldValue) {
								var fieldExpression = ConvertType(null, null, type).Member(field.Name).WithAnnotation(field);
								if (expr == null)
									expr = fieldExpression;
								else
									expr = new BinaryOperatorExpression(expr, BinaryOperatorType.BitwiseOr, fieldExpression);

								enumValue &= ~fieldValue;
							}
							if ((fieldValue & negatedEnumValue) == fieldValue) {
								var fieldExpression = ConvertType(null, null, type).Member(field.Name).WithAnnotation(field);
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
					return new Ast.PrimitiveExpression(CSharpPrimitiveCast.Cast(enumBaseTypeCode, val, false)).CastTo(ConvertType(null, null, type));
				}
			}
			TypeCode code = TypeAnalysis.GetTypeCode(type.ToTypeSig());
			if (code == TypeCode.Object || code == TypeCode.Empty)
				code = TypeCode.Int32;
			return new Ast.PrimitiveExpression(CSharpPrimitiveCast.Cast(code, val, false));
		}

		static bool IsFlagsEnum(TypeDef type)
		{
			if (!type.HasCustomAttributes)
				return false;

			return type.CustomAttributes.Any(attr => attr.AttributeType.FullName == "System.FlagsAttribute");
		}

		/// <summary>
		/// Sets new modifier if the member hides some other member from a base type.
		/// </summary>
		/// <param name="member">The node of the member which new modifier state should be determined.</param>
		static void SetNewModifier(EntityDeclaration member)
		{
			try {
				bool addNewModifier = false;
				if (member is IndexerDeclaration) {
					var propertyDef = member.Annotation<PropertyDef>();
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

		private static bool HidesBaseMember(EntityDeclaration member)
		{
			var memberDefinition = member.Annotation<IMemberDef>();
			bool addNewModifier = false;
			var methodDefinition = memberDefinition as MethodDef;
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
		static bool HidesByName(IMemberDef member, bool includeBaseMethods)
		{
			Debug.Assert(!(member is PropertyDef) || !((PropertyDef)member).IsIndexer());

			if (member.DeclaringType.BaseType != null) {
				var baseTypeRef = member.DeclaringType.BaseType;
				while (baseTypeRef != null) {
					var baseType = baseTypeRef.ResolveTypeDefThrow();
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

		static bool AnyIsHiddenBy<T>(IEnumerable<T> members, IMemberDef derived, Predicate<T> condition = null)
			where T : IMemberDef
		{
			return members.Any(m => m.Name == derived.Name
			                   && (condition == null || condition(m))
			                   && TypesHierarchyHelpers.IsVisibleFromDerived(m, derived.DeclaringType));
		}
	}
}
