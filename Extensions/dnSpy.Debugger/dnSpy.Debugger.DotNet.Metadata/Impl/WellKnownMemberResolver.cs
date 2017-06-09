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

namespace dnSpy.Debugger.DotNet.Metadata.Impl {
	sealed partial class WellKnownMemberResolver {
		readonly object lockObj;
		readonly HashSet<DmdModule> checkedModules;
		readonly DmdAppDomain appDomain;
		readonly DmdType[] wellKnownTypes;

		public WellKnownMemberResolver(DmdAppDomain appDomain) {
			lockObj = new object();
			checkedModules = new HashSet<DmdModule>();
			this.appDomain = appDomain ?? throw new ArgumentNullException(nameof(appDomain));
			wellKnownTypes = new DmdType[WELL_KNOWN_TYPES_COUNT];
		}

		public DmdMemberInfo GetWellKnownMember(DmdWellKnownMember wellKnownMember) => throw new NotImplementedException();//TODO:

		public DmdType GetWellKnownType(DmdWellKnownType wellKnownType, bool onlyCorLib) {
			if ((uint)wellKnownType >= (uint)wellKnownTypes.Length)
				return null;

			var cachedType = wellKnownTypes[(int)wellKnownType];
			if (cachedType != null)
				return cachedType;

			lock (lockObj) {
				DmdAssembly[] assemblies;
				if (onlyCorLib) {
					var corlib = appDomain.CorLib;
					if (corlib == null)
						return null;
					assemblies = new[] { corlib };
				}
				else
					assemblies = appDomain.GetAssemblies();
				foreach (var assembly in assemblies) {
					foreach (var module in assembly.GetModules()) {
						if (checkedModules.Contains(module))
							continue;

						foreach (var type in module.GetTypes()) {
							if (type.IsPublic) {
								if (toNonNestedWellKnownType.TryGetValue(new TypeName(type.Namespace, type.Name), out var wkt))
									wellKnownTypes[(int)wkt] = type;
							}
							else if (type.IsNestedPublic && type.Namespace == null) {
								var nonNested = type.DeclaringType;
								if (nonNested == null || !nonNested.IsPublic)
									continue;
								if (!toNonNestedWellKnownType.ContainsKey(new TypeName(nonNested.Namespace, nonNested.Name)))
									continue;
								if (toNestedWellKnownType.TryGetValue(type.Name, out var wkt))
									wellKnownTypes[(int)wkt] = type;
							}
						}

						checkedModules.Add(module);

						cachedType = wellKnownTypes[(int)wellKnownType];
						if (cachedType != null)
							return cachedType;
					}
				}
			}

			return null;
		}
	}
}
