// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using Debugger.Interop.CorDebug;
using Debugger.MetaData;
using System.Collections.Generic;

namespace Debugger
{
	public class AppDomain: DebuggerObject
	{
		Process process;
		
		ICorDebugAppDomain corAppDomain;
		
		internal Dictionary<ICorDebugType, DebugType> DebugTypeCache = new Dictionary<ICorDebugType, DebugType>();
		
		public Process Process {
			get { return process; }
		}
		
		public uint ID {
			get {
				return corAppDomain.GetID();
			}
		}
		
		Module mscorlib;
		
		public Module Mscorlib {
			get {
				if (mscorlib != null) return mscorlib;
				foreach(Module m in Process.Modules) {
					if (m.Name == "mscorlib.dll" &&
					    m.AppDomain == this) {
						mscorlib = m;
						return mscorlib;
					}
				}
				throw new DebuggerException("Mscorlib not loaded");
			}
		}
		
		internal DebugType ObjectType {
			get { return DebugType.CreateFromType(this.Mscorlib, typeof(object)); }
		}
		
		internal ICorDebugAppDomain CorAppDomain {
			get { return corAppDomain; }
		}
		
		internal ICorDebugAppDomain2 CorAppDomain2 {
			get { return (ICorDebugAppDomain2)corAppDomain; }
		}
		
		public AppDomain(Process process, ICorDebugAppDomain corAppDomain)
		{
			this.process = process;
			this.corAppDomain = corAppDomain;
		}
	}
}
