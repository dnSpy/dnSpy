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
using System.Linq;
using dndbg.Engine;
using dnSpy.Contracts.Scripting.Debugger;
using dnSpy.Shared.Scripting;

namespace dnSpy.Debugger.Scripting {
	sealed class DebuggerAssembly : IDebuggerAssembly {
		public IAppDomain AppDomain {
			get { return debugger.Dispatcher.UI(() => new DebuggerAppDomain(debugger, asm.AppDomain)); }
		}

		public string FullName {
			get { return debugger.Dispatcher.UI(() => asm.FullName); }
		}

		public bool HasUnloaded {
			get { return debugger.Dispatcher.UI(() => asm.HasUnloaded); }
		}

		public int UniqueId {
			get { return uniqueId; }
		}

		public bool IsFullyTrusted {
			get { return debugger.Dispatcher.UI(() => asm.CorAssembly.IsFullyTrusted); }
		}

		public IDebuggerModule ManifestModule {
			get {
				return debugger.Dispatcher.UI(() => {
					var manifestModule = asm.CorAssembly.ManifestModule;
					if (manifestModule == null)
						return null;
					var mod = asm.Modules.FirstOrDefault(a => a.CorModule == manifestModule);
					return mod == null ? null : new DebuggerModule(debugger, mod);
				});
			}
		}

		public IEnumerable<IDebuggerModule> Modules {
			get { return debugger.Dispatcher.UIIter(GetModulesUI); }
		}

		IEnumerable<IDebuggerModule> GetModulesUI() {
			foreach (var m in asm.Modules)
				yield return new DebuggerModule(debugger, m);
		}

		public string Name {
			get { return name; }
		}

		readonly Debugger debugger;
		readonly DnAssembly asm;
		readonly int hashCode;
		readonly int uniqueId;
		readonly string name;

		public DebuggerAssembly(Debugger debugger, DnAssembly asm) {
			debugger.Dispatcher.VerifyAccess();
			this.debugger = debugger;
			this.asm = asm;
			this.hashCode = asm.GetHashCode();
			this.uniqueId = asm.UniqueId;
			this.name = asm.Name;
		}

		public override bool Equals(object obj) {
			var other = obj as DebuggerAssembly;
			return other != null && other.asm == asm;
		}

		public override int GetHashCode() {
			return hashCode;
		}

		public override string ToString() {
			return debugger.Dispatcher.UI(() => asm.ToString());
		}
	}
}
