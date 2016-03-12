/*
    Copyright (C) 2014-2016 de4dot@gmail.com

    This file is part of dnSpy

    dnSpy is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    dnSpy is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with dnSpy.  If not, see <http://www.gnu.org/licenses/>.
*/

using System.Collections.Generic;
using dndbg.Engine;
using dnSpy.Contracts.Scripting.Debugger;
using dnSpy.Shared.Scripting;

namespace dnSpy.Debugger.Scripting {
	sealed class DebuggerAppDomain : IAppDomain {
		public int Id {
			get { return id; }
		}

		public bool IsAttached {
			get { return debugger.Dispatcher.UI(() => appDomain.CorAppDomain.IsAttached); }
		}

		public bool IsRunning {
			get { return debugger.Dispatcher.UI(() => appDomain.CorAppDomain.IsRunning); }
		}

		public string Name {
			get { return debugger.Dispatcher.UI(() => appDomain.Name); }
		}

		public bool HasExited {
			get { return debugger.Dispatcher.UI(() => appDomain.HasExited); }
		}

		public IEnumerable<IThread> Threads {
			get { return debugger.Dispatcher.UIIter(GetThreadsUI); }
		}

		IEnumerable<IThread> GetThreadsUI() {
			foreach (var t in appDomain.Threads)
				yield return new DebuggerThread(debugger, t);
		}

		public IEnumerable<IDebuggerAssembly> Assemblies {
			get { return debugger.Dispatcher.UIIter(GetAssembliesUI); }
		}

		IEnumerable<IDebuggerAssembly> GetAssembliesUI() {
			foreach (var a in appDomain.Assemblies)
				yield return new DebuggerAssembly(debugger, a);
		}

		public IEnumerable<IDebuggerModule> Modules {
			get { return debugger.Dispatcher.UIIter(GetModulesUI); }
		}

		IEnumerable<IDebuggerModule> GetModulesUI() {
			foreach (var m in appDomain.Modules)
				yield return new DebuggerModule(debugger, m);
		}

		public IDebuggerValue Object {
			get {
				return debugger.Dispatcher.UI(() => {
					var value = appDomain.CorAppDomain.Object;
					return value == null ? null : new DebuggerValue(debugger, value);
				});
			}
		}

		readonly Debugger debugger;
		readonly DnAppDomain appDomain;
		readonly int hashCode;
		readonly int id;

		public DebuggerAppDomain(Debugger debugger, DnAppDomain appDomain) {
			debugger.Dispatcher.VerifyAccess();
			this.debugger = debugger;
			this.appDomain = appDomain;
			this.hashCode = appDomain.GetHashCode();
			this.id = appDomain.Id;
		}

		public void SetAllThreadsDebugState(ThreadState state, IThread thread) {
			debugger.Dispatcher.UI(() => appDomain.CorAppDomain.SetAllThreadsDebugState((dndbg.COM.CorDebug.CorDebugThreadState)state, thread == null ? null : ((DebuggerThread)thread).DnThread.CorThread));
		}

		public override bool Equals(object obj) {
			var other = obj as DebuggerAppDomain;
			return other != null && other.appDomain == appDomain;
		}

		public override int GetHashCode() {
			return hashCode;
		}

		public override string ToString() {
			return debugger.Dispatcher.UI(() => appDomain.ToString());
		}
	}
}
