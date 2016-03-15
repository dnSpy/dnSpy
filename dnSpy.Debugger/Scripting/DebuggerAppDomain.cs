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
using System.Collections.Generic;
using System.Linq;
using dndbg.Engine;
using dnlib.DotNet;
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

		public IEnumerable<IDebuggerThread> Threads {
			get { return debugger.Dispatcher.UIIter(GetThreadsUI); }
		}

		IEnumerable<IDebuggerThread> GetThreadsUI() {
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

		public IDebuggerValue CLRObject {
			get {
				return debugger.Dispatcher.UI(() => {
					var value = appDomain.CorAppDomain.Object;
					return value == null ? null : new DebuggerValue(debugger, value);
				});
			}
		}

		public IDebuggerModule CorLib {
			get {
				if (corLib != null)
					return corLib;
				return debugger.Dispatcher.UI(() => {
					if (corLib != null)
						return corLib;
					corLib = Modules.FirstOrDefault();
					return corLib;
				});
			}
		}
		IDebuggerModule corLib;

		public DnAppDomain DnAppDomain {
			get { return appDomain; }
		}
		readonly DnAppDomain appDomain;

		readonly Debugger debugger;
		readonly int hashCode;
		readonly int id;

		public DebuggerAppDomain(Debugger debugger, DnAppDomain appDomain) {
			debugger.Dispatcher.VerifyAccess();
			this.debugger = debugger;
			this.appDomain = appDomain;
			this.hashCode = appDomain.GetHashCode();
			this.id = appDomain.Id;
		}

		public void SetAllThreadsDebugState(ThreadState state, IDebuggerThread thread) {
			debugger.Dispatcher.UI(() => appDomain.CorAppDomain.SetAllThreadsDebugState((dndbg.COM.CorDebug.CorDebugThreadState)state, thread == null ? null : ((DebuggerThread)thread).DnThread.CorThread));
		}

		public IDebuggerModule FindModule(ModuleName name) {
			return debugger.Dispatcher.UI(() => {
				foreach (var m in Modules) {
					if (name == m.ModuleName)
						return m;
				}
				return null;
			});
		}

		public IDebuggerModule FindModuleByName(string name) {
			return debugger.Dispatcher.UI(() => {
				foreach (var m in Modules) {
					if (Utils.IsSameFile(m.ModuleName.Name, name))
						return m;
				}
				return null;
			});
		}

		public IDebuggerAssembly FindAssembly(string name) {
			return debugger.Dispatcher.UI(() => {
				foreach (var a in Assemblies) {
					if (Utils.IsSameFile(a.Name, name))
						return a;
					if (StringComparer.OrdinalIgnoreCase.Equals(a.FullName, name))
						return a;
					if (StringComparer.OrdinalIgnoreCase.Equals(new AssemblyNameInfo(a.FullName).Name, name))
						return a;
				}
				return null;
			});
		}

		public IDebuggerClass FindClass(string modName, string className) {
			return debugger.Dispatcher.UI(() => {
				var mod = FindModuleByName(modName);
				return mod == null ? null : mod.FindClass(className);
			});
		}

		public IDebuggerFunction FindMethod(string modName, string className, string methodName) {
			return debugger.Dispatcher.UI(() => {
				var mod = FindModuleByName(modName);
				return mod == null ? null : mod.FindMethod(className, methodName);
			});
		}

		public IDebuggerType CreateRefType(string modName, string className) {
			return debugger.Dispatcher.UI(() => {
				var mod = FindModuleByName(modName);
				return mod == null ? null : mod.CreateRefType(className);
			});
		}

		public IDebuggerType CreateValueType(string modName, string className) {
			return debugger.Dispatcher.UI(() => {
				var mod = FindModuleByName(modName);
				return mod == null ? null : mod.CreateValueType(className);
			});
		}

		public IDebuggerType CreateRefType(string modName, string className, params IDebuggerType[] genericArguments) {
			return debugger.Dispatcher.UI(() => {
				var mod = FindModuleByName(modName);
				return mod == null ? null : mod.CreateRefType(className, genericArguments);
			});
		}

		public IDebuggerType CreateValueType(string modName, string className, params IDebuggerType[] genericArguments) {
			return debugger.Dispatcher.UI(() => {
				var mod = FindModuleByName(modName);
				return mod == null ? null : mod.CreateValueType(className, genericArguments);
			});
		}

		public IDebuggerType CreateFnPtr(params IDebuggerType[] types) {
			return debugger.Dispatcher.UI(() => {
				var type = appDomain.CorAppDomain.GetFnPtr(types.ToCorType());
				return type == null ? null : new DebuggerType(debugger, type);
			});
		}

		public IDebuggerType CreateFunctionPointer(params IDebuggerType[] types) {
			return CreateFnPtr(types);
		}

		IDebuggerType IAppDomain.Void {
			get {
				if (_Void != null)
					return _Void;
				return debugger.Dispatcher.UI(() => {
					if (_Void != null)
						return _Void;
					var mod = CorLib;
					return _Void = mod == null ? null : mod.CreateValueType("System.Void");
				});
			}
		}
		IDebuggerType _Void;

		IDebuggerType IAppDomain.Boolean {
			get {
				if (_Boolean != null)
					return _Boolean;
				return debugger.Dispatcher.UI(() => {
					if (_Boolean != null)
						return _Boolean;
					var mod = CorLib;
					return _Boolean = mod == null ? null : mod.CreateValueType("System.Boolean");
				});
			}
		}
		IDebuggerType _Boolean;

		IDebuggerType IAppDomain.Char {
			get {
				if (_Char != null)
					return _Char;
				return debugger.Dispatcher.UI(() => {
					if (_Char != null)
						return _Char;
					var mod = CorLib;
					return _Char = mod == null ? null : mod.CreateValueType("System.Char");
				});
			}
		}
		IDebuggerType _Char;

		IDebuggerType IAppDomain.SByte {
			get {
				if (_SByte != null)
					return _SByte;
				return debugger.Dispatcher.UI(() => {
					if (_SByte != null)
						return _SByte;
					var mod = CorLib;
					return _SByte = mod == null ? null : mod.CreateValueType("System.SByte");
				});
			}
		}
		IDebuggerType _SByte;

		IDebuggerType IAppDomain.Byte {
			get {
				if (_Byte != null)
					return _Byte;
				return debugger.Dispatcher.UI(() => {
					if (_Byte != null)
						return _Byte;
					var mod = CorLib;
					return _Byte = mod == null ? null : mod.CreateValueType("System.Byte");
				});
			}
		}
		IDebuggerType _Byte;

		IDebuggerType IAppDomain.Int16 {
			get {
				if (_Int16 != null)
					return _Int16;
				return debugger.Dispatcher.UI(() => {
					if (_Int16 != null)
						return _Int16;
					var mod = CorLib;
					return _Int16 = mod == null ? null : mod.CreateValueType("System.Int16");
				});
			}
		}
		IDebuggerType _Int16;

		IDebuggerType IAppDomain.UInt16 {
			get {
				if (_UInt16 != null)
					return _UInt16;
				return debugger.Dispatcher.UI(() => {
					if (_UInt16 != null)
						return _UInt16;
					var mod = CorLib;
					return _UInt16 = mod == null ? null : mod.CreateValueType("System.UInt16");
				});
			}
		}
		IDebuggerType _UInt16;

		IDebuggerType IAppDomain.Int32 {
			get {
				if (_Int32 != null)
					return _Int32;
				return debugger.Dispatcher.UI(() => {
					if (_Int32 != null)
						return _Int32;
					var mod = CorLib;
					return _Int32 = mod == null ? null : mod.CreateValueType("System.Int32");
				});
			}
		}
		IDebuggerType _Int32;

		IDebuggerType IAppDomain.UInt32 {
			get {
				if (_UInt32 != null)
					return _UInt32;
				return debugger.Dispatcher.UI(() => {
					if (_UInt32 != null)
						return _UInt32;
					var mod = CorLib;
					return _UInt32 = mod == null ? null : mod.CreateValueType("System.UInt32");
				});
			}
		}
		IDebuggerType _UInt32;

		IDebuggerType IAppDomain.Int64 {
			get {
				if (_Int64 != null)
					return _Int64;
				return debugger.Dispatcher.UI(() => {
					if (_Int64 != null)
						return _Int64;
					var mod = CorLib;
					return _Int64 = mod == null ? null : mod.CreateValueType("System.Int64");
				});
			}
		}
		IDebuggerType _Int64;

		IDebuggerType IAppDomain.UInt64 {
			get {
				if (_UInt64 != null)
					return _UInt64;
				return debugger.Dispatcher.UI(() => {
					if (_UInt64 != null)
						return _UInt64;
					var mod = CorLib;
					return _UInt64 = mod == null ? null : mod.CreateValueType("System.UInt64");
				});
			}
		}
		IDebuggerType _UInt64;

		IDebuggerType IAppDomain.Single {
			get {
				if (_Single != null)
					return _Single;
				return debugger.Dispatcher.UI(() => {
					if (_Single != null)
						return _Single;
					var mod = CorLib;
					return _Single = mod == null ? null : mod.CreateValueType("System.Single");
				});
			}
		}
		IDebuggerType _Single;

		IDebuggerType IAppDomain.Double {
			get {
				if (_Double != null)
					return _Double;
				return debugger.Dispatcher.UI(() => {
					if (_Double != null)
						return _Double;
					var mod = CorLib;
					return _Double = mod == null ? null : mod.CreateValueType("System.Double");
				});
			}
		}
		IDebuggerType _Double;

		IDebuggerType IAppDomain.String {
			get {
				if (_String != null)
					return _String;
				return debugger.Dispatcher.UI(() => {
					if (_String != null)
						return _String;
					var mod = CorLib;
					return _String = mod == null ? null : mod.CreateRefType("System.String");
				});
			}
		}
		IDebuggerType _String;

		IDebuggerType IAppDomain.TypedReference {
			get {
				if (_TypedReference != null)
					return _TypedReference;
				return debugger.Dispatcher.UI(() => {
					if (_TypedReference != null)
						return _TypedReference;
					var mod = CorLib;
					return _TypedReference = mod == null ? null : mod.CreateValueType("System.TypedReference");
				});
			}
		}
		IDebuggerType _TypedReference;

		IDebuggerType IAppDomain.IntPtr {
			get {
				if (_IntPtr != null)
					return _IntPtr;
				return debugger.Dispatcher.UI(() => {
					if (_IntPtr != null)
						return _IntPtr;
					var mod = CorLib;
					return _IntPtr = mod == null ? null : mod.CreateValueType("System.IntPtr");
				});
			}
		}
		IDebuggerType _IntPtr;

		IDebuggerType IAppDomain.UIntPtr {
			get {
				if (_UIntPtr != null)
					return _UIntPtr;
				return debugger.Dispatcher.UI(() => {
					if (_UIntPtr != null)
						return _UIntPtr;
					var mod = CorLib;
					return _UIntPtr = mod == null ? null : mod.CreateValueType("System.UIntPtr");
				});
			}
		}
		IDebuggerType _UIntPtr;

		IDebuggerType IAppDomain.Object {
			get {
				if (_Object != null)
					return _Object;
				return debugger.Dispatcher.UI(() => {
					if (_Object != null)
						return _Object;
					var mod = CorLib;
					return _Object = mod == null ? null : mod.CreateRefType("System.Object");
				});
			}
		}
		IDebuggerType _Object;

		IDebuggerType IAppDomain.Decimal {
			get {
				if (_Decimal != null)
					return _Decimal;
				return debugger.Dispatcher.UI(() => {
					if (_Decimal != null)
						return _Decimal;
					var mod = CorLib;
					return _Decimal = mod == null ? null : mod.CreateValueType("System.Decimal");
				});
			}
		}
		IDebuggerType _Decimal;

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
