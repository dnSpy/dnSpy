// Copyright (c) AlphaSierraPapa for the SharpDevelop Team
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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using ICSharpCode.NRefactory.CSharp.TypeSystem;
using ICSharpCode.NRefactory.CSharp.TypeSystem.ConstantValues;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.TypeSystem.Implementation;

namespace ICSharpCode.NRefactory.CSharp.TypeSystem
{
	/// <summary>
	/// Produces type and member definitions from the DOM.
	/// </summary>
	public class TypeSystemConvertVisitor : DepthFirstAstVisitor<IUnresolvedEntity>
	{
		readonly CSharpUnresolvedFile unresolvedFile;
		UsingScope usingScope;
		CSharpUnresolvedTypeDefinition currentTypeDefinition;
		DefaultUnresolvedMethod currentMethod;
		
		IInterningProvider interningProvider = new SimpleInterningProvider();
		
		/// <summary>
		/// Gets/Sets the interning provider to use.
		/// The default value is a new <see cref="SimpleInterningProvider"/> instance.
		/// </summary>
		public IInterningProvider InterningProvider {
			get { return interningProvider; }
			set { interningProvider = value; }
		}
		
		/// <summary>
		/// Gets/Sets whether to ignore XML documentation.
		/// The default value is false.
		/// </summary>
		public bool SkipXmlDocumentation { get; set; }
		
		/// <summary>
		/// Creates a new TypeSystemConvertVisitor.
		/// </summary>
		/// <param name="fileName">The file name (used for DomRegions).</param>
		public TypeSystemConvertVisitor(string fileName)
		{
			if (fileName == null)
				throw new ArgumentNullException("fileName");
			this.unresolvedFile = new CSharpUnresolvedFile(fileName);
			this.usingScope = unresolvedFile.RootUsingScope;
		}
		
		/// <summary>
		/// Creates a new TypeSystemConvertVisitor and initializes it with a given context.
		/// </summary>
		/// <param name="unresolvedFile">The parsed file to which members should be added.</param>
		/// <param name="currentUsingScope">The current using scope.</param>
		/// <param name="currentTypeDefinition">The current type definition.</param>
		public TypeSystemConvertVisitor(CSharpUnresolvedFile unresolvedFile, UsingScope currentUsingScope = null, CSharpUnresolvedTypeDefinition currentTypeDefinition = null)
		{
			if (unresolvedFile == null)
				throw new ArgumentNullException("unresolvedFile");
			this.unresolvedFile = unresolvedFile;
			this.usingScope = currentUsingScope ?? unresolvedFile.RootUsingScope;
			this.currentTypeDefinition = currentTypeDefinition;
		}
		
		public CSharpUnresolvedFile UnresolvedFile {
			get { return unresolvedFile; }
		}
		
		DomRegion MakeRegion(TextLocation start, TextLocation end)
		{
			return new DomRegion(unresolvedFile.FileName, start.Line, start.Column, end.Line, end.Column);
		}
		
		DomRegion MakeRegion(AstNode node)
		{
			if (node == null || node.IsNull)
				return DomRegion.Empty;
			else
				return MakeRegion(GetStartLocationAfterAttributes(node), node.EndLocation);
		}
		
		internal static TextLocation GetStartLocationAfterAttributes(AstNode node)
		{
			AstNode child = node.FirstChild;
			// Skip attributes and comments between attributes for the purpose of
			// getting a declaration's region.
			while (child != null && (child is AttributeSection || child.NodeType == NodeType.Whitespace))
				child = child.NextSibling;
			return (child ?? node).StartLocation;
		}
		
		DomRegion MakeBraceRegion(AstNode node)
		{
			if (node == null || node.IsNull)
				return DomRegion.Empty;
			else
				return MakeRegion(node.GetChildByRole(Roles.LBrace).StartLocation,
				                  node.GetChildByRole(Roles.RBrace).EndLocation);
		}
		
		#region Compilation Unit
		public override IUnresolvedEntity VisitSyntaxTree (SyntaxTree unit)
		{
			unresolvedFile.Errors = unit.Errors;
			return base.VisitSyntaxTree (unit);
		}
		#endregion
		
		#region Using Declarations
		public override IUnresolvedEntity VisitExternAliasDeclaration(ExternAliasDeclaration externAliasDeclaration)
		{
			usingScope.ExternAliases.Add(externAliasDeclaration.Name);
			return null;
		}
		
		public override IUnresolvedEntity VisitUsingDeclaration(UsingDeclaration usingDeclaration)
		{
			TypeOrNamespaceReference u = usingDeclaration.Import.ToTypeReference(NameLookupMode.TypeInUsingDeclaration) as TypeOrNamespaceReference;
			if (u != null) {
				if (interningProvider != null)
					u = interningProvider.Intern(u);
				usingScope.Usings.Add(u);
			}
			return null;
		}
		
		public override IUnresolvedEntity VisitUsingAliasDeclaration(UsingAliasDeclaration usingDeclaration)
		{
			TypeOrNamespaceReference u = usingDeclaration.Import.ToTypeReference(NameLookupMode.TypeInUsingDeclaration) as TypeOrNamespaceReference;
			if (u != null) {
				if (interningProvider != null)
					u = interningProvider.Intern(u);
				usingScope.UsingAliases.Add(new KeyValuePair<string, TypeOrNamespaceReference>(usingDeclaration.Alias, u));
			}
			return null;
		}
		#endregion
		
		#region Namespace Declaration
		public override IUnresolvedEntity VisitNamespaceDeclaration(NamespaceDeclaration namespaceDeclaration)
		{
			DomRegion region = MakeRegion(namespaceDeclaration);
			UsingScope previousUsingScope = usingScope;
			foreach (Identifier ident in namespaceDeclaration.Identifiers) {
				usingScope = new UsingScope(usingScope, ident.Name);
				usingScope.Region = region;
			}
			base.VisitNamespaceDeclaration(namespaceDeclaration);
			unresolvedFile.UsingScopes.Add(usingScope); // add after visiting children so that nested scopes come first
			usingScope = previousUsingScope;
			return null;
		}
		#endregion
		
		#region Type Definitions
		CSharpUnresolvedTypeDefinition CreateTypeDefinition(string name)
		{
			CSharpUnresolvedTypeDefinition newType;
			if (currentTypeDefinition != null) {
				newType = new CSharpUnresolvedTypeDefinition(currentTypeDefinition, name);
				foreach (var typeParameter in currentTypeDefinition.TypeParameters)
					newType.TypeParameters.Add(typeParameter);
				currentTypeDefinition.NestedTypes.Add(newType);
			} else {
				newType = new CSharpUnresolvedTypeDefinition(usingScope, name);
				unresolvedFile.TopLevelTypeDefinitions.Add(newType);
			}
			newType.UnresolvedFile = unresolvedFile;
			newType.HasExtensionMethods = false; // gets set to true when an extension method is added
			return newType;
		}
		
