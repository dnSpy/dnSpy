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

using dnlib.DotNet;

namespace dnSpy.Analyzer.TreeNodes {
	static class Extensions {
		public static ITypeDefOrRef? GetScopeType(this ITypeDefOrRef? type) {
			if (type is TypeSpec ts) {
				var sig = ts.TypeSig.RemovePinnedAndModifiers();
				if (sig is GenericInstSig gis)
					return gis.GenericType?.TypeDefOrRef;
				if (sig is TypeDefOrRefSig tdrs)
					return tdrs.TypeDefOrRef;
			}
			return type;
		}

		public static IType? GetScopeType(this IType? type) {
			if (type is TypeDef td)
				return td;
			if (type is TypeRef tr)
				return tr;
			if (!(type is TypeSig sig)) {
				if (!(type is TypeSpec ts))
					return type;
				sig = ts.TypeSig;
			}
			sig = sig.RemovePinnedAndModifiers();
			if (sig is GenericInstSig gis)
				return gis.GenericType?.TypeDefOrRef;
			if (sig is TypeDefOrRefSig tdrs)
				return tdrs.TypeDefOrRef;
			return type;
		}
	}
}
