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
	public sealed class CorCode : COMObject<ICorDebugCode>, IEquatable<CorCode> {
		/// <summary>
		/// true if it's IL code
		/// </summary>
		public bool IsIL {
			get { return isIL; }
		}
		readonly bool isIL;

		/// <summary>
		/// Gets the size of the code
		/// </summary>
		public uint Size {
			get { return size; }
		}
		readonly uint size;

		/// <summary>
		/// Gets the address of code (eg. IL instructions). If it's IL, it doesn't include the
		/// method header.
		/// </summary>
		public ulong Address {
			get { return address; }
		}
		readonly ulong address;

		/// <summary>
		/// Gets the EnC (edit and continue) version number of this function
		/// </summary>
		public uint VersionNumber {
			get {
				uint ver;
				int hr = obj.GetVersionNumber(out ver);
				return hr < 0 ? 0 : ver;
			}
		}

		/// <summary>
		/// Gets the function or null
		/// </summary>
		public CorFunction Function {
			get {
				ICorDebugFunction func;
				int hr = obj.GetFunction(out func);
				return hr < 0 || func == null ? null : new CorFunction(func);
			}
		}

		/// <summary>
		/// Gets the JIT/NGEN compiler flags
		/// </summary>
		public CorDebugJITCompilerFlags CompilerFlags {
			get {
				var c2 = obj as ICorDebugCode2;
				if (c2 == null)
					return 0;
				CorDebugJITCompilerFlags flags;
				int hr = c2.GetCompilerFlags(out flags);
				return hr < 0 ? 0 : flags;
			}
		}

		public CorCode(ICorDebugCode code)
			: base(code) {
			int i;
			int hr = code.IsIL(out i);
			this.isIL = hr >= 0 && i != 0;

			hr = code.GetSize(out this.size);
			if (hr < 0)
				this.size = 0;

			hr = code.GetAddress(out this.address);
			if (hr < 0)
				this.address = 0;

			//TODO: ICorDebugCode::GetCode
			//TODO: ICorDebugCode2::GetCodeChunks
			//TODO: ICorDebugCode::GetILToNativeMapping
			//TODO: ICorDebugCode3::GetReturnValueLiveOffset
		}

		/// <summary>
		/// Creates a function breakpoint
		/// </summary>
		/// <param name="offset">Offset relative to the start of the method</param>
		/// <returns></returns>
		public CorFunctionBreakpoint CreateBreakpoint(uint offset) {
			ICorDebugFunctionBreakpoint fnbp;
			int hr = obj.CreateBreakpoint(offset, out fnbp);
			return hr < 0 || fnbp == null ? null : new CorFunctionBreakpoint(fnbp);
		}

		public static bool operator ==(CorCode a, CorCode b) {
			if (ReferenceEquals(a, b))
				return true;
			if (ReferenceEquals(a, null) || ReferenceEquals(b, null))
				return false;
			return a.Equals(b);
		}

		public static bool operator !=(CorCode a, CorCode b) {
			return !(a == b);
		}

		public bool Equals(CorCode other) {
			return !ReferenceEquals(other, null) &&
				RawObject == other.RawObject;
		}

		public override bool Equals(object obj) {
			return Equals(obj as CorCode);
		}

		public override int GetHashCode() {
			return RawObject.GetHashCode();
		}

		public T Write<T>(T output, TypePrinterFlags flags, Func<DnEval> getEval = null) where T : ITypeOutput {
			new TypePrinter(output, flags, getEval).Write(this);
			return output;
		}

		public string ToString(TypePrinterFlags flags) {
			return Write(new StringBuilderTypeOutput(), flags).ToString();
		}

		public override string ToString() {
			return ToString(TypePrinterFlags.Default);
		}
	}
}
