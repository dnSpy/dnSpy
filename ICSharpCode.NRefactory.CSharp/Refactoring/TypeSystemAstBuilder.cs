// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT license (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using ICSharpCode.NRefactory.CSharp.Resolver;
using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.TypeSystem.Implementation;
using ICSharpCode.NRefactory.Utils;

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	/// <summary>
	/// Converts from type system to the C# AST.
	/// </summary>
	public class TypeSystemAstBuilder
	{
		readonly CSharpResolver resolver;
		readonly ITypeResolveContext context;
		
		#region Constructor
		/// <summary>
		/// Creates a new TypeSystemAstBuilder.
		/// </summary>
		/// <param name="resolver">
		/// A resolver initialized for the position where the type will be inserted.
		/// </param>
		public TypeSystemAstBuilder(CSharpResolver resolver)
		{
			if (resolver == null)
				throw new ArgumentNullException("resolver");
			this.resolver = resolver;
			this.context = resolver.Context;
			InitProperties();
		}
		
		/// <summary>
		/// Creates a new TypeSystemAstBuilder.
		/// </summary>
		/// <param name="context">
		/// Context used for resolving types.
		/// </param>
		public TypeSystemAstBuilder(ITypeResolveContext context)
		{
			if (context == null)
				throw new ArgumentNullException("context");
			this.context = context;
			InitProperties();
		}
		#endregion
		
		#region Properties
		void InitProperties()
		{
			this.ShowAccessibility = true;
			this.ShowModifiers = true;
			this.ShowBaseTypes = true;
			this.ShowTypeParameters = true;
			this.ShowTypeParameterConstraints = true;
			this.ShowParameterNames = true;
			this.ShowConstantValues = true;
		}
		
		/// <summary>
		/// Controls the accessibility modifiers are shown.
		/// </summary>
		public bool ShowAccessibility { get; set; }
		
		/// <summary>
		/// Controls the non-accessibility modifiers are shown.
		/// </summary>
		public bool ShowModifiers { get; set; }
		
		/// <summary>
		/// Controls whether base type references are shown.
		/// </summary>
		public bool ShowBaseTypes { get; set; }
		
		/// <summary>
		/// Controls whether type parameter declarations are shown.
		/// </summary>
		public bool ShowTypeParameters { get; set; }
		
		/// <summary>
		/// Controls whether contraints on type parameter declarations are shown.
		/// Has no effect if ShowTypeParameters is false.
		/// </summary>
		public bool ShowTypeParameterConstraints { get; set; }
		
		/// <summary>
		/// Controls whether the names of parameters are shown.
		/// </summary>
		public bool ShowParameterNames { get; set; }
		
		/// <summary>
		/// Controls whether to show default values of optional parameters, and the values of constant fields.
		/// </summary>
		public bool ShowConstantValues { get; set; }
		#endregion
		
		#region Convert Type
		public AstType ConvertType(IType type)
		{
			if (type == null)
				throw new ArgumentNullException("type");
			TypeWithElementType typeWithElementType = type as TypeWithElementType;
			if (typeWithElementType != null) {
				if (typeWithElementType is PointerType) {
					return ConvertType(typeWithElementType.ElementType).MakePointerType();
				} else if (typeWithElementType is ArrayType) {
					return ConvertType(typeWithElementType.ElementType).MakeArrayType(((ArrayType)type).Dimensions);
				} else {
					// e.g. ByReferenceType; not supported as type in C#
					return ConvertType(typeWithElementType.ElementType);
				}
			}
			ParameterizedType pt = type as ParameterizedType;
			if (pt != null) {
				if (pt.Name == "Nullable" && pt.Namespace == "System" && pt.TypeParameterCount == 1) {
					return ConvertType(pt.TypeArguments[0]).MakeNullableType();
				}
				return ConvertTypeHelper(pt.GetDefinition(), pt.TypeArguments);
			}
			ITypeDefinition typeDef = type as ITypeDefinition;
			if (typeDef != null) {
				if (typeDef.TypeParameterCount > 0) {
					// Unbound type
					IType[] typeArguments = new IType[typeDef.TypeParameterCount];
					for (int i = 0; i < typeArguments.Length; i++) {
						typeArguments[i] = SharedTypes.UnboundTypeArgument;
					}
					return ConvertTypeHelper(typeDef, typeArguments);
				} else {
					return ConvertTypeHelper(typeDef, EmptyList<IType>.Instance);
				}
			}
			return new SimpleType(type.Name);
		}
		
		public AstType ConvertTypeReference(ITypeReference typeRef)
		{
			ArrayTypeReference array = typeRef as ArrayTypeReference;
			if (array != null) {
				return ConvertTypeReference(array.ElementType).MakeArrayType(array.Dimensions);
			}
			PointerTypeReference pointer = typeRef as PointerTypeReference;
			if (pointer != null) {
				return ConvertTypeReference(pointer.ElementType).MakePointerType();
			}
			ByReferenceType brt = typeRef as ByReferenceType;
			if (brt != null) {
				return ConvertTypeReference(brt.ElementType);
			}
			
			IType type = typeRef.Resolve(context);
			if (type.Kind != TypeKind.Unknown)
				return ConvertType(type);
			// Unknown type, let's try if we can find an appropriate type
			// (anything is better than displaying a question mark)
			KnownTypeReference knownType = typeRef as KnownTypeReference;
			if (knownType != null) {
				string keyword = ReflectionHelper.GetCSharpNameByTypeCode(knownType.TypeCode);
				if (keyword != null)
					return new PrimitiveType(keyword);
			}
			SimpleTypeOrNamespaceReference str = typeRef as SimpleTypeOrNamespaceReference;
			if (str != null) {
				return new SimpleType(str.Identifier, str.TypeArguments.Select(ConvertTypeReference));
			}
			MemberTypeOrNamespaceReference mtr = typeRef as MemberTypeOrNamespaceReference;
			if (mtr != null) {
				return new MemberType(ConvertTypeReference(mtr.Target), mtr.Identifier, mtr.TypeArguments.Select(ConvertTypeReference)) {
					IsDoubleColon = mtr.Target is AliasNamespaceReference
				};
			}
			AliasNamespaceReference alias = typeRef as AliasNamespaceReference;
			if (alias != null) {
				return new SimpleType(alias.Identifier);
			}
			// Unknown type reference that couldn't be resolved
			return new SimpleType("?");
		}
		
		AstType ConvertTypeHelper(ITypeDefinition typeDef, IList<IType> typeArguments)
		{
			Debug.Assert(typeArguments.Count >= typeDef.TypeParameterCount);
			TypeCode typeCode = ReflectionHelper.GetTypeCode(typeDef);
			if (typeCode != TypeCode.Empty) {
				string keyword = ReflectionHelper.GetCSharpNameByTypeCode(typeCode);
				if (keyword != null)
					return new PrimitiveType(keyword);
			}
			// There is no type code for System.Void
			if (typeDef.Kind == TypeKind.Void)
				return new PrimitiveType("void");
			
			// The number of type parameters belonging to outer classes
			int outerTypeParameterCount;
			if (typeDef.DeclaringType != null)
				outerTypeParameterCount = typeDef.DeclaringType.TypeParameterCount;
			else
				outerTypeParameterCount = 0;
			
			if (resolver != null) {
				// Look if there's an alias to the target type
				for (UsingScope usingScope = resolver.CurrentUsingScope; usingScope != null; usingScope = usingScope.Parent) {
					foreach (var pair in usingScope.UsingAliases) {
						IType type = pair.Value.Resolve(resolver.Context);
						if (TypeMatches(type, typeDef, typeArguments))
							return new SimpleType(pair.Key);
					}
				}
				
				IList<IType> localTypeArguments;
				if (typeDef.TypeParameterCount > outerTypeParameterCount) {
					localTypeArguments = new IType[typeDef.TypeParameterCount - outerTypeParameterCount];
					for (int i = 0; i < localTypeArguments.Count; i++) {
						localTypeArguments[i] = typeArguments[outerTypeParameterCount + i];
					}
				} else {
					localTypeArguments = EmptyList<IType>.Instance;
				}
				TypeResolveResult trr = resolver.ResolveSimpleName(typeDef.Name, localTypeArguments) as TypeResolveResult;
				if (trr != null && !trr.IsError && TypeMatches(trr.Type, typeDef, typeArguments)) {
					// We can use the short type name
					SimpleType shortResult = new SimpleType(typeDef.Name);
					AddTypeArguments(shortResult, typeArguments, outerTypeParameterCount, typeDef.TypeParameterCount);
					return shortResult;
				}
			}
			
			MemberType result = new MemberType();
			if (typeDef.DeclaringTypeDefinition != null) {
				// Handle nested types
				result.Target = ConvertTypeHelper(typeDef.DeclaringTypeDefinition, typeArguments);
			} else {
				// Handle top-level types
				if (string.IsNullOrEmpty(typeDef.Namespace)) {
					result.Target = new SimpleType("global");
					result.IsDoubleColon = true;
				} else {
					result.Target = ConvertNamespace(typeDef.Namespace);
				}
			}
			result.MemberName = typeDef.Name;
			AddTypeArguments(result, typeArguments, outerTypeParameterCount, typeDef.TypeParameterCount);
			return result;
		}
		
		/// <summary>
		/// Gets whether 'type' is the same as 'typeDef' parameterized with the given type arguments.
		/// </summary>
		bool TypeMatches(IType type, ITypeDefinition typeDef, IList<IType> typeArguments)
		{
			if (typeDef.TypeParameterCount == 0) {
				return typeDef.Equals(type);
			} else {
				if (!typeDef.Equals(type.GetDefinition()))
					return false;
				ParameterizedType pt = type as ParameterizedType;
				if (pt == null) {
					return typeArguments.All(t => t.Kind == TypeKind.UnboundTypeArgument);
				}
				var ta = pt.TypeArguments;
				for (int i = 0; i < ta.Count; i++) {
					if (!ta[i].Equals(typeArguments[i]))
						return false;
				}
				return true;
			}
		}
		
		/// <summary>
		/// Adds type arguments to the result type.
		/// </summary>
		/// <param name="result">The result AST node (a SimpleType or MemberType)</param>
		/// <param name="typeArguments">The list of type arguments</param>
		/// <param name="startIndex">Index of first type argument to add</param>
		/// <param name="endIndex">Index after last type argument to add</param>
		void AddTypeArguments(AstType result, IList<IType> typeArguments, int startIndex, int endIndex)
		{
			for (int i = startIndex; i < endIndex; i++) {
				result.AddChild(ConvertType(typeArguments[i]), AstType.Roles.TypeArgument);
			}
		}
		
		AstType ConvertNamespace(string ns)
		{
			if (resolver != null) {
				// Look if there's an alias to the target namespace
				for (UsingScope usingScope = resolver.CurrentUsingScope; usingScope != null; usingScope = usingScope.Parent) {
					foreach (var pair in usingScope.UsingAliases) {
						// maybe add some caching? we're resolving all aliases N times when converting a namespace name with N parts
						NamespaceResolveResult nrr = pair.Value.ResolveNamespace(resolver.Context);
						if (nrr != null && nrr.NamespaceName == ns)
							return new SimpleType(pair.Key);
					}
				}
			}
			
			int pos = ns.LastIndexOf('.');
			if (pos < 0) {
				if (IsValidNamespace(ns)) {
					return new SimpleType(ns);
				} else {
					return new MemberType {
						Target = new SimpleType("global"),
						IsDoubleColon = true,
						MemberName = ns
					};
				}
			} else {
				string parentNamespace = ns.Substring(0, pos);
				string localNamespace = ns.Substring(pos + 1);
				return new MemberType {
					Target = ConvertNamespace(parentNamespace),
					MemberName = localNamespace
				};
			}
		}
		
		bool IsValidNamespace(string firstNamespacePart)
		{
			if (resolver == null)
				return true; // just assume namespaces are valid if we don't have a resolver
			NamespaceResolveResult nrr = resolver.ResolveSimpleName(firstNamespacePart, EmptyList<IType>.Instance) as NamespaceResolveResult;
			return nrr != null && !nrr.IsError && nrr.NamespaceName == firstNamespacePart;
		}
		#endregion
		
		#region Convert Constant Value
		public Expression ConvertConstantValue(IConstantValue constantValue)
		{
			if (constantValue == null)
				throw new ArgumentNullException("constantValue");
			return ConvertConstantValue(constantValue.Resolve(context));
		}
		
		Expression ConvertConstantValue(ResolveResult rr)
		{
			if (rr is TypeOfResolveResult) {
				return new TypeOfExpression(ConvertType(((TypeOfResolveResult)rr).Type));
			} else if (rr is ArrayCreateResolveResult) {
				ArrayCreateResolveResult acrr = (ArrayCreateResolveResult)rr;
				AstType type = ConvertType(acrr.Type);
				throw new NotImplementedException();
			} else if (rr.IsCompileTimeConstant) {
				object val = rr.ConstantValue;
				if (val == null) {
					if (rr.Type.IsReferenceType(context) == true)
						return new NullReferenceExpression();
					else
						return new DefaultValueExpression(ConvertType(rr.Type));
				} else if (rr.Type.Kind == TypeKind.Enum) {
					throw new NotImplementedException();
				} else {
					return new PrimitiveExpression(val);
				}
			} else {
				return new EmptyExpression();
			}
		}
		#endregion
		
		#region Convert Parameter
		public ParameterDeclaration ConvertParameter(IParameter parameter)
		{
			if (parameter == null)
				throw new ArgumentNullException("parameter");
			ParameterDeclaration decl = new ParameterDeclaration();
			if (parameter.IsRef) {
				decl.ParameterModifier = ParameterModifier.Ref;
			} else if (parameter.IsOut) {
				decl.ParameterModifier = ParameterModifier.Out;
			} else if (parameter.IsParams) {
				decl.ParameterModifier = ParameterModifier.Params;
			}
			decl.Type = ConvertTypeReference(parameter.Type);
			if (this.ShowParameterNames) {
				decl.Name = parameter.Name;
			}
			if (parameter.IsOptional && this.ShowConstantValues) {
				decl.DefaultExpression = ConvertConstantValue(parameter.DefaultValue);
			}
			return decl;
		}
		#endregion
		
		#region Convert Entity
		public AstNode ConvertEntity(IEntity entity)
		{
			if (entity == null)
				throw new ArgumentNullException("entity");
			switch (entity.EntityType) {
				case EntityType.TypeDefinition:
					return ConvertTypeDefinition((ITypeDefinition)entity);
				case EntityType.Field:
					return ConvertField((IField)entity);
				case EntityType.Property:
					return ConvertProperty((IProperty)entity);
				case EntityType.Indexer:
					return ConvertIndexer((IProperty)entity);
				case EntityType.Event:
					return ConvertEvent((IEvent)entity);
				case EntityType.Method:
					return ConvertMethod((IMethod)entity);
				case EntityType.Operator:
					return ConvertOperator((IMethod)entity);
				case EntityType.Constructor:
					return ConvertConstructor((IMethod)entity);
				case EntityType.Destructor:
					return ConvertDestructor((IMethod)entity);
				default:
					throw new ArgumentException("Invalid value for EntityType: " + entity.EntityType);
			}
		}
		
		AttributedNode ConvertTypeDefinition(ITypeDefinition typeDefinition)
		{
			Modifiers modifiers = ModifierFromAccessibility(typeDefinition.Accessibility);
			if (this.ShowModifiers) {
				if (typeDefinition.IsStatic) {
					modifiers |= Modifiers.Static;
				} else if (typeDefinition.IsAbstract) {
					modifiers |= Modifiers.Abstract;
				} else if (typeDefinition.IsSealed) {
					modifiers |= Modifiers.Sealed;
				}
				if (typeDefinition.IsShadowing) {
					modifiers |= Modifiers.New;
				}
			}
			
			ClassType classType;
			switch (typeDefinition.Kind) {
				case TypeKind.Struct:
					classType = ClassType.Struct;
					break;
				case TypeKind.Enum:
					classType = ClassType.Enum;
					break;
				case TypeKind.Interface:
					classType = ClassType.Interface;
					break;
				case TypeKind.Delegate:
					IMethod invoke = typeDefinition.GetDelegateInvokeMethod();
					if (invoke != null)
						return ConvertDelegate(invoke, modifiers);
					else
						goto default;
				default:
					classType = ClassType.Class;
					break;
			}
			
			TypeDeclaration decl = new TypeDeclaration();
			decl.Modifiers = modifiers;
			decl.ClassType = classType;
			decl.Name = typeDefinition.Name;
			
			if (this.ShowTypeParameters) {
				foreach (ITypeParameter tp in typeDefinition.TypeParameters) {
					decl.TypeParameters.Add(ConvertTypeParameter(tp));
				}
			}
			
			if (this.ShowBaseTypes) {
				foreach (ITypeReference baseType in typeDefinition.BaseTypes) {
					decl.BaseTypes.Add(ConvertTypeReference(baseType));
				}
			}
			
			if (this.ShowTypeParameters && this.ShowTypeParameterConstraints) {
				foreach (ITypeParameter tp in typeDefinition.TypeParameters) {
					var constraint = ConvertTypeParameterConstraint(tp);
					if (constraint != null)
						decl.Constraints.Add(constraint);
				}
			}
			return decl;
		}
		
		DelegateDeclaration ConvertDelegate(IMethod invokeMethod, Modifiers modifiers)
		{
			ITypeDefinition d = invokeMethod.DeclaringTypeDefinition;
			
			DelegateDeclaration decl = new DelegateDeclaration();
			decl.Modifiers = modifiers;
			decl.ReturnType = ConvertTypeReference(invokeMethod.ReturnType);
			decl.Name = d.Name;
			
			if (this.ShowTypeParameters) {
				foreach (ITypeParameter tp in d.TypeParameters) {
					decl.TypeParameters.Add(ConvertTypeParameter(tp));
				}
			}
			
			foreach (IParameter p in invokeMethod.Parameters) {
				decl.Parameters.Add(ConvertParameter(p));
			}
			
			if (this.ShowTypeParameters && this.ShowTypeParameterConstraints) {
				foreach (ITypeParameter tp in d.TypeParameters) {
					var constraint = ConvertTypeParameterConstraint(tp);
					if (constraint != null)
						decl.Constraints.Add(constraint);
				}
			}
			return decl;
		}
		
		AstNode ConvertField(IField field)
		{
			FieldDeclaration decl = new FieldDeclaration();
			if (ShowModifiers) {
				Modifiers m = GetMemberModifiers(field);
				if (field.IsConst) {
					m &= ~Modifiers.Static;
					m |= Modifiers.Const;
				} else if (field.IsReadOnly) {
					m |= Modifiers.Readonly;
				} else if (field.IsVolatile) {
					m |= Modifiers.Volatile;
				}
				decl.Modifiers = m;
			}
			decl.ReturnType = ConvertTypeReference(field.ReturnType);
			Expression initializer = null;
			if (field.IsConst && this.ShowConstantValues)
				initializer = ConvertConstantValue(field.ConstantValue);
			decl.Variables.Add(new VariableInitializer(field.Name, initializer));
			return decl;
		}
		
		Accessor ConvertAccessor(IAccessor accessor)
		{
			if (accessor == null)
				return Accessor.Null;
			Accessor decl = new Accessor();
			decl.Modifiers = ModifierFromAccessibility(accessor.Accessibility);
			return decl;
		}
		
		PropertyDeclaration ConvertProperty(IProperty property)
		{
			PropertyDeclaration decl = new PropertyDeclaration();
			decl.Modifiers = GetMemberModifiers(property);
			decl.ReturnType = ConvertTypeReference(property.ReturnType);
			decl.Name = property.Name;
			decl.Getter = ConvertAccessor(property.Getter);
			decl.Setter = ConvertAccessor(property.Setter);
			return decl;
		}
		
		IndexerDeclaration ConvertIndexer(IProperty indexer)
		{
			IndexerDeclaration decl = new IndexerDeclaration();
			decl.Modifiers = GetMemberModifiers(indexer);
			decl.ReturnType = ConvertTypeReference(indexer.ReturnType);
			foreach (IParameter p in indexer.Parameters) {
				decl.Parameters.Add(ConvertParameter(p));
			}
			decl.Getter = ConvertAccessor(indexer.Getter);
			decl.Setter = ConvertAccessor(indexer.Setter);
			return decl;
		}
		
		EventDeclaration ConvertEvent(IEvent ev)
		{
			EventDeclaration decl = new EventDeclaration();
			decl.Modifiers = GetMemberModifiers(ev);
			decl.ReturnType = ConvertTypeReference(ev.ReturnType);
			decl.Variables.Add(new VariableInitializer(ev.Name));
			return decl;
		}
		
		MethodDeclaration ConvertMethod(IMethod method)
		{
			MethodDeclaration decl = new MethodDeclaration();
			decl.Modifiers = GetMemberModifiers(method);
			decl.ReturnType = ConvertTypeReference(method.ReturnType);
			decl.Name = method.Name;
			
			if (this.ShowTypeParameters) {
				foreach (ITypeParameter tp in method.TypeParameters) {
					decl.TypeParameters.Add(ConvertTypeParameter(tp));
				}
			}
			
			foreach (IParameter p in method.Parameters) {
				decl.Parameters.Add(ConvertParameter(p));
			}
			
			if (this.ShowTypeParameters && this.ShowTypeParameterConstraints) {
				foreach (ITypeParameter tp in method.TypeParameters) {
					var constraint = ConvertTypeParameterConstraint(tp);
					if (constraint != null)
						decl.Constraints.Add(constraint);
				}
			}
			return decl;
		}
		
		AstNode ConvertOperator(IMethod op)
		{
			OperatorType? opType = OperatorDeclaration.GetOperatorType(op.Name);
			if (opType == null)
				return ConvertMethod(op);
			
			OperatorDeclaration decl = new OperatorDeclaration();
			decl.Modifiers = GetMemberModifiers(op);
			decl.OperatorType = opType.Value;
			decl.ReturnType = ConvertTypeReference(op.ReturnType);
			foreach (IParameter p in op.Parameters) {
				decl.Parameters.Add(ConvertParameter(p));
			}
			return decl;
		}
		
		ConstructorDeclaration ConvertConstructor(IMethod ctor)
		{
			ConstructorDeclaration decl = new ConstructorDeclaration();
			decl.Modifiers = GetMemberModifiers(ctor);
			decl.Name = ctor.DeclaringTypeDefinition.Name;
			foreach (IParameter p in ctor.Parameters) {
				decl.Parameters.Add(ConvertParameter(p));
			}
			return decl;
		}
		
		DestructorDeclaration ConvertDestructor(IMethod dtor)
		{
			DestructorDeclaration decl = new DestructorDeclaration();
			decl.Name = dtor.DeclaringTypeDefinition.Name;
			return decl;
		}
		#endregion
		
		#region Convert Modifiers
		Modifiers ModifierFromAccessibility(Accessibility accessibility)
		{
			if (!this.ShowAccessibility)
				return Modifiers.None;
			switch (accessibility) {
				case Accessibility.Private:
					return Modifiers.Private;
				case Accessibility.Public:
					return Modifiers.Public;
				case Accessibility.Protected:
					return Modifiers.Protected;
				case Accessibility.Internal:
					return Modifiers.Internal;
				case Accessibility.ProtectedOrInternal:
				case Accessibility.ProtectedAndInternal:
					return Modifiers.Protected | Modifiers.Internal;
				default:
					return Modifiers.None;
			}
		}
		
		Modifiers GetMemberModifiers(IMember member)
		{
			Modifiers m = ModifierFromAccessibility(member.Accessibility);
			if (this.ShowModifiers) {
				if (member.IsStatic) {
					m |= Modifiers.Static;
				} else {
					if (member.IsAbstract)
						m |= Modifiers.Abstract;
					if (member.IsOverride)
						m |= Modifiers.Override;
					if (member.IsVirtual && !member.IsAbstract && !member.IsOverride)
						m |= Modifiers.Virtual;
					if (member.IsSealed)
						m |= Modifiers.Sealed;
				}
				if (member.IsShadowing)
					m |= Modifiers.New;
			}
			return m;
		}
		#endregion
		
		#region Convert Type Parameter
		TypeParameterDeclaration ConvertTypeParameter(ITypeParameter tp)
		{
			TypeParameterDeclaration decl = new TypeParameterDeclaration();
			decl.Variance = tp.Variance;
			decl.Name = tp.Name;
			return decl;
		}
		
		Constraint ConvertTypeParameterConstraint(ITypeParameter tp)
		{
			if (tp.Constraints.Count == 0 && !tp.HasDefaultConstructorConstraint && !tp.HasReferenceTypeConstraint && !tp.HasValueTypeConstraint) {
				return null;
			}
			Constraint c = new Constraint();
			c.TypeParameter = tp.Name;
			if (tp.HasReferenceTypeConstraint) {
				c.BaseTypes.Add(new PrimitiveType("class"));
			} else if (tp.HasValueTypeConstraint) {
				c.BaseTypes.Add(new PrimitiveType("struct"));
			}
			foreach (ITypeReference tr in tp.Constraints) {
				c.BaseTypes.Add(ConvertTypeReference(tr));
			}
			if (tp.HasDefaultConstructorConstraint) {
				c.BaseTypes.Add(new PrimitiveType("new"));
			}
			return c;
		}
		#endregion
		
		#region Convert Variable
		public VariableDeclarationStatement ConvertVariable(IVariable v)
		{
			VariableDeclarationStatement decl = new VariableDeclarationStatement();
			decl.Modifiers = v.IsConst ? Modifiers.Const : Modifiers.None;
			decl.Type = ConvertTypeReference(v.Type);
			Expression initializer = null;
			if (v.IsConst)
				initializer = ConvertConstantValue(v.ConstantValue);
			decl.Variables.Add(new VariableInitializer(v.Name, initializer));
			return decl;
		}
		#endregion
	}
}
