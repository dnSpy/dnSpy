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
using dnSpy.Contracts.Files.Tabs.DocViewer;
using dnSpy.Contracts.Text.Editor;
using Microsoft.VisualStudio.Text;

namespace dnSpy.Files.Tabs.DocViewer {
	sealed class StructureVisualizerServiceDataProvider : IStructureVisualizerServiceDataProvider {
		readonly LazyStructureVisualizerCollection lazyCollection;

		StructureVisualizerServiceDataProvider(LazyStructureVisualizerCollection lazyCollection) {
			if (lazyCollection == null)
				throw new ArgumentNullException(nameof(lazyCollection));
			this.lazyCollection = lazyCollection;
		}

		public static IStructureVisualizerServiceDataProvider TryCreate(IDocumentViewer documentViewer) {
			var lazyColl = documentViewer.Content.GetCustomData<LazyStructureVisualizerCollection>(DocumentViewerContentDataIds.StructureVisualizer);
			return lazyColl == null ? null : new StructureVisualizerServiceDataProvider(lazyColl);
		}

		public void GetData(SnapshotSpan lineExtent, List<StructureVisualizerData> list) => lazyCollection.Collection.GetData(lineExtent, list);
	}
}
