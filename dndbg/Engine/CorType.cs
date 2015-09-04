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
using dndbg.Engine.COM.CorDebug;

namespace dndbg.Engine {
	public sealed class CorType : COMObject<ICorDebugType>, IEquatable<CorType> {
		/// <summary>
		/// Gets the element type
		/// </summary>
		public CorElementType ElementType {
			get { return elemType; }
		}
		readonly CorElementType elemType;

		/// <summary>
		/// Gets the rank of the array
		/// </summary>
		public uint Rank {
			get { return rank; }
		}
		readonly uint rank;

		/// <summary>
		/// Gets the first type parameter if it's a generic type
		/// </summary>
		public CorType FirstTypeParameter {
			get {
				ICorDebugType type;
				int hr = obj.GetFirstTypeParameter(out type);
				return hr < 0 || type == null ? null : new CorType(type);
			}
		}

		/// <summary>
		/// Gets all type parameters. If it's a <see cref="CorElementType.Class"/> or a <see cref="CorElementType.ValueType"/>,
		/// then the returned parameters are the type parameters in the correct order. If it's
		/// a <see cref="CorElementType.FnPtr"/>, the first type is the return type, followed by
		/// all the method argument types in correct order. If it's a <see cref="CorElementType.Array"/>,
		/// <see cref="CorElementType.SZArray"/>, <see cref="CorElementType.ByRef"/> or a
		/// <see cref="CorElementType.Ptr"/>, the returned type is the inner type, eg. int if the
		/// type is int[]. In this case, <see cref="FirstTypeParameter"/> can be called instead.
		/// </summary>
		public IEnumerable<CorType> TypeParameters {
			get {
				ICorDebugTypeEnum typeEnum;
				int hr = obj.EnumerateTypeParameters(out typeEnum);
				if (hr < 0)
					yield break;
				for (;;) {
					ICorDebugType type = null;
					hr = typeEnum.Next(1, out type, IntPtr.Zero);
					if (hr != 0 || type == null)
						break;
					yield return new CorType(type);
				}
			}
		}

		/// <summary>
		/// true if <see cref="Class"/> can be accessed
		/// </summary>
		public bool HasClass {
			get { return ElementType == CorElementType.Class || ElementType == CorElementType.ValueType; }
		}

		/// <summary>
		/// Gets the class or null. Should only be called if <see cref="ElementType"/> is
		/// <see cref="CorElementType.Class"/> or <see cref="CorElementType.ValueType"/>,
		/// see <see cref="HasClass"/>.
		/// </summary>
		public CorClass Class {
			get {
				ICorDebugClass cls;
				int hr = obj.GetClass(out cls);
				return hr < 0 || cls == null ? null : new CorClass(cls);
			}
		}

		/// <summary>
		/// Gets the base class or null
		/// </summary>
		public CorType Base {
			get {
				ICorDebugType @base;
				int hr = obj.GetBase(out @base);
				return hr < 0 || @base == null ? null : new CorType(@base);
			}
		}

		internal CorType(ICorDebugType type)
			: base(type) {
			int hr = type.GetType(out this.elemType);
			if (hr < 0)
				this.elemType = 0;

			hr = type.GetRank(out this.rank);
			if (hr < 0)
				this.rank = 0;

			//TODO: ICorDebugType::GetStaticFieldValue
		}

		public static bool operator ==(CorType a, CorType b) {
			if (ReferenceEquals(a, b))
				return true;
			if (ReferenceEquals(a, null) || ReferenceEquals(b, null))
				return false;
			return a.Equals(b);
		}

		public static bool operator !=(CorType a, CorType b) {
			return !(a == b);
		}

		public bool Equals(CorType other) {
			return !ReferenceEquals(other, null) &&
				RawObject == other.RawObject;
		}

		public override bool Equals(object obj) {
			return Equals(obj as CorType);
		}

		public override int GetHashCode() {
			return RawObject.GetHashCode();
		}

		public override string ToString() {
			if (HasClass)
				return string.Format("[Type] {0} {1}", ElementType, Class);
			return string.Format("[Type] {0}", ElementType);
		}
	}
}
