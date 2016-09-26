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
using System.ComponentModel.Composition;
using dnSpy.Contracts.Documents.Tabs;
using dnSpy.Contracts.Documents.TreeView;

namespace dnSpy.Documents.Tabs {
	[ExportDefaultDocumentTabContentProvider(Order = DefaultDocumentTabContentProviderConstants.DEFAULT_HANDLER)]
	sealed class DefaultDocumentTabContentProvider : IDefaultDocumentTabContentProvider {
		readonly IDocumentTabContentFactoryService documentTabContentFactoryService;

		[ImportingConstructor]
		DefaultDocumentTabContentProvider(IDocumentTabContentFactoryService documentTabContentFactoryService) {
			this.documentTabContentFactoryService = documentTabContentFactoryService;
		}

		public IDocumentTabContent Create(IDocumentTabService documentTabService) =>
			documentTabContentFactoryService.CreateTabContent(Array.Empty<IDocumentTreeNodeData>());
	}
}
