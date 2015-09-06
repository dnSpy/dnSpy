/*
    Copyright (C) 2014-2015 de4dot@gmail.com

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
using System.Text;
using dndbg.Engine.COM.CorDebug;

namespace dndbg.Engine {
	public sealed class CorAssembly : COMObject<ICorDebugAssembly>, IEquatable<CorAssembly> {
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
		/// Gets the AppDomain or null
		/// </summary>
		public CorAppDomain AppDomain {
			get {
				ICorDebugAppDomain appDomain;
				int hr = obj.GetAppDomain(out appDomain);
				return hr < 0 || appDomain == null ? null : new CorAppDomain(appDomain);
			}
		}

		/// <summary>
		/// Gets all modules
		/// </summary>
		public IEnumerable<CorModule> Modules {
			get {
				ICorDebugModuleEnum moduleEnum;
				int hr = obj.EnumerateModules(out moduleEnum);
				if (hr < 0)
					yield break;
				for (;;) {
					ICorDebugModule module = null;
					uint count;
					hr = moduleEnum.Next(1, out module, out count);
					if (hr != 0 || module == null)
						break;
					yield return new CorModule(module);
				}
			}
		}

		/// <summary>
		/// true if the assembly has been granted full trust by the runtime security system
		/// </summary>
		public bool IsFullyTrusted {
			get {
				var asm2 = obj as ICorDebugAssembly2;
				if (asm2 == null)
					return false;
				int ft;
				int hr = asm2.IsFullyTrusted(out ft);
				return hr >= 0 && ft != 0;
			}
		}

		/// <summary>
		/// Assembly name, and is usually the full path to the manifest (first) module on disk
		/// (the EXE or DLL file).
		/// </summary>
		public string Name {
			get { return name; }
		}
		readonly string name;

		internal CorAssembly(ICorDebugAssembly assembly)
			: base(assembly) {
			this.name = GetName(assembly) ?? string.Empty;
		}

		static string GetName(ICorDebugAssembly assembly) {
			uint cchName = 0;
			int hr = assembly.GetName(0, out cchName, null);
			if (hr < 0)
				return null;
			var sb = new StringBuilder((int)cchName);
			hr = assembly.GetName(cchName, out cchName, sb);
			if (hr < 0)
				return null;
			return sb.ToString();
		}

		public static bool operator ==(CorAssembly a, CorAssembly b) {
			if (ReferenceEquals(a, b))
				return true;
			if (ReferenceEquals(a, null) || ReferenceEquals(b, null))
				return false;
			return a.Equals(b);
		}

		public static bool operator !=(CorAssembly a, CorAssembly b) {
			return !(a == b);
		}

		public bool Equals(CorAssembly other) {
			return !ReferenceEquals(other, null) &&
				RawObject == other.RawObject;
		}

		public override bool Equals(object obj) {
			return Equals(obj as CorAssembly);
		}

		public override int GetHashCode() {
			return RawObject.GetHashCode();
		}

		public override string ToString() {
			return string.Format("[Assembly] {0}", Name);
		}
	}
}
