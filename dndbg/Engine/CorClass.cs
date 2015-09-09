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
	public sealed class CorClass : COMObject<ICorDebugClass>, IEquatable<CorClass> {
		/// <summary>
		/// Gets the token
		/// </summary>
		public uint Token {
			get { return token; }
		}
		readonly uint token;

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

		public CorClass(ICorDebugClass cls)
			: base(cls) {
			int hr = cls.GetToken(out this.token);
			if (hr < 0)
				this.token = 0;

			//TODO: ICorDebugClass::GetStaticFieldValue
			//TODO: ICorDebugClass2::GetParameterizedType
		}

		/// <summary>
		/// Mark all methods in the type as user code
		/// </summary>
		/// <param name="jmc">true to set user code</param>
		/// <returns></returns>
		public bool SetJustMyCode(bool jmc) {
			var c2 = obj as ICorDebugClass2;
			if (c2 == null)
				return false;
			int hr = c2.SetJMCStatus(jmc ? 1 : 0);
			return hr >= 0;
		}

		public static bool operator ==(CorClass a, CorClass b) {
			if (ReferenceEquals(a, b))
				return true;
			if (ReferenceEquals(a, null) || ReferenceEquals(b, null))
				return false;
			return a.Equals(b);
		}

		public static bool operator !=(CorClass a, CorClass b) {
			return !(a == b);
		}

		public bool Equals(CorClass other) {
			return !ReferenceEquals(other, null) &&
				RawObject == other.RawObject;
		}

		public override bool Equals(object obj) {
			return Equals(obj as CorClass);
		}

		public override int GetHashCode() {
			return RawObject.GetHashCode();
		}

		public T Write<T>(T output, TypePrinterFlags flags) where T : ITypeOutput {
			new TypePrinter(output, flags).Write(this);
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
