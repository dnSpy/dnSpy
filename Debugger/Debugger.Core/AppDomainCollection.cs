// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using Debugger.Interop.CorDebug;

namespace Debugger
{
	public class AppDomainCollection: CollectionWithEvents<AppDomain>
	{
		public AppDomainCollection(NDebugger dbgr): base(dbgr) {}
		
		public AppDomain this[ICorDebugAppDomain corAppDomain] {
			get {
				foreach(AppDomain a in this) {
					if (a.CorAppDomain.Equals(corAppDomain)) {
						return a;
					}
				}
				throw new DebuggerException("AppDomain not found");
			}
		}
	}
}
