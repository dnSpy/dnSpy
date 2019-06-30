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
using System.ComponentModel.Composition;
using System.Linq;
using dnSpy.Contracts.App;
using dnSpy.Contracts.Documents;

namespace dnSpy.Documents.Tabs {
	[Export]
	sealed class DocumentTabSerializer {
		readonly IDocumentTabContentFactoryService documentTabContentFactoryService;
		readonly DocumentTabService documentTabService;

		[ImportingConstructor]
		DocumentTabSerializer(IDocumentTabContentFactoryService documentTabContentFactoryService, DocumentTabService documentTabService) {
			this.documentTabContentFactoryService = documentTabContentFactoryService;
			this.documentTabService = documentTabService;
		}

		public IEnumerable<object?> Restore(List<SerializedTabGroupWindow> tgws) {
			var mainTgw = tgws.FirstOrDefault(a => a.Name == SerializedTabGroupWindow.MAIN_NAME);
			if (!(mainTgw is null)) {
				foreach (var o in Load(mainTgw))
					yield return o;
				yield return null;
			}
		}

		IEnumerable<object?> Load(SerializedTabGroupWindow tgw) {
			bool addedAutoLoadedAssembly = false;
			foreach (var f in GetAutoLoadedAssemblies(tgw)) {
				addedAutoLoadedAssembly = true;
				documentTabService.DocumentTreeView.DocumentService.TryGetOrCreate(f, true);
				yield return null;
			}
			if (addedAutoLoadedAssembly)
				yield return LoaderConstants.Delay;

			foreach (var o in tgw.Restore(documentTabService, documentTabContentFactoryService, documentTabService.TabGroupService))
				yield return o;
		}

		IEnumerable<DsDocumentInfo> GetAutoLoadedAssemblies(SerializedTabGroupWindow tgw) {
			foreach (var g in tgw.TabGroups) {
				foreach (var t in g.Tabs) {
					foreach (var f in t.AutoLoadedDocuments)
						yield return f;
				}
			}
		}

		public List<SerializedTabGroupWindow> SaveTabs() {
			var tgws = new List<SerializedTabGroupWindow>();
			tgws.Add(SerializedTabGroupWindow.Create(documentTabContentFactoryService, documentTabService.TabGroupService, SerializedTabGroupWindow.MAIN_NAME));
			return tgws;
		}
	}
}
