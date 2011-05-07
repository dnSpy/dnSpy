// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.TypeSystem.Implementation;

namespace ICSharpCode.NRefactory.CSharp.Resolver
{
	public sealed class CSharpAttribute : Immutable, IAttribute
	{
		ITypeReference attributeType;
		DomRegion region;
		IList<IConstantValue> positionalArguments;
		IList<KeyValuePair<string, IConstantValue>> namedCtorArguments;
		IList<KeyValuePair<string, IConstantValue>> namedArguments;
		
		public CSharpAttribute(ITypeReference attributeType, DomRegion region,
		                       IList<IConstantValue> positionalArguments,
		                       IList<KeyValuePair<string, IConstantValue>> namedCtorArguments,
		                       IList<KeyValuePair<string, IConstantValue>> namedArguments)
		{
			if (attributeType == null)
				throw new ArgumentNullException("attributeType");
			this.attributeType = attributeType;
			this.region = region;
			this.positionalArguments = positionalArguments;
			this.namedCtorArguments = namedCtorArguments;
			this.namedArguments = namedArguments;
		}
		
		public DomRegion Region {
			get { return region; }
		}
		
		public ITypeReference AttributeType {
			get { return attributeType; }
		}
		
		public IMethod ResolveConstructor(ITypeResolveContext context)
		{
			IType type = attributeType.Resolve(context);
			throw new NotImplementedException();
		}
		
		public IList<IConstantValue> GetPositionalArguments(ITypeResolveContext context)
		{
			if (namedCtorArguments == null || namedCtorArguments.Count == 0) {
				// no namedCtorArguments: just return the positionalArguments
				if (positionalArguments != null)
					return new ReadOnlyCollection<IConstantValue>(positionalArguments);
				else
					return EmptyList<IConstantValue>.Instance;
			}
			// we do have namedCtorArguments, which need to be re-ordered and appended to the positional arguments
			List<IConstantValue> result = new List<IConstantValue>(this.positionalArguments);
			IMethod method = ResolveConstructor(context);
			if (method != null) {
				for (int i = result.Count; i < method.Parameters.Count; i++) {
					IParameter p = method.Parameters[i];
					bool found = false;
					foreach (var pair in namedCtorArguments) {
						if (pair.Key == p.Name) {
							result.Add(pair.Value);
							found = true;
						}
					}
					if (!found) {
						// add the parameter's default value:
						result.Add(p.DefaultValue ?? new SimpleConstantValue(p.Type, CSharpResolver.GetDefaultValue(p.Type.Resolve(context))));
					}
				}
			}
			return result.AsReadOnly();
		}
		
		public IList<KeyValuePair<string, IConstantValue>> GetNamedArguments(ITypeResolveContext context)
		{
			if (namedArguments != null)
				return new ReadOnlyCollection<KeyValuePair<string, IConstantValue>>(namedArguments);
			else
				return EmptyList<KeyValuePair<string, IConstantValue>>.Instance;
		}
	}
	
	/// <summary>
	/// Type reference used within an attribute.
	/// Looks up both 'withoutSuffix' and 'withSuffix' and returns the type that exists.
	/// </summary>
	public sealed class AttributeTypeReference : ITypeReference, ISupportsInterning
	{
		ITypeReference withoutSuffix, withSuffix;
		
		public AttributeTypeReference(ITypeReference withoutSuffix, ITypeReference withSuffix)
		{
			if (withoutSuffix == null)
				throw new ArgumentNullException("withoutSuffix");
			if (withSuffix == null)
				throw new ArgumentNullException("withSuffix");
			this.withoutSuffix = withoutSuffix;
			this.withSuffix = withSuffix;
		}
		
		public IType Resolve(ITypeResolveContext context)
		{
			// If both types exist, C# considers that to be an ambiguity, but we are less strict.
			IType type = withoutSuffix.Resolve(context);
			if (type == SharedTypes.UnknownType)
				return withSuffix.Resolve(context);
			else
				return type;
		}
		
		public override string ToString()
		{
			return withoutSuffix.ToString() + "[Attribute]";
		}
		
		void ISupportsInterning.PrepareForInterning(IInterningProvider provider)
		{
			withoutSuffix = provider.Intern(withoutSuffix);
			withSuffix = provider.Intern(withSuffix);
		}
		
		int ISupportsInterning.GetHashCodeForInterning()
		{
			unchecked {
				return withoutSuffix.GetHashCode() + 715613 * withSuffix.GetHashCode();
			}
		}
		
		bool ISupportsInterning.EqualsForInterning(ISupportsInterning other)
		{
			AttributeTypeReference atr = other as AttributeTypeReference;
			return atr != null && this.withoutSuffix == atr.withoutSuffix && this.withSuffix == atr.withSuffix;
		}
	}
}
