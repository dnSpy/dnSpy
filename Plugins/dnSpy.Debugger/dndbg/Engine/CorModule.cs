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
using System.Diagnostics;
using System.Text;
using System.Threading;
using dndbg.COM.CorDebug;
using dndbg.COM.MetaData;
using dnlib.DotNet;
using dnlib.DotNet.MD;

namespace dndbg.Engine {
	public sealed class CorModule : COMObject<ICorDebugModule>, IEquatable<CorModule> {
		/// <summary>
		/// Gets the process or null
		/// </summary>
		public CorProcess Process {
			get {
				ICorDebugProcess process;
				int hr = obj.GetProcess(out process);
				return hr < 0 || process == null ? null : new CorProcess(process);
			}
		}

		/// <summary>
		/// Gets the assembly or null
		/// </summary>
		public CorAssembly Assembly {
			get {
				ICorDebugAssembly assembly;
				int hr = obj.GetAssembly(out assembly);
				return hr < 0 || assembly == null ? null : new CorAssembly(assembly);
			}
		}

		/// <summary>
		/// true if this is the manifest module
		/// </summary>
		public bool IsManifestModule {
			get {
				var mdi = this.GetMetaDataInterface<IMetaDataImport>();
				// Only the manifest module should have an assembly row
				return mdi != null && mdi.IsValidToken(new MDToken(Table.Assembly, 1).Raw);
			}
		}

		/// <summary>
		/// For on-disk modules this is a full path. For dynamic modules this is just the filename
		/// if one was provided. Otherwise, and for other in-memory modules, this is just the simple
		/// name stored in the module's metadata.
		/// </summary>
		public string Name {
			get { return name; }
		}
		readonly string name;

		/// <summary>
		/// Gets the name from the MD, which is the same as <see cref="ModuleDef.Name"/>
		/// </summary>
		public string DnlibName {
			get {
				if (dnlibName == null)
					Interlocked.CompareExchange(ref dnlibName, CalculateDnlibName(this), null);
				return dnlibName;
			}
		}
		string dnlibName;

		internal UTF8String CalculateDnlibName(CorModule module) {
			var mdi = this.GetMetaDataInterface<IMetaDataImport>();
			uint token = new MDToken(Table.Module, 1).Raw;

			return DotNet.Utils.GetUTF8String(MDAPI.GetUtf8Name(mdi, token), MDAPI.GetModuleName(mdi) ?? string.Empty);
		}

		/// <summary>
		/// Gets the name of the module. If it's an in-memory module, the hash code is included to
		/// make it uniquer since <see cref="Name"/> could have any value.
		/// </summary>
		public string UniquerName {
			get {
				if (IsInMemory)
					return string.Format("{0}[{1:X8}]", Name, GetHashCode());
				return Name;
			}
		}

		/// <summary>
		/// Gets the base address of the module or 0
		/// </summary>
		public ulong Address {
			get { return address; }
		}
		readonly ulong address;

		/// <summary>
		/// Gets the size of the module or 0
		/// </summary>
		public uint Size {
			get { return size; }
		}
		readonly uint size;

		/// <summary>
		/// Gets the token or 0
		/// </summary>
		public uint Token {
			get { return token; }
		}
		readonly uint token;

		/// <summary>
		/// true if it's a dynamic module that can add/remove types
		/// </summary>
		public bool IsDynamic {
			get { return isDynamic; }
		}
		readonly bool isDynamic;

		/// <summary>
		/// true if this is an in-memory module
		/// </summary>
		public bool IsInMemory {
			get { return isInMemory; }
		}
		readonly bool isInMemory;

		string SerializedName {
			get {
				if (!IsInMemory)
					return Name;	// filename
				return DnlibName;
			}
		}

		public SerializedDnModule SerializedDnModule {
			get {
				var asm = Assembly;
				return new SerializedDnModule(asm == null ? string.Empty : asm.FullName, SerializedName, IsDynamic, IsInMemory, false);
			}
		}

