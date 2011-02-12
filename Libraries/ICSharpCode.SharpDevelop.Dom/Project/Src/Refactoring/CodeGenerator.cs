// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ICSharpCode.NRefactory.Ast;
using ICSharpCode.NRefactory.AstBuilder;
using NR = ICSharpCode.NRefactory.Ast;

namespace ICSharpCode.SharpDevelop.Dom.Refactoring
{
	/// <summary>
	/// Provides code generation facilities.
	/// </summary>
	public abstract class CodeGenerator
	{
		protected CodeGenerator()
		{
			HostCallback.InitializeCodeGeneratorOptions(this);
		}
		
		#region Dummy Code Generator
		public static readonly CodeGenerator DummyCodeGenerator = new DummyCodeGeneratorClass();
		
		private class DummyCodeGeneratorClass : CodeGenerator
		{
			public override string GenerateCode(AbstractNode node, string indentation)
			{
				return " -  there is no code generator for this language - ";
			}
		}
		#endregion
		
		#region DOM -> NRefactory conversion (static)
		public static TypeReference ConvertType(IReturnType returnType, ClassFinder context)
		{
			if (returnType == null)           return TypeReference.Null;
			if (returnType is NullReturnType) return TypeReference.Null;
			
			ArrayReturnType arrayReturnType = returnType.CastToArrayReturnType();
			if (arrayReturnType != null) {
				TypeReference typeRef = ConvertType(arrayReturnType.ArrayElementType, context);
				int[] rank = typeRef.RankSpecifier ?? new int[0];
				Array.Resize(ref rank, rank.Length + 1);
				rank[rank.Length - 1] = arrayReturnType.ArrayDimensions - 1;
				typeRef.RankSpecifier = rank;
				return typeRef;
			}
			PointerReturnType pointerReturnType = returnType.CastToDecoratingReturnType<PointerReturnType>();
			if (pointerReturnType != null) {
				TypeReference typeRef = ConvertType(pointerReturnType.BaseType, context);
				typeRef.PointerNestingLevel++;
				return typeRef;
			}
			
			IList<IReturnType> typeArguments = EmptyList<IReturnType>.Instance;
			if (returnType.IsConstructedReturnType) {
				typeArguments = returnType.CastToConstructedReturnType().TypeArguments;
			}
			IClass c = returnType.GetUnderlyingClass();
			if (c != null) {
				return CreateTypeReference(c, typeArguments, context);
			} else {
				TypeReference typeRef;
				if (IsPrimitiveType(returnType))
					typeRef = new TypeReference(returnType.FullyQualifiedName, true);
				else if (context != null && CanUseShortTypeName(returnType, context))
					typeRef = new TypeReference(returnType.Name);
				else {
					string fullName = returnType.FullyQualifiedName;
					if (string.IsNullOrEmpty(fullName))
						fullName = returnType.Name;
					typeRef = new TypeReference(fullName);
				}
				foreach (IReturnType typeArgument in typeArguments) {
					typeRef.GenericTypes.Add(ConvertType(typeArgument, context));
				}
				return typeRef;
			}
		}
		
		static TypeReference CreateTypeReference(IClass c, IList<IReturnType> typeArguments, ClassFinder context)
		{
			if (c.DeclaringType != null) {
				TypeReference outerClass = CreateTypeReference(c.DeclaringType, typeArguments, context);
				List<TypeReference> args = new List<TypeReference>();
				for (int i = c.DeclaringType.TypeParameters.Count; i < Math.Min(c.TypeParameters.Count, typeArguments.Count); i++) {
					args.Add(ConvertType(typeArguments[i], context));
				}
				return new InnerClassTypeReference(outerClass, c.Name, args);
			} else {
				TypeReference typeRef;
				if (IsPrimitiveType(c.DefaultReturnType))
					typeRef = new TypeReference(c.FullyQualifiedName, true);
				else if (context != null && CanUseShortTypeName(c.DefaultReturnType, context))
					typeRef = new TypeReference(c.Name);
				else
					typeRef = new TypeReference(c.FullyQualifiedName);
				for (int i = 0; i < Math.Min(c.TypeParameters.Count, typeArguments.Count); i++) {
					typeRef.GenericTypes.Add(ConvertType(typeArguments[i], context));
				}
				return typeRef;
			}
		}
		
		static bool IsPrimitiveType(IReturnType returnType)
		{
			return TypeReference.PrimitiveTypesCSharpReverse.ContainsKey(returnType.FullyQualifiedName);
		}
		