		public override IUnresolvedEntity VisitTypeDeclaration(TypeDeclaration typeDeclaration)
		{
			var td = currentTypeDefinition = CreateTypeDefinition(typeDeclaration.Name);
			td.Region = MakeRegion(typeDeclaration);
			td.BodyRegion = MakeBraceRegion(typeDeclaration);
			AddXmlDocumentation(td, typeDeclaration);
			
			ApplyModifiers(td, typeDeclaration.Modifiers);
			switch (typeDeclaration.ClassType) {
				case ClassType.Enum:
					td.Kind = TypeKind.Enum;
					break;
				case ClassType.Interface:
					td.Kind = TypeKind.Interface;
					td.IsAbstract = true; // interfaces are implicitly abstract
					break;
				case ClassType.Struct:
					td.Kind = TypeKind.Struct;
					td.IsSealed = true; // enums/structs are implicitly sealed
					break;
			}
			
			ConvertAttributes(td.Attributes, typeDeclaration.Attributes);
			
			ConvertTypeParameters(td.TypeParameters, typeDeclaration.TypeParameters, typeDeclaration.Constraints, EntityType.TypeDefinition);
			
			foreach (AstType baseType in typeDeclaration.BaseTypes) {
				td.BaseTypes.Add(baseType.ToTypeReference(NameLookupMode.BaseTypeReference));
			}
			
			foreach (EntityDeclaration member in typeDeclaration.Members) {
				member.AcceptVisitor(this);
			}
			
			currentTypeDefinition = (CSharpUnresolvedTypeDefinition)currentTypeDefinition.DeclaringTypeDefinition;
			if (interningProvider != null) {
				td.ApplyInterningProvider(interningProvider);
			}
			return td;
		}
		
		public override IUnresolvedEntity VisitDelegateDeclaration(DelegateDeclaration delegateDeclaration)
		{
			var td = currentTypeDefinition = CreateTypeDefinition(delegateDeclaration.Name);
			td.Kind = TypeKind.Delegate;
			td.Region = MakeRegion(delegateDeclaration);
			td.BaseTypes.Add(KnownTypeReference.MulticastDelegate);
			AddXmlDocumentation(td, delegateDeclaration);
			
			ApplyModifiers(td, delegateDeclaration.Modifiers);
			td.IsSealed = true; // delegates are implicitly sealed
			
			ConvertTypeParameters(td.TypeParameters, delegateDeclaration.TypeParameters, delegateDeclaration.Constraints, EntityType.TypeDefinition);
			
			ITypeReference returnType = delegateDeclaration.ReturnType.ToTypeReference();
			List<IUnresolvedParameter> parameters = new List<IUnresolvedParameter>();
			ConvertParameters(parameters, delegateDeclaration.Parameters);
			AddDefaultMethodsToDelegate(td, returnType, parameters);
			
			foreach (AttributeSection section in delegateDeclaration.Attributes) {
				if (section.AttributeTarget == "return") {
					List<IUnresolvedAttribute> returnTypeAttributes = new List<IUnresolvedAttribute>();
					ConvertAttributes(returnTypeAttributes, section);
					IUnresolvedMethod invokeMethod = (IUnresolvedMethod)td.Members.Single(m => m.Name == "Invoke");
					IUnresolvedMethod endInvokeMethod = (IUnresolvedMethod)td.Members.Single(m => m.Name == "EndInvoke");
					foreach (IUnresolvedAttribute attr in returnTypeAttributes) {
						invokeMethod.ReturnTypeAttributes.Add(attr);
						endInvokeMethod.ReturnTypeAttributes.Add(attr);
					}
				} else {
					ConvertAttributes(td.Attributes, section);
				}
			}
			
			currentTypeDefinition = (CSharpUnresolvedTypeDefinition)currentTypeDefinition.DeclaringTypeDefinition;
			if (interningProvider != null) {
				td.ApplyInterningProvider(interningProvider);
			}
			return td;
		}
		
		static readonly IUnresolvedParameter delegateObjectParameter = MakeParameter(KnownTypeReference.Object, "object");
		static readonly IUnresolvedParameter delegateIntPtrMethodParameter = MakeParameter(KnownTypeReference.IntPtr, "method");
		static readonly IUnresolvedParameter delegateAsyncCallbackParameter = MakeParameter(typeof(AsyncCallback).ToTypeReference(), "callback");
		static readonly IUnresolvedParameter delegateResultParameter = MakeParameter(typeof(IAsyncResult).ToTypeReference(), "result");
		
		static IUnresolvedParameter MakeParameter(ITypeReference type, string name)
		{
			DefaultUnresolvedParameter p = new DefaultUnresolvedParameter(type, name);
			p.Freeze();
			return p;
		}
		
		/// <summary>
		/// Adds the 'Invoke', 'BeginInvoke', 'EndInvoke' methods, and a constructor, to the <paramref name="delegateType"/>.
		/// </summary>
		public static void AddDefaultMethodsToDelegate(DefaultUnresolvedTypeDefinition delegateType, ITypeReference returnType, IEnumerable<IUnresolvedParameter> parameters)
		{
			if (delegateType == null)
				throw new ArgumentNullException("delegateType");
			if (returnType == null)
				throw new ArgumentNullException("returnType");
			if (parameters == null)
				throw new ArgumentNullException("parameters");
			
			DomRegion region = delegateType.Region;
			region = new DomRegion(region.FileName, region.BeginLine, region.BeginColumn); // remove end position
			
			DefaultUnresolvedMethod invoke = new DefaultUnresolvedMethod(delegateType, "Invoke");
			invoke.Accessibility = Accessibility.Public;
			invoke.IsSynthetic = true;
			foreach (var p in parameters)
				invoke.Parameters.Add(p);
			invoke.ReturnType = returnType;
			invoke.Region = region;
			delegateType.Members.Add(invoke);
			
			DefaultUnresolvedMethod beginInvoke = new DefaultUnresolvedMethod(delegateType, "BeginInvoke");
			beginInvoke.Accessibility = Accessibility.Public;
			beginInvoke.IsSynthetic = true;
			foreach (var p in parameters)
				beginInvoke.Parameters.Add(p);
			beginInvoke.Parameters.Add(delegateAsyncCallbackParameter);
			beginInvoke.Parameters.Add(delegateObjectParameter);
			beginInvoke.ReturnType = delegateResultParameter.Type;
			beginInvoke.Region = region;
			delegateType.Members.Add(beginInvoke);
			
			DefaultUnresolvedMethod endInvoke = new DefaultUnresolvedMethod(delegateType, "EndInvoke");
			endInvoke.Accessibility = Accessibility.Public;
			endInvoke.IsSynthetic = true;
			endInvoke.Parameters.Add(delegateResultParameter);
			endInvoke.ReturnType = invoke.ReturnType;
			endInvoke.Region = region;
			delegateType.Members.Add(endInvoke);
			
			DefaultUnresolvedMethod ctor = new DefaultUnresolvedMethod(delegateType, ".ctor");
			ctor.EntityType = EntityType.Constructor;
			ctor.Accessibility = Accessibility.Public;
			ctor.IsSynthetic = true;
			ctor.Parameters.Add(delegateObjectParameter);
			ctor.Parameters.Add(delegateIntPtrMethodParameter);
			ctor.ReturnType = delegateType;
			ctor.Region = region;
			delegateType.Members.Add(ctor);
		}
		#endregion
		
