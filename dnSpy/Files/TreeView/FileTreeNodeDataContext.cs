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

using dnSpy.Contracts.Files.TreeView;
using dnSpy.Contracts.Files.TreeView.Resources;
using dnSpy.Contracts.Languages;

namespace dnSpy.Files.TreeView {
	sealed class FileTreeNodeDataContext : IFileTreeNodeDataContext {
		public IFileTreeView FileTreeView { get; private set; }
		public ILanguage Language { get; internal set; }
		public IResourceNodeFactory ResourceNodeFactory { get; private set; }
		public bool SyntaxHighlight { get; internal set; }
		public bool SingleClickExpandsChildren { get; internal set; }
		public bool ShowAssemblyVersion { get; internal set; }
		public bool ShowAssemblyPublicKeyToken { get; internal set; }
		public bool ShowToken { get; internal set; }
		public bool UseNewRenderer { get; internal set; }
		public bool DeserializeResources { get; internal set; }

		public FileTreeNodeDataContext(IFileTreeView fileTreeView, IResourceNodeFactory resourceNodeFactory) {
			this.FileTreeView = fileTreeView;
			this.ResourceNodeFactory = resourceNodeFactory;
		}
	}
}
