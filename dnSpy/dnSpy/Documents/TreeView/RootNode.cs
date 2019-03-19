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
using System.Diagnostics;
using System.Windows;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Documents.TreeView;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Text;

namespace dnSpy.Documents.TreeView {
	sealed class RootNode : DocumentTreeNodeData {
		static readonly Guid guid = new Guid("5112F4B3-3674-43CB-A252-EE9D57A619B8");

		public override Guid Guid => guid;

		public override NodePathName NodePathName {
			get { Debug.Fail("Shouldn't be called"); return new NodePathName(Guid); }
		}

		public override FilterType GetFilterType(IDocumentTreeNodeFilter filter) {
			Debug.Fail("Shouldn't be called");
			return FilterType.Default;
		}

		protected override ImageReference GetIcon(IDotNetImageService dnImgMgr) =>
			new ImageReference();

		protected override void WriteCore(ITextColorWriter output, IDecompiler decompiler, DocumentNodeWriteOptions options) { }

		public override bool CanDrop(DragEventArgs e, int index) {
			if (!Context.CanDragAndDrop) {
				e.Effects = DragDropEffects.None;
				return false;
			}

			if (e.Data.GetDataPresent(DocumentTreeViewConstants.DATAFORMAT_COPIED_ROOT_NODES) || e.Data.GetDataPresent(DataFormats.FileDrop)) {
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

			if (e.Data.GetData(DocumentTreeViewConstants.DATAFORMAT_COPIED_ROOT_NODES) is int[] nodeIndexes) {
				Debug.Assert(DropNodes != null);
				DropNodes?.Invoke(index, nodeIndexes);
				return;
			}

			if (e.Data.GetData(DataFormats.FileDrop) is string[] filenames) {
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