		#region Fields
		public override IUnresolvedEntity VisitFieldDeclaration(FieldDeclaration fieldDeclaration)
		{
			bool isSingleField = fieldDeclaration.Variables.Count == 1;
			Modifiers modifiers = fieldDeclaration.Modifiers;
			DefaultUnresolvedField field = null;
			foreach (VariableInitializer vi in fieldDeclaration.Variables) {
				field = new DefaultUnresolvedField(currentTypeDefinition, vi.Name);
				
				field.Region = isSingleField ? MakeRegion(fieldDeclaration) : MakeRegion(vi);
				field.BodyRegion = MakeRegion(vi);
				ConvertAttributes(field.Attributes, fieldDeclaration.Attributes);
				AddXmlDocumentation(field, fieldDeclaration);
				
				ApplyModifiers(field, modifiers);
				field.IsVolatile = (modifiers & Modifiers.Volatile) != 0;
				field.IsReadOnly = (modifiers & Modifiers.Readonly) != 0;
				
				field.ReturnType = fieldDeclaration.ReturnType.ToTypeReference();
				
				if ((modifiers & Modifiers.Const) != 0) {
					field.ConstantValue = ConvertConstantValue(field.ReturnType, vi.Initializer);
					field.IsStatic = true;
				}
				
				currentTypeDefinition.Members.Add(field);
				if (interningProvider != null) {
					field.ApplyInterningProvider(interningProvider);
				}
			}
			return isSingleField ? field : null;
		}
		
		public override IUnresolvedEntity VisitFixedFieldDeclaration(FixedFieldDeclaration fixedFieldDeclaration)
		{
			// TODO: add support for fixed fields
			return base.VisitFixedFieldDeclaration(fixedFieldDeclaration);
		}
		
		public override IUnresolvedEntity VisitEnumMemberDeclaration(EnumMemberDeclaration enumMemberDeclaration)
		{
			DefaultUnresolvedField field = new DefaultUnresolvedField(currentTypeDefinition, enumMemberDeclaration.Name);
			field.Region = field.BodyRegion = MakeRegion(enumMemberDeclaration);
			ConvertAttributes(field.Attributes, enumMemberDeclaration.Attributes);
			AddXmlDocumentation(field, enumMemberDeclaration);
			
			if (currentTypeDefinition.TypeParameters.Count == 0) {
				field.ReturnType = currentTypeDefinition;
			} else {
				ITypeReference[] typeArgs = new ITypeReference[currentTypeDefinition.TypeParameters.Count];
				for (int i = 0; i < typeArgs.Length; i++) {
					typeArgs[i] = new TypeParameterReference(EntityType.TypeDefinition, i);
				}
				field.ReturnType = new ParameterizedTypeReference(currentTypeDefinition, typeArgs);
			}
			field.Accessibility = Accessibility.Public;
			field.IsStatic = true;
			if (!enumMemberDeclaration.Initializer.IsNull) {
				field.ConstantValue = ConvertConstantValue(field.ReturnType, enumMemberDeclaration.Initializer);
			} else {
				DefaultUnresolvedField prevField = currentTypeDefinition.Members.LastOrDefault() as DefaultUnresolvedField;
				if (prevField == null || prevField.ConstantValue == null) {
					field.ConstantValue = ConvertConstantValue(field.ReturnType, new PrimitiveExpression(0));
				} else {
					field.ConstantValue = new IncrementConstantValue(prevField.ConstantValue);
				}
			}
			
			currentTypeDefinition.Members.Add(field);
			if (interningProvider != null) {
				field.ApplyInterningProvider(interningProvider);
			}
			return field;
		}
		#endregion
		
		#region Methods
		public override IUnresolvedEntity VisitMethodDeclaration(MethodDeclaration methodDeclaration)
		{
			DefaultUnresolvedMethod m = new DefaultUnresolvedMethod(currentTypeDefinition, methodDeclaration.Name);
			currentMethod = m; // required for resolving type parameters
			m.Region = MakeRegion(methodDeclaration);
			m.BodyRegion = MakeRegion(methodDeclaration.Body);
			AddXmlDocumentation(m, methodDeclaration);
			
			if (InheritsConstraints(methodDeclaration) && methodDeclaration.Constraints.Count == 0) {
				int index = 0;
				foreach (TypeParameterDeclaration tpDecl in methodDeclaration.TypeParameters) {
					var tp = new MethodTypeParameterWithInheritedConstraints(index++, tpDecl.Name);
					tp.Region = MakeRegion(tpDecl);
					ConvertAttributes(tp.Attributes, tpDecl.Attributes);
					tp.Variance = tpDecl.Variance;
					m.TypeParameters.Add(tp);
				}
			} else {
				ConvertTypeParameters(m.TypeParameters, methodDeclaration.TypeParameters, methodDeclaration.Constraints, EntityType.Method);
			}
			m.ReturnType = methodDeclaration.ReturnType.ToTypeReference();
			ConvertAttributes(m.Attributes, methodDeclaration.Attributes.Where(s => s.AttributeTarget != "return"));
			ConvertAttributes(m.ReturnTypeAttributes, methodDeclaration.Attributes.Where(s => s.AttributeTarget == "return"));
			
			ApplyModifiers(m, methodDeclaration.Modifiers);
			if (methodDeclaration.IsExtensionMethod) {
				m.IsExtensionMethod = true;
				currentTypeDefinition.HasExtensionMethods = true;
			}
			m.IsPartial = methodDeclaration.HasModifier(Modifiers.Partial);
			m.HasBody = !methodDeclaration.Body.IsNull;
			
			ConvertParameters(m.Parameters, methodDeclaration.Parameters);
			if (!methodDeclaration.PrivateImplementationType.IsNull) {
				m.Accessibility = Accessibility.None;
				m.IsExplicitInterfaceImplementation = true;
				m.ExplicitInterfaceImplementations.Add(new DefaultMemberReference(
					m.EntityType, methodDeclaration.PrivateImplementationType.ToTypeReference(), m.Name,
					m.TypeParameters.Count, GetParameterTypes(m.Parameters)));
			}
			
			currentTypeDefinition.Members.Add(m);
			currentMethod = null;
			if (interningProvider != null) {
				m.ApplyInterningProvider(interningProvider);
			}
			return m;
		}
		
		IList<ITypeReference> GetParameterTypes(IList<IUnresolvedParameter> parameters)
		{
			if (parameters.Count == 0)
				return EmptyList<ITypeReference>.Instance;
			ITypeReference[] types = new ITypeReference[parameters.Count];
			for (int i = 0; i < types.Length; i++) {
				types[i] = parameters[i].Type;
			}
			return types;
		}
		
