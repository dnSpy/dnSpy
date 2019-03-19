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
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using dnSpy.Contracts.Documents.Tabs;
using dnSpy.Contracts.Documents.TreeView;
using dnSpy.Contracts.Settings;

namespace dnSpy.Documents.Tabs {
	interface IDocumentTabContentFactoryService {
		DocumentTabContent CreateTabContent(DocumentTreeNodeData[] nodes);
		Guid? Serialize(DocumentTabContent content, ISettingsSection section);
		DocumentTabContent Deserialize(Guid guid, ISettingsSection section, DocumentTreeNodeData[] nodes);
	}

	[Export(typeof(IDocumentTabContentFactoryService))]
	sealed class DocumentTabContentFactoryService : IDocumentTabContentFactoryService {
		readonly Lazy<IDocumentTabContentFactory, IDocumentTabContentFactoryMetadata>[] tabContentFactories;

		[ImportingConstructor]
		DocumentTabContentFactoryService([ImportMany] IEnumerable<Lazy<IDocumentTabContentFactory, IDocumentTabContentFactoryMetadata>> mefTabContentFactories) {
			tabContentFactories = mefTabContentFactories.OrderBy(a => a.Metadata.Order).ToArray();
			Debug.Assert(tabContentFactories.Length > 0);
		}

		public DocumentTabContent CreateTabContent(DocumentTreeNodeData[] nodes) {
			var context = new DocumentTabContentFactoryContext(nodes);
			foreach (var factory in tabContentFactories) {
				var tabContent = factory.Value.Create(context);
				if (tabContent != null)
					return tabContent;
			}
			return null;
		}

		public Guid? Serialize(DocumentTabContent content, ISettingsSection section) {
			var nodes = content.Nodes.ToArray();
			var context = new DocumentTabContentFactoryContext(nodes);
			foreach (var factory in tabContentFactories) {
				var guid = factory.Value.Serialize(content, section);
				if (guid != null)
					return guid;
			}
			return null;
		}

		public DocumentTabContent Deserialize(Guid guid, ISettingsSection section, DocumentTreeNodeData[] nodes) {
			var context = new DocumentTabContentFactoryContext(nodes);
			foreach (var factory in tabContentFactories) {
				var content = factory.Value.Deserialize(guid, section, context);
				if (content != null)
					return content;
			}
			return null;
		}
	}
}
