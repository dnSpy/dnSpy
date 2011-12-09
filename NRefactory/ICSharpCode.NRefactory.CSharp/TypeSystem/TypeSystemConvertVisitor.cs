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
using System.Linq;
using ICSharpCode.NRefactory.CSharp.Analysis;
using ICSharpCode.NRefactory.CSharp.Resolver;
using ICSharpCode.NRefactory.CSharp.TypeSystem;
using ICSharpCode.NRefactory.CSharp.TypeSystem.ConstantValues;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.TypeSystem.Implementation;
using ICSharpCode.NRefactory.Utils;

namespace ICSharpCode.NRefactory.CSharp.TypeSystem
{
	/// <summary>
	/// Produces type and member definitions from the DOM.
	/// </summary>
	public class TypeSystemConvertVisitor : DepthFirstAstVisitor<object, IUnresolvedEntity>
	{
		readonly CSharpParsedFile parsedFile;
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
		/// Creates a new TypeSystemConvertVisitor.
		/// </summary>
		/// <param name="fileName">The file name (used for DomRegions).</param>
		public TypeSystemConvertVisitor(string fileName)
		{
			if (fileName == null)
				throw new ArgumentNullException("fileName");
			this.parsedFile = new CSharpParsedFile(fileName, new UsingScope());
			this.usingScope = parsedFile.RootUsingScope;
		}
		
		/// <summary>
		/// Creates a new TypeSystemConvertVisitor and initializes it with a given context.
		/// </summary>
		/// <param name="parsedFile">The parsed file to which members should be added.</param>
		/// <param name="currentUsingScope">The current using scope.</param>
		/// <param name="currentTypeDefinition">The current type definition.</param>
		public TypeSystemConvertVisitor(CSharpParsedFile parsedFile, UsingScope currentUsingScope = null, CSharpUnresolvedTypeDefinition currentTypeDefinition = null)
		{
			if (parsedFile == null)
				throw new ArgumentNullException("parsedFile");
			this.parsedFile = parsedFile;
			this.usingScope = currentUsingScope ?? parsedFile.RootUsingScope;
			this.currentTypeDefinition = currentTypeDefinition;
		}
		
		public CSharpParsedFile ParsedFile {
			get { return parsedFile; }
		}
		
		DomRegion MakeRegion(TextLocation start, TextLocation end)
		{
			return new DomRegion(parsedFile.FileName, start.Line, start.Column, end.Line, end.Column);
		}
		
		DomRegion MakeRegion(AstNode node)
		{
			if (node == null || node.IsNull)
				return DomRegion.Empty;
			else
				return MakeRegion(node.StartLocation, node.EndLocation);
		}
		
		DomRegion MakeBraceRegion(AstNode node)
		{
			if (node == null || node.IsNull)
				return DomRegion.Empty;
			else
				return MakeRegion(node.GetChildByRole(AstNode.Roles.LBrace).StartLocation,
				                  node.GetChildByRole(AstNode.Roles.RBrace).EndLocation);
		}
		
		#region Compilation Unit
		public override IUnresolvedEntity VisitCompilationUnit (CompilationUnit unit, object data)
		{
			parsedFile.Errors = unit.Errors;
			return base.VisitCompilationUnit (unit, data);
		}
		#endregion
		
		#region Using Declarations
		public override IUnresolvedEntity VisitExternAliasDeclaration(ExternAliasDeclaration externAliasDeclaration, object data)
		{
			usingScope.ExternAliases.Add(externAliasDeclaration.Name);
			return null;
		}
		
		public override IUnresolvedEntity VisitUsingDeclaration(UsingDeclaration usingDeclaration, object data)
		{
			TypeOrNamespaceReference u = ConvertType(usingDeclaration.Import, SimpleNameLookupMode.TypeInUsingDeclaration) as TypeOrNamespaceReference;
			if (u != null) {
				if (interningProvider != null)
					u = interningProvider.Intern(u);
				usingScope.Usings.Add(u);
			}
			return null;
		}
		
		public override IUnresolvedEntity VisitUsingAliasDeclaration(UsingAliasDeclaration usingDeclaration, object data)
		{
			TypeOrNamespaceReference u = ConvertType(usingDeclaration.Import, SimpleNameLookupMode.TypeInUsingDeclaration) as TypeOrNamespaceReference;
			if (u != null) {
				if (interningProvider != null)
					u = interningProvider.Intern(u);
				usingScope.UsingAliases.Add(new KeyValuePair<string, TypeOrNamespaceReference>(usingDeclaration.Alias, u));
			}
			return null;
		}
		#endregion
		