		/// <summary>
		/// Returns true if the short name of a type is valid in the given context.
		/// Returns false for primitive types because they should be passed around using their
		/// fully qualified names to allow the ambience or output visitor to use the intrinsic
		/// type name.
		/// </summary>
		public static bool CanUseShortTypeName(IReturnType returnType, ClassFinder context)
		{
			if (returnType == null || context == null)
				return false;
			IReturnType typeInTargetContext = context.SearchType(returnType.Name, returnType.TypeArgumentCount);
			return typeInTargetContext != null
				&& typeInTargetContext.FullyQualifiedName == returnType.FullyQualifiedName
				&& typeInTargetContext.TypeArgumentCount == returnType.TypeArgumentCount;
		}
		
		public static Modifiers ConvertModifier(ModifierEnum modifiers, ClassFinder targetContext)
		{
			if (targetContext != null && targetContext.ProjectContent != null && targetContext.CallingClass != null) {
				if (targetContext.ProjectContent.Language.IsClassWithImplicitlyStaticMembers(targetContext.CallingClass)) {
					return ((Modifiers)modifiers) & ~Modifiers.Static;
				}
			}
			if (modifiers.HasFlag(ModifierEnum.Static))
				modifiers &= ~(ModifierEnum.Abstract | ModifierEnum.Sealed);
			return (Modifiers)modifiers;
		}
		
		public static NR.ParameterModifiers ConvertModifier(Dom.ParameterModifiers m)
		{
			return (NR.ParameterModifiers)m;
		}
		
		public static UsingDeclaration ConvertUsing(IUsing u)
		{
			List<Using> usings = new List<Using>();
			foreach (string name in u.Usings) {
				usings.Add(new Using(name));
			}
			if (u.HasAliases) {
				foreach (KeyValuePair<string, IReturnType> pair in u.Aliases) {
					usings.Add(new Using(pair.Key, ConvertType(pair.Value, null)));
				}
			}
			return new UsingDeclaration(usings);
		}
		
		public static List<ParameterDeclarationExpression> ConvertParameters(IList<IParameter> parameters, ClassFinder targetContext)
		{
			List<ParameterDeclarationExpression> l = new List<ParameterDeclarationExpression>(parameters.Count);
			foreach (IParameter p in parameters) {
				ParameterDeclarationExpression pd = new ParameterDeclarationExpression(ConvertType(p.ReturnType, targetContext),
				                                                                       p.Name,
				                                                                       ConvertModifier(p.Modifiers));
				pd.Attributes = ConvertAttributes(p.Attributes, targetContext);
				l.Add(pd);
			}
			return l;
		}
		
		public static List<AttributeSection> ConvertAttributes(IList<IAttribute> attributes, ClassFinder targetContext)
		{
			AttributeSection sec = new AttributeSection();
			foreach (IAttribute att in attributes) {
				sec.Attributes.Add(new ICSharpCode.NRefactory.Ast.Attribute(
					ConvertType(att.AttributeType, targetContext).Type,
					att.PositionalArguments.Select(o => (Expression)new PrimitiveExpression(o)).ToList(),
					att.NamedArguments.Select(p => new NamedArgumentExpression(p.Key, new PrimitiveExpression(p.Value))).ToList()
				));
			}
			List<AttributeSection> resultList = new List<AttributeSection>(1);
			if (sec.Attributes.Count > 0)
				resultList.Add(sec);
			return resultList;
		}
		
		public static List<TemplateDefinition> ConvertTemplates(IList<ITypeParameter> l, ClassFinder targetContext)
		{
			List<TemplateDefinition> o = new List<TemplateDefinition>(l.Count);
			foreach (ITypeParameter p in l) {
				TemplateDefinition td = new TemplateDefinition(p.Name, ConvertAttributes(p.Attributes, targetContext));
				foreach (IReturnType rt in p.Constraints) {
					td.Bases.Add(ConvertType(rt, targetContext));
				}
				o.Add(td);
			}
			return o;
		}
		
		public static BlockStatement CreateNotImplementedBlock()
		{
			BlockStatement b = new BlockStatement();
			b.Throw(new TypeReference("NotImplementedException").New());
			return b;
		}
		