		bool InheritsConstraints(MethodDeclaration methodDeclaration)
		{
			// overrides and explicit interface implementations inherit constraints
			if ((methodDeclaration.Modifiers & Modifiers.Override) == Modifiers.Override)
				return true;
			return !methodDeclaration.PrivateImplementationType.IsNull;
		}
		
		void ConvertTypeParameters(IList<IUnresolvedTypeParameter> output, AstNodeCollection<TypeParameterDeclaration> typeParameters,
		                           AstNodeCollection<Constraint> constraints, EntityType ownerType)
		{
			// output might be non-empty when type parameters were copied from an outer class
			int index = output.Count;
			List<DefaultUnresolvedTypeParameter> list = new List<DefaultUnresolvedTypeParameter>();
			foreach (TypeParameterDeclaration tpDecl in typeParameters) {
				DefaultUnresolvedTypeParameter tp = new DefaultUnresolvedTypeParameter(ownerType, index++, tpDecl.Name);
				tp.Region = MakeRegion(tpDecl);
				ConvertAttributes(tp.Attributes, tpDecl.Attributes);
				tp.Variance = tpDecl.Variance;
				list.Add(tp);
				output.Add(tp); // tp must be added to list here so that it can be referenced by constraints
			}
			foreach (Constraint c in constraints) {
				foreach (var tp in list) {
					if (tp.Name == c.TypeParameter.Identifier) {
						foreach (AstType type in c.BaseTypes) {
							PrimitiveType primType = type as PrimitiveType;
							if (primType != null) {
								if (primType.Keyword == "new") {
									tp.HasDefaultConstructorConstraint = true;
									continue;
								} else if (primType.Keyword == "class") {
									tp.HasReferenceTypeConstraint = true;
									continue;
								} else if (primType.Keyword == "struct") {
									tp.HasValueTypeConstraint = true;
									continue;
								}
							}
							tp.Constraints.Add(type.ToTypeReference());
						}
						break;
					}
				}
			}
		}
		
		IMemberReference ConvertInterfaceImplementation(AstType interfaceType, AbstractUnresolvedMember unresolvedMember)
		{
			ITypeReference interfaceTypeReference = interfaceType.ToTypeReference();
			int typeParameterCount = 0;
			IList<ITypeReference> parameterTypes = null;
			if (unresolvedMember.EntityType == EntityType.Method) {
				typeParameterCount = ((IUnresolvedMethod)unresolvedMember).TypeParameters.Count;
			}
			IUnresolvedParameterizedMember parameterizedMember = unresolvedMember as IUnresolvedParameterizedMember;
			if (parameterizedMember != null) {
				parameterTypes = new ITypeReference[parameterizedMember.Parameters.Count];
				for (int i = 0; i < parameterTypes.Count; i++) {
					parameterTypes[i] = parameterizedMember.Parameters[i].Type;
				}
			}
			return new DefaultMemberReference(unresolvedMember.EntityType, interfaceTypeReference, unresolvedMember.Name, typeParameterCount, parameterTypes);
		}
		#endregion
		
		#region Operators
		public override IUnresolvedEntity VisitOperatorDeclaration(OperatorDeclaration operatorDeclaration)
		{
			DefaultUnresolvedMethod m = new DefaultUnresolvedMethod(currentTypeDefinition, operatorDeclaration.Name);
			m.EntityType = EntityType.Operator;
			m.Region = MakeRegion(operatorDeclaration);
			m.BodyRegion = MakeRegion(operatorDeclaration.Body);
			AddXmlDocumentation(m, operatorDeclaration);
			
			m.ReturnType = operatorDeclaration.ReturnType.ToTypeReference();
			ConvertAttributes(m.Attributes, operatorDeclaration.Attributes.Where(s => s.AttributeTarget != "return"));
			ConvertAttributes(m.ReturnTypeAttributes, operatorDeclaration.Attributes.Where(s => s.AttributeTarget == "return"));
			
			ApplyModifiers(m, operatorDeclaration.Modifiers);
			m.HasBody = !operatorDeclaration.Body.IsNull;
			
			ConvertParameters(m.Parameters, operatorDeclaration.Parameters);
			
			currentTypeDefinition.Members.Add(m);
			if (interningProvider != null) {
				m.ApplyInterningProvider(interningProvider);
			}
			return m;
		}
		#endregion
		
		#region Constructors
		public override IUnresolvedEntity VisitConstructorDeclaration(ConstructorDeclaration constructorDeclaration)
		{
			Modifiers modifiers = constructorDeclaration.Modifiers;
			bool isStatic = (modifiers & Modifiers.Static) != 0;
			DefaultUnresolvedMethod ctor = new DefaultUnresolvedMethod(currentTypeDefinition, isStatic ? ".cctor" : ".ctor");
			ctor.EntityType = EntityType.Constructor;
			ctor.Region = MakeRegion(constructorDeclaration);
			if (!constructorDeclaration.Initializer.IsNull) {
				ctor.BodyRegion = MakeRegion(constructorDeclaration.Initializer.StartLocation, constructorDeclaration.EndLocation);
			} else {
				ctor.BodyRegion = MakeRegion(constructorDeclaration.Body);
			}
			ctor.ReturnType = KnownTypeReference.Void;
			
			ConvertAttributes(ctor.Attributes, constructorDeclaration.Attributes);
			ConvertParameters(ctor.Parameters, constructorDeclaration.Parameters);
			AddXmlDocumentation(ctor, constructorDeclaration);
			ctor.HasBody = !constructorDeclaration.Body.IsNull;
			
			if (isStatic)
				ctor.IsStatic = true;
			else
				ApplyModifiers(ctor, modifiers);
			
			currentTypeDefinition.Members.Add(ctor);
			if (interningProvider != null) {
				ctor.ApplyInterningProvider(interningProvider);
			}
			return ctor;
		}
		#endregion
		
		#region Destructors
		public override IUnresolvedEntity VisitDestructorDeclaration(DestructorDeclaration destructorDeclaration)
		{
			DefaultUnresolvedMethod dtor = new DefaultUnresolvedMethod(currentTypeDefinition, "Finalize");
			dtor.EntityType = EntityType.Destructor;
			dtor.Region = MakeRegion(destructorDeclaration);
			dtor.BodyRegion = MakeRegion(destructorDeclaration.Body);
			dtor.Accessibility = Accessibility.Protected;
			dtor.IsOverride = true;
			dtor.ReturnType = KnownTypeReference.Void;
			dtor.HasBody = !destructorDeclaration.Body.IsNull;
			
			ConvertAttributes(dtor.Attributes, destructorDeclaration.Attributes);
			AddXmlDocumentation(dtor, destructorDeclaration);
			
			currentTypeDefinition.Members.Add(dtor);
			if (interningProvider != null) {
				dtor.ApplyInterningProvider(interningProvider);
			}
			return dtor;
		}
		#endregion
		
