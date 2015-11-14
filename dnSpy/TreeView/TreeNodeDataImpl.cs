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
using System.Collections.Generic;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.TreeView;

namespace dnSpy.TreeView {
	sealed class TreeNodeDataImpl : ITreeNodeData {
		public TreeNodeDataImpl(Guid guid) {
			this.guid = guid;
		}

		public ITreeNode TreeNode { get; set; }

		public bool SingleClickExpandsChildren { get; }

		public object Text {
			get { return null; }
		}

		public object ToolTip {
			get { return null; }
		}

		public Guid Guid {
			get { return guid; }
		}
		readonly Guid guid;

		public ITreeNodeGroup TreeNodeGroup {
			get { return null; }
		}

		public ImageReference Icon {
			get { return new ImageReference(); }
		}

		public ImageReference? ExpandedIcon {
			get { return null; }
		}

		public IEnumerable<ITreeNodeData> CreateChildren() {
			yield break;
		}

		public bool ShowExpander(bool defaultValue) {
			return defaultValue;
		}

		public void Initialize() {
		}
	}
}