		public static AttributedNode ConvertMember(IMethod m, ClassFinder targetContext)
		{
			if (m.IsConstructor) {
				return new ConstructorDeclaration(m.Name,
				                                  ConvertModifier(m.Modifiers, targetContext),
				                                  ConvertParameters(m.Parameters, targetContext),
				                                  ConvertAttributes(m.Attributes, targetContext)) {
					Body = CreateNotImplementedBlock()
				};
			} else if (m.Name == "#dtor") { // TODO : maybe add IsDestructor property?
				return new DestructorDeclaration(m.Name,
				                                 ConvertModifier(m.Modifiers, targetContext),
				                                 ConvertAttributes(m.Attributes, targetContext)) {
					Body = CreateNotImplementedBlock()
				};
			} else {
				return new MethodDeclaration {
					Name = m.Name,
					Modifier = ConvertModifier(m.Modifiers, targetContext),
					TypeReference = ConvertType(m.ReturnType, targetContext),
					Parameters = ConvertParameters(m.Parameters, targetContext),
					Attributes = ConvertAttributes(m.Attributes, targetContext),
					Templates = ConvertTemplates(m.TypeParameters, targetContext),
					Body = m.Modifiers.HasFlag(ModifierEnum.Extern) ? null : CreateNotImplementedBlock(),
					IsExtensionMethod = m.IsExtensionMethod,
					InterfaceImplementations = ConvertInterfaceImplementations(m.InterfaceImplementations, targetContext)
				};
			}
		}
		
		public static List<InterfaceImplementation> ConvertInterfaceImplementations(IEnumerable<ExplicitInterfaceImplementation> items, ClassFinder targetContext)
		{
			return items
				.Select(i => new InterfaceImplementation(ConvertType(i.InterfaceReference, targetContext), i.MemberName))
				.ToList();
		}
		
		public static AttributedNode ConvertMember(IMember m, ClassFinder targetContext)
		{
			if (m == null)
				throw new ArgumentNullException("m");
			if (m is IProperty)
				return ConvertMember((IProperty)m, targetContext);
			else if (m is IMethod)
				return ConvertMember((IMethod)m, targetContext);
			else if (m is IEvent)
				return ConvertMember((IEvent)m, targetContext);
			else if (m is IField)
				return ConvertMember((IField)m, targetContext);
			else
				throw new ArgumentException("Unknown member: " + m.GetType().FullName);
		}
		
		public static PropertyDeclaration ConvertMember(IProperty p, ClassFinder targetContext)
		{
			PropertyDeclaration md = new PropertyDeclaration(ConvertModifier(p.Modifiers, targetContext),
			                                                 ConvertAttributes(p.Attributes, targetContext),
			                                                 p.Name,
			                                                 ConvertParameters(p.Parameters, targetContext));
			md.TypeReference = ConvertType(p.ReturnType, targetContext);
			md.InterfaceImplementations = ConvertInterfaceImplementations(p.InterfaceImplementations, targetContext);
			if (p.CanGet) {
				md.GetRegion = new PropertyGetRegion(p.Modifiers.HasFlag(ModifierEnum.Extern) ? null : CreateNotImplementedBlock(), null);
				md.GetRegion.Modifier = ConvertModifier(p.GetterModifiers, null);
			}
			if (p.CanSet) {
				md.SetRegion = new PropertySetRegion(p.Modifiers.HasFlag(ModifierEnum.Extern) ? null : CreateNotImplementedBlock(), null);
				md.SetRegion.Modifier = ConvertModifier(p.SetterModifiers, null);
			}
			return md;
		}
		
		public static FieldDeclaration ConvertMember(IField f, ClassFinder targetContext)
		{
			TypeReference type = ConvertType(f.ReturnType, targetContext);
			
			FieldDeclaration fd = new FieldDeclaration(ConvertAttributes(f.Attributes, targetContext),
			                                           type, ConvertModifier(f.Modifiers, targetContext));
			
			VariableDeclaration vd = new VariableDeclaration(f.Name, null, type);
			fd.Fields.Add(vd);

			
			if (f.IsConst && f.DeclaringType.ClassType != ClassType.Enum)
				vd.Initializer = ExpressionBuilder.CreateDefaultValueForType(type);
			else if (f.Modifiers.HasFlag(ModifierEnum.Fixed)) {
				if (f.ReturnType.IsArrayReturnType)
					fd.TypeReference = ConvertType(f.ReturnType.CastToArrayReturnType().ArrayElementType, targetContext);
				vd.FixedArrayInitialization = new PrimitiveExpression(1);
			}
			
			return fd;
		}
		
		public static EventDeclaration ConvertMember(IEvent e, ClassFinder targetContext)
		{
			return new EventDeclaration {
				TypeReference = ConvertType(e.ReturnType, targetContext),
				Name = e.Name,
				Modifier = ConvertModifier(e.Modifiers, targetContext),
				Attributes = ConvertAttributes(e.Attributes, targetContext),
				InterfaceImplementations = ConvertInterfaceImplementations(e.InterfaceImplementations, targetContext)

			};
		}
		