		#region Namespace Declaration
		public override IUnresolvedEntity VisitNamespaceDeclaration(NamespaceDeclaration namespaceDeclaration, object data)
		{
			DomRegion region = MakeRegion(namespaceDeclaration);
			UsingScope previousUsingScope = usingScope;
			foreach (Identifier ident in namespaceDeclaration.Identifiers) {
				usingScope = new UsingScope(usingScope, ident.Name);
				usingScope.Region = region;
			}
			base.VisitNamespaceDeclaration(namespaceDeclaration, data);
			parsedFile.UsingScopes.Add(usingScope); // add after visiting children so that nested scopes come first
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
				parsedFile.TopLevelTypeDefinitions.Add(newType);
			}
			newType.ParsedFile = parsedFile;
			newType.HasExtensionMethods = false; // gets set to true when an extension method is added
			return newType;
		}
		
		public override IUnresolvedEntity VisitTypeDeclaration(TypeDeclaration typeDeclaration, object data)
		{
			var td = currentTypeDefinition = CreateTypeDefinition(typeDeclaration.Name);
			td.Region = MakeRegion(typeDeclaration);
			td.BodyRegion = MakeBraceRegion(typeDeclaration);
			
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
				td.BaseTypes.Add(ConvertType(baseType, SimpleNameLookupMode.BaseTypeReference));
			}
			
			foreach (AttributedNode member in typeDeclaration.Members) {
				member.AcceptVisitor(this, data);
			}
			
