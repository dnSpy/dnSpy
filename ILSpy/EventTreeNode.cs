// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using ICSharpCode.TreeView;
using Mono.Cecil;

namespace ICSharpCode.ILSpy
{
	/// <summary>
	/// Represents an event in the TreeView.
	/// </summary>
	sealed class EventTreeNode : SharpTreeNode
	{
		readonly EventDefinition ev;
		
		public EventTreeNode(EventDefinition ev)
		{
			if (ev == null)
				throw new ArgumentNullException("ev");
			this.ev = ev;
			this.LazyLoading = true;
		}
		
		public override object Text {
			get { return ev.Name + " : " + Language.Current.TypeToString(ev.EventType); }
		}
		
		public override object Icon {
			get {
				return Images.Event;
			}
		}
		
		protected override void LoadChildren()
		{
			if (ev.AddMethod != null)
				this.Children.Add(new MethodTreeNode(ev.AddMethod));
			if (ev.RemoveMethod != null)
				this.Children.Add(new MethodTreeNode(ev.RemoveMethod));
			if (ev.InvokeMethod != null)
				this.Children.Add(new MethodTreeNode(ev.InvokeMethod));
			if (ev.HasOtherMethods) {
				foreach (var m in ev.OtherMethods)
					this.Children.Add(new MethodTreeNode(m));
			}
		}
	}
}
