// Copyright (c) 2010 AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;

namespace ICSharpCode.NRefactory.TypeSystem.Implementation
{
	/// <summary>
	/// Default implementation of <see cref="IEvent"/>.
	/// </summary>
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
