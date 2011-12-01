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
	public sealed class DefaultResolvedAccessor : IAccessor
	{
		readonly Accessibility accessibility;
		readonly DomRegion region;
		readonly IList<IAttribute> attributes;
		readonly IList<IAttribute> returnTypeAttributes;
		
		public DefaultResolvedAccessor(Accessibility accessibility, DomRegion region = default(DomRegion), IList<IAttribute> attributes = null, IList<IAttribute> returnTypeAttributes = null)
		{
			this.accessibility = accessibility;
			this.region = region;
			this.attributes = attributes ?? EmptyList<IAttribute>.Instance;
			this.returnTypeAttributes = returnTypeAttributes ?? EmptyList<IAttribute>.Instance;
		}
		
		public DomRegion Region {
			get { return region; }
		}
		
		public IList<IAttribute> Attributes {
			get { return attributes; }
		}
		
		public IList<IAttribute> ReturnTypeAttributes {
			get { return returnTypeAttributes; }
		}
		
		public Accessibility Accessibility {
			get { return accessibility; }
		}
		
		public bool IsPrivate {
			get { return accessibility == Accessibility.Private; }
		}
		
		public bool IsPublic {
			get { return accessibility == Accessibility.Public; }
		}
		
		public bool IsProtected {
			get { return accessibility == Accessibility.Protected; }
		}
		
		public bool IsInternal {
			get { return accessibility == Accessibility.Internal; }
		}
		
		public bool IsProtectedOrInternal {
			get { return accessibility == Accessibility.ProtectedOrInternal; }
		}
		
		public bool IsProtectedAndInternal {
			get { return accessibility == Accessibility.ProtectedAndInternal; }
		}
	}
}
