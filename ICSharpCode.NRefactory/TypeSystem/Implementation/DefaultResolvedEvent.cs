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
using ICSharpCode.NRefactory.Utils;

namespace ICSharpCode.NRefactory.TypeSystem.Implementation
{
	public class DefaultResolvedEvent : AbstractResolvedMember, IEvent
	{
		protected new readonly IUnresolvedEvent unresolved;
		IAccessor addAccessor;
		IAccessor removeAccessor;
		IAccessor invokeAccessor;
		
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
		
		public IAccessor AddAccessor {
			get {
				if (!unresolved.CanAdd)
					return null;
				IAccessor result = this.addAccessor;
				if (result != null) {
					LazyInit.ReadBarrier();
					return result;
				} else {
					return LazyInit.GetOrSet(ref this.addAccessor, unresolved.AddAccessor.CreateResolvedAccessor(context));
				}
			}
		}
		
		public IAccessor RemoveAccessor {
			get {
				if (!unresolved.CanRemove)
					return null;
				IAccessor result = this.removeAccessor;
				if (result != null) {
					LazyInit.ReadBarrier();
					return result;
				} else {
					return LazyInit.GetOrSet(ref this.removeAccessor, unresolved.RemoveAccessor.CreateResolvedAccessor(context));
				}
			}
		}
		
		public IAccessor InvokeAccessor {
			get {
				if (!unresolved.CanInvoke)
					return null;
				IAccessor result = this.invokeAccessor;
				if (result != null) {
					LazyInit.ReadBarrier();
					return result;
				} else {
					return LazyInit.GetOrSet(ref this.invokeAccessor, unresolved.InvokeAccessor.CreateResolvedAccessor(context));
				}
			}
		}
	}
}
