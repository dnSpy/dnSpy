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
using System.Diagnostics;
using System.Windows;
using dnSpy.Contracts.Files.TreeView;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Languages;
using dnSpy.Contracts.Text;
using dnSpy.Shared.Files.TreeView;

namespace dnSpy.Files.TreeView {
	sealed class RootNode : FileTreeNodeData {
		static readonly Guid guid = new Guid("5112F4B3-3674-43CB-A252-EE9D57A619B8");

		public override Guid Guid => guid;

		public override NodePathName NodePathName {
			get { Debug.Fail("Shouldn't be called"); return new NodePathName(Guid); }
		}

		public override FilterType GetFilterType(IFileTreeNodeFilter filter) {
			Debug.Fail("Shouldn't be called");
			return FilterType.Default;
		}

		protected override ImageReference GetIcon(IDotNetImageManager dnImgMgr) =>
			new ImageReference();

		protected override void Write(IOutputColorWriter output, ILanguage language) { }

		public override bool CanDrop(DragEventArgs e, int index) {
			if (!Context.CanDragAndDrop) {
				e.Effects = DragDropEffects.None;
				return false;
			}

			if (e.Data.GetDataPresent(FileTVConstants.DATAFORMAT_COPIED_ROOT_NODES) || e.Data.GetDataPresent(DataFormats.FileDrop)) {
				e.Effects = DragDropEffects.Move;
				return true;
			}

			e.Effects = DragDropEffects.None;
			return false;
		}

		public override void Drop(DragEventArgs e, int index) {
			Debug.Assert(Context.CanDragAndDrop);
			if (!Context.CanDragAndDrop)
				return;

			var nodeIndexes = e.Data.GetData(FileTVConstants.DATAFORMAT_COPIED_ROOT_NODES) as int[];
			if (nodeIndexes != null) {
				Debug.Assert(DropNodes != null);
				DropNodes?.Invoke(index, nodeIndexes);
				return;
			}

			var filenames = e.Data.GetData(DataFormats.FileDrop) as string[];
			if (filenames != null) {
				Debug.Assert(DropFiles != null);
				DropFiles?.Invoke(index, filenames);
				return;
			}

			Debug.Fail("Unknown drop data format");
		}

		public Action<int, int[]> DropNodes;
		public Action<int, string[]> DropFiles;
	}
}
