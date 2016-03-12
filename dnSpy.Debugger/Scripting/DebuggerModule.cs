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

using System;
using System.Linq;
using dndbg.Engine;
using dnSpy.Contracts.Scripting.Debugger;
using dnSpy.Shared.Scripting;

namespace dnSpy.Debugger.Scripting {
	sealed class DebuggerModule : IDebuggerModule {
		public ModuleName ModuleName {
			get { return moduleName; }
		}

		public IAppDomain AppDomain {
			get { return debugger.Dispatcher.UI(() => new DebuggerAppDomain(debugger, mod.AppDomain)); }
		}

		public IDebuggerAssembly Assembly {
			get { return debugger.Dispatcher.UI(() => new DebuggerAssembly(debugger, mod.Assembly)); }
		}

		public string DnlibName {
			get {
				if (dnlibName != null)
					return dnlibName;
				debugger.Dispatcher.UI(() => {
					if (dnlibName == null)
						dnlibName = mod.DnlibName;
				});
				return dnlibName;
			}
		}
		string dnlibName;

		public bool HasUnloaded {
			get { return debugger.Dispatcher.UI(() => mod.HasUnloaded); }
		}

		public int IncrementedId {
			get { return incrementedId; }
		}

		public bool IsDynamic {
			get { return moduleName.IsDynamic; }
		}

		public bool IsInMemory {
			get { return moduleName.IsInMemory; }
		}

		public bool IsManifestModule {
			get { return debugger.Dispatcher.UI(() => mod.CorModule.IsManifestModule); }
		}

		public int ModuleOrder {
			get { return moduleOrder; }
		}

		public string Name {
			get { return name; }
		}

		public string UniquerName {
			get { return debugger.Dispatcher.UI(() => mod.CorModule.UniquerName); }
		}

		public ulong Address {
			get { return address; }
		}

		public uint Size {
			get { return size; }
		}

		readonly Debugger debugger;
		readonly DnModule mod;
		readonly int hashCode;
		readonly int incrementedId;
		readonly int moduleOrder;
		readonly ulong address;
		readonly uint size;
		readonly string name;
		/*readonly*/ ModuleName moduleName;

		public DebuggerModule(Debugger debugger, DnModule mod) {
			debugger.Dispatcher.VerifyAccess();
			this.debugger = debugger;
			this.mod = mod;
			this.hashCode = mod.GetHashCode();
			this.incrementedId = mod.IncrementedId;
			this.moduleOrder = mod.ModuleOrder;
			this.name = mod.Name;
			this.address = mod.Address;
			this.size = mod.Size;
			var serMod = mod.SerializedDnModule;
			this.moduleName = new ModuleName(serMod.AssemblyFullName, serMod.ModuleName, serMod.IsDynamic, serMod.IsInMemory, serMod.ModuleNameOnly);
		}

		public IDebuggerAssembly ResolveAssembly(uint asmRefToken) {
			return debugger.Dispatcher.UI(() => {
				var corAsm = mod.CorModule.ResolveAssembly(asmRefToken);
				if (corAsm == null)
					return null;
				var asm = mod.AppDomain.Assemblies.FirstOrDefault(a => a.CorAssembly == corAsm);
				return asm == null ? null : new DebuggerAssembly(debugger, asm);
			});
		}

		public IDebuggerFunction GetFunction(uint token) {
			return debugger.Dispatcher.UI(() => {
				var func = mod.CorModule.GetFunctionFromToken(token);
				return func == null ? null : new DebuggerFunction(debugger, func);
			});
		}

		public IDebuggerClass GetClass(uint token) {
			return debugger.Dispatcher.UI(() => {
				var cls = mod.CorModule.GetClassFromToken(token);
				return cls == null ? null : new DebuggerClass(debugger, cls);
			});
		}

		public IDebuggerValue GetGlobalVariableValue(uint fdToken) {
			return debugger.Dispatcher.UI(() => {
				var value = mod.CorModule.GetGlobalVariableValue(fdToken);
				return value == null ? null : new DebuggerValue(debugger, value);
			});
		}

		public void SetJMCStatus(bool isJustMyCode) {
			debugger.Dispatcher.UI(() => mod.CorModule.SetJMCStatus(isJustMyCode));
		}

		public override bool Equals(object obj) {
			var other = obj as DebuggerModule;
			return other != null && other.mod == mod;
		}

		public override int GetHashCode() {
			return hashCode;
		}

		public override string ToString() {
			return debugger.Dispatcher.UI(() => mod.ToString());
		}
	}
}
