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
using System.Collections.Generic;
using dnlib.DotNet;

namespace dnSpy.Contracts.Decompiler {
	/// <summary>
	/// Member comparer base class
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public abstract class MemberRefComparer<T> : IComparer<T> where T : IMemberRef {
		/// <summary>
		/// Compares two instances
		/// </summary>
		/// <param name="x">First instance to compare</param>
		/// <param name="y">Second instance to compare</param>
		/// <returns></returns>
		public int Compare(T x, T y) {
			int c = StringComparer.OrdinalIgnoreCase.Compare(x.Name, y.Name);
			if (c != 0)
				return c;
			c = x.MDToken.Raw.CompareTo(y.MDToken.Raw);
			if (c != 0)
				return c;
			return x.GetHashCode().CompareTo(y.GetHashCode());
		}
	}

	/// <summary>
	/// <see cref="TypeDef"/> comparer
	/// </summary>
	public sealed class TypeDefComparer : MemberRefComparer<TypeDef> {
		/// <summary>
		/// Gets the instance
		/// </summary>
		public static readonly TypeDefComparer Instance = new TypeDefComparer();
	}

	/// <summary>
	/// <see cref="FieldDef"/> comparer
	/// </summary>
	public sealed class FieldDefComparer : MemberRefComparer<FieldDef> {
		/// <summary>
		/// Gets the instance
		/// </summary>
		public static readonly FieldDefComparer Instance = new FieldDefComparer();
	}

	/// <summary>
	/// <see cref="EventDef"/> comparer
	/// </summary>
	public sealed class EventDefComparer : MemberRefComparer<EventDef> {
		/// <summary>
		/// Gets the instance
		/// </summary>
		public static readonly EventDefComparer Instance = new EventDefComparer();
	}

	/// <summary>
	/// <see cref="PropertyDef"/> comparer
	/// </summary>
	public sealed class PropertyDefComparer : MemberRefComparer<PropertyDef> {
		/// <summary>
		/// Gets the instance
		/// </summary>
		public static readonly PropertyDefComparer Instance = new PropertyDefComparer();
	}

	/// <summary>
	/// <see cref="MethodDef"/> comparer
	/// </summary>
	public sealed class MethodDefComparer : IComparer<MethodDef> {
		/// <summary>
		/// Gets the instance
		/// </summary>
		public static readonly MethodDefComparer Instance = new MethodDefComparer();

		/// <summary>
		/// Compares two instances
		/// </summary>
		/// <param name="x">First instance to compare</param>
		/// <param name="y">Second instance to compare</param>
		/// <returns></returns>
		public int Compare(MethodDef x, MethodDef y) {
			int c = StringComparer.OrdinalIgnoreCase.Compare(x.Name, y.Name);
			if (c != 0)
				return c;
			c = x.MethodSig.GetParamCount().CompareTo(y.MethodSig.GetParamCount());
			if (c != 0)
				return c;
			c = x.MethodSig.GetGenParamCount().CompareTo(y.MethodSig.GetGenParamCount());
			if (c != 0)
				return c;
			c = x.MDToken.Raw.CompareTo(y.MDToken.Raw);
			if (c != 0)
				return c;
			return x.GetHashCode().CompareTo(y.GetHashCode());
		}
	}
}
