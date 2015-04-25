// Copyright (c) 2010-2013 AlphaSierraPapa for the SharpDevelop Team
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
using System.Linq;
using ICSharpCode.NRefactory.Utils;

namespace ICSharpCode.NRefactory.TypeSystem.Implementation
{
	/// <summary>
	/// Base class for <see cref="IUnresolvedMember"/> implementations.
	/// </summary>
	[Serializable]
	public abstract class AbstractUnresolvedMember : AbstractUnresolvedEntity, IUnresolvedMember
	{
		ITypeReference returnType = SpecialType.UnknownType;
		IList<IMemberReference> interfaceImplementations;
		
		public override void ApplyInterningProvider(InterningProvider provider)
		{
			base.ApplyInterningProvider(provider);
			interfaceImplementations = provider.InternList(interfaceImplementations);
		}
		
		protected override void FreezeInternal()
		{
			base.FreezeInternal();
			interfaceImplementations = FreezableHelper.FreezeList(interfaceImplementations);
		}
		
		public override object Clone()
		{
			var copy = (AbstractUnresolvedMember)base.Clone();
			if (interfaceImplementations != null)
				copy.interfaceImplementations = new List<IMemberReference>(interfaceImplementations);
			return copy;
		}
		
		/*
		[Serializable]
		internal new class RareFields : AbstractUnresolvedEntity.RareFields
		{
			internal IList<IMemberReference> interfaceImplementations;
			
			public override void ApplyInterningProvider(IInterningProvider provider)
			{
				base.ApplyInterningProvider(provider);
				interfaceImplementations = provider.InternList(interfaceImplementations);
			}
			
			protected internal override void FreezeInternal()
			{
				interfaceImplementations = FreezableHelper.FreezeListAndElements(interfaceImplementations);
				base.FreezeInternal();
			}
			
			override Clone(){}
		}
		
		internal override AbstractUnresolvedEntity.RareFields WriteRareFields()
		{
			ThrowIfFrozen();
			if (rareFields == null) rareFields = new RareFields();
			return rareFields;
		}*/
		
		public ITypeReference ReturnType {
			get { return returnType; }
			set {
				if (value == null)
					throw new ArgumentNullException("value");
				ThrowIfFrozen();
				returnType = value;
			}
		}
		
		public bool IsExplicitInterfaceImplementation {
			get { return flags[FlagExplicitInterfaceImplementation]; }
			set {
				ThrowIfFrozen();
				flags[FlagExplicitInterfaceImplementation] = value;
			}
		}
		
		public IList<IMemberReference> ExplicitInterfaceImplementations {
			get {
				/*
				RareFields rareFields = (RareFields)this.rareFields;
				if (rareFields == null || rareFields.interfaceImplementations == null) {
					rareFields = (RareFields)WriteRareFields();
					return rareFields.interfaceImplementations = new List<IMemberReference>();
				}
				return rareFields.interfaceImplementations;
				*/
				if (interfaceImplementations == null)
					interfaceImplementations = new List<IMemberReference>();
				return interfaceImplementations;
			}
		}
		
		public bool IsVirtual {
			get { return flags[FlagVirtual]; }
			set {
				ThrowIfFrozen();
				flags[FlagVirtual] = value;
			}
		}
		
		public bool IsOverride {
			get { return flags[FlagOverride]; }
			set {
				ThrowIfFrozen();
				flags[FlagOverride] = value;
			}
		}
		
		public bool IsOverridable {
			get {
				// override or virtual or abstract but not sealed
				return (flags.Data & (FlagOverride | FlagVirtual | FlagAbstract)) != 0 && !this.IsSealed;
			}
		}
		
		ITypeReference IMemberReference.DeclaringTypeReference {
			get { return this.DeclaringTypeDefinition; }
		}
		
		#region Resolve
		public abstract IMember CreateResolved(ITypeResolveContext context);
		
		public virtual IMember Resolve(ITypeResolveContext context)
		{
			ITypeReference interfaceTypeReference = null;
			if (this.IsExplicitInterfaceImplementation && this.ExplicitInterfaceImplementations.Count == 1)
				interfaceTypeReference = this.ExplicitInterfaceImplementations[0].DeclaringTypeReference;
			return Resolve(ExtendContextForType(context, this.DeclaringTypeDefinition), this.SymbolKind, this.Name, interfaceTypeReference);
		}
		
		ISymbol ISymbolReference.Resolve(ITypeResolveContext context)
		{
			return ((IUnresolvedMember)this).Resolve(context);
		}
		
