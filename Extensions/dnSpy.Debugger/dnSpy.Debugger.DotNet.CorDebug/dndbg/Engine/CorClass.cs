/*
    Copyright (C) 2014-2017 de4dot@gmail.com

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
using System.Linq;
using System.Text;
using dndbg.COM.CorDebug;
using dndbg.COM.MetaData;
using dnlib.DotNet;

namespace dndbg.Engine {
	sealed class CorClass : COMObject<ICorDebugClass>, IEquatable<CorClass> {
		/// <summary>
		/// Gets the token
		/// </summary>
		public uint Token => token;
		readonly uint token;

		/// <summary>
		/// Gets the module or null
		/// </summary>
		public CorModule Module {
			get {
				int hr = obj.GetModule(out var module);
				return hr < 0 || module == null ? null : new CorModule(module);
			}
		}

		/// <summary>
		/// true if this is <c>System.Enum</c>
		/// </summary>
		public bool IsSystemEnum => IsSystem("Enum");

		/// <summary>
		/// true if this is <c>System.ValueType</c>
		/// </summary>
		public bool IsSystemValueType => IsSystem("ValueType");

		/// <summary>
		/// true if this is <c>System.Object</c>
		/// </summary>
		public bool IsSystemObject => IsSystem("Object");

		/// <summary>
		/// true if this is <c>System.Decimal</c>
		/// </summary>
		public bool IsSystemDecimal => IsSystem("Decimal");

		/// <summary>
		/// true if this is <c>System.DateTime</c>
		/// </summary>
		public bool IsSystemDateTime => IsSystem("DateTime");

		public CorClass(ICorDebugClass cls)
			: base(cls) {
			int hr = cls.GetToken(out token);
			if (hr < 0)
				token = 0;
		}

		public TypeAttributes GetTypeAttributes() {
			var mdi = Module?.GetMetaDataInterface<IMetaDataImport>();
			return MDAPI.GetTypeDefAttributes(mdi, token) ?? 0;
		}

		/// <summary>
		/// Creates a <see cref="CorType"/>
		/// </summary>
		/// <param name="etype">Element type, must be <see cref="CorElementType.Class"/> or <see cref="CorElementType.ValueType"/></param>
		/// <param name="typeArgs">Generic type arguments or null</param>
		/// <returns></returns>
		public CorType GetParameterizedType(CorElementType etype, CorType[] typeArgs = null) {
			Debug.Assert(etype == CorElementType.Class || etype == CorElementType.ValueType);
			var c2 = obj as ICorDebugClass2;
			if (c2 == null)
				return null;
			int hr = c2.GetParameterizedType(etype, typeArgs?.Length ?? 0, typeArgs.ToCorDebugArray(), out var value);
			return hr < 0 || value == null ? null : new CorType(value);
		}

		/// <summary>
		/// Returns true if it's a System.XXX type in the corlib (eg. mscorlib)
		/// </summary>
		/// <param name="name">Name (not including namespace)</param>
		/// <returns></returns>
		public bool IsSystem(string name) {
			var mod = Module;
			if (mod == null)
				return false;
			var names = MetaDataUtils.GetTypeDefFullNames(mod.GetMetaDataInterface<IMetaDataImport>(), Token);
			if (names.Count != 1)
				return false;
			if (names[0].Name != "System." + name)
				return false;

			//TODO: Check if it's mscorlib

			return true;
		}

		public static bool operator ==(CorClass a, CorClass b) {
			if (ReferenceEquals(a, b))
				return true;
			if (ReferenceEquals(a, null) || ReferenceEquals(b, null))
				return false;
			return a.Equals(b);
		}

		public static bool operator !=(CorClass a, CorClass b) => !(a == b);
		public bool Equals(CorClass other) => !ReferenceEquals(other, null) && RawObject == other.RawObject;
		public override bool Equals(object obj) => Equals(obj as CorClass);
		public override int GetHashCode() => RawObject.GetHashCode();

		public T Write<T>(T output, TypeFormatterFlags flags) where T : ITypeOutput {
			new TypeFormatter(output, flags).Write(this);
			return output;
		}

		public string ToString(TypeFormatterFlags flags) => Write(new StringBuilderTypeOutput(), flags).ToString();
		public override string ToString() => ToString(TypeFormatterFlags.Default);
	}
}
