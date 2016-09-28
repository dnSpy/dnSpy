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
using System.ComponentModel.Composition;
using System.Linq;
using dnSpy.Contracts.Documents.Tabs.DocViewer;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Menus;

namespace dnSpy.BamlDecompiler {
	static class Constants {
		public const string GROUP_CTX_FILES_BAML_OPTIONS = "500,42F3D00A-71E7-4BC6-859E-2ADDAE583213";
	}

	[ExportMenuItem(Header = "res:DisassembleBAMLCommand", Icon = DsImagesAttribute.WPFFile, Group = Constants.GROUP_CTX_FILES_BAML_OPTIONS, Order = 0)]
	sealed class DisassembleBamlCommand : MenuItemBase {
		readonly BamlSettingsImpl bamlSettings;

		[ImportingConstructor]
		DisassembleBamlCommand(BamlSettingsImpl bamlSettings) {
			this.bamlSettings = bamlSettings;
		}

		public override void Execute(IMenuItemContext context) => bamlSettings.DisassembleBaml = !bamlSettings.DisassembleBaml;
		public override bool IsChecked(IMenuItemContext context) => bamlSettings.DisassembleBaml;

		public override bool IsVisible(IMenuItemContext context) {
			if (context.CreatorObject.Guid != new Guid(MenuConstants.GUIDOBJ_DOCUMENTVIEWERCONTROL_GUID))
				return false;
			var uiContext = context.Find<IDocumentViewer>();
			if (uiContext == null)
				return false;
			var nodes = uiContext.DocumentTab.Content.Nodes.ToArray();
			return nodes.Length == 1 && nodes[0] is BamlResourceElementNode;
		}
	}
}
