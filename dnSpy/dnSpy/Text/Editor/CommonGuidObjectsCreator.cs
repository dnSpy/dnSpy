/*
    Copyright (C) 2014-2016 de4dot@gmail.com

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
	sealed class CommonGuidObjectsCreator : IGuidObjectsCreator {
		readonly Func<GuidObjectsCreatorArgs, IEnumerable<GuidObject>> createGuidObjects;
		readonly IGuidObjectsCreator guidObjectsCreator;

		CommonGuidObjectsCreator(Func<GuidObjectsCreatorArgs, IEnumerable<GuidObject>> createGuidObjects, IGuidObjectsCreator guidObjectsCreator) {
			this.createGuidObjects = createGuidObjects;
			this.guidObjectsCreator = guidObjectsCreator;
		}

		public static Func<GuidObjectsCreatorArgs, IEnumerable<GuidObject>> Create(Func<GuidObjectsCreatorArgs, IEnumerable<GuidObject>> createGuidObjects, IGuidObjectsCreator guidObjectsCreator) {
			var creator = new CommonGuidObjectsCreator(createGuidObjects, guidObjectsCreator);
			return creator.GetGuidObjects;
		}

		public IEnumerable<GuidObject> GetGuidObjects(GuidObjectsCreatorArgs args) {
			if (createGuidObjects != null) {
				foreach (var guidObject in createGuidObjects(args))
					yield return guidObject;
			}

			if (guidObjectsCreator != null) {
				foreach (var guidObject in guidObjectsCreator.GetGuidObjects(args))
					yield return guidObject;
			}
		}
	}
}
