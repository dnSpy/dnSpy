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
using dndbg.COM.CorDebug;
using dndbg.COM.MetaData;
using dnlib.DotNet;

namespace dndbg.Engine {
	public sealed class CorField : IEquatable<CorField> {
		/// <summary>
		/// Gets the module or null
		/// </summary>
		public CorModule Module => Class.Module;

		/// <summary>
		/// Gets the class
		/// </summary>
		public CorClass Class { get; }

		/// <summary>
		/// Gets the token
		/// </summary>
		public uint Token { get; }

		public CorField(CorClass cls, uint token) {
			Class = cls;
			Token = token;
		}

		public FieldAttributes GetAttributes() => MDAPI.GetFieldAttributes(Module?.GetMetaDataInterface<IMetaDataImport>(), Token);
		public string GetName() => MDAPI.GetFieldName(Module?.GetMetaDataInterface<IMetaDataImport>(), Token);
		public FieldSig GetFieldSig() => MetaDataUtils.ReadFieldSig(Module?.GetMetaDataInterface<IMetaDataImport>(), Token);
		public CorValue ReadStatic(CorFrame frame) => Class.GetStaticFieldValue(Token, frame);

		public object GetConstant(out CorElementType etype) {
			var c = MDAPI.GetFieldConstant(Module?.GetMetaDataInterface<IMetaDataImport>(), Token, out etype);
			if (etype == CorElementType.End)
				return null;
			return c;
		}

		public object GetConstant() {
			CorElementType etype;
			return GetConstant(out etype);
		}

		public static bool operator ==(CorField a, CorField b) {
			if (ReferenceEquals(a, b))
				return true;
			if (ReferenceEquals(a, null) || ReferenceEquals(b, null))
				return false;
			return a.Equals(b);
		}

		public static bool operator !=(CorField a, CorField b) => !(a == b);

		public bool Equals(CorField other) {
			return !ReferenceEquals(other, null) &&
				Token == other.Token &&
				Class == other.Class;
		}

		public override bool Equals(object obj) => Equals(obj as CorField);
		public override int GetHashCode() => (int)Token ^ Class.GetHashCode();

		public T Write<T>(T output, TypePrinterFlags flags) where T : ITypeOutput {
			new TypePrinter(output, flags).Write(this);
			return output;
		}

		public string ToString(TypePrinterFlags flags) => Write(new StringBuilderTypeOutput(), flags).ToString();
		public override string ToString() => ToString(TypePrinterFlags.Default);
	}
}
