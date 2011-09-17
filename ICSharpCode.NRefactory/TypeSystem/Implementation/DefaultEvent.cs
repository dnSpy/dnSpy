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

namespace ICSharpCode.NRefactory.TypeSystem.Implementation
{
	/// <summary>
	/// Default implementation of <see cref="IEvent"/>.
	/// </summary>
	[Serializable]
	public class DefaultEvent : AbstractMember, IEvent
	{
		IAccessor addAccessor, removeAccessor, invokeAccessor;
		
		protected override void FreezeInternal()
		{
			base.FreezeInternal();
			if (addAccessor != null)    addAccessor.Freeze();
			if (removeAccessor != null) removeAccessor.Freeze();
			if (invokeAccessor != null) invokeAccessor.Freeze();
		}
		
		public DefaultEvent(ITypeDefinition declaringTypeDefinition, string name)
			: base(declaringTypeDefinition, name, EntityType.Event)
		{
		}
		
		/// <summary>
		/// Copy constructor
		/// </summary>
		protected DefaultEvent(IEvent ev)
			: base(ev)
		{
			this.addAccessor = ev.AddAccessor;
			this.removeAccessor = ev.RemoveAccessor;
			this.invokeAccessor = ev.InvokeAccessor;
		}
		
		public bool CanAdd {
			get { return addAccessor != null; }
		}
		
		public bool CanRemove {
			get { return removeAccessor != null; }
		}
		
		public bool CanInvoke {
			get { return invokeAccessor != null; }
		}
		
		public IAccessor AddAccessor{
			get { return addAccessor; }
			set {
				CheckBeforeMutation();
				addAccessor = value;
			}
		}
		
		public IAccessor RemoveAccessor {
			get { return removeAccessor; }
			set {
				CheckBeforeMutation();
				removeAccessor = value;
			}
		}
		
		public IAccessor InvokeAccessor {
			get { return invokeAccessor; }
			set {
				CheckBeforeMutation();
				invokeAccessor = value;
			}
		}
	}
}
