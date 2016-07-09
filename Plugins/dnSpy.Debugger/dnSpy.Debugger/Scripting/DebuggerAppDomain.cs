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
using System.IO;
using System.Linq;
using System.Reflection;
using dndbg.Engine;
using dnlib.DotNet;
using dnSpy.Contracts.Scripting;
using dnSpy.Contracts.Scripting.Debugger;

namespace dnSpy.Debugger.Scripting {
	sealed class DebuggerAppDomain : IAppDomain {
		public int Id { get; }
		public bool IsAttached => debugger.Dispatcher.UI(() => this.DnAppDomain.CorAppDomain.IsAttached);
		public bool IsRunning => debugger.Dispatcher.UI(() => this.DnAppDomain.CorAppDomain.IsRunning);
		public string Name => debugger.Dispatcher.UI(() => this.DnAppDomain.Name);
		public bool HasExited => debugger.Dispatcher.UI(() => this.DnAppDomain.HasExited);
		public IEnumerable<IDebuggerThread> Threads => debugger.Dispatcher.UIIter(GetThreadsUI);

		IEnumerable<IDebuggerThread> GetThreadsUI() {
			foreach (var t in DnAppDomain.Threads)
				yield return new DebuggerThread(debugger, t);
		}

		public IEnumerable<IDebuggerAssembly> Assemblies => debugger.Dispatcher.UIIter(GetAssembliesUI);

		IEnumerable<IDebuggerAssembly> GetAssembliesUI() {
			foreach (var a in DnAppDomain.Assemblies)
				yield return new DebuggerAssembly(debugger, a);
		}

		public IEnumerable<IDebuggerModule> Modules => debugger.Dispatcher.UIIter(GetModulesUI);

		IEnumerable<IDebuggerModule> GetModulesUI() {
			foreach (var m in DnAppDomain.Modules)
				yield return new DebuggerModule(debugger, m);
		}

		public IDebuggerValue CLRObject => debugger.Dispatcher.UI(() => {
			var value = this.DnAppDomain.CorAppDomain.Object;
			return value == null ? null : new DebuggerValue(debugger, value);
		});

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

		public DnAppDomain DnAppDomain { get; }

		readonly Debugger debugger;
		readonly int hashCode;

		public DebuggerAppDomain(Debugger debugger, DnAppDomain appDomain) {
			debugger.Dispatcher.VerifyAccess();
			this.debugger = debugger;
			this.DnAppDomain = appDomain;
			this.hashCode = appDomain.GetHashCode();
			this.Id = appDomain.Id;
		}

		public void SetAllThreadsDebugState(ThreadState state, IDebuggerThread thread) =>
			debugger.Dispatcher.UI(() => DnAppDomain.CorAppDomain.SetAllThreadsDebugState((dndbg.COM.CorDebug.CorDebugThreadState)state, thread == null ? null : ((DebuggerThread)thread).DnThread.CorThread));

		public IDebuggerModule GetModule(Module module) => debugger.Dispatcher.UI(() => {
			if (File.Exists(module.FullyQualifiedName)) {
				var name = Path.GetFileName(module.FullyQualifiedName);
				foreach (var m in Modules) {
					if (m.IsInMemory)
						continue;
					if (Utils.IsSameFile(m.ModuleName.Name, name))
						return m;
				}
				return null;
			}
			return null;
		});

		public IDebuggerModule GetModule(ModuleName name) => debugger.Dispatcher.UI(() => {
			foreach (var m in Modules) {
				if (name == m.ModuleName)
					return m;
			}
			return null;
		});

		public IDebuggerModule GetModuleByName(string name) => debugger.Dispatcher.UI(() => {
			foreach (var m in Modules) {
				if (Utils.IsSameFile(m.ModuleName.Name, name))
					return m;
			}
			return null;
		});

		public IDebuggerAssembly GetAssembly(Assembly asm) => debugger.Dispatcher.UI(() => {
			string fn = null;
			bool hasLoc = File.Exists(asm.Location);
			if (hasLoc)
				fn = Path.GetFileName(asm.Location);
			var asmName = asm.GetName().Name;
			foreach (var a in Assemblies) {
				if (hasLoc && Utils.IsSameFile(a.Name, fn))
					return a;
				if (StringComparer.OrdinalIgnoreCase.Equals(new AssemblyNameInfo(a.FullName).Name, asmName))
					return a;
			}
			return null;
		});

