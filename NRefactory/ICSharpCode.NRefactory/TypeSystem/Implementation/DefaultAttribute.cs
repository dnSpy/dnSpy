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
using ICSharpCode.NRefactory.Semantics;

namespace ICSharpCode.NRefactory.TypeSystem.Implementation
{
	/// <summary>
	/// IAttribute implementation for already-resolved attributes.
	/// </summary>
	public class DefaultAttribute : IAttribute
	{
		readonly IType attributeType;
		readonly IList<ResolveResult> positionalArguments;
		readonly IList<KeyValuePair<IMember, ResolveResult>> namedArguments;
		readonly DomRegion region;
		volatile IMethod constructor;
		
		public DefaultAttribute(IType attributeType, IList<ResolveResult> positionalArguments = null,
		                        IList<KeyValuePair<IMember, ResolveResult>> namedArguments = null,
		                        DomRegion region = default(DomRegion))
		{
			if (attributeType == null)
				throw new ArgumentNullException("attributeType");
			this.attributeType = attributeType;
			this.positionalArguments = positionalArguments ?? EmptyList<ResolveResult>.Instance;
			this.namedArguments = namedArguments ?? EmptyList<KeyValuePair<IMember, ResolveResult>>.Instance;
			this.region = region;
		}
		
		public DefaultAttribute(IMethod constructor, IList<ResolveResult> positionalArguments = null,
		                        IList<KeyValuePair<IMember, ResolveResult>> namedArguments = null,
		                        DomRegion region = default(DomRegion))
		{
			if (constructor == null)
				throw new ArgumentNullException("constructor");
			this.constructor = constructor;
			this.attributeType = constructor.DeclaringType;
			this.positionalArguments = positionalArguments ?? EmptyList<ResolveResult>.Instance;
			this.namedArguments = namedArguments ?? EmptyList<KeyValuePair<IMember, ResolveResult>>.Instance;
			this.region = region;
			if (this.positionalArguments.Count != constructor.Parameters.Count) {
				throw new ArgumentException("Positional argument count must match the constructor's parameter count");
			}
		}
		
		public IType AttributeType {
			get { return attributeType; }
		}
		
		public DomRegion Region {
			get { return region; }
		}
		
		public IMethod Constructor {
			get {
				IMethod ctor = this.constructor;
				if (ctor == null) {
					foreach (IMethod candidate in this.AttributeType.GetConstructors(m => m.Parameters.Count == positionalArguments.Count)) {
						if (candidate.Parameters.Select(p => p.Type).SequenceEqual(this.PositionalArguments.Select(a => a.Type))) {
							ctor = candidate;
							break;
						}
					}
					this.constructor = ctor;
				}
				return ctor;
			}
		}
		
		public IList<ResolveResult> PositionalArguments {
			get { return positionalArguments; }
		}
		
		public IList<KeyValuePair<IMember, ResolveResult>> NamedArguments {
			get { return namedArguments; }
		}
	}
}
