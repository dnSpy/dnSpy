// Copyright (c) 2010 AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ICSharpCode.NRefactory.TypeSystem.Implementation
{
	/// <summary>
	/// Default implementation of <see cref="IAttribute"/>.
	/// </summary>
	public sealed class DefaultAttribute : AbstractFreezable, IAttribute, ISupportsInterning
	{
		DomRegion region;
		ITypeReference attributeType;
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
		
		public DefaultAttribute(ITypeReference attributeType)
		{
			if (attributeType == null)
				throw new ArgumentNullException("attributeType");
			this.attributeType = attributeType;
		}
		
		public DomRegion Region {
			get { return region; }
			set {
				CheckBeforeMutation();
				region = value;
			}
		}
		
		public ITypeReference AttributeType {
			get { return attributeType; }
			set {
				CheckBeforeMutation();
				attributeType = value;
			}
		}
		
		public IList<IConstantValue> PositionalArguments {
			get {
				if (positionalArguments == null)
					positionalArguments = new List<IConstantValue>();
				return positionalArguments;
			}
		}
		
		public IList<KeyValuePair<string, IConstantValue>> NamedArguments {
			get {
				if (namedArguments == null)
					namedArguments = new List<KeyValuePair<string, IConstantValue>>();
				return namedArguments;
			}
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
