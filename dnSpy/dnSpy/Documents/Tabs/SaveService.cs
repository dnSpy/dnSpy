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
using System.ComponentModel.Composition;
using System.Linq;
using dnSpy.Contracts.Documents.Tabs;
using dnSpy.Properties;

namespace dnSpy.Documents.Tabs {
	[Export(typeof(ISaveService))]
	sealed class SaveService : ISaveService {
		readonly ITabSaverProvider[] tabSaverProviders;

		[ImportingConstructor]
		SaveService([ImportMany] IEnumerable<Lazy<ITabSaverProvider, ITabSaverProviderMetadata>> tabSaverProviders) => this.tabSaverProviders = tabSaverProviders.OrderBy(a => a.Metadata.Order).Select(a => a.Value).ToArray();

		ITabSaver GetTabSaver(IDocumentTab tab) {
			if (tab == null)
				return null;
			foreach (var provider in tabSaverProviders) {
				var ts = provider.Create(tab);
				if (ts != null)
					return ts;
			}
			return null;
		}

		public bool CanSave(IDocumentTab tab) {
			var ts = GetTabSaver(tab);
			return ts != null && ts.CanSave;
		}

		public string GetMenuHeader(IDocumentTab tab) {
			var ts = GetTabSaver(tab);
			return (ts == null ? null : ts.MenuHeader) ?? dnSpy_Resources.Button_Save;
		}

		public void Save(IDocumentTab tab) {
			var ts = GetTabSaver(tab);
			if (ts == null || !ts.CanSave)
				return;
			ts.Save();
		}
	}
}