		/// <summary>
		/// Gets/sets the JIT compiler flags. The setter can only be called from the
		/// ICorDebugManagedCallback::LoadModule handler. The getter can only be called when the
		/// debugged process is synchronized (paused).
		/// </summary>
		public CorDebugJITCompilerFlags JITCompilerFlags {
			get {
				var m2 = obj as ICorDebugModule2;
				if (m2 == null)
					return 0;
				CorDebugJITCompilerFlags flags;
				int hr = m2.GetJITCompilerFlags(out flags);
				return hr < 0 ? 0 : flags;
			}
			set {
				var m2 = obj as ICorDebugModule2;
				if (m2 == null)
					return;
				int hr = m2.SetJITCompilerFlags(value);
			}
		}

		public CorModule(ICorDebugModule module)
			: base(module) {
			this.name = GetName(module) ?? string.Empty;

			int hr = module.GetBaseAddress(out this.address);
			if (hr < 0)
				this.address = 0;
			hr = module.GetSize(out this.size);
			if (hr < 0)
				this.size = 0;
			hr = module.GetToken(out this.token);
			if (hr < 0)
				this.token = 0;

			int b;
			hr = module.IsDynamic(out b);
			this.isDynamic = hr >= 0 && b != 0;
			hr = module.IsInMemory(out b);
			this.isInMemory = hr >= 0 && b != 0;

			//TODO: ICorDebugModule2::ApplyChanges
		}

		static string GetName(ICorDebugModule module) {
			uint cchName = 0;
			int hr = module.GetName(0, out cchName, null);
			if (hr < 0)
				return null;
			var sb = new StringBuilder((int)cchName);
			hr = module.GetName(cchName, out cchName, sb);
			if (hr < 0)
				return null;
			return sb.ToString();
		}

		public CorField GetFieldFromToken(uint token) {
			var mdi = this.GetMetaDataInterface<IMetaDataImport>();
			uint tdToken = 0x02000000 + MDAPI.GetFieldOwnerRid(mdi, token);
			var cls = GetClassFromToken(tdToken);
			return cls == null ? null : new CorField(cls, token);
		}

		public CorProperty GetPropertyFromToken(uint token) {
			var mdi = this.GetMetaDataInterface<IMetaDataImport>();
			uint tdToken = 0x02000000 + MDAPI.GetPropertyOwnerRid(mdi, token);
			var cls = GetClassFromToken(tdToken);
			return cls == null ? null : new CorProperty(cls, token);
		}

		public CorEvent GetEventFromToken(uint token) {
			var mdi = this.GetMetaDataInterface<IMetaDataImport>();
			uint tdToken = 0x02000000 + MDAPI.GetEventOwnerRid(mdi, token);
			var cls = GetClassFromToken(tdToken);
			return cls == null ? null : new CorEvent(cls, token);
		}

		public CorFunction GetFunctionFromToken(uint token) {
			ICorDebugFunction func;
			int hr = obj.GetFunctionFromToken(token, out func);
			return hr < 0 || func == null ? null : new CorFunction(func, this);
		}

		public void EnableJITDebugging(bool trackJITInfo, bool allowJitOpts) {
			int hr = obj.EnableJITDebugging(trackJITInfo ? 1 : 0, allowJitOpts ? 1 : 0);
		}

		public void EnableClassLoadCallbacks(bool classLoadCallbacks) {
			int hr = obj.EnableClassLoadCallbacks(classLoadCallbacks ? 1 : 0);
		}

		public void SetJMCStatus(bool isJustMyCode) {
			var m2 = obj as ICorDebugModule2;
			if (m2 == null)
				return;
			int hr = m2.SetJMCStatus(isJustMyCode ? 1 : 0, 0, IntPtr.Zero);
		}

		/// <summary>
		/// Gets the value of a global field
		/// </summary>
		/// <param name="fdToken">Token of a global field</param>
		/// <returns></returns>
		public CorValue GetGlobalVariableValue(uint fdToken) {
			ICorDebugValue value;
			int hr = obj.GetGlobalVariableValue(fdToken, out value);
			return hr < 0 || value == null ? null : new CorValue(value);
		}

		/// <summary>
		/// Gets a class
		/// </summary>
		/// <param name="token">TypeDef token</param>
		/// <returns></returns>
		public CorClass GetClassFromToken(uint token) {
			ICorDebugClass cls;
			int hr = obj.GetClassFromToken(token, out cls);
			return hr < 0 || cls == null ? null : new CorClass(cls);
		}

