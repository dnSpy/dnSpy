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
using System.Windows;
using dnSpy.Contracts.Files.Tabs;
using dnSpy.Contracts.Files.TreeView;

namespace dnSpy.Files.Tabs {
	sealed class NullFileTabContent : IFileTabContent {
		public IFileTab FileTab {
			get { return fileTab; }
			set {
				if (value == null)
					throw new ArgumentNullException();
				if (fileTab == null)
					fileTab = value;
				else if (fileTab != value)
					throw new InvalidOperationException();
			}
		}
		IFileTab fileTab;

		public IFileTabContent Clone() {
			return new NullFileTabContent();
		}

		public IFileTabUIContext CreateUIContext(IFileTabUIContextLocator locator) {
			return locator.Get(typeof(NullFileTabUIContext), () => new NullFileTabUIContext());
		}

		public string Title {
			get { return "<empty>"; }
		}

		public object ToolTip {
			get { return null; }
		}

		public IEnumerable<IFileTreeNodeData> Nodes {
			get { yield break; }
		}

		public void OnHide() {
		}

		public void OnShow(IFileTabUIContext uiContext) {
		}
	}

	sealed class NullFileTabUIContext : IFileTabUIContext {
		public IFileTab FileTab {
			get { return fileTab; }
			set {
				if (value == null)
					throw new ArgumentNullException();
				if (fileTab == null)
					fileTab = value;
				else if (fileTab != value)
					throw new InvalidOperationException();
			}
		}
		IFileTab fileTab;

		public UIElement FocusedElement {
			get { return null; }
		}

		public object UIObject {
			get { return string.Empty; }
		}

		public void Clear() {
		}

		public void Deserialize(object obj) {
		}

		public object Serialize() {
			return null;
		}
	}
}
