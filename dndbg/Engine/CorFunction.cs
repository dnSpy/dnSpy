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
using dndbg.Engine.COM.CorDebug;

namespace dndbg.Engine {
	public sealed class CorFunction : COMObject<ICorDebugFunction>, IEquatable<CorFunction> {
		/// <summary>
		/// Gets the module or null
		/// </summary>
		public CorModule Module {
			get {
				ICorDebugModule module;
				int hr = obj.GetModule(out module);
				return hr < 0 || module == null ? null : new CorModule(module);
			}
		}

		/// <summary>
		/// Gets the class or null
		/// </summary>
		public CorClass Class {
			get {
				ICorDebugClass cls;
				int hr = obj.GetClass(out cls);
				return hr < 0 || cls == null ? null : new CorClass(cls);
			}
		}

		/// <summary>
		/// Gets the token or 0
		/// </summary>
		public uint Token {
			get {
				uint token;
				int hr = obj.GetToken(out token);
				return hr < 0 ? 0 : token;
			}
		}

		/// <summary>
		/// Gets/sets JMC (just my code) flag
		/// </summary>
		public bool JustMyCode {
			get {
				var func2 = obj as ICorDebugFunction2;
				if (func2 == null)
					return false;
				int status;
				int hr = func2.GetJMCStatus(out status);
				return hr >= 0 && status != 0;
			}
			set {
				var func2 = obj as ICorDebugFunction2;
				if (func2 == null)
					return;
				int hr = func2.SetJMCStatus(value ? 1 : 0);
			}
		}

		/// <summary>
		/// Gets EnC (edit and continue) version number of the latest edit, and might be greater
		/// than this function's version number. See <see cref="VersionNumber"/>.
		/// </summary>
		public uint CurrentVersionNumber {
			get {
				uint ver;
				int hr = obj.GetCurrentVersionNumber(out ver);
				return hr < 0 ? 0 : ver;
			}
		}

		/// <summary>
		/// Gets the EnC (edit and continue) version number of this function
		/// </summary>
		public uint VersionNumber {
			get {
				var func2 = obj as ICorDebugFunction2;
				if (func2 == null)
					return CurrentVersionNumber;
				uint ver;
				int hr = func2.GetVersionNumber(out ver);
				return hr < 0 ? 0 : ver;
			}
		}

		/// <summary>
		/// Gets the local variables signature token or 0 if none
		/// </summary>
		public uint LocalVarSigToken {
			get {
				uint token;
				int hr = obj.GetLocalVarSigToken(out token);
				return hr < 0 ? 0 : token;
			}
		}

		/// <summary>
		/// Gets the IL code or null
		/// </summary>
		public CorCode ILCode {
			get {
				ICorDebugCode code;
				int hr = obj.GetILCode(out code);
				return hr < 0 || code == null ? null : new CorCode(code);
			}
		}

		/// <summary>
		/// Gets the native code or null. If it's a generic method that's been JITed more than once,
		/// the returned code could be any one of the JITed codes.
		/// </summary>
		/// <remarks><c>EnumerateNativeCode()</c> should be called but that method hasn't been
		/// implemented by the CLR debugger yet.</remarks>
		public CorCode NativeCode {
			get {
				ICorDebugCode code;
				int hr = obj.GetNativeCode(out code);
				return hr < 0 || code == null ? null : new CorCode(code);
			}
		}

		internal CorFunction(ICorDebugFunction func)
			: base(func) {
			//TODO: ICorDebugFunction2::EnumerateNativeCode
			//TODO: ICorDebugFunction3::GetActiveReJitRequestILCode
		}

		/// <summary>
		/// Creates a function breakpoint at the beginning of the function
		/// </summary>
		/// <returns></returns>
		public CorFunctionBreakpoint CreateBreakpoint() {
			ICorDebugFunctionBreakpoint fnbp;
			int hr = obj.CreateBreakpoint(out fnbp);
			return hr < 0 || fnbp == null ? null : new CorFunctionBreakpoint(fnbp);
		}

		public static bool operator ==(CorFunction a, CorFunction b) {
			if (ReferenceEquals(a, b))
				return true;
			if (ReferenceEquals(a, null) || ReferenceEquals(b, null))
				return false;
			return a.Equals(b);
		}

		public static bool operator !=(CorFunction a, CorFunction b) {
			return !(a == b);
		}

		public bool Equals(CorFunction other) {
			return !ReferenceEquals(other, null) &&
				RawObject == other.RawObject;
		}

		public override bool Equals(object obj) {
			return Equals(obj as CorFunction);
		}

		public override int GetHashCode() {
			return RawObject.GetHashCode();
		}

		public override string ToString() {
			return string.Format("[Function] Token: {0:X8}", Token);
		}
	}
}