		/// <summary>
		/// Resolves an assembly reference. If the assembly hasn't been loaded, or if
		/// <paramref name="asmRefToken"/> is invalid, null is returned.
		/// </summary>
		/// <param name="asmRefToken">Valid assembly reference token in this module</param>
		/// <returns></returns>
		public CorAssembly ResolveAssembly(uint asmRefToken) {
			var m2 = obj as ICorDebugModule2;
			if (m2 == null)
				return null;
			ICorDebugAssembly asm;
			int hr = m2.ResolveAssembly(asmRefToken, out asm);
			return hr < 0 || asm == null ? null : new CorAssembly(asm);
		}

		/// <summary>
		/// Gets a metadata interface, eg. <see cref="IMetaDataImport"/> or <see cref="IMetaDataImport2"/>
		/// </summary>
		/// <typeparam name="T">Type of COM metadata interface</typeparam>
		/// <returns></returns>
		public T GetMetaDataInterface<T>() where T : class {
			object o;
			var riid = typeof(T).GUID;
			int hr = obj.GetMetaDataInterface(ref riid, out o);
			return o as T;
		}

		/// <summary>
		/// Finds a class
		/// </summary>
		/// <param name="name">Full class name</param>
		/// <returns></returns>
		public CorClass FindClass(string name) {
			var mdi = this.GetMetaDataInterface<IMetaDataImport>();
			foreach (var tdToken in MDAPI.GetTypeDefTokens(mdi)) {
				if (MDAPI.GetTypeDefName(mdi, tdToken) == name)
					return this.GetClassFromToken(tdToken);
			}
			return null;
		}

		/// <summary>
		/// Finds a class using a cache. Shouldn't be called if it's a dynamic module since types
		/// can be added.
		/// </summary>
		/// <param name="name">Full class name</param>
		/// <returns></returns>
		public CorClass FindClassCache(string name) {
			uint token;
			if (findClassCacheDict != null && findClassCacheDict.TryGetValue(name, out token))
				return this.GetClassFromToken(token);

			if (findClassCacheDict == null) {
				Debug.Assert(findClassCacheEnum == null);
				findClassCacheDict = new Dictionary<string, uint>(StringComparer.Ordinal);
				findClassCacheEnum = GetClasses().GetEnumerator();
			}
			else if (findClassCacheEnum == null)
				return null;
			while (findClassCacheEnum.MoveNext()) {
				var t = findClassCacheEnum.Current;
				var typeName = t.Item1;
				if (!findClassCacheDict.ContainsKey(typeName))
					findClassCacheDict[typeName] = t.Item2;
				if (typeName == name)
					return this.GetClassFromToken(t.Item2);
			}
			findClassCacheEnum.Dispose();
			findClassCacheEnum = null;
			return null;
		}
		Dictionary<string, uint> findClassCacheDict;
		IEnumerator<Tuple<string, uint>> findClassCacheEnum;

		IEnumerable<Tuple<string, uint>> GetClasses() {
			var mdi = this.GetMetaDataInterface<IMetaDataImport>();
			foreach (var tdToken in MDAPI.GetTypeDefTokens(mdi))
				yield return Tuple.Create(MDAPI.GetTypeDefName(mdi, tdToken), tdToken);
		}

		public CorType CreateTypeFromTypeDefOrRef(uint token) {
			return null;//TODO:
		}

		public static bool operator ==(CorModule a, CorModule b) {
			if (ReferenceEquals(a, b))
				return true;
			if (ReferenceEquals(a, null) || ReferenceEquals(b, null))
				return false;
			return a.Equals(b);
		}

		public static bool operator !=(CorModule a, CorModule b) {
			return !(a == b);
		}

		public bool Equals(CorModule other) {
			return !ReferenceEquals(other, null) &&
				RawObject == other.RawObject;
		}

		public override bool Equals(object obj) {
			return Equals(obj as CorModule);
		}

		public override int GetHashCode() {
			return RawObject.GetHashCode();
		}

		public override string ToString() {
			return string.Format("[Module] DYN={0} MEM={1} A={2:X8} S={3:X8} {4}", IsDynamic ? 1 : 0, IsInMemory ? 1 : 0, Address, Size, Name);
		}
	}
}
