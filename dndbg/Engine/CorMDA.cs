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
using System.Text;
using dndbg.Engine.COM.CorDebug;

namespace dndbg.Engine {
	public sealed class CorMDA : COMObject<ICorDebugMDA>, IEquatable<CorMDA> {
		/// <summary>
		/// true if the thread on which the MDA was fired has slipped since the MDA was fired.
		/// 
		/// When the call stack no longer describes where the MDA was originally raised, the thread
		/// is considered to have slipped. This is an unusual circumstance brought about by the
		/// thread's execution of an invalid operation upon exiting.
		/// </summary>
		public bool ThreadSlipped {
			get { return (Flags & CorDebugMDAFlags.MDA_FLAG_SLIP) != 0; }
		}

		/// <summary>
		/// Gets the flags
		/// </summary>
		public CorDebugMDAFlags Flags {
			get {
				CorDebugMDAFlags flags = 0;
				int hr = obj.GetFlags(ref flags);
				return hr < 0 ? 0 : flags;
			}
		}

		/// <summary>
		/// Gets the OS thread ID. This could be a non-managed thread ID.
		/// </summary>
		public uint OSThreadId {
			get {
				uint osThreadId;
				int hr = obj.GetOSThreadId(out osThreadId);
				return hr < 0 ? 0 : osThreadId;
			}
		}

		/// <summary>
		/// Gets the name or null on error
		/// </summary>
		public string Name {
			get {
				uint cchName;
				int hr = obj.GetName(0, out cchName, null);
				StringBuilder sb = null;
				if (hr >= 0) {
					sb = new StringBuilder((int)cchName);
					hr = obj.GetName((uint)sb.MaxCapacity, out cchName, sb);
				}
				return hr < 0 ? null : sb.ToString();
			}
		}

		/// <summary>
		/// Gets the description or null on error
		/// </summary>
		public string Description {
			get {
				uint cchName;
				int hr = obj.GetDescription(0, out cchName, null);
				StringBuilder sb = null;
				if (hr >= 0) {
					sb = new StringBuilder((int)cchName);
					hr = obj.GetDescription((uint)sb.MaxCapacity, out cchName, sb);
				}
				return hr < 0 ? null : sb.ToString();
			}
		}

		/// <summary>
		/// Gets the XML or null on error
		/// </summary>
		public string XML {
			get {
				uint cchName;
				int hr = obj.GetXML(0, out cchName, null);
				StringBuilder sb = null;
				if (hr >= 0) {
					sb = new StringBuilder((int)cchName);
					hr = obj.GetXML((uint)sb.MaxCapacity, out cchName, sb);
				}
				return hr < 0 ? null : sb.ToString();
			}
		}

		public CorMDA(ICorDebugMDA code)
			: base(code) {
		}

		public static bool operator ==(CorMDA a, CorMDA b) {
			if (ReferenceEquals(a, b))
				return true;
			if (ReferenceEquals(a, null) || ReferenceEquals(b, null))
				return false;
			return a.Equals(b);
		}

		public static bool operator !=(CorMDA a, CorMDA b) {
			return !(a == b);
		}

		public bool Equals(CorMDA other) {
			return !ReferenceEquals(other, null) &&
				RawObject == other.RawObject;
		}

		public override bool Equals(object obj) {
			return Equals(obj as CorMDA);
		}

		public override int GetHashCode() {
			return RawObject.GetHashCode();
		}

		public override string ToString() {
			return string.Format("MDA: TID={0} {1}", OSThreadId, Name);
		}
	}
}
