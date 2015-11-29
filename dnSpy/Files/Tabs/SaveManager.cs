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
using System.ComponentModel.Composition;
using System.Linq;
using dnSpy.Contracts.Files.Tabs;

namespace dnSpy.Files.Tabs {
	[Export, Export(typeof(ISaveManager)), PartCreationPolicy(CreationPolicy.Shared)]
	sealed class SaveManager : ISaveManager {
		readonly ITabSaverCreator[] creators;

		[ImportingConstructor]
		SaveManager([ImportMany] IEnumerable<Lazy<ITabSaverCreator, ITabSaverCreatorMetadata>> mefCreators) {
			this.creators = mefCreators.OrderBy(a => a.Metadata.Order).Select(a => a.Value).ToArray();
		}

		ITabSaver GetTabSaver(IFileTab tab) {
			if (tab == null)
				return null;
			foreach (var creator in creators) {
				var ts = creator.Create(tab);
				if (ts != null)
					return ts;
			}
			return null;
		}

		public bool CanSave(IFileTab tab) {
			var ts = GetTabSaver(tab);
			return ts != null && ts.CanSave;
		}

		public string GetMenuHeader(IFileTab tab) {
			var ts = GetTabSaver(tab);
			return (ts == null ? null : ts.MenuHeader) ?? "_Save...";
		}

		public void Save(IFileTab tab) {
			var ts = GetTabSaver(tab);
			if (ts == null || !ts.CanSave)
				return;
			ts.Save();
		}
	}
}