		public static AttributedNode ConvertClass(IClass c, ClassFinder targetContext)
		{
			if (c.ClassType == Dom.ClassType.Delegate) {
				IMethod invoke = c.Methods.First(m => m.Name == "Invoke");
				
				var d = new DelegateDeclaration(ConvertModifier(c.Modifiers, targetContext), ConvertAttributes(c.Attributes, targetContext)) {
					Name = c.Name,
					Parameters = ConvertParameters(invoke.Parameters, targetContext),
					ReturnType = ConvertType(invoke.ReturnType, targetContext),
					Templates = ConvertTemplates(c.TypeParameters, targetContext)
				};
				
				return d;
			} else {
				var t = new TypeDeclaration(ConvertModifier(c.Modifiers, targetContext), ConvertAttributes(c.Attributes, targetContext)) {
					Type = (NRefactory.Ast.ClassType)c.ClassType,
					BaseTypes = c.BaseTypes.Select(type => ConvertType(type, targetContext)).ToList(),
					Templates = ConvertTemplates(c.TypeParameters, targetContext),
					Name = c.Name
				};
				
				AttributedNode[] members = c.AllMembers.Select(m => ConvertMember(m, targetContext)).ToArray();
				
				if (c.ClassType == ClassType.Interface) {
					foreach (MethodDeclaration node in members.OfType<MethodDeclaration>()) {
						node.Modifier &= ~(Modifiers.Public | Modifiers.Private | Modifiers.Protected | Modifiers.Internal);
						node.Body = null;
					}
					foreach (PropertyDeclaration node in members.OfType<PropertyDeclaration>()) {
						node.Modifier &= ~(Modifiers.Public | Modifiers.Private | Modifiers.Protected | Modifiers.Internal);
						node.GetRegion.Block = null;
						node.SetRegion.Block = null;
					}
					foreach (EventDeclaration node in members.OfType<EventDeclaration>()) {
						node.Modifier &= ~(Modifiers.Public | Modifiers.Private | Modifiers.Protected | Modifiers.Internal);
					}
				}
				
				t.Children.AddRange(members);
				t.Children.AddRange(c.InnerClasses.Select(c2 => ConvertClass(c2, targetContext)));
				
				return t;
			}
		}
		#endregion
		
		readonly CodeGeneratorOptions options = new CodeGeneratorOptions();
		
		public CodeGeneratorOptions Options {
			get { return options; }
		}
		
		#region Code generation / insertion
		public virtual void InsertCodeAfter(IClass @class, IRefactoringDocument document, params AbstractNode[] nodes)
		{
			InsertCodeAfter(@class.BodyRegion.EndLine, document,
			                GetIndentation(document, @class.BodyRegion.BeginLine), nodes);
		}
		
		public virtual void InsertCodeAfter(IMember member, IRefactoringDocument document, params AbstractNode[] nodes)
		{
			if (member is IMethodOrProperty) {
				InsertCodeAfter(((IMethodOrProperty)member).BodyRegion.EndLine, document,
				                GetIndentation(document, member.Region.BeginLine), nodes);
			} else {
				int line = member.Region.EndLine;
				// VB uses the position after the EOL as end location for fields, so insert after
				// the previous line if the end position is pointing to the start of a line.
				if (member.Region.EndColumn == 1)
					line--;
				InsertCodeAfter(line, document,
				                GetIndentation(document, member.Region.BeginLine), nodes);
			}
		}
		
		public virtual void InsertCodeAtEnd(DomRegion region, IRefactoringDocument document, params AbstractNode[] nodes)
		{
			InsertCodeAfter(region.EndLine - 1, document,
			                GetIndentation(document, region.BeginLine) + options.IndentString, nodes);
		}
		
		public virtual void InsertCodeInClass(IClass c, IRefactoringDocument document, int targetLine, params AbstractNode[] nodes)
		{
			InsertCodeAfter(targetLine, document,
			                GetIndentation(document, c.Region.BeginLine) + options.IndentString, false, nodes);
		}
		
		protected string GetIndentation(IRefactoringDocument document, int line)
		{
			string lineText = document.GetLine(line).Text;
			return lineText.Substring(0, lineText.Length - lineText.TrimStart().Length);
		}
		
		/// <summary>
		/// Generates code for <paramref name="nodes"/> and inserts it into <paramref name="document"/>
		/// after the line <paramref name="insertLine"/>.
		/// </summary>
		protected void InsertCodeAfter(int insertLine, IRefactoringDocument document, string indentation, params AbstractNode[] nodes)
		{
			InsertCodeAfter(insertLine, document, indentation, true, nodes);
		}
		