		public IDebuggerAssembly GetAssembly(string name) => debugger.Dispatcher.UI(() => {
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

		public IDebuggerClass GetClass(string modName, string className) => debugger.Dispatcher.UI(() => GetModuleByName(modName)?.GetClass(className));
		public IDebuggerMethod GetMethod(string modName, string className, string methodName) => debugger.Dispatcher.UI(() => GetModuleByName(modName)?.GetMethod(className, methodName));
		public IDebuggerField GetField(string modName, string className, string fieldName) => debugger.Dispatcher.UI(() => GetModuleByName(modName)?.GetField(className, fieldName));
		public IDebuggerProperty GetProperty(string modName, string className, string propertyName) => debugger.Dispatcher.UI(() => GetModuleByName(modName)?.GetProperty(className, propertyName));
		public IDebuggerEvent GetEvent(string modName, string className, string eventName) => debugger.Dispatcher.UI(() => GetModuleByName(modName)?.GetEvent(className, eventName));
		public IDebuggerMethod GetMethod(string modName, uint token) => debugger.Dispatcher.UI(() => GetModuleByName(modName)?.GetMethod(token));
		public IDebuggerField GetField(string modName, uint token) => debugger.Dispatcher.UI(() => GetModuleByName(modName)?.GetField(token));
		public IDebuggerProperty GetProperty(string modName, uint token) => debugger.Dispatcher.UI(() => GetModuleByName(modName)?.GetProperty(token));
		public IDebuggerEvent GetEvent(string modName, uint token) => debugger.Dispatcher.UI(() => GetModuleByName(modName)?.GetEvent(token));
		public IDebuggerType GetType(string modName, string className) => debugger.Dispatcher.UI(() => GetModuleByName(modName)?.GetType(className));
		public IDebuggerType GetType(string modName, string className, params IDebuggerType[] genericArguments) => debugger.Dispatcher.UI(() => GetModuleByName(modName)?.GetType(className, genericArguments));

		public IDebuggerType GetType(Type type) => debugger.Dispatcher.UI(() => {
			if (type.IsPointer) {
				var r = GetType(type.GetElementType());
				return r?.ToPointer();
			}
			else if (type.IsArray) {
				var r = GetType(type.GetElementType());
				if (r == null)
					return null;
				if (type.FullName.EndsWith("[]"))
					return r.ToSZArray();
				return r.ToArray(type.GetArrayRank());
			}
			else if (type.IsByRef) {
				var r = GetType(type.GetElementType());
				return r?.ToByRef();
			}
			else {
				var mod = GetModule(type.Module);
				if (mod == null)
					return null;
				if (type.IsGenericType)
					return mod.GetType(type.GetGenericTypeDefinition().FullName, GetTypes(type.GetGenericArguments()));
				return mod.GetType(type.FullName);
			}
		});

		IDebuggerType[] GetTypes(Type[] types) {
			var res = new List<IDebuggerType>(types.Length);
			for (int i = 0; i < res.Count; i++) {
				var t = GetType(types[i]);
				if (t != null)
					res.Add(t);
			}
			return res.ToArray();
		}

		public IDebuggerField GetField(FieldInfo field) => debugger.Dispatcher.UI(() => GetModule(field.Module)?.GetField(field));
		public IDebuggerMethod GetMethod(MethodBase method) => debugger.Dispatcher.UI(() => GetModule(method.Module)?.GetMethod(method));
		public IDebuggerProperty GetProperty(PropertyInfo prop) => debugger.Dispatcher.UI(() => GetModule(prop.Module)?.GetProperty(prop));
		public IDebuggerEvent GetEvent(EventInfo evt) => debugger.Dispatcher.UI(() => GetModule(evt.Module)?.GetEvent(evt));

		public IDebuggerType CreateFnPtr(params IDebuggerType[] types) => debugger.Dispatcher.UI(() => {
			var type = this.DnAppDomain.CorAppDomain.GetFnPtr(types.ToCorTypes());
			return type == null ? null : new DebuggerType(debugger, type);
		});

		public IDebuggerType CreateFunctionPointer(params IDebuggerType[] types) => CreateFnPtr(types);

		IDebuggerType IAppDomain.Void {
			get {
				if (_Void != null)
					return _Void;
				return debugger.Dispatcher.UI(() => {
					if (_Void != null)
						return _Void;
					var mod = CorLib;
					return _Void = mod?.GetType("System.Void");
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
					return _Boolean = mod?.GetType("System.Boolean");
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
					return _Char = mod?.GetType("System.Char");
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
					return _SByte = mod?.GetType("System.SByte");
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
					return _Byte = mod?.GetType("System.Byte");
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
					return _Int16 = mod?.GetType("System.Int16");
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
					return _UInt16 = mod?.GetType("System.UInt16");
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
					return _Int32 = mod?.GetType("System.Int32");
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
					return _UInt32 = mod?.GetType("System.UInt32");
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
					return _Int64 = mod?.GetType("System.Int64");
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
					return _UInt64 = mod?.GetType("System.UInt64");
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
					return _Single = mod?.GetType("System.Single");
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
					return _Double = mod?.GetType("System.Double");
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
					return _String = mod?.GetType("System.String");
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
					return _TypedReference = mod?.GetType("System.TypedReference");
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
					return _IntPtr = mod?.GetType("System.IntPtr");
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
					return _UIntPtr = mod?.GetType("System.UIntPtr");
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
					return _Object = mod?.GetType("System.Object");
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
					return _Decimal = mod?.GetType("System.Decimal");
				});
			}
		}
		IDebuggerType _Decimal;

		public override bool Equals(object obj) => (obj as DebuggerAppDomain)?.DnAppDomain == DnAppDomain;
		public override int GetHashCode() => hashCode;
		public override string ToString() => debugger.Dispatcher.UI(() => this.DnAppDomain.ToString());
	}
}
