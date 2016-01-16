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
using dnSpy.Contracts.Files.Tabs;
using dnSpy.Contracts.Files.Tabs.TextEditor;
using dnSpy.Contracts.Files.TreeView;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.Settings;
using dnSpy.Decompiler.Shared;
using dnSpy.Properties;
using dnSpy.Shared.UI.Decompiler;
using dnSpy.Shared.UI.Menus;

namespace dnSpy.MainApp {
	[Export, ExportFileTabContentFactory(Order = double.MaxValue)]
	sealed class DecompileFileTabContentFactory : IFileTabContentFactory {
		public IFileTabContent Create(IFileTabContentFactoryContext context) {
			return null;
		}

		static readonly Guid GUID_SerializedContent = new Guid("1C931C0F-D968-4664-B22D-87287A226EEC");

		public IFileTabContent Deserialize(Guid guid, ISettingsSection section, IFileTabContentFactoryContext context) {
			if (guid == GUID_SerializedContent)
				return new AboutScreenFileTabContent();
			return null;
		}

		public Guid? Serialize(IFileTabContent content, ISettingsSection section) {
			if (content is AboutScreenFileTabContent)
				return GUID_SerializedContent;
			return null;
		}
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_HELP_GUID, Header = "res:About_Menu", Group = MenuConstants.GROUP_APP_MENU_HELP_ABOUT, Order = 1000000)]
	sealed class AboutScreenMenuItem : MenuItemBase {
		readonly IFileTabManager fileTabManager;

		[ImportingConstructor]
		AboutScreenMenuItem(IFileTabManager fileTabManager) {
			this.fileTabManager = fileTabManager;
		}

		public override void Execute(IMenuItemContext context) {
			var tab = fileTabManager.GetOrCreateActiveTab();
			tab.Show(new AboutScreenFileTabContent(), null, null);
			fileTabManager.SetFocus(tab);
		}
	}

	sealed class AboutScreenFileTabContent : IFileTabContent {
		public IFileTab FileTab { get; set; }

		public IEnumerable<IFileTreeNodeData> Nodes {
			get { yield break; }
		}

		public string Title {
			get { return dnSpy_Resources.About_TabTitle; }
		}

		public object ToolTip {
			get { return null; }
		}

		public IFileTabContent Clone() {
			return new AboutScreenFileTabContent();
		}

		public IFileTabUIContext CreateUIContext(IFileTabUIContextLocator locator) {
			return locator.Get<ITextEditorUIContext>();
		}

		public void OnHide() {
		}

		public void OnSelected() {
		}

		public void OnUnselected() {
		}

		public object OnShow(IFileTabUIContext uiContext) {
			var ctx = (ITextEditorUIContext)uiContext;
			var output = new AvalonEditTextOutput();
			Write(output);
			ctx.SetOutput(output, null);
			return null;
		}

		void Write(AvalonEditTextOutput output) {
			output.WriteLine(string.Format("dnSpy {0}", GetType().Assembly.GetName().Version), TextTokenKind.Text);

			//TODO: Add more stuff...
		}
	}
}