		protected static ITypeResolveContext ExtendContextForType(ITypeResolveContext assemblyContext, IUnresolvedTypeDefinition typeDef)
		{
			if (typeDef == null)
				return assemblyContext;
			ITypeResolveContext parentContext;
			if (typeDef.DeclaringTypeDefinition != null)
				parentContext = ExtendContextForType(assemblyContext, typeDef.DeclaringTypeDefinition);
			else
				parentContext = assemblyContext;
			ITypeDefinition resolvedTypeDef = typeDef.Resolve(assemblyContext).GetDefinition();
			return typeDef.CreateResolveContext(parentContext).WithCurrentTypeDefinition(resolvedTypeDef);
		}
		
		public static IMember Resolve(ITypeResolveContext context,
		                              SymbolKind symbolKind,
		                              string name,
		                              ITypeReference explicitInterfaceTypeReference = null,
		                              IList<string> typeParameterNames = null,
		                              IList<ITypeReference> parameterTypeReferences = null)
		{
			if (context.CurrentTypeDefinition == null)
				return null;
			if (parameterTypeReferences == null)
				parameterTypeReferences = EmptyList<ITypeReference>.Instance;
			if (typeParameterNames == null || typeParameterNames.Count == 0) {
				// non-generic member
				// In this case, we can simply resolve the parameter types in the given context
				var parameterTypes = parameterTypeReferences.Resolve(context);
				if (explicitInterfaceTypeReference == null) {
					foreach (IMember member in context.CurrentTypeDefinition.Members) {
						if (member.IsExplicitInterfaceImplementation)
							continue;
						if (IsNonGenericMatch(member, symbolKind, name, parameterTypes))
							return member;
					}
				} else {
					IType explicitInterfaceType = explicitInterfaceTypeReference.Resolve(context);
					foreach (IMember member in context.CurrentTypeDefinition.Members) {
						if (!member.IsExplicitInterfaceImplementation)
							continue;
						if (member.ImplementedInterfaceMembers.Count != 1)
							continue;
						if (IsNonGenericMatch(member, symbolKind, name, parameterTypes)) {
							if (explicitInterfaceType.Equals(member.ImplementedInterfaceMembers[0].DeclaringType))
								return member;
						}
					}
				}
			} else {
				// generic member
				// In this case, we must specify the correct context for resolving the parameter types
				foreach (IMethod method in context.CurrentTypeDefinition.Methods) {
					if (method.SymbolKind != symbolKind)
						continue;
					if (method.Name != name)
						continue;
					if (method.Parameters.Count != parameterTypeReferences.Count)
						continue;
					// Compare type parameter count and names:
					if (!typeParameterNames.SequenceEqual(method.TypeParameters.Select(tp => tp.Name)))
						continue;
					// Once we know the type parameter names are fitting, we can resolve the
					// type references in the context of the method:
					var contextForMethod = context.WithCurrentMember(method);
					var parameterTypes = parameterTypeReferences.Resolve(contextForMethod);
					if (!IsParameterTypeMatch(method, parameterTypes))
						continue;
					if (explicitInterfaceTypeReference == null) {
						if (!method.IsExplicitInterfaceImplementation)
							return method;
					} else if (method.IsExplicitInterfaceImplementation && method.ImplementedInterfaceMembers.Count == 1) {
						IType explicitInterfaceType = explicitInterfaceTypeReference.Resolve(contextForMethod);
						if (explicitInterfaceType.Equals(method.ImplementedInterfaceMembers[0].DeclaringType))
							return method;
					}
				}
			}
			return null;
		}
		
		static bool IsNonGenericMatch(IMember member, SymbolKind symbolKind, string name, IList<IType> parameterTypes)
		{
			if (member.SymbolKind != symbolKind)
				return false;
			if (member.Name != name)
				return false;
			IMethod method = member as IMethod;
			if (method != null && method.TypeParameters.Count > 0)
				return false;
			return IsParameterTypeMatch(member, parameterTypes);
		}
		
		static bool IsParameterTypeMatch(IMember member, IList<IType> parameterTypes)
		{
			IParameterizedMember parameterizedMember = member as IParameterizedMember;
			if (parameterizedMember == null) {
				return parameterTypes.Count == 0;
			} else if (parameterTypes.Count == parameterizedMember.Parameters.Count) {
				for (int i = 0; i < parameterTypes.Count; i++) {
					IType type1 = parameterTypes[i];
					IType type2 = parameterizedMember.Parameters[i].Type;
					if (!type1.Equals(type2)) {
						return false;
					}
				}
				return true;
			} else {
				return false;
			}
		}
		#endregion
	}
}
