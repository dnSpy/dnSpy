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
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Themes;
using dnSpy.Contracts.TreeView;

namespace dnSpy.TreeView {
	[Export, Export(typeof(ITreeViewManager)), PartCreationPolicy(CreationPolicy.Shared)]
	sealed class TreeViewManager : ITreeViewManager {
		readonly IThemeManager themeManager;
		readonly IImageManager imageManager;
		readonly Dictionary<Guid, List<Lazy<ITreeNodeDataCreator, ITreeNodeDataCreatorMetadata>>> guidToCreator;

		[ImportingConstructor]
		TreeViewManager(IThemeManager themeManager, IImageManager imageManager, [ImportMany] IEnumerable<Lazy<ITreeNodeDataCreator, ITreeNodeDataCreatorMetadata>> treeNodeDataCreators) {
			this.themeManager = themeManager;
			this.imageManager = imageManager;
			this.guidToCreator = new Dictionary<Guid, List<Lazy<ITreeNodeDataCreator, ITreeNodeDataCreatorMetadata>>>();
			InitializeGuidToCreator(treeNodeDataCreators);
		}

		void InitializeGuidToCreator(IEnumerable<Lazy<ITreeNodeDataCreator, ITreeNodeDataCreatorMetadata>> treeNodeDataCreators) {
			foreach (var creator in treeNodeDataCreators.OrderBy(a => a.Metadata.Order)) {
				Guid guid;
				bool b = Guid.TryParse(creator.Metadata.Guid, out guid);
				Debug.Assert(b, string.Format("Couldn't parse guid: '{0}'", creator.Metadata.Guid));
				if (!b)
					continue;

				List<Lazy<ITreeNodeDataCreator, ITreeNodeDataCreatorMetadata>> list;
				if (!guidToCreator.TryGetValue(guid, out list))
					guidToCreator.Add(guid, list = new List<Lazy<ITreeNodeDataCreator, ITreeNodeDataCreatorMetadata>>());
				list.Add(creator);
			}
		}

		public ITreeView Create(Guid guid, TreeViewOptions options) {
			return new TreeViewImpl(this, themeManager, imageManager, guid, options);
		}

		public IEnumerable<ITreeNodeDataCreator> GetCreators(Guid guid) {
			List<Lazy<ITreeNodeDataCreator, ITreeNodeDataCreatorMetadata>> list;
			if (!guidToCreator.TryGetValue(guid, out list))
				return new ITreeNodeDataCreator[0];
			return list.Select(a => a.Value);
		}
	}
}
