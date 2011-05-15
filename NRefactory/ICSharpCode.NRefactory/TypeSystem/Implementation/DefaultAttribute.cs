// Copyright (c) 2010 AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace ICSharpCode.NRefactory.TypeSystem.Implementation
{
	/// <summary>
	/// Default implementation of <see cref="IAttribute"/>.
	/// </summary>
	public sealed class DefaultAttribute : AbstractFreezable, IAttribute, ISupportsInterning
	{
		ITypeReference attributeType;
		readonly ITypeReference[] constructorParameterTypes;
		DomRegion region;
		IList<IConstantValue> positionalArguments;
		IList<KeyValuePair<string, IConstantValue>> namedArguments;
		
		protected override void FreezeInternal()
		{
			positionalArguments = FreezeList(positionalArguments);
			
			if (namedArguments == null || namedArguments.Count == 0) {
				namedArguments = EmptyList<KeyValuePair<string, IConstantValue>>.Instance;
			} else {
				namedArguments = Array.AsReadOnly(namedArguments.ToArray());
				foreach (var pair in namedArguments) {
					pair.Value.Freeze();
				}
			}
			
			base.FreezeInternal();
		}
		
		public DefaultAttribute(ITypeReference attributeType, IEnumerable<ITypeReference> constructorParameterTypes)
		{
			if (attributeType == null)
				throw new ArgumentNullException("attributeType");
			this.attributeType = attributeType;
			this.constructorParameterTypes = constructorParameterTypes != null ? constructorParameterTypes.ToArray() : null;
		}
		
		public ITypeReference AttributeType {
			get { return attributeType; }
		}
		
		public ReadOnlyCollection<ITypeReference> ConstructorParameterTypes {
			get { return Array.AsReadOnly(constructorParameterTypes); }
		}
		
		public DomRegion Region {
			get { return region; }
			set {
				CheckBeforeMutation();
				region = value;
			}
		}
		
		public IList<IConstantValue> PositionalArguments {
			get {
				if (positionalArguments == null)
					positionalArguments = new List<IConstantValue>();
				return positionalArguments;
			}
		}
		
		IList<IConstantValue> IAttribute.GetPositionalArguments(ITypeResolveContext context)
		{
			return this.PositionalArguments;
		}
		
		public IList<KeyValuePair<string, IConstantValue>> NamedArguments {
			get {
				if (namedArguments == null)
					namedArguments = new List<KeyValuePair<string, IConstantValue>>();
				return namedArguments;
			}
		}
		
		IList<KeyValuePair<string, IConstantValue>> IAttribute.GetNamedArguments(ITypeResolveContext context)
		{
			return this.NamedArguments;
		}
		
		public IMethod ResolveConstructor(ITypeResolveContext context)
		{
			IType[] parameterTypes = null;
			if (constructorParameterTypes != null && constructorParameterTypes.Length > 0) {
				parameterTypes = new IType[constructorParameterTypes.Length];
				for (int i = 0; i < parameterTypes.Length; i++) {
					parameterTypes[i] = constructorParameterTypes[i].Resolve(context);
				}
			}
			IMethod bestMatch = null;
			foreach (IMethod ctor in attributeType.Resolve(context).GetConstructors(context)) {
				if (ctor.IsStatic)
					continue;
				if (parameterTypes == null) {
					if (ctor.Parameters.Count == 0)
						return ctor;
				} else if (ctor.Parameters.Count == parameterTypes.Length) {
					bestMatch = ctor;
					bool ok = true;
					for (int i = 0; i < parameterTypes.Length; i++) {
						if (ctor.Parameters[i].Type != parameterTypes[i]) {
							ok = false;
							break;
						}
					}
					if (ok)
						return ctor;
				}
			}
			return bestMatch;
		}
		
		public override string ToString()
		{
			StringBuilder b = new StringBuilder();
			b.Append('[');
			b.Append(attributeType.ToString());
			if (this.PositionalArguments.Count + this.NamedArguments.Count > 0) {
				b.Append('(');
				bool first = true;
				foreach (var element in this.PositionalArguments) {
					if (first) first = false; else b.Append(", ");
					b.Append(element.ToString());
				}
				foreach (var pair in this.NamedArguments) {
					if (first) first = false; else b.Append(", ");
					b.Append(pair.Key);
					b.Append('=');
					b.Append(pair.Value.ToString());
				}
				b.Append(')');
			}
			b.Append(']');
			return b.ToString();
		}
		
		void ISupportsInterning.PrepareForInterning(IInterningProvider provider)
		{
			attributeType = provider.Intern(attributeType);
			if (constructorParameterTypes != null) {
				for (int i = 0; i < constructorParameterTypes.Length; i++) {
					constructorParameterTypes[i] = provider.Intern(constructorParameterTypes[i]);
				}
			}
			positionalArguments = provider.InternList(positionalArguments);
		}
		
		int ISupportsInterning.GetHashCodeForInterning()
		{
			return attributeType.GetHashCode() ^ (positionalArguments != null ? positionalArguments.GetHashCode() : 0) ^ (namedArguments != null ? namedArguments.GetHashCode() : 0) ^ region.GetHashCode();
		}
		
		bool ISupportsInterning.EqualsForInterning(ISupportsInterning other)
		{
			DefaultAttribute a = other as DefaultAttribute;
			return a != null && attributeType == a.attributeType && positionalArguments == a.positionalArguments && namedArguments == a.namedArguments && region == a.region;
		}
	}
}
