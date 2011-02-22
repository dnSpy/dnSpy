using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Decompiler.Transforms;
using ICSharpCode.Decompiler;
using ICSharpCode.NRefactory.CSharp;
using Mono.Cecil;
using Mono.Cecil.Cil;
using ClassType = ICSharpCode.NRefactory.TypeSystem.ClassType;
using VarianceModifier = ICSharpCode.NRefactory.TypeSystem.VarianceModifier;

namespace Decompiler
{
	public class AstBuilder
	{
		DecompilerContext context = new DecompilerContext();
		CompilationUnit astCompileUnit = new CompilationUnit();
		Dictionary<string, NamespaceDeclaration> astNamespaces = new Dictionary<string, NamespaceDeclaration>();
		
		public AstBuilder(DecompilerContext context)
		{
			if (context == null)
				throw new ArgumentNullException("context");
			this.context = context;
		}
		
		public static bool MemberIsHidden(MemberReference member)
		{
			MethodDefinition method = member as MethodDefinition;
			if (method != null && (method.IsGetter || method.IsSetter || method.IsAddOn || method.IsRemoveOn))
				return true;
			if (method != null && method.Name.StartsWith("<", StringComparison.Ordinal) && method.IsCompilerGenerated())
				return true;
			TypeDefinition type = member as TypeDefinition;
			if (type != null && type.DeclaringType != null && type.Name.StartsWith("<>c__DisplayClass", StringComparison.Ordinal) && type.IsCompilerGenerated())
				return true;
			FieldDefinition field = member as FieldDefinition;
			if (field != null && field.Name.StartsWith("CS$<>", StringComparison.Ordinal) && field.IsCompilerGenerated())
				return true;
			return false;
		}
		
		public void GenerateCode(ITextOutput output)
		{
			GenerateCode(output, null);
		}
		
		public void GenerateCode(ITextOutput output, Predicate<IAstTransform> transformAbortCondition)
		{
			TransformationPipeline.RunTransformationsUntil(astCompileUnit, transformAbortCondition, context);
			astCompileUnit.AcceptVisitor(new InsertParenthesesVisitor { InsertParenthesesForReadability = true }, null);
			
			var outputFormatter = new TextOutputFormatter(output);
			var formattingPolicy = new CSharpFormattingPolicy();
			// disable whitespace in front of parentheses:
			formattingPolicy.BeforeMethodCallParentheses = false;
			formattingPolicy.BeforeMethodDeclarationParentheses = false;
			formattingPolicy.BeforeConstructorDeclarationParentheses = false;
			formattingPolicy.BeforeDelegateDeclarationParentheses = false;
			astCompileUnit.AcceptVisitor(new OutputVisitor(outputFormatter, formattingPolicy), null);
		}
		
