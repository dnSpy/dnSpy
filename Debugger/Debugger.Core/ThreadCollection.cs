// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using Debugger.Interop.CorDebug;

namespace Debugger
{
	public class ThreadCollection: CollectionWithEvents<Thread>
	{
		public ThreadCollection(NDebugger debugger): base(debugger) {}
		
		Thread selected;
		
		public Thread Selected {
			get { return selected; }
			set { selected = value; }
		}
		
		public Thread Find(Predicate<Thread> predicate)
		{
			if (predicate == null)
				return null;
			
			foreach (var thread in this)
			{
				if (predicate(thread))
					return thread;
			}
			
			return null;
		}
		
		internal bool Contains(ICorDebugThread corThread)
		{
			foreach(Thread thread in this) {
				if (thread.CorThread == corThread) return true;
			}
			return false;
		}
		
		internal Thread this[ICorDebugThread corThread] {
			get {
				foreach(Thread thread in this) {
					if (thread.CorThread == corThread) {
						return thread;
					}
				}
				throw new DebuggerException("Thread is not in collection");
			}
		}
	}
}
