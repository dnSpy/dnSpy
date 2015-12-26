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
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using dnSpy.Contracts.App;
using dnSpy.Contracts.Files.Tabs;
using dnSpy.Contracts.Files.TreeView;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.Settings;
using dnSpy.Shared.UI.Hex;
using dnSpy.Shared.UI.HexEditor;

namespace dnSpy.AsmEditor.Hex {
	[ExportFileTabContentFactory(Order = TabConstants.ORDER_HEXBOXFILETABCONTENTFACTORY)]
	sealed class HexBoxFileTabContentFactory : IFileTabContentFactory {
		readonly Lazy<IHexBoxFileTabContentCreator> hexBoxFileTabContentCreator;

		[ImportingConstructor]
		HexBoxFileTabContentFactory(Lazy<IHexBoxFileTabContentCreator> hexBoxFileTabContentCreator) {
			this.hexBoxFileTabContentCreator = hexBoxFileTabContentCreator;
		}

		public IFileTabContent Create(IFileTabContentFactoryContext context) {
			return null;
		}

		static readonly Guid GUID_SerializedContent = new Guid("3125CEDA-98DE-447E-9363-8583A45BDE8C");

		public Guid? Serialize(IFileTabContent content, ISettingsSection section) {
			var hb = content as HexBoxFileTabContent;
			if (hb == null)
				return null;

			section.Attribute("filename", hb.Filename);
			return GUID_SerializedContent;
		}

		public IFileTabContent Deserialize(Guid guid, ISettingsSection section, IFileTabContentFactoryContext context) {
			if (guid != GUID_SerializedContent)
				return null;

			var filename = section.Attribute<string>("filename");
			return hexBoxFileTabContentCreator.Value.TryCreate(filename);
		}
	}

	interface IHexBoxFileTabContentCreator {
		HexBoxFileTabContent TryCreate(string filename);
	}

	[Export, Export(typeof(IHexBoxFileTabContentCreator)), PartCreationPolicy(CreationPolicy.Shared)]
	sealed class HexBoxFileTabContentCreator : IHexBoxFileTabContentCreator {
		readonly Lazy<IHexDocumentManager> hexDocumentManager;
		readonly IMenuManager menuManager;
		readonly IHexEditorSettings hexEditorSettings;
		readonly IAppSettings appSettings;
		readonly Lazy<IHexBoxUndoManager> hexBoxUndoManager;

		[ImportingConstructor]
		HexBoxFileTabContentCreator(Lazy<IHexDocumentManager> hexDocumentManager, IMenuManager menuManager, IHexEditorSettings hexEditorSettings, IAppSettings appSettings, Lazy<IHexBoxUndoManager> hexBoxUndoManager) {
			this.hexDocumentManager = hexDocumentManager;
			this.menuManager = menuManager;
			this.hexEditorSettings = hexEditorSettings;
			this.appSettings = appSettings;
			this.hexBoxUndoManager = hexBoxUndoManager;
		}

		public HexBoxFileTabContent TryCreate(string filename) {
			var doc = hexDocumentManager.Value.GetOrCreate(filename);
			if (doc == null)
				return null;

			return new HexBoxFileTabContent(doc, menuManager, hexEditorSettings, appSettings, hexBoxUndoManager);
		}
	}

	sealed class HexBoxFileTabContent : IFileTabContent {
		public IFileTab FileTab { get; set; }

		public IEnumerable<IFileTreeNodeData> Nodes {
			get { yield break; }
		}

		public string Title {
			get {
				var filename = Filename;
				try {
					return Path.GetFileName(filename);
				}
				catch {
				}
				return filename;
			}
		}

		public object ToolTip {
			get { return Filename; }
		}

		public string Filename {
			get { return hexDocument.Name; }
		}

		readonly HexDocument hexDocument;
		readonly IMenuManager menuManager;
		readonly IHexEditorSettings hexEditorSettings;
		readonly IAppSettings appSettings;
		readonly Lazy<IHexBoxUndoManager> hexBoxUndoManager;

		public HexBoxFileTabContent(HexDocument hexDocument, IMenuManager menuManager, IHexEditorSettings hexEditorSettings, IAppSettings appSettings, Lazy<IHexBoxUndoManager> hexBoxUndoManager) {
			if (hexDocument == null)
				throw new ArgumentNullException();
			this.hexDocument = hexDocument;
			this.menuManager = menuManager;
			this.hexEditorSettings = hexEditorSettings;
			this.appSettings = appSettings;
			this.hexBoxUndoManager = hexBoxUndoManager;
		}

		public IFileTabContent Clone() {
			return new HexBoxFileTabContent(hexDocument, menuManager, hexEditorSettings, appSettings, hexBoxUndoManager);
		}

		public IFileTabUIContext CreateUIContext(IFileTabUIContextLocator locator) {
			return locator.Get(hexDocument, () => new HexBoxFileTabUIContext(hexDocument, menuManager, hexEditorSettings, appSettings, hexBoxUndoManager.Value));
		}

		public void OnHide() {
		}

		public void OnSelected() {
		}

