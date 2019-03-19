/*
    Copyright (C) 2014-2019 de4dot@gmail.com

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
using System.IO;
using System.Text;
using System.Threading;
using dndbg.COM.CorDebug;
using dndbg.COM.MetaData;
using dnlib.DotNet;
using dnlib.DotNet.MD;

namespace dndbg.Engine {
	sealed class CorModule : COMObject<ICorDebugModule>, IEquatable<CorModule> {
		public CorProcess Process {
			get {
				int hr = obj.GetProcess(out var process);
				return hr < 0 || process == null ? null : new CorProcess(process);
			}
		}

		public CorAssembly Assembly {
			get {
				int hr = obj.GetAssembly(out var assembly);
				return hr < 0 || assembly == null ? null : new CorAssembly(assembly);
			}
		}

		public bool IsManifestModule => Assembly?.ManifestModule == this;

		public bool HasAssemblyRow {
			get {
				var mdi = GetMetaDataInterface<IMetaDataImport>();
				return mdi != null && mdi.IsValidToken(new MDToken(Table.Assembly, 1).Raw);
			}
		}

		/// <summary>
		/// For on-disk modules this is a full path. For dynamic modules this is just the filename
		/// if one was provided. Otherwise, and for other in-memory modules, this is just the simple
		/// name stored in the module's metadata.
		/// </summary>
		public string Name { get; }

		string DnlibName {
			get {
				if (dnlibName == null)
					Interlocked.CompareExchange(ref dnlibName, CalculateDnlibName(this), null);
				return dnlibName;
			}
		}
		string dnlibName;

		internal void ClearCachedDnlibName() => dnlibName = null;

		internal UTF8String CalculateDnlibName(CorModule module) {
			var mdi = GetMetaDataInterface<IMetaDataImport>();
			uint token = new MDToken(Table.Module, 1).Raw;

			return DotNet.Utils.GetUTF8String(MDAPI.GetUtf8Name(mdi, token), MDAPI.GetModuleName(mdi) ?? string.Empty);
		}

		public ulong Address => address;
		readonly ulong address;

		public uint Size => size;
		readonly uint size;

		public uint Token => token;
		readonly uint token;

		public bool IsDynamic { get; }
		public bool IsInMemory { get; }

		string GetSerializedName(uint id) {
			if (IsInMemory || IsDynamic) {
				// If it's a dynamic module or an in-memory module, it doesn't have a filename. The module ID
				// won't necessarily be unique so we must use an extra id.
				return DnlibName + " (id=" + id.ToString() + ")";
			}

			// Filename
			return Name;
		}

		public DnModuleId GetModuleId(uint id) => new DnModuleId(Assembly?.FullName ?? string.Empty, GetSerializedName(id), IsDynamic, IsInMemory, false);

		public CorDebugJITCompilerFlags JITCompilerFlags {
			get {
				var m2 = obj as ICorDebugModule2;
				if (m2 == null)
					return 0;
				int hr = m2.GetJITCompilerFlags(out var flags);
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
			Name = GetName(module) ?? string.Empty;

			int hr = module.GetBaseAddress(out address);
			if (hr < 0)
				address = 0;
			hr = module.GetSize(out size);
			if (hr < 0)
				size = 0;
			hr = module.GetToken(out token);
			if (hr < 0)
				token = 0;

			hr = module.IsDynamic(out int b);
			IsDynamic = hr >= 0 && b != 0;
			hr = module.IsInMemory(out b);
			IsInMemory = hr >= 0 && b != 0;
			if (!IsDynamic && !IsInMemory)
				Name = NormalizeFilename(Name);
		}

		static string NormalizeFilename(string filename) {
			if (!File.Exists(filename))
				return filename;
			try {
				return Path.GetFullPath(filename);
			}
			catch {
			}
			return filename;
		}

		static string GetName(ICorDebugModule module) {
			int hr = module.GetName(0, out uint cchName, null);
			if (hr < 0)
				return null;
			var sb = new StringBuilder((int)cchName);
			hr = module.GetName(cchName, out cchName, sb);
			if (hr < 0)
				return null;
			return sb.ToString();
		}

		public CorFunction GetFunctionFromToken(uint token) {
			int hr = obj.GetFunctionFromToken(token, out var func);
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

		public CorClass GetClassFromToken(uint token) {
			int hr = obj.GetClassFromToken(token, out var cls);
			return hr < 0 || cls == null ? null : new CorClass(cls);
		}

		public T GetMetaDataInterface<T>() where T : class {
			var riid = typeof(T).GUID;
			int hr = obj.GetMetaDataInterface(ref riid, out object o);
			return o as T;
		}

		public static bool operator ==(CorModule a, CorModule b) {
			if (ReferenceEquals(a, b))
				return true;
			if (ReferenceEquals(a, null) || ReferenceEquals(b, null))
				return false;
			return a.Equals(b);
		}

		public static bool operator !=(CorModule a, CorModule b) => !(a == b);
		public bool Equals(CorModule other) => !ReferenceEquals(other, null) && RawObject == other.RawObject;
		public override bool Equals(object obj) => Equals(obj as CorModule);
		public override int GetHashCode() => RawObject.GetHashCode();
		public override string ToString() => $"[Module] DYN={(IsDynamic ? 1 : 0)} MEM={(IsInMemory ? 1 : 0)} A={Address:X8} S={Size:X8} {Name}";
	}
}
