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
using System.Diagnostics;
using dnSpy.Contracts.Files.TreeView;
using dnSpy.Contracts.Highlighting;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Languages;
using dnSpy.Shared.UI.Files.TreeView;

namespace dnSpy.Files.TreeView {
	sealed class RootNode : FileTreeNodeData {
		static readonly Guid guid = new Guid("5112F4B3-3674-43CB-A252-EE9D57A619B8");

		public override Guid Guid {
			get { return guid; }
		}

		public override NodePathName NodePathName {
			get { Debug.Fail("Shouldn't be called"); return new NodePathName(Guid); }
		}

		public override FilterType GetFilterType(IFileTreeNodeFilter filter) {
			Debug.Fail("Shouldn't be called");
			return FilterType.Default;
		}

		protected override ImageReference GetIcon(IDotNetImageManager dnImgMgr) {
			return new ImageReference();
		}

		protected override void Write(ISyntaxHighlightOutput output, ILanguage language) {
		}
	}
}