		#region Properties / Indexers
		public override IUnresolvedEntity VisitPropertyDeclaration(PropertyDeclaration propertyDeclaration)
		{
			DefaultUnresolvedProperty p = new DefaultUnresolvedProperty(currentTypeDefinition, propertyDeclaration.Name);
			p.Region = MakeRegion(propertyDeclaration);
			p.BodyRegion = MakeBraceRegion(propertyDeclaration);
			ApplyModifiers(p, propertyDeclaration.Modifiers);
			p.ReturnType = propertyDeclaration.ReturnType.ToTypeReference();
			ConvertAttributes(p.Attributes, propertyDeclaration.Attributes);
			AddXmlDocumentation(p, propertyDeclaration);
			if (!propertyDeclaration.PrivateImplementationType.IsNull) {
				p.Accessibility = Accessibility.None;
				p.IsExplicitInterfaceImplementation = true;
				p.ExplicitInterfaceImplementations.Add(new DefaultMemberReference(
					p.EntityType, propertyDeclaration.PrivateImplementationType.ToTypeReference(), p.Name));
			}
			bool isExtern = propertyDeclaration.HasModifier(Modifiers.Extern);
			p.Getter = ConvertAccessor(propertyDeclaration.Getter, p, "get_", isExtern);
			p.Setter = ConvertAccessor(propertyDeclaration.Setter, p, "set_", isExtern);
			currentTypeDefinition.Members.Add(p);
			if (interningProvider != null) {
				p.ApplyInterningProvider(interningProvider);
			}
			return p;
		}
		
		public override IUnresolvedEntity VisitIndexerDeclaration(IndexerDeclaration indexerDeclaration)
		{
			DefaultUnresolvedProperty p = new DefaultUnresolvedProperty(currentTypeDefinition, "Item");
			p.EntityType = EntityType.Indexer;
			p.Region = MakeRegion(indexerDeclaration);
			p.BodyRegion = MakeBraceRegion(indexerDeclaration);
			ApplyModifiers(p, indexerDeclaration.Modifiers);
			p.ReturnType = indexerDeclaration.ReturnType.ToTypeReference();
			ConvertAttributes(p.Attributes, indexerDeclaration.Attributes);
			AddXmlDocumentation(p, indexerDeclaration);
			
			ConvertParameters(p.Parameters, indexerDeclaration.Parameters);
			
			if (!indexerDeclaration.PrivateImplementationType.IsNull) {
				p.Accessibility = Accessibility.None;
				p.IsExplicitInterfaceImplementation = true;
				p.ExplicitInterfaceImplementations.Add(new DefaultMemberReference(
					p.EntityType, indexerDeclaration.PrivateImplementationType.ToTypeReference(), p.Name, 0, GetParameterTypes(p.Parameters)));
			}
			bool isExtern = indexerDeclaration.HasModifier(Modifiers.Extern);
			p.Getter = ConvertAccessor(indexerDeclaration.Getter, p, "get_", isExtern);
			p.Setter = ConvertAccessor(indexerDeclaration.Setter, p, "set_", isExtern);
			
			currentTypeDefinition.Members.Add(p);
			if (interningProvider != null) {
				p.ApplyInterningProvider(interningProvider);
			}
			return p;
		}
		
		DefaultUnresolvedMethod ConvertAccessor(Accessor accessor, IUnresolvedMember p, string prefix, bool memberIsExtern)
		{
			if (accessor.IsNull)
				return null;
			var a = new DefaultUnresolvedMethod(currentTypeDefinition, prefix + p.Name);
			a.EntityType = EntityType.Accessor;
			a.AccessorOwner = p;
			a.Accessibility = GetAccessibility(accessor.Modifiers) ?? p.Accessibility;
			a.IsAbstract = p.IsAbstract;
			a.IsOverride = p.IsOverride;
			a.IsSealed = p.IsSealed;
			a.IsStatic = p.IsStatic;
			a.IsSynthetic = p.IsSynthetic;
			a.IsVirtual = p.IsVirtual;
			
			a.Region = MakeRegion(accessor);
			a.BodyRegion = MakeRegion(accessor.Body);
			// An accessor has no body if all both are true:
			//  a) there's no body in the code
			//  b) the member is either abstract or extern
			a.HasBody = !(accessor.Body.IsNull && (p.IsAbstract || memberIsExtern));
			if (p.EntityType == EntityType.Indexer) {
				foreach (var indexerParam in ((IUnresolvedProperty)p).Parameters)
					a.Parameters.Add(indexerParam);
			}
			DefaultUnresolvedParameter param = null;
			if (accessor.Role == PropertyDeclaration.GetterRole) {
				a.ReturnType = p.ReturnType;
			} else {
				param = new DefaultUnresolvedParameter(p.ReturnType, "value");
				a.Parameters.Add(param);
				a.ReturnType = KnownTypeReference.Void;
			}
			foreach (AttributeSection section in accessor.Attributes) {
				if (section.AttributeTarget == "return") {
					ConvertAttributes(a.ReturnTypeAttributes, section);
				} else if (param != null && section.AttributeTarget == "param") {
					ConvertAttributes(param.Attributes, section);
				} else {
					ConvertAttributes(a.Attributes, section);
				}
			}
			if (p.IsExplicitInterfaceImplementation) {
				a.IsExplicitInterfaceImplementation = true;
				Debug.Assert(p.ExplicitInterfaceImplementations.Count == 1);
				a.ExplicitInterfaceImplementations.Add(new DefaultMemberReference(
					EntityType.Accessor,
					p.ExplicitInterfaceImplementations[0].DeclaringTypeReference,
					a.Name, 0, GetParameterTypes(a.Parameters)
				));
			}
			return a;
		}
		#endregion
		
		#region Events
		public override IUnresolvedEntity VisitEventDeclaration(EventDeclaration eventDeclaration)
		{
			bool isSingleEvent = eventDeclaration.Variables.Count == 1;
			Modifiers modifiers = eventDeclaration.Modifiers;
			DefaultUnresolvedEvent ev = null;
			foreach (VariableInitializer vi in eventDeclaration.Variables) {
				ev = new DefaultUnresolvedEvent(currentTypeDefinition, vi.Name);
				
				ev.Region = isSingleEvent ? MakeRegion(eventDeclaration) : MakeRegion(vi);
				ev.BodyRegion = MakeRegion(vi);
				
				ApplyModifiers(ev, modifiers);
				AddXmlDocumentation(ev, eventDeclaration);
				
				ev.ReturnType = eventDeclaration.ReturnType.ToTypeReference();
				
				var valueParameter = new DefaultUnresolvedParameter(ev.ReturnType, "value");
				ev.AddAccessor = CreateDefaultEventAccessor(ev, "add_" + ev.Name, valueParameter);
				ev.RemoveAccessor = CreateDefaultEventAccessor(ev, "remove_" + ev.Name, valueParameter);
				
				foreach (AttributeSection section in eventDeclaration.Attributes) {
					if (section.AttributeTarget == "method") {
						foreach (var attrNode in section.Attributes) {
							IUnresolvedAttribute attr = ConvertAttribute(attrNode);
							ev.AddAccessor.Attributes.Add(attr);
							ev.RemoveAccessor.Attributes.Add(attr);
						}
					} else if (section.AttributeTarget != "field") {
						ConvertAttributes(ev.Attributes, section);
					}
				}
				
				currentTypeDefinition.Members.Add(ev);
				if (interningProvider != null) {
					ev.ApplyInterningProvider(interningProvider);
				}
			}
			return isSingleEvent ? ev : null;
		}
		