		/// <summary>
		/// Generates code for <paramref name="nodes"/> and inserts it into <paramref name="document"/>
		/// after the line <paramref name="insertLine"/>.
		/// </summary>
		protected void InsertCodeAfter(int insertLine, IRefactoringDocument document, string indentation, bool startWithEmptyLine, params AbstractNode[] nodes)
		{
			StringBuilder b = new StringBuilder();
			for (int i = 0; i < nodes.Length; i++) {
				if (options.EmptyLinesBetweenMembers) {
					if (startWithEmptyLine || i > 0) {
						b.AppendLine(indentation);
					}
				}
				b.Append(GenerateCode(nodes[i], indentation));
			}
			if (insertLine < document.TotalNumberOfLines) {
				IRefactoringDocumentLine lineSegment = document.GetLine(insertLine + 1);
				document.Insert(lineSegment.Offset, b.ToString());
			} else {
				b.Insert(0, Environment.NewLine);
				document.Insert(document.TextLength, b.ToString());
			}
		}
		
		/// <summary>
		/// Generates code for the NRefactory node.
		/// </summary>
		public abstract string GenerateCode(AbstractNode node, string indentation);
		#endregion
		
		#region Generate property
		public virtual string GetPropertyName(string fieldName)
		{
			if (string.IsNullOrEmpty(fieldName))
				return fieldName;
			if (fieldName.StartsWith("_") && fieldName.Length > 1)
				return Char.ToUpper(fieldName[1]) + fieldName.Substring(2);
			else if (fieldName.StartsWith("m_") && fieldName.Length > 2)
				return Char.ToUpper(fieldName[2]) + fieldName.Substring(3);
			else
				return Char.ToUpper(fieldName[0]) + fieldName.Substring(1);
		}
		
		public virtual string GetParameterName(string fieldName)
		{
			if (string.IsNullOrEmpty(fieldName))
				return fieldName;
			if (fieldName.StartsWith("_") && fieldName.Length > 1)
				return Char.ToLower(fieldName[1]) + fieldName.Substring(2);
			else if (fieldName.StartsWith("m_") && fieldName.Length > 2)
				return Char.ToLower(fieldName[2]) + fieldName.Substring(3);
			else
				return Char.ToLower(fieldName[0]) + fieldName.Substring(1);
		}
		
		public virtual string GetFieldName(string propertyName)
		{
			if (string.IsNullOrEmpty(propertyName))
				return propertyName;
			string newName = Char.ToLower(propertyName[0]) + propertyName.Substring(1);
			if (newName == propertyName)
				return "_" + newName;
			else
				return newName;
		}
		
		public virtual PropertyDeclaration CreateProperty(IField field, bool createGetter, bool createSetter)
		{
			ClassFinder targetContext = new ClassFinder(field);
			string name = GetPropertyName(field.Name);
			PropertyDeclaration property = new PropertyDeclaration(ConvertModifier(field.Modifiers, targetContext),
			                                                       null,
			                                                       name,
			                                                       null);
			property.TypeReference = ConvertType(field.ReturnType, new ClassFinder(field));
			if (createGetter) {
				BlockStatement block = new BlockStatement();
				block.Return(new IdentifierExpression(field.Name));
				property.GetRegion = new PropertyGetRegion(block, null);
			}
			if (createSetter) {
				BlockStatement block = new BlockStatement();
				block.Assign(new IdentifierExpression(field.Name), new IdentifierExpression("value"));
				property.SetRegion = new PropertySetRegion(block, null);
			}
			
			property.Modifier = Modifiers.Public | (property.Modifier & Modifiers.Static);
			return property;
		}
		#endregion
		
		#region Generate Changed Event
		public virtual void CreateChangedEvent(IProperty property, IRefactoringDocument document)
		{
			ClassFinder targetContext = new ClassFinder(property);
			string name = property.Name + "Changed";
			EventDeclaration ed = new EventDeclaration {
				TypeReference = new TypeReference("EventHandler"),
				Name = name,
				Modifier = ConvertModifier(property.Modifiers & (ModifierEnum.VisibilityMask | ModifierEnum.Static), targetContext),
			};
			InsertCodeAfter(property, document, ed);
			
			List<Expression> arguments = new List<Expression>(2);
			if (property.IsStatic)
				arguments.Add(new PrimitiveExpression(null, "null"));
			else
				arguments.Add(new ThisReferenceExpression());
			arguments.Add(new IdentifierExpression("EventArgs").Member("Empty"));
			InsertCodeAtEnd(property.SetterRegion, document,
			                new RaiseEventStatement(name, arguments));
		}
		#endregion
		
