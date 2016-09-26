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
	[Export(typeof(ITreeViewService))]
	sealed class TreeViewService : ITreeViewService {
		readonly IThemeService themeService;
		readonly IImageService imageService;
		readonly Dictionary<Guid, List<Lazy<ITreeNodeDataProvider, ITreeNodeDataProviderMetadata>>> guidToProvider;

		[ImportingConstructor]
		TreeViewService(IThemeService themeService, IImageService imageService, [ImportMany] IEnumerable<Lazy<ITreeNodeDataProvider, ITreeNodeDataProviderMetadata>> treeNodeDataProviders) {
			this.themeService = themeService;
			this.imageService = imageService;
			this.guidToProvider = new Dictionary<Guid, List<Lazy<ITreeNodeDataProvider, ITreeNodeDataProviderMetadata>>>();
			InitializeGuidToProvider(treeNodeDataProviders);
		}

		void InitializeGuidToProvider(IEnumerable<Lazy<ITreeNodeDataProvider, ITreeNodeDataProviderMetadata>> treeNodeDataProviders) {
			foreach (var provider in treeNodeDataProviders.OrderBy(a => a.Metadata.Order)) {
				Guid guid;
				bool b = Guid.TryParse(provider.Metadata.Guid, out guid);
				Debug.Assert(b, string.Format("Couldn't parse guid: '{0}'", provider.Metadata.Guid));
				if (!b)
					continue;

				List<Lazy<ITreeNodeDataProvider, ITreeNodeDataProviderMetadata>> list;
				if (!guidToProvider.TryGetValue(guid, out list))
					guidToProvider.Add(guid, list = new List<Lazy<ITreeNodeDataProvider, ITreeNodeDataProviderMetadata>>());
				list.Add(provider);
			}
		}

		public ITreeView Create(Guid guid, TreeViewOptions options) => new TreeViewImpl(this, themeService, imageService, guid, options);

		public IEnumerable<ITreeNodeDataProvider> GetProviders(Guid guid) {
			List<Lazy<ITreeNodeDataProvider, ITreeNodeDataProviderMetadata>> list;
			if (!guidToProvider.TryGetValue(guid, out list))
				return Array.Empty<ITreeNodeDataProvider>();
			return list.Select(a => a.Value);
		}
	}
}
