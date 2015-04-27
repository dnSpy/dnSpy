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
using ICSharpCode.NRefactory.Utils;

namespace ICSharpCode.NRefactory.TypeSystem.Implementation
{
	public class DefaultResolvedEvent : AbstractResolvedMember, IEvent
	{
		protected new readonly IUnresolvedEvent unresolved;
		IMethod addAccessor;
		IMethod removeAccessor;
		IMethod invokeAccessor;
		
		public DefaultResolvedEvent(IUnresolvedEvent unresolved, ITypeResolveContext parentContext)
			: base(unresolved, parentContext)
		{
			this.unresolved = unresolved;
		}
		
		public bool CanAdd {
			get { return unresolved.CanAdd; }
		}
		
		public bool CanRemove {
			get { return unresolved.CanRemove; }
		}
		
		public bool CanInvoke {
			get { return unresolved.CanInvoke; }
		}
		
		public IMethod AddAccessor {
			get { return GetAccessor(ref addAccessor, unresolved.AddAccessor); }
		}
		
		public IMethod RemoveAccessor {
			get { return GetAccessor(ref removeAccessor, unresolved.RemoveAccessor); }
		}
		
		public IMethod InvokeAccessor {
			get { return GetAccessor(ref invokeAccessor, unresolved.InvokeAccessor); }
		}
		
		public override IMember Specialize(TypeParameterSubstitution substitution)
		{
			if (TypeParameterSubstitution.Identity.Equals(substitution))
				return this;
			return new SpecializedEvent(this, substitution);
		}
	}
}
