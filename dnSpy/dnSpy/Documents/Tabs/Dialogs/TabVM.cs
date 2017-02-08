/*
    Copyright (C) 2014-2017 de4dot@gmail.com

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
using dnSpy.Contracts.Documents;
using dnSpy.Contracts.Documents.Tabs;
using dnSpy.Contracts.Documents.TreeView;
using dnSpy.Contracts.MVVM;

namespace dnSpy.Documents.Tabs.Dialogs {
	sealed class TabVM : ViewModelBase {
		public IDocumentTab Tab { get; }
		public object NameObject => this;
		public object ModuleObject => this;
		public object PathObject => this;
		public string Name => Tab.Content.Title;
		public TabsVM Owner { get; }

		readonly IDsDocument document;

		public string Module {
			get {
				if (document == null)
					return string.Empty;
				return System.IO.Path.GetFileName(document.Filename);
			}
		}

		public string Path {
			get {
				if (document == null)
					return string.Empty;
				return document.Filename;
			}
		}

		public TabVM(TabsVM owner, IDocumentTab tab) {
			Owner = owner;
			Tab = tab;
			var node = tab.Content.Nodes.FirstOrDefault().GetTopNode();
			document = node?.Document;
		}
	}
}
