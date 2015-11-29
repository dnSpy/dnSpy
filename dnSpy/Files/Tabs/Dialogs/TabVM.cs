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

using System.Linq;
using dnSpy.Contracts.Files;
using dnSpy.Contracts.Files.Tabs;
using dnSpy.Contracts.Files.TreeView;
using dnSpy.Shared.UI.MVVM;

namespace dnSpy.Files.Tabs.Dialogs {
	sealed class TabVM : ViewModelBase {
		public IFileTab Tab {
			get { return tab; }
		}
		readonly IFileTab tab;

		readonly IDnSpyFile file;

		public object NameObject { get { return this; } }
		public object ModuleObject { get { return this; } }
		public object PathObject { get { return this; } }

		public string Name {
			get { return tab.Content.Title; }
		}

		public string Module {
			get {
				if (file == null)
					return string.Empty;
				return System.IO.Path.GetFileName(file.Filename);
			}
		}

		public string Path {
			get {
				if (file == null)
					return string.Empty;
				return file.Filename;
			}
		}

		public TabsVM Owner {
			get { return owner; }
		}
		readonly TabsVM owner;

		public TabVM(TabsVM owner, IFileTab tab) {
			this.owner = owner;
			this.tab = tab;
			var node = tab.Content.Nodes.FirstOrDefault().GetTopNode();
			this.file = node == null ? null : node.DnSpyFile;
		}
	}
}