		#region Generate OnEventMethod
		public virtual MethodDeclaration CreateOnEventMethod(IEvent e)
		{
			ClassFinder context = new ClassFinder(e);
			List<ParameterDeclarationExpression> parameters = new List<ParameterDeclarationExpression>();
			bool sender = false;
			if (e.ReturnType != null) {
				IMethod invoke = e.ReturnType.GetMethods().Find(delegate(IMethod m) { return m.Name=="Invoke"; });
				if (invoke != null) {
					foreach (IParameter param in invoke.Parameters) {
						parameters.Add(new ParameterDeclarationExpression(ConvertType(param.ReturnType, context), param.Name));
					}
					if (parameters.Count > 0 && string.Equals(parameters[0].ParameterName, "sender", StringComparison.InvariantCultureIgnoreCase)) {
						sender = true;
						parameters.RemoveAt(0);
					}
				}
			}
			
			ModifierEnum modifier;
			if (e.IsStatic)
				modifier = ModifierEnum.Private | ModifierEnum.Static;
			else if (e.DeclaringType.IsSealed)
				modifier = ModifierEnum.Protected;
			else
				modifier = ModifierEnum.Protected | ModifierEnum.Virtual;
			MethodDeclaration method = new MethodDeclaration {
				Name = "On" + e.Name,
				Modifier = ConvertModifier(modifier, context),
				TypeReference = new TypeReference("System.Void", true),
				Parameters = parameters
			};
			
			List<Expression> arguments = new List<Expression>();
			if (sender) {
				if (e.IsStatic)
					arguments.Add(new PrimitiveExpression(null, "null"));
				else
					arguments.Add(new ThisReferenceExpression());
			}
			foreach (ParameterDeclarationExpression param in parameters) {
				arguments.Add(new IdentifierExpression(param.ParameterName));
			}
			method.Body = new BlockStatement();
			method.Body.AddChild(new RaiseEventStatement(e.Name, arguments));
			
			return method;
		}
		#endregion
		
		#region Interface implementation
		protected string GetInterfaceName(IReturnType interf, IMember member, ClassFinder context)
		{
			if (CanUseShortTypeName(member.DeclaringType.DefaultReturnType, context))
				return member.DeclaringType.Name;
			else
				return member.DeclaringType.FullyQualifiedName;
		}
		
		public virtual void ImplementInterface(IReturnType interf, IRefactoringDocument document, bool explicitImpl, IClass targetClass)
		{
			List<AbstractNode> nodes = new List<AbstractNode>();
			ImplementInterface(nodes, interf, explicitImpl, targetClass);
			InsertCodeAtEnd(targetClass.Region, document, nodes.ToArray());
		}
		
		static bool InterfaceMemberAlreadyImplementedParametersAreIdentical(IMember a, IMember b)
		{
			if (a is IMethodOrProperty && b is IMethodOrProperty) {
				return DiffUtility.Compare(((IMethodOrProperty)a).Parameters,
				                           ((IMethodOrProperty)b).Parameters) == 0;
			} else {
				return true;
			}
		}
		
		static T CloneAndAddExplicitImpl<T>(T member, IClass targetClass)
			where T : class, IMember
		{
			T copy = (T)member.Clone();
			copy.DeclaringTypeReference = targetClass.DefaultReturnType;
			copy.InterfaceImplementations.Add(new ExplicitInterfaceImplementation(member.DeclaringTypeReference, member.Name));
			return copy;
		}
		
		// FIXME this whole method could be probably replaced by DOM.ExtensionMethodsPublic.HasMember
		public static bool InterfaceMemberAlreadyImplemented<T>(IEnumerable<T> existingMembers, T interfaceMember,
		                                                        out bool requireAlternativeImplementation)
			where T : class, IMember
		{
			IReturnType interf = interfaceMember.DeclaringTypeReference;
			requireAlternativeImplementation = false;
			foreach (T existing in existingMembers) {
				StringComparer nameComparer = existing.DeclaringType.ProjectContent.Language.NameComparer;
				
				// if existing has same name as interfaceMember, and for methods the parameter list must also be identical:
				if (nameComparer.Equals(existing.Name, interfaceMember.Name)) {
					if (InterfaceMemberAlreadyImplementedParametersAreIdentical(existing, interfaceMember)) {
						// implicit implementation found
						if (object.Equals(existing.ReturnType, interfaceMember.ReturnType)) {
							return true;
						} else {
							requireAlternativeImplementation = true;
						}
					}
				} else {
					foreach (ExplicitInterfaceImplementation eii in existing.InterfaceImplementations) {
						if (object.Equals(eii.InterfaceReference, interf) && nameComparer.Equals(eii.MemberName, interfaceMember.Name)) {
							if (InterfaceMemberAlreadyImplementedParametersAreIdentical(existing, interfaceMember)) {
								// explicit implementation found
								if (object.Equals(existing.ReturnType, interfaceMember.ReturnType)) {
									return true;
								} else {
									requireAlternativeImplementation = true;
								}
							}
						}
					}
				}
			}
			return false;
		}
		
		static InterfaceImplementation CreateInterfaceImplementation(IMember interfaceMember, ClassFinder context)
		{
			return new InterfaceImplementation(ConvertType(interfaceMember.DeclaringTypeReference, context), interfaceMember.Name);
		}
		