		DefaultUnresolvedMethod CreateDefaultEventAccessor(IUnresolvedEvent ev, string name, IUnresolvedParameter valueParameter)
		{
			var a = new DefaultUnresolvedMethod(currentTypeDefinition, name);
			a.EntityType = EntityType.Accessor;
			a.AccessorOwner = ev;
			a.Region = ev.BodyRegion;
			a.BodyRegion = ev.BodyRegion;
			a.Accessibility = ev.Accessibility;
			a.IsAbstract = ev.IsAbstract;
			a.IsOverride = ev.IsOverride;
			a.IsSealed = ev.IsSealed;
			a.IsStatic = ev.IsStatic;
			a.IsSynthetic = ev.IsSynthetic;
			a.IsVirtual = ev.IsVirtual;
			a.HasBody = true;
			a.ReturnType = KnownTypeReference.Void;
			a.Parameters.Add(valueParameter);
			return a;
		}
		
		public override IUnresolvedEntity VisitCustomEventDeclaration(CustomEventDeclaration eventDeclaration)
		{
			DefaultUnresolvedEvent e = new DefaultUnresolvedEvent(currentTypeDefinition, eventDeclaration.Name);
			e.Region = MakeRegion(eventDeclaration);
			e.BodyRegion = MakeBraceRegion(eventDeclaration);
			ApplyModifiers(e, eventDeclaration.Modifiers);
			e.ReturnType = eventDeclaration.ReturnType.ToTypeReference();
			ConvertAttributes(e.Attributes, eventDeclaration.Attributes);
			AddXmlDocumentation(e, eventDeclaration);
			
			if (!eventDeclaration.PrivateImplementationType.IsNull) {
				e.Accessibility = Accessibility.None;
				e.IsExplicitInterfaceImplementation = true;
				e.ExplicitInterfaceImplementations.Add(new DefaultMemberReference(
					e.EntityType, eventDeclaration.PrivateImplementationType.ToTypeReference(), e.Name));
			}
			
			// custom events can't be extern; the non-custom event syntax must be used for extern events
			e.AddAccessor = ConvertAccessor(eventDeclaration.AddAccessor, e, "add_", false);
			e.RemoveAccessor = ConvertAccessor(eventDeclaration.RemoveAccessor, e, "remove_", false);
			
			currentTypeDefinition.Members.Add(e);
			if (interningProvider != null) {
				e.ApplyInterningProvider(interningProvider);
			}
			return e;
		}
		#endregion
		
		#region Modifiers
		static void ApplyModifiers(DefaultUnresolvedTypeDefinition td, Modifiers modifiers)
		{
			td.Accessibility = GetAccessibility(modifiers) ?? (td.DeclaringTypeDefinition != null ? Accessibility.Private : Accessibility.Internal);
			td.IsAbstract = (modifiers & (Modifiers.Abstract | Modifiers.Static)) != 0;
			td.IsSealed = (modifiers & (Modifiers.Sealed | Modifiers.Static)) != 0;
			td.IsShadowing = (modifiers & Modifiers.New) != 0;
		}
		
		static void ApplyModifiers(AbstractUnresolvedMember m, Modifiers modifiers)
		{
			// members from interfaces are always Public+Abstract.
			if (m.DeclaringTypeDefinition.Kind == TypeKind.Interface) {
				m.Accessibility = Accessibility.Public;
				m.IsAbstract = true;
				return;
			}
			m.Accessibility = GetAccessibility(modifiers) ?? Accessibility.Private;
			m.IsAbstract = (modifiers & Modifiers.Abstract) != 0;
			m.IsOverride = (modifiers & Modifiers.Override) != 0;
			m.IsSealed = (modifiers & Modifiers.Sealed) != 0;
			m.IsShadowing = (modifiers & Modifiers.New) != 0;
			m.IsStatic = (modifiers & Modifiers.Static) != 0;
			m.IsVirtual = (modifiers & Modifiers.Virtual) != 0;
		}
		
		static Accessibility? GetAccessibility(Modifiers modifiers)
		{
			switch (modifiers & Modifiers.VisibilityMask) {
				case Modifiers.Private:
					return Accessibility.Private;
				case Modifiers.Internal:
					return Accessibility.Internal;
				case Modifiers.Protected | Modifiers.Internal:
					return Accessibility.ProtectedOrInternal;
				case Modifiers.Protected:
					return Accessibility.Protected;
				case Modifiers.Public:
					return Accessibility.Public;
				default:
					return null;
			}
		}
		#endregion
		
		#region Attributes
		public override IUnresolvedEntity VisitAttributeSection(AttributeSection attributeSection)
		{
			// non-assembly attributes are handled by their parent entity
			if (attributeSection.AttributeTarget == "assembly") {
				ConvertAttributes(unresolvedFile.AssemblyAttributes, attributeSection);
			} else if (attributeSection.AttributeTarget == "module") {
				ConvertAttributes(unresolvedFile.ModuleAttributes, attributeSection);
			}
			return null;
		}
		
		void ConvertAttributes(IList<IUnresolvedAttribute> outputList, IEnumerable<AttributeSection> attributes)
		{
			foreach (AttributeSection section in attributes) {
				ConvertAttributes(outputList, section);
			}
		}
		
		void ConvertAttributes(IList<IUnresolvedAttribute> outputList, AttributeSection attributeSection)
		{
			foreach (CSharp.Attribute attr in attributeSection.Attributes) {
				outputList.Add(ConvertAttribute(attr));
			}
		}
		
		internal static ITypeReference ConvertAttributeType(AstType type)
		{
			ITypeReference tr = type.ToTypeReference();
			if (!type.GetChildByRole(Roles.Identifier).IsVerbatim) {
				// Try to add "Attribute" suffix, but only if the identifier
				// (=last identifier in fully qualified name) isn't a verbatim identifier.
				SimpleTypeOrNamespaceReference st = tr as SimpleTypeOrNamespaceReference;
				MemberTypeOrNamespaceReference mt = tr as MemberTypeOrNamespaceReference;
				if (st != null)
					return new AttributeTypeReference(st, st.AddSuffix("Attribute"));
				else if (mt != null)
					return new AttributeTypeReference(mt, mt.AddSuffix("Attribute"));
			}
			return tr;
		}
		
