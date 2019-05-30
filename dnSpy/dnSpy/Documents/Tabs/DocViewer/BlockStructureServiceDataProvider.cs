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
using dnSpy.Contracts.Documents.Tabs.DocViewer;
using dnSpy.Contracts.Text.Editor;
using Microsoft.VisualStudio.Text;

namespace dnSpy.Documents.Tabs.DocViewer {
	sealed class BlockStructureServiceDataProvider : IBlockStructureServiceDataProvider {
		readonly LazyBlockStructureCollection lazyCollection;

		BlockStructureServiceDataProvider(LazyBlockStructureCollection lazyCollection) => this.lazyCollection = lazyCollection ?? throw new ArgumentNullException(nameof(lazyCollection));

		public static IBlockStructureServiceDataProvider? TryCreate(IDocumentViewer documentViewer) {
			var lazyColl = documentViewer.Content.GetCustomData<LazyBlockStructureCollection>(DocumentViewerContentDataIds.BlockStructure);
			return lazyColl is null ? null : new BlockStructureServiceDataProvider(lazyColl);
		}

		public void GetData(SnapshotSpan lineExtent, List<BlockStructureData> list) => lazyCollection.Collection.GetData(lineExtent, list);
	}
}
