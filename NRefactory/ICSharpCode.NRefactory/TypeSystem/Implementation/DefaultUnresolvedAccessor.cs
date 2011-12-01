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
using ICSharpCode.NRefactory.Utils;

namespace ICSharpCode.NRefactory.TypeSystem.Implementation
{
	/// <summary>
	/// Default implementation of <see cref="IAccessor"/>.
	/// </summary>
	[Serializable]
	public sealed class DefaultUnresolvedAccessor : AbstractFreezable, IUnresolvedAccessor, ISupportsInterning, IFreezable
	{
		static readonly DefaultUnresolvedAccessor[] defaultAccessors = CreateDefaultAccessors();
		
		static DefaultUnresolvedAccessor[] CreateDefaultAccessors()
		{
			DefaultUnresolvedAccessor[] accessors = new DefaultUnresolvedAccessor[(int)Accessibility.ProtectedAndInternal + 1];
			for (int i = 0; i < accessors.Length; i++) {
				accessors[i] = new DefaultUnresolvedAccessor();
				accessors[i].accessibility = (Accessibility)i;
				accessors[i].Freeze();
			}
			return accessors;
		}
		
		/// <summary>
		/// Gets the default accessor with the specified accessibility (and without attributes or region).
		/// </summary>
		public static IUnresolvedAccessor GetFromAccessibility(Accessibility accessibility)
		{
			int index = (int)accessibility;
			if (index >= 0 && index < defaultAccessors.Length) {
				return defaultAccessors[index];
			} else {
				DefaultUnresolvedAccessor a = new DefaultUnresolvedAccessor();
				a.accessibility = accessibility;
				a.Freeze();
				return a;
			}
		}
		
		Accessibility accessibility;
		DomRegion region;
		IList<IUnresolvedAttribute> attributes;
		IList<IUnresolvedAttribute> returnTypeAttributes;
		
		protected override void FreezeInternal()
		{
			base.FreezeInternal();
			this.attributes = FreezableHelper.FreezeListAndElements(this.attributes);
			this.returnTypeAttributes = FreezableHelper.FreezeListAndElements(this.returnTypeAttributes);
		}
		
		public Accessibility Accessibility {
			get { return accessibility; }
			set {
				FreezableHelper.ThrowIfFrozen(this);
				accessibility = value;
			}
		}
		
		public DomRegion Region {
			get { return region; }
			set {
				FreezableHelper.ThrowIfFrozen(this);
				region = value;
			}
		}
		
		public IList<IUnresolvedAttribute> Attributes {
			get {
				if (attributes == null)
					attributes = new List<IUnresolvedAttribute>();
				return attributes;
			}
		}
		
		public IList<IUnresolvedAttribute> ReturnTypeAttributes {
			get {
				if (returnTypeAttributes == null)
					returnTypeAttributes = new List<IUnresolvedAttribute>();
				return returnTypeAttributes;
			}
		}
		
		bool IHasAccessibility.IsPrivate {
			get { return accessibility == Accessibility.Private; }
		}
		
		bool IHasAccessibility.IsPublic {
			get { return accessibility == Accessibility.Public; }
		}
		
		bool IHasAccessibility.IsProtected {
			get { return accessibility == Accessibility.Protected; }
		}
		
		bool IHasAccessibility.IsInternal {
			get { return accessibility == Accessibility.Internal; }
		}
		
		bool IHasAccessibility.IsProtectedOrInternal {
			get { return accessibility == Accessibility.ProtectedOrInternal; }
		}
		
		bool IHasAccessibility.IsProtectedAndInternal {
			get { return accessibility == Accessibility.ProtectedAndInternal; }
		}
		
		void ISupportsInterning.PrepareForInterning(IInterningProvider provider)
		{
			if (!this.IsFrozen) {
				attributes = provider.InternList(attributes);
				returnTypeAttributes = provider.InternList(returnTypeAttributes);
			}
		}
		
		int ISupportsInterning.GetHashCodeForInterning()
		{
			return (attributes != null ? attributes.GetHashCode() : 0)
				^ (returnTypeAttributes != null ? returnTypeAttributes.GetHashCode() : 0)
				^ region.GetHashCode() ^ (int)accessibility;
		}
		
		bool ISupportsInterning.EqualsForInterning(ISupportsInterning other)
		{
			DefaultUnresolvedAccessor o = other as DefaultUnresolvedAccessor;
			return o != null && (attributes == o.attributes && returnTypeAttributes == o.returnTypeAttributes
			                     && accessibility == o.accessibility && region == o.region);
		}
		
		public IAccessor CreateResolvedAccessor(ITypeResolveContext context)
		{
			Freeze();
			return new DefaultResolvedAccessor(accessibility, region, attributes.CreateResolvedAttributes(context), returnTypeAttributes.CreateResolvedAttributes(context));
		}
	}
}
