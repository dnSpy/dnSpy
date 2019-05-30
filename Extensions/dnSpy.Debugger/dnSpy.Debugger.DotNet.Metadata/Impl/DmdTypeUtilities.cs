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

namespace dnSpy.Debugger.DotNet.Metadata.Impl {
	static class DmdTypeUtilities {
		public static bool IsFullyResolved(IList<DmdType> list) {
			for (int i = 0; i < list.Count; i++) {
				if (!((DmdTypeBase)list[i]).IsFullyResolved)
					return false;
			}
			return true;
		}

		public static IList<DmdType>? FullResolve(IList<DmdType> list) {
			if (IsFullyResolved(list))
				return list;
			var res = new DmdType[list.Count];
			for (int i = 0; i < res.Length; i++) {
				var type = ((DmdTypeBase)list[i]).FullResolve();
				if (type is null)
					return null;
				res[i] = type;
			}
			return res;
		}

		public static void SplitFullName(string fullName, out string? @namespace, out string name) {
			int index = fullName.LastIndexOf('.');
			if (index < 0) {
				@namespace = null;
				name = fullName;
			}
			else {
				@namespace = index == 0 ? null : fullName.Substring(0, index);
				name = fullName.Substring(index + 1);
			}
		}

		public static DmdType? GetNonNestedType(DmdType typeRef) {
			for (int i = 0; i < 1000; i++) {
				var next = typeRef.DeclaringType;
				if (next is null)
					return typeRef;
				typeRef = next;
			}
			return null;
		}

		public static DmdType[]? ToDmdType(this IList<Type>? types, DmdAppDomain appDomain) {
			if (types is null)
				return null;
			return ToDmdTypeNoNull(types, appDomain);
		}

		public static DmdType[] ToDmdTypeNoNull(this IList<Type> types, DmdAppDomain appDomain) {
			if (types is null)
				throw new ArgumentNullException(nameof(types));
			if (types.Count == 0)
				return Array.Empty<DmdType>();
			var newTypes = new DmdType[types.Count];
			for (int i = 0; i < newTypes.Length; i++)
				newTypes[i] = appDomain.GetTypeThrow(types[i]);
			return newTypes;
		}

		public static DmdType? ToDmdType(Type? type, DmdAppDomain appDomain) {
			if (type is null)
				return null;
			return appDomain.GetTypeThrow(type);
		}

		public static DmdType ToDmdTypeNoNull(Type type, DmdAppDomain appDomain) {
			if (type is null)
				throw new ArgumentNullException(nameof(type));
			return appDomain.GetTypeThrow(type);
		}
	}
}