		/// <summary>
		/// Adds the methods implementing the <paramref name="interf"/> to the list
		/// <paramref name="nodes"/>.
		/// </summary>
		public virtual void ImplementInterface(IList<AbstractNode> nodes, IReturnType interf, bool explicitImpl, IClass targetClass)
		{
			ClassFinder context = new ClassFinder(targetClass, targetClass.Region.BeginLine + 1, 0);
			Modifiers implicitImplModifier = ConvertModifier(ModifierEnum.Public, context);
			Modifiers explicitImplModifier = ConvertModifier(context.Language.ExplicitInterfaceImplementationIsPrivateScope ? ModifierEnum.None : ModifierEnum.Public, context);
			List<IEvent> targetClassEvents = targetClass.DefaultReturnType.GetEvents();
			bool requireAlternativeImplementation;
			foreach (IEvent e in interf.GetEvents()) {
				if (!InterfaceMemberAlreadyImplemented(targetClassEvents, e, out requireAlternativeImplementation)) {
					EventDeclaration ed = ConvertMember(e, context);
					ed.Attributes.Clear();
					if (explicitImpl || requireAlternativeImplementation) {
						ed.InterfaceImplementations.Add(CreateInterfaceImplementation(e, context));
						
						if (context.Language.RequiresAddRemoveRegionInExplicitInterfaceImplementation) {
							ed.AddRegion = new EventAddRegion(null);
							ed.AddRegion.Block = CreateNotImplementedBlock();
							ed.RemoveRegion = new EventRemoveRegion(null);
							ed.RemoveRegion.Block = CreateNotImplementedBlock();
						}
						
						targetClassEvents.Add(CloneAndAddExplicitImpl(e, targetClass));
						ed.Modifier = explicitImplModifier;
					} else {
						targetClassEvents.Add(e);
						ed.Modifier = implicitImplModifier;
					}
					nodes.Add(ed);
				}
			}
			List<IProperty> targetClassProperties = targetClass.DefaultReturnType.GetProperties();
			foreach (IProperty p in interf.GetProperties()) {
				if (!InterfaceMemberAlreadyImplemented(targetClassProperties, p, out requireAlternativeImplementation)) {
					AttributedNode pd = ConvertMember(p, context);
					pd.Attributes.Clear();
					if (explicitImpl || requireAlternativeImplementation) {
						InterfaceImplementation impl = CreateInterfaceImplementation(p, context);
						((PropertyDeclaration)pd).InterfaceImplementations.Add(impl);
						targetClassProperties.Add(CloneAndAddExplicitImpl(p, targetClass));
						pd.Modifier = explicitImplModifier;
					} else {
						targetClassProperties.Add(p);
						pd.Modifier = implicitImplModifier;
					}
					nodes.Add(pd);
				}
			}
			List<IMethod> targetClassMethods = targetClass.DefaultReturnType.GetMethods();
			foreach (IMethod m in interf.GetMethods()) {
				if (!InterfaceMemberAlreadyImplemented(targetClassMethods, m, out requireAlternativeImplementation)) {
					MethodDeclaration md = ConvertMember(m, context) as MethodDeclaration;
					md.Attributes.Clear();
					if (md != null) {
						if (explicitImpl || requireAlternativeImplementation) {
							md.InterfaceImplementations.Add(CreateInterfaceImplementation(m, context));
							targetClassMethods.Add(CloneAndAddExplicitImpl(m, targetClass));
							md.Modifier = explicitImplModifier;
						} else {
							targetClassMethods.Add(m);
							md.Modifier = implicitImplModifier;
						}
						nodes.Add(md);
					}
				}
			}
		}
		#endregion
		
		#region Abstract class implementation
		public static void ImplementAbstractClass(IRefactoringDocument doc, IClass target, IReturnType abstractClass)
		{
			CodeGenerator generator = target.ProjectContent.Language.CodeGenerator;
			var pos = doc.OffsetToPosition(doc.PositionToOffset(target.BodyRegion.EndLine, target.BodyRegion.EndColumn) - 1);
			ClassFinder context = new ClassFinder(target, pos.Line, pos.Column);
			
			foreach (IMember member in MemberLookupHelper.GetAccessibleMembers(abstractClass, target, LanguageProperties.CSharp, true)
			         .Where(m => m.IsAbstract && !target.HasMember(m))) {
				generator.InsertCodeAtEnd(target.BodyRegion, doc, generator.GetOverridingMethod(member, context));
			}
		}
		#endregion
		
