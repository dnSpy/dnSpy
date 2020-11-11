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
	sealed class WellKnownMemberResolver {
		readonly object lockObj;
		readonly HashSet<DmdModule> checkedModules;
		readonly DmdAppDomain appDomain;
		readonly DmdType[] wellKnownTypes;

		public WellKnownMemberResolver(DmdAppDomain appDomain) {
			lockObj = new object();
			checkedModules = new HashSet<DmdModule>();
			this.appDomain = appDomain ?? throw new ArgumentNullException(nameof(appDomain));
			wellKnownTypes = new DmdType[DmdWellKnownTypeUtils.WellKnownTypesCount];
		}

		public DmdType? GetWellKnownType(DmdWellKnownType wellKnownType, bool onlyCorLib) {
			if ((uint)wellKnownType >= (uint)wellKnownTypes.Length)
				return null;

			ref var cachedType = ref wellKnownTypes[(int)wellKnownType];
			if (cachedType is not null)
				return cachedType;

			lock (lockObj) {
				DmdAssembly[] assemblies;
				if (onlyCorLib) {
					var corlib = appDomain.CorLib;
					if (corlib is null)
						return null;
					assemblies = new[] { corlib };
				}
				else
					assemblies = appDomain.GetAssemblies();
				foreach (var assembly in assemblies) {
					foreach (var module in assembly.GetModules()) {
						if (module.IsSynthetic)
							continue;
						if (!checkedModules.Add(module))
							continue;

						// There are no well known types in dynamic/in-memory assemblies
						if (module.IsDynamic || module.IsInMemory)
							continue;

						foreach (var type in module.GetTypes()) {
							if (DmdWellKnownTypeUtils.TryGetWellKnownType(DmdTypeName.Create(type), out var wkt)) {
								ref var elem = ref wellKnownTypes[(int)wkt];
								if (elem is null)
									elem = type;
							}
						}

						if (cachedType is not null)
							return cachedType;
					}
				}
			}

			return null;
		}
	}
}
