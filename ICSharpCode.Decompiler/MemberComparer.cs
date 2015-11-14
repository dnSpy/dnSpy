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
using dnlib.DotNet;

namespace dnSpy.Decompiler {
	public abstract class MemberRefComparer<T> : IComparer<T> where T : IMemberRef {
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

	public sealed class TypeDefComparer : MemberRefComparer<TypeDef> {
		public static readonly TypeDefComparer Instance = new TypeDefComparer();
	}

	public sealed class FieldDefComparer : MemberRefComparer<FieldDef> {
		public static readonly FieldDefComparer Instance = new FieldDefComparer();
	}

	public sealed class EventDefComparer : MemberRefComparer<EventDef> {
		public static readonly EventDefComparer Instance = new EventDefComparer();
	}

	public sealed class PropertyDefComparer : MemberRefComparer<PropertyDef> {
		public static readonly PropertyDefComparer Instance = new PropertyDefComparer();
	}

	public sealed class MethodDefComparer : IComparer<MethodDef> {
		public static readonly MethodDefComparer Instance = new MethodDefComparer();

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
