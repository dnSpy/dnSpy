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
using dnSpy.Contracts.Menus;

namespace dnSpy.Text.Editor {
	sealed class CommonGuidObjectsProvider : IGuidObjectsProvider {
		readonly Func<GuidObjectsProviderArgs, IEnumerable<GuidObject>>? createGuidObjects;
		readonly IGuidObjectsProvider? guidObjectsProvider;

		CommonGuidObjectsProvider(Func<GuidObjectsProviderArgs, IEnumerable<GuidObject>>? createGuidObjects, IGuidObjectsProvider? guidObjectsProvider) {
			this.createGuidObjects = createGuidObjects;
			this.guidObjectsProvider = guidObjectsProvider;
		}

		public static Func<GuidObjectsProviderArgs, IEnumerable<GuidObject>> Create(Func<GuidObjectsProviderArgs, IEnumerable<GuidObject>>? createGuidObjects, IGuidObjectsProvider? guidObjectsProvider) {
			var provider = new CommonGuidObjectsProvider(createGuidObjects, guidObjectsProvider);
			return provider.GetGuidObjects;
		}

		public IEnumerable<GuidObject> GetGuidObjects(GuidObjectsProviderArgs args) {
			if (!(createGuidObjects is null)) {
				foreach (var guidObject in createGuidObjects(args))
					yield return guidObject;
			}

			if (!(guidObjectsProvider is null)) {
				foreach (var guidObject in guidObjectsProvider.GetGuidObjects(args))
					yield return guidObject;
			}
		}
	}
}