			currentTypeDefinition = (CSharpUnresolvedTypeDefinition)currentTypeDefinition.DeclaringTypeDefinition;
			if (interningProvider != null) {
				td.ApplyInterningProvider(interningProvider);
			}
			return td;
		}
		
		public override IUnresolvedEntity VisitDelegateDeclaration(DelegateDeclaration delegateDeclaration, object data)
		{
			var td = currentTypeDefinition = CreateTypeDefinition(delegateDeclaration.Name);
			td.Kind = TypeKind.Delegate;
			td.Region = MakeRegion(delegateDeclaration);
			td.BaseTypes.Add(KnownTypeReference.MulticastDelegate);
			
			ApplyModifiers(td, delegateDeclaration.Modifiers);
			td.IsSealed = true; // delegates are implicitly sealed
			
			ConvertTypeParameters(td.TypeParameters, delegateDeclaration.TypeParameters, delegateDeclaration.Constraints, EntityType.TypeDefinition);
			
			ITypeReference returnType = ConvertType(delegateDeclaration.ReturnType);
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
		public override IUnresolvedEntity VisitFieldDeclaration(FieldDeclaration fieldDeclaration, object data)
		{
			bool isSingleField = fieldDeclaration.Variables.Count == 1;
			Modifiers modifiers = fieldDeclaration.Modifiers;
			DefaultUnresolvedField field = null;
			foreach (VariableInitializer vi in fieldDeclaration.Variables) {
				field = new DefaultUnresolvedField(currentTypeDefinition, vi.Name);
				
				field.Region = isSingleField ? MakeRegion(fieldDeclaration) : MakeRegion(vi);
				field.BodyRegion = MakeRegion(vi);
				ConvertAttributes(field.Attributes, fieldDeclaration.Attributes);
				
				ApplyModifiers(field, modifiers);
				field.IsVolatile = (modifiers & Modifiers.Volatile) != 0;
				field.IsReadOnly = (modifiers & Modifiers.Readonly) != 0;
				
				field.ReturnType = ConvertType(fieldDeclaration.ReturnType);
				
				if ((modifiers & Modifiers.Const) != 0) {
					field.ConstantValue = ConvertConstantValue(field.ReturnType, vi.Initializer);
				}
				
				currentTypeDefinition.Members.Add(field);
				if (interningProvider != null) {
					field.ApplyInterningProvider(interningProvider);
				}
			}
			return isSingleField ? field : null;
		}
		
		public override IUnresolvedEntity VisitFixedFieldDeclaration(FixedFieldDeclaration fixedFieldDeclaration, object data)
		{
			// TODO: add support for fixed fields
			return base.VisitFixedFieldDeclaration(fixedFieldDeclaration, data);
		}
		
		public override IUnresolvedEntity VisitEnumMemberDeclaration(EnumMemberDeclaration enumMemberDeclaration, object data)
		{
			DefaultUnresolvedField field = new DefaultUnresolvedField(currentTypeDefinition, enumMemberDeclaration.Name);
			field.Region = field.BodyRegion = MakeRegion(enumMemberDeclaration);
			ConvertAttributes(field.Attributes, enumMemberDeclaration.Attributes);
			
			field.ReturnType = currentTypeDefinition;
			field.Accessibility = Accessibility.Public;
			field.IsStatic = true;
			if (!enumMemberDeclaration.Initializer.IsNull) {
				field.ConstantValue = ConvertConstantValue(currentTypeDefinition, enumMemberDeclaration.Initializer);
			} else {
				DefaultUnresolvedField prevField = currentTypeDefinition.Members.LastOrDefault() as DefaultUnresolvedField;
				if (prevField == null || prevField.ConstantValue == null) {
					field.ConstantValue = ConvertConstantValue(currentTypeDefinition, new PrimitiveExpression(0));
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
		public override IUnresolvedEntity VisitMethodDeclaration(MethodDeclaration methodDeclaration, object data)
		{
			DefaultUnresolvedMethod m = new DefaultUnresolvedMethod(currentTypeDefinition, methodDeclaration.Name);
			currentMethod = m; // required for resolving type parameters
			m.Region = MakeRegion(methodDeclaration);
			m.BodyRegion = MakeRegion(methodDeclaration.Body);
			
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
			m.ReturnType = ConvertType(methodDeclaration.ReturnType);
			ConvertAttributes(m.Attributes, methodDeclaration.Attributes.Where(s => s.AttributeTarget != "return"));
			ConvertAttributes(m.ReturnTypeAttributes, methodDeclaration.Attributes.Where(s => s.AttributeTarget == "return"));
			
			ApplyModifiers(m, methodDeclaration.Modifiers);
			if (methodDeclaration.IsExtensionMethod) {
				m.IsExtensionMethod = true;
				currentTypeDefinition.HasExtensionMethods = true;
			}
			
			ConvertParameters(m.Parameters, methodDeclaration.Parameters);
			if (!methodDeclaration.PrivateImplementationType.IsNull) {
				m.Accessibility = Accessibility.None;
				m.InterfaceImplementations.Add(new DefaultMemberReference(
					m.EntityType, ConvertType(methodDeclaration.PrivateImplementationType), m.Name,
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
							tp.Constraints.Add(ConvertType(type));
						}
						break;
					}
				}
			}
		}
		
		IMemberReference ConvertInterfaceImplementation(AstType interfaceType, AbstractUnresolvedMember unresolvedMember)
		{
			ITypeReference interfaceTypeReference = ConvertType(interfaceType);
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
			return new DefaultMemberReference(unresolvedMember.EntityType, ConvertType(interfaceType), unresolvedMember.Name, typeParameterCount, parameterTypes);
		}
		#endregion
		
		#region Operators
		public override IUnresolvedEntity VisitOperatorDeclaration(OperatorDeclaration operatorDeclaration, object data)
		{
			DefaultUnresolvedMethod m = new DefaultUnresolvedMethod(currentTypeDefinition, operatorDeclaration.Name);
			m.EntityType = EntityType.Operator;
			m.Region = MakeRegion(operatorDeclaration);
			m.BodyRegion = MakeRegion(operatorDeclaration.Body);
			
			m.ReturnType = ConvertType(operatorDeclaration.ReturnType);
			ConvertAttributes(m.Attributes, operatorDeclaration.Attributes.Where(s => s.AttributeTarget != "return"));
			ConvertAttributes(m.ReturnTypeAttributes, operatorDeclaration.Attributes.Where(s => s.AttributeTarget == "return"));
			
			ApplyModifiers(m, operatorDeclaration.Modifiers);
			
			ConvertParameters(m.Parameters, operatorDeclaration.Parameters);
			
			currentTypeDefinition.Members.Add(m);
			if (interningProvider != null) {
				m.ApplyInterningProvider(interningProvider);
			}
			return m;
		}
		#endregion
		
		#region Constructors
		public override IUnresolvedEntity VisitConstructorDeclaration(ConstructorDeclaration constructorDeclaration, object data)
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
		public override IUnresolvedEntity VisitDestructorDeclaration(DestructorDeclaration destructorDeclaration, object data)
		{
			DefaultUnresolvedMethod dtor = new DefaultUnresolvedMethod(currentTypeDefinition, "Finalize");
			dtor.EntityType = EntityType.Destructor;
			dtor.Region = MakeRegion(destructorDeclaration);
			dtor.BodyRegion = MakeRegion(destructorDeclaration.Body);
			dtor.Accessibility = Accessibility.Protected;
			dtor.IsOverride = true;
			dtor.ReturnType = KnownTypeReference.Void;
			
			ConvertAttributes(dtor.Attributes, destructorDeclaration.Attributes);
			
			currentTypeDefinition.Members.Add(dtor);
			if (interningProvider != null) {
				dtor.ApplyInterningProvider(interningProvider);
			}
			return dtor;
		}
		#endregion
		
		#region Properties / Indexers
		public override IUnresolvedEntity VisitPropertyDeclaration(PropertyDeclaration propertyDeclaration, object data)
		{
			DefaultUnresolvedProperty p = new DefaultUnresolvedProperty(currentTypeDefinition, propertyDeclaration.Name);
			p.Region = MakeRegion(propertyDeclaration);
			p.BodyRegion = MakeBraceRegion(propertyDeclaration);
			ApplyModifiers(p, propertyDeclaration.Modifiers);
			p.ReturnType = ConvertType(propertyDeclaration.ReturnType);
			ConvertAttributes(p.Attributes, propertyDeclaration.Attributes);
			if (!propertyDeclaration.PrivateImplementationType.IsNull) {
				p.Accessibility = Accessibility.None;
				p.InterfaceImplementations.Add(new DefaultMemberReference(
					p.EntityType, ConvertType(propertyDeclaration.PrivateImplementationType), p.Name));
			}
			p.Getter = ConvertAccessor(propertyDeclaration.Getter, p.Accessibility);
			p.Setter = ConvertAccessor(propertyDeclaration.Setter, p.Accessibility);
			currentTypeDefinition.Members.Add(p);
			if (interningProvider != null) {
				p.ApplyInterningProvider(interningProvider);
			}
			return p;
		}
		
		public override IUnresolvedEntity VisitIndexerDeclaration(IndexerDeclaration indexerDeclaration, object data)
		{
			DefaultUnresolvedProperty p = new DefaultUnresolvedProperty(currentTypeDefinition, "Item");
			p.EntityType = EntityType.Indexer;
			p.Region = MakeRegion(indexerDeclaration);
			p.BodyRegion = MakeBraceRegion(indexerDeclaration);
			ApplyModifiers(p, indexerDeclaration.Modifiers);
			p.ReturnType = ConvertType(indexerDeclaration.ReturnType);
			ConvertAttributes(p.Attributes, indexerDeclaration.Attributes);
			
			p.Getter = ConvertAccessor(indexerDeclaration.Getter, p.Accessibility);
			p.Setter = ConvertAccessor(indexerDeclaration.Setter, p.Accessibility);
			ConvertParameters(p.Parameters, indexerDeclaration.Parameters);
			
			if (!indexerDeclaration.PrivateImplementationType.IsNull) {
				p.Accessibility = Accessibility.None;
				p.InterfaceImplementations.Add(new DefaultMemberReference(
					p.EntityType, ConvertType(indexerDeclaration.PrivateImplementationType), p.Name, 0, GetParameterTypes(p.Parameters)));
			}
			
			currentTypeDefinition.Members.Add(p);
			if (interningProvider != null) {
				p.ApplyInterningProvider(interningProvider);
			}
			return p;
		}
		
		IUnresolvedAccessor ConvertAccessor(Accessor accessor, Accessibility defaultAccessibility)
		{
			if (accessor.IsNull)
				return null;
			DefaultUnresolvedAccessor a = new DefaultUnresolvedAccessor();
			a.Accessibility = GetAccessibility(accessor.Modifiers) ?? defaultAccessibility;
			a.Region = MakeRegion(accessor);
			foreach (AttributeSection section in accessor.Attributes) {
				if (section.AttributeTarget == "return") {
					ConvertAttributes(a.ReturnTypeAttributes, section);
				} else if (section.AttributeTarget != "param") {
					ConvertAttributes(a.Attributes, section);
				}
			}
			return a;
		}
		#endregion
		
		#region Events
		public override IUnresolvedEntity VisitEventDeclaration(EventDeclaration eventDeclaration, object data)
		{
			bool isSingleEvent = eventDeclaration.Variables.Count == 1;
			Modifiers modifiers = eventDeclaration.Modifiers;
			DefaultUnresolvedEvent ev = null;
			foreach (VariableInitializer vi in eventDeclaration.Variables) {
				ev = new DefaultUnresolvedEvent(currentTypeDefinition, vi.Name);
				
				ev.Region = isSingleEvent ? MakeRegion(eventDeclaration) : MakeRegion(vi);
				ev.BodyRegion = MakeRegion(vi);
				
				ApplyModifiers(ev, modifiers);
				
				ev.ReturnType = ConvertType(eventDeclaration.ReturnType);
				
				if (eventDeclaration.Attributes.Any(a => a.AttributeTarget == "method")) {
					ev.AddAccessor = ev.RemoveAccessor = new DefaultUnresolvedAccessor { Accessibility = ev.Accessibility };
				} else {
					// if there's no attributes on the accessors, we can re-use the shared accessor instance
					ev.AddAccessor = ev.RemoveAccessor = DefaultUnresolvedAccessor.GetFromAccessibility(ev.Accessibility);
				}
				foreach (AttributeSection section in eventDeclaration.Attributes) {
					if (section.AttributeTarget == "method") {
						// as we use the same instance for AddAccessor and RemoveAccessor, we only need to add the attribute once
						ConvertAttributes(ev.AddAccessor.Attributes, section);
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
		
		public override IUnresolvedEntity VisitCustomEventDeclaration(CustomEventDeclaration eventDeclaration, object data)
		{
			DefaultUnresolvedEvent e = new DefaultUnresolvedEvent(currentTypeDefinition, eventDeclaration.Name);
			e.Region = MakeRegion(eventDeclaration);
			e.BodyRegion = MakeBraceRegion(eventDeclaration);
			ApplyModifiers(e, eventDeclaration.Modifiers);
			e.ReturnType = ConvertType(eventDeclaration.ReturnType);
			ConvertAttributes(e.Attributes, eventDeclaration.Attributes);
			
			if (!eventDeclaration.PrivateImplementationType.IsNull) {
				e.Accessibility = Accessibility.None;
				e.InterfaceImplementations.Add(new DefaultMemberReference(
					e.EntityType, ConvertType(eventDeclaration.PrivateImplementationType), e.Name));
			}
			
			e.AddAccessor = ConvertAccessor(eventDeclaration.AddAccessor, e.Accessibility);
			e.RemoveAccessor = ConvertAccessor(eventDeclaration.RemoveAccessor, e.Accessibility);
			
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
			//m.IsPartial = (modifiers & Modifiers.Partial) != 0;
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
		public override IUnresolvedEntity VisitAttributeSection(AttributeSection attributeSection, object data)
		{
			// non-assembly attributes are handled by their parent entity
			if (attributeSection.AttributeTarget == "assembly") {
				ConvertAttributes(parsedFile.AssemblyAttributes, attributeSection);
			} else if (attributeSection.AttributeTarget == "module") {
				ConvertAttributes(parsedFile.ModuleAttributes, attributeSection);
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
			ITypeReference tr = ConvertType(type, SimpleNameLookupMode.Type);
			if (!type.GetChildByRole(AstNode.Roles.Identifier).IsVerbatim) {
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
					namedCtorArguments.Add(new KeyValuePair<string, IConstantValue>(nae.Identifier, ConvertAttributeArgument(nae.Expression)));
				} else {
					NamedExpression namedExpression = expr as NamedExpression;
					if (namedExpression != null) {
						string name = namedExpression.Identifier;
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
		public static ITypeReference ConvertType(AstType type, SimpleNameLookupMode lookupMode = SimpleNameLookupMode.Type)
		{
			SimpleType s = type as SimpleType;
			if (s != null) {
				List<ITypeReference> typeArguments = new List<ITypeReference>();
				foreach (var ta in s.TypeArguments) {
					typeArguments.Add(ConvertType(ta, lookupMode));
				}
				if (typeArguments.Count == 0 && string.IsNullOrEmpty(s.Identifier)) {
					// empty SimpleType is used for typeof(List<>).
					return SpecialType.UnboundTypeArgument;
				}
				return new SimpleTypeOrNamespaceReference(s.Identifier, typeArguments, lookupMode);
			}
			
			PrimitiveType p = type as PrimitiveType;
			if (p != null) {
				return ConvertPrimitiveType(p.Keyword);
			}
			MemberType m = type as MemberType;
			if (m != null) {
				TypeOrNamespaceReference t;
				if (m.IsDoubleColon) {
					SimpleType st = m.Target as SimpleType;
					if (st != null) {
						t = new AliasNamespaceReference(st.Identifier);
					} else {
						t = null;
					}
				} else {
					t = ConvertType(m.Target, lookupMode) as TypeOrNamespaceReference;
				}
				if (t == null)
					return SpecialType.UnknownType;
				List<ITypeReference> typeArguments = new List<ITypeReference>();
				foreach (var ta in m.TypeArguments) {
					typeArguments.Add(ConvertType(ta, lookupMode));
				}
				return new MemberTypeOrNamespaceReference(t, m.MemberName, typeArguments);
			}
			ComposedType c = type as ComposedType;
			if (c != null) {
				ITypeReference t = ConvertType(c.BaseType, lookupMode);
				if (c.HasNullableSpecifier) {
					t = NullableType.Create(t);
				}
				for (int i = 0; i < c.PointerRank; i++) {
					t = new PointerTypeReference(t);
				}
				foreach (var a in c.ArraySpecifiers.Reverse()) {
					t = new ArrayTypeReference(t, a.Dimensions);
				}
				return t;
			}
			if (!type.IsNull)
				Debug.WriteLine("Unknown node used as type: " + type);
			return SpecialType.UnknownType;
		}
		
		public static ITypeReference ConvertPrimitiveType(string keyword)
		{
			KnownTypeCode typeCode = GetTypeCodeForPrimitiveType(keyword);
			if (typeCode == KnownTypeCode.None)
				return new UnknownType(null, keyword);
			else
				return KnownTypeReference.Get(typeCode);
		}
		
		public static KnownTypeCode GetTypeCodeForPrimitiveType(string keyword)
		{
			switch (keyword) {
				case "string":
					return KnownTypeCode.String;
				case "int":
					return KnownTypeCode.Int32;
				case "uint":
					return KnownTypeCode.UInt32;
				case "object":
					return KnownTypeCode.Object;
				case "bool":
					return KnownTypeCode.Boolean;
				case "sbyte":
					return KnownTypeCode.SByte;
				case "byte":
					return KnownTypeCode.Byte;
				case "short":
					return KnownTypeCode.Int16;
				case "ushort":
					return KnownTypeCode.UInt16;
				case "long":
					return KnownTypeCode.Int64;
				case "ulong":
					return KnownTypeCode.UInt64;
				case "float":
					return KnownTypeCode.Single;
				case "double":
					return KnownTypeCode.Double;
				case "decimal":
					return KnownTypeCode.Decimal;
				case "char":
					return KnownTypeCode.Char;
				case "void":
					return KnownTypeCode.Void;
				default:
					return KnownTypeCode.None;
			}
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
			ConstantExpression c = expression.AcceptVisitor(b, null);
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
			return expression.AcceptVisitor(b, null);
		}
		
		sealed class ConstantValueBuilder : DepthFirstAstVisitor<object, ConstantExpression>
		{
			readonly bool isAttributeArgument;
			
			public ConstantValueBuilder(bool isAttributeArgument)
			{
				this.isAttributeArgument = isAttributeArgument;
			}
			
			protected override ConstantExpression VisitChildren(AstNode node, object data)
			{
				return null;
			}
			
			public override ConstantExpression VisitNullReferenceExpression(NullReferenceExpression nullReferenceExpression, object data)
			{
				return new PrimitiveConstantExpression(KnownTypeReference.Object, null);
			}
			
			public override ConstantExpression VisitPrimitiveExpression(PrimitiveExpression primitiveExpression, object data)
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
					result[pos++] = ConvertType(type);
				}
				return result;
			}
			
			public override ConstantExpression VisitIdentifierExpression(IdentifierExpression identifierExpression, object data)
			{
				return new ConstantIdentifierReference(identifierExpression.Identifier, ConvertTypeArguments(identifierExpression.TypeArguments));
			}
			
			public override ConstantExpression VisitMemberReferenceExpression(MemberReferenceExpression memberReferenceExpression, object data)
			{
				TypeReferenceExpression tre = memberReferenceExpression.Target as TypeReferenceExpression;
				if (tre != null) {
					// handle "int.MaxValue"
					return new ConstantMemberReference(
						ConvertType(tre.Type),
						memberReferenceExpression.MemberName,
						ConvertTypeArguments(memberReferenceExpression.TypeArguments));
				}
				ConstantExpression v = memberReferenceExpression.Target.AcceptVisitor(this, data);
				if (v == null)
					return null;
				return new ConstantMemberReference(
					v, memberReferenceExpression.MemberName,
					ConvertTypeArguments(memberReferenceExpression.TypeArguments));
			}
			
			public override ConstantExpression VisitParenthesizedExpression(ParenthesizedExpression parenthesizedExpression, object data)
			{
				return parenthesizedExpression.Expression.AcceptVisitor(this, data);
			}
			
			public override ConstantExpression VisitCastExpression(CastExpression castExpression, object data)
			{
				ConstantExpression v = castExpression.Expression.AcceptVisitor(this, data);
				if (v == null)
					return null;
				return new ConstantCast(ConvertType(castExpression.Type), v);
			}
			
			public override ConstantExpression VisitCheckedExpression(CheckedExpression checkedExpression, object data)
			{
				ConstantExpression v = checkedExpression.Expression.AcceptVisitor(this, data);
				if (v != null)
					return new ConstantCheckedExpression(true, v);
				else
					return null;
			}
			
			public override ConstantExpression VisitUncheckedExpression(UncheckedExpression uncheckedExpression, object data)
			{
				ConstantExpression v = uncheckedExpression.Expression.AcceptVisitor(this, data);
				if (v != null)
					return new ConstantCheckedExpression(false, v);
				else
					return null;
			}
			
			public override ConstantExpression VisitDefaultValueExpression(DefaultValueExpression defaultValueExpression, object data)
			{
				return new ConstantDefaultValue(ConvertType(defaultValueExpression.Type));
			}
			
			public override ConstantExpression VisitUnaryOperatorExpression(UnaryOperatorExpression unaryOperatorExpression, object data)
			{
				ConstantExpression v = unaryOperatorExpression.Expression.AcceptVisitor(this, data);
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
			
			public override ConstantExpression VisitBinaryOperatorExpression(BinaryOperatorExpression binaryOperatorExpression, object data)
			{
				ConstantExpression left = binaryOperatorExpression.Left.AcceptVisitor(this, data);
				ConstantExpression right = binaryOperatorExpression.Right.AcceptVisitor(this, data);
				if (left == null || right == null)
					return null;
				return new ConstantBinaryOperator(left, binaryOperatorExpression.Operator, right);
			}
			
			public override ConstantExpression VisitTypeOfExpression(TypeOfExpression typeOfExpression, object data)
			{
				if (isAttributeArgument) {
					return new TypeOfConstantExpression(ConvertType(typeOfExpression.Type));
				} else {
					return null;
				}
			}
			
			public override ConstantExpression VisitArrayCreateExpression(ArrayCreateExpression arrayCreateExpression, object data)
			{
				var initializer = arrayCreateExpression.Initializer;
				// Attributes only allow one-dimensional arrays
				if (isAttributeArgument && !initializer.IsNull && arrayCreateExpression.Arguments.Count < 2) {
					ITypeReference type;
					if (arrayCreateExpression.Type.IsNull) {
						type = null;
					} else {
						type = ConvertType(arrayCreateExpression.Type);
						foreach (var spec in arrayCreateExpression.AdditionalArraySpecifiers.Reverse()) {
							type = new ArrayTypeReference(type, spec.Dimensions);
						}
					}
					ConstantExpression[] elements = new ConstantExpression[initializer.Elements.Count];
					int pos = 0;
					foreach (Expression expr in initializer.Elements) {
						ConstantExpression c = expr.AcceptVisitor(this, data);
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
				DefaultUnresolvedParameter p = new DefaultUnresolvedParameter(ConvertType(pd.Type), pd.Name);
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
		#endregion
	}
}
