// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using Debugger.Interop.CorDebug;

namespace Debugger
{
	public class BreakpointCollection: CollectionWithEvents<Breakpoint>
	{
		public event EventHandler<CollectionItemEventArgs<Breakpoint>> Hit;
		
		protected internal void OnHit(Breakpoint item)
		{
			if (Hit != null) {
				Hit(this, new CollectionItemEventArgs<Breakpoint>(item));
			}
		}
		
		public BreakpointCollection(NDebugger debugger):base(debugger) { }
		
		internal Breakpoint this[ICorDebugBreakpoint corBreakpoint] {
			get {
				foreach (Breakpoint breakpoint in this) {
					if (breakpoint.IsOwnerOf(corBreakpoint)) {
						return breakpoint;
					}
				}
				return null;
			}
		}
		
		public new void Add(Breakpoint breakpoint)
		{
			base.Add(breakpoint);
		}
		
		protected override void OnAdded(Breakpoint breakpoint)
		{
			var ilbp = breakpoint as ILBreakpoint;
			if (ilbp != null) {
				foreach (Process process in this.Debugger.Processes) {
					foreach (Module module in process.Modules) {
						if (!ilbp.MethodKey.IsSameModule(module.FullPath))
							continue;

						ilbp.SetBreakpoint(module);
					}
				}
			}
			
			base.OnAdded(breakpoint);
		}
		
		public new void Remove(Breakpoint breakpoint)
		{
			base.Remove(breakpoint);
		}
		
		public void RemoveAll()
		{
			for (int i = Count - 1; i >= 0; --i) {
				this[i].Remove();
			}
		}
		
		protected override void OnRemoved(Breakpoint breakpoint)
		{
			breakpoint.Deactivate();
			
			base.OnRemoved(breakpoint);
		}
	}
}