		CSharpAttribute ConvertAttribute(CSharp.Attribute attr)
		{
			DomRegion region = MakeRegion(attr);
			ITypeReference type = ConvertAttributeType(attr.Type);
			List<IConstantValue> positionalArguments = null;
			List<KeyValuePair<string, IConstantValue>> namedCtorArguments = null;
			List<KeyValuePair<string, IConstantValue>> namedArguments = null;
			foreach (Expression expr in attr.Arguments) {
				NamedArgumentExpression nae = expr as NamedArgumentExpression;
				if (nae != null) {
					if (namedCtorArguments == null)
						namedCtorArguments = new List<KeyValuePair<string, IConstantValue>>();
					namedCtorArguments.Add(new KeyValuePair<string, IConstantValue>(nae.Name, ConvertAttributeArgument(nae.Expression)));
				} else {
					NamedExpression namedExpression = expr as NamedExpression;
					if (namedExpression != null) {
						string name = namedExpression.Name;
						if (namedArguments == null)
							namedArguments = new List<KeyValuePair<string, IConstantValue>>();
						namedArguments.Add(new KeyValuePair<string, IConstantValue>(name, ConvertAttributeArgument(namedExpression.Expression)));
					} else {
						if (positionalArguments == null)
							positionalArguments = new List<IConstantValue>();
						positionalArguments.Add(ConvertAttributeArgument(expr));
					}
				}
			}
			return new CSharpAttribute(type, region, positionalArguments, namedCtorArguments, namedArguments);
		}
		#endregion
		
		#region Types
		[Obsolete("Use AstType.ToTypeReference() instead.")]
		public static ITypeReference ConvertType(AstType type, NameLookupMode lookupMode = NameLookupMode.Type)
		{
			return type.ToTypeReference(lookupMode);
		}
		#endregion
		
		#region Constant Values
		IConstantValue ConvertConstantValue(ITypeReference targetType, AstNode expression)
		{
			return ConvertConstantValue(targetType, expression, currentTypeDefinition, currentMethod, usingScope);
		}
		
		internal static IConstantValue ConvertConstantValue(
			ITypeReference targetType, AstNode expression,
			IUnresolvedTypeDefinition parentTypeDefinition, IUnresolvedMethod parentMethodDefinition, UsingScope parentUsingScope)
		{
			ConstantValueBuilder b = new ConstantValueBuilder(false);
			ConstantExpression c = expression.AcceptVisitor(b);
			if (c == null)
				return new ErrorConstantValue(targetType);
			PrimitiveConstantExpression pc = c as PrimitiveConstantExpression;
			if (pc != null && pc.Type == targetType) {
				// Save memory by directly using a SimpleConstantValue.
				return new SimpleConstantValue(targetType, pc.Value);
			}
			// cast to the desired type
			return new ConstantCast(targetType, c);
		}
		
		IConstantValue ConvertAttributeArgument(Expression expression)
		{
			ConstantValueBuilder b = new ConstantValueBuilder(true);
			return expression.AcceptVisitor(b);
		}
		
		sealed class ConstantValueBuilder : DepthFirstAstVisitor<ConstantExpression>
		{
			readonly bool isAttributeArgument;
			
			public ConstantValueBuilder(bool isAttributeArgument)
			{
				this.isAttributeArgument = isAttributeArgument;
			}
			
			protected override ConstantExpression VisitChildren(AstNode node)
			{
				return null;
			}
			
			public override ConstantExpression VisitNullReferenceExpression(NullReferenceExpression nullReferenceExpression)
			{
				return new PrimitiveConstantExpression(KnownTypeReference.Object, null);
			}
			
			public override ConstantExpression VisitPrimitiveExpression(PrimitiveExpression primitiveExpression)
			{
				TypeCode typeCode = Type.GetTypeCode(primitiveExpression.Value.GetType());
				return new PrimitiveConstantExpression(typeCode.ToTypeReference(), primitiveExpression.Value);
			}
			
			IList<ITypeReference> ConvertTypeArguments(AstNodeCollection<AstType> types)
			{
				int count = types.Count;
				if (count == 0)
					return null;
				ITypeReference[] result = new ITypeReference[count];
				int pos = 0;
				foreach (AstType type in types) {
					result[pos++] = type.ToTypeReference();
				}
				return result;
			}
			
			public override ConstantExpression VisitIdentifierExpression(IdentifierExpression identifierExpression)
			{
				return new ConstantIdentifierReference(identifierExpression.Identifier, ConvertTypeArguments(identifierExpression.TypeArguments));
			}
			
			public override ConstantExpression VisitMemberReferenceExpression(MemberReferenceExpression memberReferenceExpression)
			{
				TypeReferenceExpression tre = memberReferenceExpression.Target as TypeReferenceExpression;
				if (tre != null) {
					// handle "int.MaxValue"
					return new ConstantMemberReference(
						tre.Type.ToTypeReference(),
						memberReferenceExpression.MemberName,
						ConvertTypeArguments(memberReferenceExpression.TypeArguments));
				}
				ConstantExpression v = memberReferenceExpression.Target.AcceptVisitor(this);
				if (v == null)
					return null;
				return new ConstantMemberReference(
					v, memberReferenceExpression.MemberName,
					ConvertTypeArguments(memberReferenceExpression.TypeArguments));
			}
			
			public override ConstantExpression VisitParenthesizedExpression(ParenthesizedExpression parenthesizedExpression)
			{
				return parenthesizedExpression.Expression.AcceptVisitor(this);
			}
			
			public override ConstantExpression VisitCastExpression(CastExpression castExpression)
			{
				ConstantExpression v = castExpression.Expression.AcceptVisitor(this);
				if (v == null)
					return null;
				return new ConstantCast(castExpression.Type.ToTypeReference(), v);
			}
			
			public override ConstantExpression VisitCheckedExpression(CheckedExpression checkedExpression)
			{
				ConstantExpression v = checkedExpression.Expression.AcceptVisitor(this);
				if (v != null)
					return new ConstantCheckedExpression(true, v);
				else
					return null;
			}
			
			public override ConstantExpression VisitUncheckedExpression(UncheckedExpression uncheckedExpression)
			{
				ConstantExpression v = uncheckedExpression.Expression.AcceptVisitor(this);
				if (v != null)
					return new ConstantCheckedExpression(false, v);
				else
					return null;
			}
			
			public override ConstantExpression VisitDefaultValueExpression(DefaultValueExpression defaultValueExpression)
			{
				return new ConstantDefaultValue(defaultValueExpression.Type.ToTypeReference());
			}
			
