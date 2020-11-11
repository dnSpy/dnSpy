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
	sealed class CorMDA : COMObject<ICorDebugMDA>, IEquatable<CorMDA?> {
		public CorDebugMDAFlags Flags {
			get {
				CorDebugMDAFlags flags = 0;
				int hr = obj.GetFlags(ref flags);
				return hr < 0 ? 0 : flags;
			}
		}

		public uint OSThreadId {
			get {
				int hr = obj.GetOSThreadId(out uint osThreadId);
				return hr < 0 ? 0 : osThreadId;
			}
		}

		public string? Name {
			get {
				int hr = obj.GetName(0, out uint cchName, null);
				StringBuilder? sb = null;
				if (hr >= 0) {
					sb = new StringBuilder((int)cchName);
					hr = obj.GetName((uint)sb.Capacity, out cchName, sb);
				}
				return hr < 0 ? null : sb!.ToString();
			}
		}

		public string? Description {
			get {
				int hr = obj.GetDescription(0, out uint cchName, null);
				StringBuilder? sb = null;
				if (hr >= 0) {
					sb = new StringBuilder((int)cchName);
					hr = obj.GetDescription((uint)sb.Capacity, out cchName, sb);
				}
				return hr < 0 ? null : sb!.ToString();
			}
		}

		public string? XML {
			get {
				int hr = obj.GetXML(0, out uint cchName, null);
				StringBuilder? sb = null;
				if (hr >= 0) {
					sb = new StringBuilder((int)cchName);
					hr = obj.GetXML((uint)sb.Capacity, out cchName, sb);
				}
				return hr < 0 ? null : sb!.ToString();
			}
		}

		public CorMDA(ICorDebugMDA code)
			: base(code) {
		}

		public bool Equals(CorMDA? other) => other is not null && RawObject == other.RawObject;
		public override bool Equals(object? obj) => Equals(obj as CorMDA);
		public override int GetHashCode() => RawObject.GetHashCode();
		public override string ToString() => $"MDA: TID={OSThreadId} {Name}";
	}
}
