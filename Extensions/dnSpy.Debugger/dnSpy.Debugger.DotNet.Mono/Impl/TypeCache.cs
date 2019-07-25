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

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using dnSpy.Debugger.DotNet.Metadata;
using Mono.Debugger.Soft;

namespace dnSpy.Debugger.DotNet.Mono.Impl {
	sealed class TypeCache {
		readonly Dictionary<TypeMirror, DmdType> toReflectionType = new Dictionary<TypeMirror, DmdType>();
		readonly Dictionary<DmdType, TypeMirror> toMonoType = new Dictionary<DmdType, TypeMirror>(DmdMemberInfoEqualityComparer.DefaultType);

		public bool TryGetType(TypeMirror monoType, [NotNullWhen(true)] out DmdType? type) => toReflectionType.TryGetValue(monoType, out type);
		public bool TryGetType(DmdType type, [NotNullWhen(true)] out TypeMirror? monoType) => toMonoType.TryGetValue(type, out monoType);

		public void Add(TypeMirror monoType, DmdType reflectionType) {
			toReflectionType[monoType] = reflectionType;
			toMonoType[reflectionType] = monoType;
		}

		public static TypeCache GetOrCreate(DmdAppDomain reflectionAppDomain) {
			if (reflectionAppDomain.TryGetData(out TypeCache? typeCache))
				return typeCache;
			return GetOrCreateTypeCacheCore(reflectionAppDomain);

			static TypeCache GetOrCreateTypeCacheCore(DmdAppDomain reflectionAppDomain2) =>
				reflectionAppDomain2.GetOrCreateData(() => new TypeCache());
		}
	}
}