			public override ConstantExpression VisitUnaryOperatorExpression(UnaryOperatorExpression unaryOperatorExpression)
			{
				ConstantExpression v = unaryOperatorExpression.Expression.AcceptVisitor(this);
				if (v == null)
					return null;
				switch (unaryOperatorExpression.Operator) {
					case UnaryOperatorType.Not:
					case UnaryOperatorType.BitNot:
					case UnaryOperatorType.Minus:
					case UnaryOperatorType.Plus:
						return new ConstantUnaryOperator(unaryOperatorExpression.Operator, v);
					default:
						return null;
				}
			}
			
			public override ConstantExpression VisitBinaryOperatorExpression(BinaryOperatorExpression binaryOperatorExpression)
			{
				ConstantExpression left = binaryOperatorExpression.Left.AcceptVisitor(this);
				ConstantExpression right = binaryOperatorExpression.Right.AcceptVisitor(this);
				if (left == null || right == null)
					return null;
				return new ConstantBinaryOperator(left, binaryOperatorExpression.Operator, right);
			}
			
			public override ConstantExpression VisitTypeOfExpression(TypeOfExpression typeOfExpression)
			{
				if (isAttributeArgument) {
					return new TypeOfConstantExpression(typeOfExpression.Type.ToTypeReference());
				} else {
					return null;
				}
			}
			
			public override ConstantExpression VisitArrayCreateExpression(ArrayCreateExpression arrayCreateExpression)
			{
				var initializer = arrayCreateExpression.Initializer;
				// Attributes only allow one-dimensional arrays
				if (isAttributeArgument && !initializer.IsNull && arrayCreateExpression.Arguments.Count < 2) {
					ITypeReference type;
					if (arrayCreateExpression.Type.IsNull) {
						type = null;
					} else {
						type = arrayCreateExpression.Type.ToTypeReference();
						foreach (var spec in arrayCreateExpression.AdditionalArraySpecifiers.Reverse()) {
							type = new ArrayTypeReference(type, spec.Dimensions);
						}
					}
					ConstantExpression[] elements = new ConstantExpression[initializer.Elements.Count];
					int pos = 0;
					foreach (Expression expr in initializer.Elements) {
						ConstantExpression c = expr.AcceptVisitor(this);
						if (c == null)
							return null;
						elements[pos++] = c;
					}
					return new ConstantArrayCreation(type, elements);
				} else {
					return null;
				}
			}
		}
		#endregion
		
		#region Parameters
		void ConvertParameters(IList<IUnresolvedParameter> outputList, IEnumerable<ParameterDeclaration> parameters)
		{
			foreach (ParameterDeclaration pd in parameters) {
				DefaultUnresolvedParameter p = new DefaultUnresolvedParameter(pd.Type.ToTypeReference(), pd.Name);
				p.Region = MakeRegion(pd);
				ConvertAttributes(p.Attributes, pd.Attributes);
				switch (pd.ParameterModifier) {
					case ParameterModifier.Ref:
						p.IsRef = true;
						p.Type = new ByReferenceTypeReference(p.Type);
						break;
					case ParameterModifier.Out:
						p.IsOut = true;
						p.Type = new ByReferenceTypeReference(p.Type);
						break;
					case ParameterModifier.Params:
						p.IsParams = true;
						break;
				}
				if (!pd.DefaultExpression.IsNull)
					p.DefaultValue = ConvertConstantValue(p.Type, pd.DefaultExpression);
				outputList.Add(p);
			}
		}
		
		internal static IList<ITypeReference> GetParameterTypes(IEnumerable<ParameterDeclaration> parameters)
		{
			List<ITypeReference> result = new List<ITypeReference>();
			foreach (ParameterDeclaration pd in parameters) {
				ITypeReference type = pd.Type.ToTypeReference();
				if (pd.ParameterModifier == ParameterModifier.Ref || pd.ParameterModifier == ParameterModifier.Out)
					type = new ByReferenceTypeReference(type);
				result.Add(type);
			}
			return result;
		}
		#endregion
		
		#region XML Documentation
		void AddXmlDocumentation(IUnresolvedEntity entity, AstNode entityDeclaration)
		{
			if (this.SkipXmlDocumentation)
				return;
			List<string> documentation = null;
			// traverse AST backwards until the next non-whitespace node
			for (AstNode node = entityDeclaration.PrevSibling; node != null && node.NodeType == NodeType.Whitespace; node = node.PrevSibling) {
				Comment c = node as Comment;
				if (c != null && (c.CommentType == CommentType.Documentation || c.CommentType == CommentType.MultiLineDocumentation)) {
					if (documentation == null)
						documentation = new List<string>();
					if (c.CommentType == CommentType.MultiLineDocumentation) {
						documentation.Add(PrepareMultilineDocumentation(c.Content));
					} else {
						if (c.Content.Length > 0 && c.Content[0] == ' ')
							documentation.Add(c.Content.Substring(1));
						else
							documentation.Add(c.Content);
					}
				}
			}
			if (documentation != null) {
				documentation.Reverse(); // bring documentation in correct order
				unresolvedFile.AddDocumentation(entity, string.Join(Environment.NewLine, documentation));
			}
		}
		
		string PrepareMultilineDocumentation(string content)
		{
			StringBuilder b = new StringBuilder();
			using (var reader = new StringReader(content)) {
				string firstLine = reader.ReadLine();
				// Add first line only if it's not empty:
				if (!string.IsNullOrWhiteSpace(firstLine)) {
					if (firstLine[0] == ' ')
						b.Append(firstLine, 1, firstLine.Length - 1);
					else
						b.Append(firstLine);
				}
				// Read lines into list:
				List<string> lines = new List<string>();
				string line;
				while ((line = reader.ReadLine()) != null)
					lines.Add(line);
				// If the last line (the line with '*/' delimiter) is white space only, ignore it.
				if (lines.Count > 0 && string.IsNullOrWhiteSpace(lines[lines.Count - 1]))
					lines.RemoveAt(lines.Count - 1);
				if (lines.Count > 0) {
					// Extract pattern from lines[0]: whitespace, asterisk, whitespace
					int patternLength = 0;
					string secondLine = lines[0];
					while (patternLength < secondLine.Length && char.IsWhiteSpace(secondLine[patternLength]))
						patternLength++;
					if (patternLength < secondLine.Length && secondLine[patternLength] == '*') {
						patternLength++;
						while (patternLength < secondLine.Length && char.IsWhiteSpace(secondLine[patternLength]))
							patternLength++;
					} else {
						// no asterisk
						patternLength = 0;
					}
					// Now reduce pattern length to the common pattern:
					for (int i = 1; i < lines.Count; i++) {
						line = lines[i];
						if (line.Length < patternLength)
							patternLength = line.Length;
						for (int j = 0; j < patternLength; j++) {
							if (secondLine[j] != line[j])
								patternLength = j;
						}
					}
					// Append the lines to the string builder:
					for (int i = 0; i < lines.Count; i++) {
						if (b.Length > 0 || i > 0)
							b.Append(Environment.NewLine);
						b.Append(lines[i], patternLength, lines[i].Length - patternLength);
					}
				}
			}
			return b.ToString();
		}
		#endregion
	}
}