		public void AddAssembly(AssemblyDefinition assemblyDefinition)
		{
			astCompileUnit.AddChild(
				new UsingDeclaration {
					Import = new SimpleType("System")
				}, CompilationUnit.MemberRole);
			
			foreach(TypeDefinition typeDef in assemblyDefinition.MainModule.Types) {
				// Skip nested types - they will be added by the parent type
				if (typeDef.DeclaringType != null) continue;
				// Skip the <Module> class
				if (typeDef.Name == "<Module>") continue;
				
				AddType(typeDef);
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
			TypeDeclaration astType = CreateType(typeDef);
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
		
		public TypeDeclaration CreateType(TypeDefinition typeDef)
		{
			// create IL code mappings - used for debugger
			if (!CSharpCodeMapping.SourceCodeMappings.ContainsKey(typeDef.FullName)) {
				CSharpCodeMapping.SourceCodeMappings.TryAdd(typeDef.FullName, new List<MethodMapping>());
			} else {
				CSharpCodeMapping.SourceCodeMappings[typeDef.FullName].Clear();
			}
			
			// create type
			TypeDeclaration astType = new TypeDeclaration();
			astType.AddAnnotation(typeDef);
			astType.Modifiers = ConvertModifiers(typeDef);
			astType.Name = typeDef.Name;
			
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
			
			astType.TypeParameters.AddRange(MakeTypeParameters(typeDef.GenericParameters));
			astType.Constraints.AddRange(MakeConstraints(typeDef.GenericParameters));
			
			// Nested types
			foreach(TypeDefinition nestedTypeDef in typeDef.NestedTypes) {
				if (MemberIsHidden(nestedTypeDef))
					continue;
				astType.AddChild(CreateType(nestedTypeDef), TypeDeclaration.MemberRole);
			}
			
			
			if (typeDef.IsEnum) {
				foreach (FieldDefinition field in typeDef.Fields) {
					if (field.IsRuntimeSpecialName) {
						// the value__ field
						astType.AddChild(ConvertType(field.FieldType), TypeDeclaration.BaseTypeRole);
					} else {
						EnumMemberDeclaration enumMember = new EnumMemberDeclaration();
						enumMember.Name = field.Name;
						astType.AddChild(enumMember, TypeDeclaration.MemberRole);
					}
				}
			} else {
				// Base type
				if (typeDef.BaseType != null && !typeDef.IsValueType && typeDef.BaseType.FullName != "System.Object") {
					astType.AddChild(ConvertType(typeDef.BaseType), TypeDeclaration.BaseTypeRole);
				}
				foreach (var i in typeDef.Interfaces)
					astType.AddChild(ConvertType(i), TypeDeclaration.BaseTypeRole);
				
				
				AddTypeMembers(astType, typeDef);
			}
			
			return astType;
		}
		
		#region Convert Type Reference
		/// <summary>
		/// Converts a type reference.
		/// </summary>
		/// <param name="type">The Cecil type reference that should be converted into
		/// a type system type reference.</param>
		/// <param name="typeAttributes">Attributes associated with the Cecil type reference.
		/// This is used to support the 'dynamic' type.</param>
		public static AstType ConvertType(TypeReference type, ICustomAttributeProvider typeAttributes = null)
		{
			int typeIndex = 0;
			return ConvertType(type, typeAttributes, ref typeIndex);
		}
		
		static AstType ConvertType(TypeReference type, ICustomAttributeProvider typeAttributes, ref int typeIndex)
		{
			while (type is OptionalModifierType || type is RequiredModifierType) {
				type = ((TypeSpecification)type).ElementType;
			}
			if (type == null) {
				return AstType.Null;
			}
			
			if (type is Mono.Cecil.ByReferenceType) {
				typeIndex++;
				// ignore by reference type (cannot be represented in C#)
				return ConvertType((type as Mono.Cecil.ByReferenceType).ElementType, typeAttributes, ref typeIndex);
			} else if (type is Mono.Cecil.PointerType) {
				typeIndex++;
				return ConvertType((type as Mono.Cecil.PointerType).ElementType, typeAttributes, ref typeIndex)
					.MakePointerType();
			} else if (type is Mono.Cecil.ArrayType) {
				typeIndex++;
				return ConvertType((type as Mono.Cecil.ArrayType).ElementType, typeAttributes, ref typeIndex)
					.MakeArrayType((type as Mono.Cecil.ArrayType).Rank);
			} else if (type is GenericInstanceType) {
				GenericInstanceType gType = (GenericInstanceType)type;
				if (gType.ElementType.Namespace == "System" && gType.ElementType.Name == "Nullable`1" && gType.GenericArguments.Count == 1) {
					typeIndex++;
					return new ComposedType {
						BaseType = ConvertType(gType.GenericArguments[0], typeAttributes, ref typeIndex),
						HasNullableSpecifier = true
					};
				}
				AstType baseType = ConvertType(gType.ElementType, typeAttributes, ref typeIndex);
				foreach (var typeArgument in gType.GenericArguments) {
					typeIndex++;
					baseType.AddChild(ConvertType(typeArgument, typeAttributes, ref typeIndex), AstType.Roles.TypeArgument);
				}
				return baseType;
			} else if (type is GenericParameter) {
				return new SimpleType(type.Name);
			} else if (type.IsNested) {
				AstType typeRef = ConvertType(type.DeclaringType, typeAttributes, ref typeIndex);
				string namepart = ICSharpCode.NRefactory.TypeSystem.ReflectionHelper.SplitTypeParameterCountFromReflectionName(type.Name);
				return new MemberType { Target = typeRef, MemberName = namepart }.WithAnnotation(type);
			} else {
				string ns = type.Namespace ?? string.Empty;
				string name = type.Name;
				if (name == null)
					throw new InvalidOperationException("type.Name returned null. Type: " + type.ToString());
				
				if (name == "Object" && ns == "System" && HasDynamicAttribute(typeAttributes, typeIndex)) {
					return new PrimitiveType("dynamic");
				} else {
					if (ns == "System") {
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
					
					name = ICSharpCode.NRefactory.TypeSystem.ReflectionHelper.SplitTypeParameterCountFromReflectionName(name);
					
					// TODO: Until we can simplify type with 'using', use just the name without namesapce
					return new SimpleType(name).WithAnnotation(type);
					
//					if (ns.Length == 0)
//						return new SimpleType(name).WithAnnotation(type);
//					string[] parts = ns.Split('.');
//					AstType nsType = new SimpleType(parts[0]);
//					for (int i = 1; i < parts.Length; i++) {
//						nsType = new MemberType { Target = nsType, MemberName = parts[i] };
//					}
//					return new MemberType { Target = nsType, MemberName = name }.WithAnnotation(type);
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
			// Add fields
			foreach(FieldDefinition fieldDef in typeDef.Fields) {
				if (MemberIsHidden(fieldDef)) continue;
				astType.AddChild(CreateField(fieldDef), TypeDeclaration.MemberRole);
			}
			
			// Add events
			foreach(EventDefinition eventDef in typeDef.Events) {
				astType.AddChild(CreateEvent(eventDef), TypeDeclaration.MemberRole);
			}
			
			// Add properties
			foreach(PropertyDefinition propDef in typeDef.Properties) {
				astType.AddChild(CreateProperty(propDef), TypeDeclaration.MemberRole);
			}
			
			// Add constructors
			foreach(MethodDefinition methodDef in typeDef.Methods) {
				if (!methodDef.IsConstructor) continue;
				
				astType.AddChild(CreateConstructor(methodDef), TypeDeclaration.MemberRole);
			}
			
			// Add methods
			foreach(MethodDefinition methodDef in typeDef.Methods) {
				if (methodDef.IsConstructor || MemberIsHidden(methodDef)) continue;
				
				astType.AddChild(CreateMethod(methodDef), TypeDeclaration.MemberRole);
			}
		}

		MethodDeclaration CreateMethod(MethodDefinition methodDef)
		{
			// Create mapping - used in debugger
			MethodMapping methodMapping = methodDef.CreateCodeMapping(CSharpCodeMapping.SourceCodeMappings);
			
			MethodDeclaration astMethod = new MethodDeclaration();
			astMethod.AddAnnotation(methodDef);
			astMethod.AddAnnotation(methodMapping);
			astMethod.ReturnType = ConvertType(methodDef.ReturnType, methodDef.MethodReturnType);
			astMethod.Name = methodDef.Name;
			astMethod.TypeParameters.AddRange(MakeTypeParameters(methodDef.GenericParameters));
			astMethod.Parameters.AddRange(MakeParameters(methodDef.Parameters));
			astMethod.Constraints.AddRange(MakeConstraints(methodDef.GenericParameters));
			if (!methodDef.DeclaringType.IsInterface) {
				astMethod.Modifiers = ConvertModifiers(methodDef);
				astMethod.Body = AstMethodBodyBuilder.CreateMethodBody(methodDef, context);
			}
			return astMethod;
		}
		
		IEnumerable<TypeParameterDeclaration> MakeTypeParameters(IEnumerable<GenericParameter> genericParameters)
		{
			return genericParameters.Select(
				gp => new TypeParameterDeclaration {
					Name = gp.Name,
					Variance = gp.IsContravariant ? VarianceModifier.Contravariant : gp.IsCovariant ? VarianceModifier.Covariant : VarianceModifier.Invariant
				});
		}
		
		IEnumerable<Constraint> MakeConstraints(IEnumerable<GenericParameter> genericParameters)
		{
			// TODO
			return Enumerable.Empty<Constraint>();
		}
		
		ConstructorDeclaration CreateConstructor(MethodDefinition methodDef)
		{
			// Create mapping - used in debugger
			MethodMapping methodMapping = methodDef.CreateCodeMapping(CSharpCodeMapping.SourceCodeMappings);
			
			ConstructorDeclaration astMethod = new ConstructorDeclaration();
			astMethod.AddAnnotation(methodDef);
			if (methodMapping != null)
				astMethod.AddAnnotation(methodMapping);
			astMethod.Modifiers = ConvertModifiers(methodDef);
			if (methodDef.IsStatic) {
				// don't show visibility for static ctors
				astMethod.Modifiers &= ~Modifiers.VisibilityMask;
			}
			astMethod.Name = methodDef.DeclaringType.Name;
			astMethod.Parameters.AddRange(MakeParameters(methodDef.Parameters));
			astMethod.Body = AstMethodBodyBuilder.CreateMethodBody(methodDef, context);
			return astMethod;
		}

		PropertyDeclaration CreateProperty(PropertyDefinition propDef)
		{
			PropertyDeclaration astProp = new PropertyDeclaration();
			astProp.AddAnnotation(propDef);
			astProp.Modifiers = ConvertModifiers(propDef.GetMethod ?? propDef.SetMethod);
			astProp.Name = propDef.Name;
			astProp.ReturnType = ConvertType(propDef.PropertyType, propDef);
			if (propDef.GetMethod != null) {
				// Create mapping - used in debugger
				MethodMapping methodMapping = propDef.GetMethod.CreateCodeMapping(CSharpCodeMapping.SourceCodeMappings);
				
				astProp.Getter = new Accessor {
					Body = AstMethodBodyBuilder.CreateMethodBody(propDef.GetMethod, context)
				}.WithAnnotation(propDef.GetMethod);
				
				if (methodMapping != null)
					astProp.Getter.AddAnnotation(methodMapping);
			}
			if (propDef.SetMethod != null) {
				// Create mapping - used in debugger
				MethodMapping methodMapping = propDef.SetMethod.CreateCodeMapping(CSharpCodeMapping.SourceCodeMappings);
				
				astProp.Setter = new Accessor {
					Body = AstMethodBodyBuilder.CreateMethodBody(propDef.SetMethod, context)
				}.WithAnnotation(propDef.SetMethod);
				
				if (methodMapping != null)
					astProp.Setter.AddAnnotation(methodMapping);
			}
			return astProp;
		}

		CustomEventDeclaration CreateEvent(EventDefinition eventDef)
		{
			CustomEventDeclaration astEvent = new CustomEventDeclaration();
			astEvent.AddAnnotation(eventDef);
			astEvent.Name = eventDef.Name;
			astEvent.ReturnType = ConvertType(eventDef.EventType, eventDef);
			astEvent.Modifiers = ConvertModifiers(eventDef.AddMethod);
			if (eventDef.AddMethod != null) {
				// Create mapping - used in debugger
				MethodMapping methodMapping = eventDef.AddMethod.CreateCodeMapping(CSharpCodeMapping.SourceCodeMappings);
				
				astEvent.AddAccessor = new Accessor {
					Body = AstMethodBodyBuilder.CreateMethodBody(eventDef.AddMethod, context)
				}.WithAnnotation(eventDef.AddMethod);
				
				if (methodMapping != null)
					astEvent.AddAccessor.AddAnnotation(methodMapping);
			}
			if (eventDef.RemoveMethod != null) {
				// Create mapping - used in debugger
				MethodMapping methodMapping = eventDef.RemoveMethod.CreateCodeMapping(CSharpCodeMapping.SourceCodeMappings);
				
				astEvent.RemoveAccessor = new Accessor {
					Body = AstMethodBodyBuilder.CreateMethodBody(eventDef.RemoveMethod, context)
				}.WithAnnotation(eventDef.RemoveMethod);

				if (methodMapping != null)
					astEvent.RemoveAccessor.AddAnnotation(methodMapping);
			}
			return astEvent;
		}

		FieldDeclaration CreateField(FieldDefinition fieldDef)
		{
			FieldDeclaration astField = new FieldDeclaration();
			astField.AddAnnotation(fieldDef);
			VariableInitializer initializer = new VariableInitializer(fieldDef.Name);
			astField.AddChild(initializer, FieldDeclaration.Roles.Variable);
			astField.ReturnType = ConvertType(fieldDef.FieldType, fieldDef);
			astField.Modifiers = ConvertModifiers(fieldDef);
			if (fieldDef.HasConstant) {
				if (fieldDef.Constant == null)
					initializer.Initializer = new NullReferenceExpression();
				else
					initializer.Initializer = new PrimitiveExpression(fieldDef.Constant);
			}
			return astField;
		}
		
		public static IEnumerable<ParameterDeclaration> MakeParameters(IEnumerable<ParameterDefinition> paramCol)
		{
			foreach(ParameterDefinition paramDef in paramCol) {
				ParameterDeclaration astParam = new ParameterDeclaration();
				astParam.Type = ConvertType(paramDef.ParameterType, paramDef);
				astParam.Name = paramDef.Name;
				
				if (!paramDef.IsIn && paramDef.IsOut) astParam.ParameterModifier = ParameterModifier.Out;
				if (paramDef.IsIn && paramDef.IsOut)  astParam.ParameterModifier = ParameterModifier.Ref;
				// TODO: params, this
				
				yield return astParam;
			}
		}
	}
}
