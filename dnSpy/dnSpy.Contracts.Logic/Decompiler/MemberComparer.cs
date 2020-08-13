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
using System.Diagnostics.CodeAnalysis;
using dnlib.DotNet;

namespace dnSpy.Contracts.Decompiler {
	/// <summary>
	/// Member comparer base class
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public abstract class MemberRefComparer<T> : IComparer<T> where T : class, IMemberRef {
		/// <summary>
		/// Compares two instances
		/// </summary>
		/// <param name="x">First instance to compare</param>
		/// <param name="y">Second instance to compare</param>
		/// <returns></returns>
		public int Compare([AllowNull] T x, [AllowNull] T y) {
			if ((object?)x == y)
				return 0;
			if (x is null)
				return -1;
			if (y is null)
				return 1;
			int c = StringComparer.OrdinalIgnoreCase.Compare(x.Name, y.Name);
			if (c != 0) return c;
			c = x.MDToken.Raw.CompareTo(y.MDToken.Raw);
			if (c != 0) return c;
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
	/// <see cref="MemberRef"/> comparer
	/// </summary>
	public sealed class MemberRefComparer : MemberRefComparer<MemberRef> {
		/// <summary>
		/// Gets the instance
		/// </summary>
		public static readonly MemberRefComparer Instance = new MemberRefComparer();
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
		public int Compare([AllowNull] MethodDef x, [AllowNull] MethodDef y) => MethodRefComparer.Instance.Compare(x, y);
	}

	/// <summary>
	/// Method reference comparer
	/// </summary>
	public sealed class MethodRefComparer : IComparer<IMethod> {
		/// <summary>
		/// Gets the instance
		/// </summary>
		public static readonly MethodRefComparer Instance = new MethodRefComparer();

		/// <summary>
		/// Compares two instances
		/// </summary>
		/// <param name="x">First instance to compare</param>
		/// <param name="y">Second instance to compare</param>
		/// <returns></returns>
		public int Compare([AllowNull] IMethod x, [AllowNull] IMethod y) {
			if ((object?)x == y)
				return 0;
			if (x is null)
				return -1;
			if (y is null)
				return 1;
			int c = StringComparer.OrdinalIgnoreCase.Compare(x.Name, y.Name);
			if (c != 0)
				return c;
			return CompareNoName(x, y);
		}

		internal static int CompareNoName(IMethod x, IMethod y) {
			int c = x.MethodSig.GetParamCount().CompareTo(y.MethodSig.GetParamCount());
			if (c != 0) return c;
			c = x.MethodSig.GetGenParamCount().CompareTo(y.MethodSig.GetGenParamCount());
			if (c != 0) return c;
			c = x.MDToken.Raw.CompareTo(y.MDToken.Raw);
			if (c != 0) return c;
			return x.GetHashCode().CompareTo(y.GetHashCode());
		}
	}

	/// <summary>
	/// Property reference comparer
	/// </summary>
	public sealed class PropertyRefComparer : IComparer<IMethod> {
		/// <summary>
		/// Gets the instance
		/// </summary>
		public static readonly PropertyRefComparer Instance = new PropertyRefComparer();

		static int GetAccessor(string name, out string propName) {
			if (name.StartsWith("get_")) {
				propName = name.Substring(4);
				return 0;
			}
			if (name.StartsWith("set_")) {
				propName = name.Substring(4);
				return 1;
			}
			propName = name;
			return int.MaxValue;
		}

		/// <summary>
		/// Compares two instances
		/// </summary>
		/// <param name="x">First instance to compare</param>
		/// <param name="y">Second instance to compare</param>
		/// <returns></returns>
		public int Compare([AllowNull] IMethod x, [AllowNull] IMethod y) {
			if ((object?)x == y)
				return 0;
			if (x is null)
				return -1;
			if (y is null)
				return 1;
			string xn = x.Name;
			string yn = y.Name;
			int xacc = GetAccessor(xn, out var xPropName);
			int yacc = GetAccessor(yn, out var yPropName);
			int c = StringComparer.OrdinalIgnoreCase.Compare(xPropName, yPropName);
			if (c != 0) return c;
			c = xacc - yacc;
			if (c != 0) return c;
			return MethodRefComparer.CompareNoName(x, y);
		}
	}

	/// <summary>
	/// Event reference comparer
	/// </summary>
	public sealed class EventRefComparer : IComparer<IMethod> {
		/// <summary>
		/// Gets the instance
		/// </summary>
		public static readonly EventRefComparer Instance = new EventRefComparer();

		static int GetAccessor(string name, out string propName) {
			if (name.StartsWith("add_")) {
				propName = name.Substring(4);
				return 0;
			}
			if (name.StartsWith("remove_")) {
				propName = name.Substring(7);
				return 1;
			}
			if (name.StartsWith("raise_")) {
				propName = name.Substring(6);
				return 2;
			}
			propName = name;
			return int.MaxValue;
		}

		/// <summary>
		/// Compares two instances
		/// </summary>
		/// <param name="x">First instance to compare</param>
		/// <param name="y">Second instance to compare</param>
		/// <returns></returns>
		public int Compare([AllowNull] IMethod x, [AllowNull] IMethod y) {
			if ((object?)x == y)
				return 0;
			if (x is null)
				return -1;
			if (y is null)
				return 1;
			string xn = x.Name;
			string yn = y.Name;
			int xacc = GetAccessor(xn, out var xPropName);
			int yacc = GetAccessor(yn, out var yPropName);
			int c = StringComparer.OrdinalIgnoreCase.Compare(xPropName, yPropName);
			if (c != 0) return c;
			c = xacc - yacc;
			if (c != 0) return c;
			return MethodRefComparer.CompareNoName(x, y);
		}
	}
}