		#region Override member
		public virtual AttributedNode GetOverridingMethod(IMember baseMember, ClassFinder targetContext)
		{
			AbstractMember newMember = (AbstractMember)baseMember.Clone();
			newMember.Modifiers &= ~(ModifierEnum.Virtual | ModifierEnum.Abstract);
			newMember.Modifiers |= ModifierEnum.Override;
			// set modifiers be before calling convert so that a body is generated
			AttributedNode node = ConvertMember(newMember, targetContext);
			node.Attributes.Clear(); // don't copy over attributes
			
			if (!baseMember.IsAbstract) {
				// replace the method/property body with a call to the base method/property
				MethodDeclaration method = node as MethodDeclaration;
				if (method != null) {
					method.Body.Children.Clear();
					if (method.TypeReference.Type == "System.Void") {
						method.Body.AddChild(new ExpressionStatement(CreateForwardingMethodCall(method)));
					} else {
						method.Body.AddChild(new ReturnStatement(CreateForwardingMethodCall(method)));
					}
				}
				PropertyDeclaration property = node as PropertyDeclaration;
				if (property != null) {
					Expression field = new BaseReferenceExpression().Member(property.Name);
					if (!property.GetRegion.Block.IsNull) {
						property.GetRegion.Block.Children.Clear();
						property.GetRegion.Block.Return(field);
					}
					if (!property.SetRegion.Block.IsNull) {
						property.SetRegion.Block.Children.Clear();
						property.SetRegion.Block.Assign(field, new IdentifierExpression("value"));
					}
				}
			}
			return node;
		}
		
		static InvocationExpression CreateForwardingMethodCall(MethodDeclaration method)
		{
			Expression methodName = new MemberReferenceExpression(new BaseReferenceExpression(),
			                                                      method.Name);
			InvocationExpression ie = new InvocationExpression(methodName, null);
			foreach (ParameterDeclarationExpression param in method.Parameters) {
				Expression expr = new IdentifierExpression(param.ParameterName);
				if (param.ParamModifier == NR.ParameterModifiers.Ref) {
					expr = new DirectionExpression(FieldDirection.Ref, expr);
				} else if (param.ParamModifier == NR.ParameterModifiers.Out) {
					expr = new DirectionExpression(FieldDirection.Out, expr);
				}
				ie.Arguments.Add(expr);
			}
			return ie;
		}
		#endregion
		
		#region Using statements
		public virtual void ReplaceUsings(IRefactoringDocument document, IList<IUsing> oldUsings, IList<IUsing> newUsings)
		{
			if (oldUsings.Count == newUsings.Count) {
				bool identical = true;
				for (int i = 0; i < oldUsings.Count; i++) {
					if (oldUsings[i] != newUsings[i]) {
						identical = false;
						break;
					}
				}
				if (identical) return;
			}
			
			int firstLine = int.MaxValue;
			List<KeyValuePair<int, int>> regions = new List<KeyValuePair<int, int>>();
			foreach (IUsing u in oldUsings) {
				if (u.Region.BeginLine < firstLine)
					firstLine = u.Region.BeginLine;
				int st = document.PositionToOffset(u.Region.BeginLine, u.Region.BeginColumn);
				int en = document.PositionToOffset(u.Region.EndLine, u.Region.EndColumn);
				regions.Add(new KeyValuePair<int, int>(st, en - st));
			}
			
			regions.Sort(delegate(KeyValuePair<int, int> a, KeyValuePair<int, int> b) {
			             	return a.Key.CompareTo(b.Key);
			             });
			int insertionOffset = regions.Count == 0 ? 0 : regions[0].Key;
			string indentation;
			if (firstLine != int.MaxValue) {
				indentation = GetIndentation(document, firstLine);
				insertionOffset -= indentation.Length;
			} else {
				indentation = "";
			}
			
			document.StartUndoableAction();
			for (int i = regions.Count - 1; i >= 0; i--) {
				document.Remove(regions[i].Key, regions[i].Value);
			}
			int lastNewLine = insertionOffset;
			for (int i = insertionOffset; i < document.TextLength; i++) {
				char c = document.GetCharAt(i);
				if (!char.IsWhiteSpace(c))
					break;
				if (c == '\n') {
					if (i > 0 && document.GetCharAt(i - 1) == '\r')
						lastNewLine = i - 1;
					else
						lastNewLine = i;
				}
			}
			if (lastNewLine != insertionOffset) {
				document.Remove(insertionOffset, lastNewLine - insertionOffset);
			}
			StringBuilder txt = new StringBuilder();
			foreach (IUsing us in newUsings) {
				if (us == null)
					txt.AppendLine(indentation);
				else
					txt.Append(GenerateCode(ConvertUsing(us), indentation));
			}
			document.Insert(insertionOffset, txt.ToString());
			document.EndUndoableAction();
		}
		#endregion
	}
}
