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
using System.Linq;

namespace ICSharpCode.NRefactory.TypeSystem.Implementation
{
	/// <summary>
	/// Default implementation of <see cref="IAccessor"/>.
	/// </summary>
	[Serializable]
	public sealed class DefaultAccessor : AbstractFreezable, IAccessor, ISupportsInterning
	{
		static readonly DefaultAccessor[] defaultAccessors = CreateDefaultAccessors();
		
		static DefaultAccessor[] CreateDefaultAccessors()
		{
			DefaultAccessor[] accessors = new DefaultAccessor[(int)Accessibility.ProtectedAndInternal + 1];
			for (int i = 0; i < accessors.Length; i++) {
				accessors[i] = new DefaultAccessor();
				accessors[i].accessibility = (Accessibility)i;
				accessors[i].Freeze();
			}
			return accessors;
		}
		
		/// <summary>
		/// Gets the default accessor with the specified accessibility (and without attributes or region).
		/// </summary>
		public static IAccessor GetFromAccessibility(Accessibility accessibility)
		{
			int index = (int)accessibility;
			if (index >= 0 && index < defaultAccessors.Length) {
				return defaultAccessors[index];
			} else {
				DefaultAccessor a = new DefaultAccessor();
				a.accessibility = accessibility;
				a.Freeze();
				return a;
			}
		}
		
		Accessibility accessibility;
		DomRegion region;
		IList<IAttribute> attributes;
		IList<IAttribute> returnTypeAttributes;
		
		protected override void FreezeInternal()
		{
			base.FreezeInternal();
			this.attributes = FreezeList(this.attributes);
		}
		
		public Accessibility Accessibility {
			get { return accessibility; }
			set {
				CheckBeforeMutation();
				accessibility = value;
			}
		}
		
		public DomRegion Region {
			get { return region; }
			set {
				CheckBeforeMutation();
				region = value;
			}
		}
		
		public IList<IAttribute> Attributes {
			get {
				if (attributes == null)
					attributes = new List<IAttribute>();
				return attributes;
			}
		}
		
		public IList<IAttribute> ReturnTypeAttributes {
			get {
				if (returnTypeAttributes == null)
					returnTypeAttributes = new List<IAttribute>();
				return returnTypeAttributes;
			}
		}
		
		void ISupportsInterning.PrepareForInterning(IInterningProvider provider)
		{
			attributes = provider.InternList(attributes);
			returnTypeAttributes = provider.InternList(returnTypeAttributes);
		}
		
		int ISupportsInterning.GetHashCodeForInterning()
		{
			return (attributes != null ? attributes.GetHashCode() : 0)
				^ (returnTypeAttributes != null ? returnTypeAttributes.GetHashCode() : 0)
				^ region.GetHashCode() ^ (int)accessibility;
		}
		
		bool ISupportsInterning.EqualsForInterning(ISupportsInterning other)
		{
			DefaultAccessor a = other as DefaultAccessor;
			return a != null && (attributes == a.attributes && returnTypeAttributes == a.returnTypeAttributes 
			                     && accessibility == a.accessibility && region == a.region);
		}
	}
}
