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
using System.Text;
using dndbg.COM.CorDebug;

namespace dndbg.Engine {
	sealed class CorAppDomain : COMObject<ICorDebugAppDomain>, IEquatable<CorAppDomain> {
		public CorProcess Process {
			get {
				int hr = obj.GetProcess(out var process);
				return hr < 0 || process == null ? null : new CorProcess(process);
			}
		}

		public int Id => id;
		readonly int id;

		public string Name => GetName(obj) ?? string.Empty;

		static string GetName(ICorDebugAppDomain appDomain) {
			int hr = appDomain.GetName(0, out uint cchName, null);
			if (hr < 0)
				return null;
			var sb = new StringBuilder((int)cchName);
			hr = appDomain.GetName(cchName, out cchName, sb);
			if (hr < 0)
				return null;
			return sb.ToString();
		}

		public CorAppDomain(ICorDebugAppDomain appDomain)
			: base(appDomain) {
			int hr = appDomain.GetID(out id);
			if (hr < 0)
				id = -1;
		}

		public CorType GetPtr(CorType type) {
			var ad2 = obj as ICorDebugAppDomain2;
			if (ad2 == null)
				return null;
			int hr = ad2.GetArrayOrPointerType(CorElementType.Ptr, 0, type.RawObject, out var res);
			return res == null ? null : new CorType(res);
		}

		public CorType GetByRef(CorType type) {
			var ad2 = obj as ICorDebugAppDomain2;
			if (ad2 == null)
				return null;
			int hr = ad2.GetArrayOrPointerType(CorElementType.ByRef, 0, type.RawObject, out var res);
			return res == null ? null : new CorType(res);
		}

		public CorType GetSZArray(CorType type) {
			var ad2 = obj as ICorDebugAppDomain2;
			if (ad2 == null)
				return null;
			int hr = ad2.GetArrayOrPointerType(CorElementType.SZArray, 1, type.RawObject, out var res);
			return res == null ? null : new CorType(res);
		}

		public CorType GetArray(CorType type, uint rank) {
			var ad2 = obj as ICorDebugAppDomain2;
			if (ad2 == null)
				return null;
			int hr = ad2.GetArrayOrPointerType(CorElementType.Array, rank, type.RawObject, out var res);
			return res == null ? null : new CorType(res);
		}

		public CorType GetFnPtr(CorType[] args) {
			var ad2 = obj as ICorDebugAppDomain2;
			if (ad2 == null)
				return null;
			int hr = ad2.GetFunctionPointerType(args.Length, args.ToCorDebugArray(), out var res);
			return res == null ? null : new CorType(res);
		}

		public static bool operator ==(CorAppDomain a, CorAppDomain b) {
			if (ReferenceEquals(a, b))
				return true;
			if (ReferenceEquals(a, null) || ReferenceEquals(b, null))
				return false;
			return a.Equals(b);
		}

		public static bool operator !=(CorAppDomain a, CorAppDomain b) => !(a == b);
		public bool Equals(CorAppDomain other) => !ReferenceEquals(other, null) && RawObject == other.RawObject;
		public override bool Equals(object obj) => Equals(obj as CorAppDomain);
		public override int GetHashCode() => RawObject.GetHashCode();
		public override string ToString() => $"[AppDomain] {Id} {Name}";
	}
}