		public object OnShow(IFileTabUIContext uiContext) {
			return null;
		}

		public void OnUnselected() {
		}
	}

	sealed class HexBoxFileTabUIContext : IFileTabUIContext, IDisposable {
		public IFileTab FileTab { get; set; }

		public IInputElement FocusedElement {
			get { return dnHexBox; }
		}

		public FrameworkElement ScaleElement {
			get { return dnHexBox; }
		}

		public object UIObject {
			get { return scrollViewer; }
		}

		public DnHexBox DnHexBox {
			get { return dnHexBox; }
		}

		readonly HexDocument hexDocument;
		readonly DnHexBox dnHexBox;
		readonly ScrollViewer scrollViewer;
		readonly IAppSettings appSettings;
		readonly IHexBoxUndoManager hexBoxUndoManager;

		public HexBoxFileTabUIContext(HexDocument hexDocument, IMenuManager menuManager, IHexEditorSettings hexEditorSettings, IAppSettings appSettings, IHexBoxUndoManager hexBoxUndoManager) {
			this.hexDocument = hexDocument;
			this.dnHexBox = new DnHexBox(menuManager, hexEditorSettings);
			this.dnHexBox.Document = this.hexDocument;
			this.dnHexBox.InitializeStartEndOffsetToDocument();
			this.scrollViewer = new ScrollViewer {
				Content = this.dnHexBox,
				CanContentScroll = true,
				HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
				VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
				Focusable = false,
			};
			this.appSettings = appSettings;
			this.hexBoxUndoManager = hexBoxUndoManager;
			appSettings.PropertyChanged += AppSettings_PropertyChanged;
			UpdateHexBoxRenderer(appSettings.UseNewRenderer_HexEditor);
			hexBoxUndoManager.Initialize(this.dnHexBox);
		}

		void AppSettings_PropertyChanged(object sender, PropertyChangedEventArgs e) {
			var appSettings = (IAppSettings)sender;
			if (e.PropertyName == "UseNewRenderer_HexEditor")
				UpdateHexBoxRenderer(appSettings.UseNewRenderer_HexEditor);
		}

		void UpdateHexBoxRenderer(bool useNewRenderer) {
			this.DnHexBox.UseNewFormatter = useNewRenderer;
		}

		public object CreateSerialized(ISettingsSection section) {
			return HexBoxUIStateSerializer.Read(section, new HexBoxUIState());
		}

		public void SaveSerialized(ISettingsSection section, object obj) {
			var s = obj as HexBoxUIState;
			if (s == null)
				return;
			HexBoxUIStateSerializer.Write(section, s);
		}

		public void Deserialize(object obj) {
			var s = obj as HexBoxUIState;
			if (s == null || s.HexBoxState == null)
				return;

			DnHexBox.BytesGroupCount = s.BytesGroupCount;
			DnHexBox.BytesPerLine = s.BytesPerLine;
			DnHexBox.UseHexPrefix = s.UseHexPrefix;
			DnHexBox.ShowAscii = s.ShowAscii;
			DnHexBox.LowerCaseHex = s.LowerCaseHex;
			DnHexBox.AsciiEncoding = s.AsciiEncoding;

			DnHexBox.HexOffsetSize = s.HexOffsetSize;
			DnHexBox.UseRelativeOffsets = s.UseRelativeOffsets;
			DnHexBox.BaseOffset = s.BaseOffset;
			if (DnHexBox.IsLoaded)
				DnHexBox.State = s.HexBoxState;
			else
				new StateRestorer(DnHexBox, s.HexBoxState);
		}

		sealed class StateRestorer {
			readonly HexBox hexBox;
			readonly HexBoxState state;

			public StateRestorer(HexBox hexBox, HexBoxState state) {
				this.hexBox = hexBox;
				this.state = state;
				this.hexBox.Loaded += HexBox_Loaded;
			}

			private void HexBox_Loaded(object sender, RoutedEventArgs e) {
				this.hexBox.Loaded -= HexBox_Loaded;
				hexBox.UpdateLayout();
				hexBox.State = state;
			}
		}

		public object Serialize() {
			var s = new HexBoxUIState();
			s.BytesGroupCount = DnHexBox.BytesGroupCount;
			s.BytesPerLine = DnHexBox.BytesPerLine;
			s.UseHexPrefix = DnHexBox.UseHexPrefix;
			s.ShowAscii = DnHexBox.ShowAscii;
			s.LowerCaseHex = DnHexBox.LowerCaseHex;
			s.AsciiEncoding = DnHexBox.AsciiEncoding;

			s.HexOffsetSize = DnHexBox.HexOffsetSize;
			s.UseRelativeOffsets = DnHexBox.UseRelativeOffsets;
			s.BaseOffset = DnHexBox.BaseOffset;
			s.HexBoxState = DnHexBox.State;
			return s;
		}

		public void OnHide() {
		}

		public void OnShow() {
		}

		public void Dispose() {
			appSettings.PropertyChanged -= AppSettings_PropertyChanged;
			hexBoxUndoManager.Uninitialize(this.dnHexBox);
		}
	}
}
