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
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using dnSpy.Contracts.App;
using dnSpy.Contracts.Documents.Tabs;
using dnSpy.Contracts.Documents.TreeView;
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.HexEditor;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.Settings;

namespace dnSpy.AsmEditor.Hex {
	[ExportDocumentTabContentFactory(Order = TabConstants.ORDER_HEXBOXDOCUMENTTABCONTENTFACTORY)]
	sealed class HexBoxDocumentTabContentFactory : IDocumentTabContentFactory {
		readonly Lazy<IHexBoxDocumentTabContentCreator> hexBoxDocumentTabContentCreator;

		[ImportingConstructor]
		HexBoxDocumentTabContentFactory(Lazy<IHexBoxDocumentTabContentCreator> hexBoxDocumentTabContentCreator) {
			this.hexBoxDocumentTabContentCreator = hexBoxDocumentTabContentCreator;
		}

		public IDocumentTabContent Create(IDocumentTabContentFactoryContext context) => null;

		static readonly Guid GUID_SerializedContent = new Guid("3125CEDA-98DE-447E-9363-8583A45BDE8C");

		public Guid? Serialize(IDocumentTabContent content, ISettingsSection section) {
			var hb = content as HexBoxDocumentTabContent;
			if (hb == null)
				return null;

			section.Attribute("filename", hb.Filename);
			return GUID_SerializedContent;
		}

		public IDocumentTabContent Deserialize(Guid guid, ISettingsSection section, IDocumentTabContentFactoryContext context) {
			if (guid != GUID_SerializedContent)
				return null;

			var filename = section.Attribute<string>("filename");
			return hexBoxDocumentTabContentCreator.Value.TryCreate(filename);
		}
	}

	interface IHexBoxDocumentTabContentCreator {
		HexBoxDocumentTabContent TryCreate(string filename);
	}

	[Export(typeof(IHexBoxDocumentTabContentCreator))]
	sealed class HexBoxDocumentTabContentCreator : IHexBoxDocumentTabContentCreator {
		readonly Lazy<IHexDocumentService> hexDocumentService;
		readonly IMenuService menuService;
		readonly IHexEditorSettings hexEditorSettings;
		readonly IAppSettings appSettings;
		readonly Lazy<IHexBoxUndoService> hexBoxUndoService;

		[ImportingConstructor]
		HexBoxDocumentTabContentCreator(Lazy<IHexDocumentService> hexDocumentService, IMenuService menuService, IHexEditorSettings hexEditorSettings, IAppSettings appSettings, Lazy<IHexBoxUndoService> hexBoxUndoService) {
			this.hexDocumentService = hexDocumentService;
			this.menuService = menuService;
			this.hexEditorSettings = hexEditorSettings;
			this.appSettings = appSettings;
			this.hexBoxUndoService = hexBoxUndoService;
		}

		public HexBoxDocumentTabContent TryCreate(string filename) {
			var doc = hexDocumentService.Value.GetOrCreate(filename);
			if (doc == null)
				return null;

			return new HexBoxDocumentTabContent(doc, menuService, hexEditorSettings, appSettings, hexBoxUndoService);
		}
	}

	sealed class HexBoxDocumentTabContent : IDocumentTabContent {
		public IDocumentTab DocumentTab { get; set; }

		public IEnumerable<IDocumentTreeNodeData> Nodes {
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

		public object ToolTip => Filename;
		public string Filename => hexDocument.Name;

		readonly HexDocument hexDocument;
		readonly IMenuService menuService;
		readonly IHexEditorSettings hexEditorSettings;
		readonly IAppSettings appSettings;
		readonly Lazy<IHexBoxUndoService> hexBoxUndoService;

		public HexBoxDocumentTabContent(HexDocument hexDocument, IMenuService menuService, IHexEditorSettings hexEditorSettings, IAppSettings appSettings, Lazy<IHexBoxUndoService> hexBoxUndoService) {
			if (hexDocument == null)
				throw new ArgumentNullException(nameof(hexDocument));
			this.hexDocument = hexDocument;
			this.menuService = menuService;
			this.hexEditorSettings = hexEditorSettings;
			this.appSettings = appSettings;
			this.hexBoxUndoService = hexBoxUndoService;
		}

		public bool CanClone => true;
		public IDocumentTabContent Clone() =>
			new HexBoxDocumentTabContent(hexDocument, menuService, hexEditorSettings, appSettings, hexBoxUndoService);
		public IDocumentTabUIContext CreateUIContext(IDocumentTabUIContextLocator locator) =>
			locator.Get(hexDocument, () => new HexBoxDocumentTabUIContext(hexDocument, menuService, hexEditorSettings, appSettings, hexBoxUndoService.Value));
		public void OnHide() { }
		public void OnSelected() { }
		public void OnShow(IShowContext ctx) { }
		public void OnUnselected() { }
	}

	sealed class HexBoxDocumentTabUIContext : IDocumentTabUIContext, IDisposable {
		public IDocumentTab DocumentTab { get; set; }
		public IInputElement FocusedElement => dnHexBox;
		public FrameworkElement ZoomElement => dnHexBox;
		public object UIObject => scrollViewer;
		public DnHexBox DnHexBox => dnHexBox;

		readonly HexDocument hexDocument;
		readonly DnHexBox dnHexBox;
		readonly ScrollViewer scrollViewer;
		readonly IAppSettings appSettings;
		readonly IHexBoxUndoService hexBoxUndoService;

		public HexBoxDocumentTabUIContext(HexDocument hexDocument, IMenuService menuService, IHexEditorSettings hexEditorSettings, IAppSettings appSettings, IHexBoxUndoService hexBoxUndoService) {
			this.hexDocument = hexDocument;
			this.dnHexBox = new DnHexBox(menuService, hexEditorSettings);
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
			this.hexBoxUndoService = hexBoxUndoService;
			appSettings.PropertyChanged += AppSettings_PropertyChanged;
			UpdateHexBoxRenderer(appSettings.UseNewRenderer_HexEditor);
			hexBoxUndoService.Initialize(this.dnHexBox);
		}

		void AppSettings_PropertyChanged(object sender, PropertyChangedEventArgs e) {
			var appSettings = (IAppSettings)sender;
			if (e.PropertyName == nameof(appSettings.UseNewRenderer_HexEditor))
				UpdateHexBoxRenderer(appSettings.UseNewRenderer_HexEditor);
		}

		void UpdateHexBoxRenderer(bool useNewRenderer) => this.DnHexBox.UseNewFormatter = useNewRenderer;
		public object CreateSerialized(ISettingsSection section) => HexBoxUIStateSerializer.Read(section, new HexBoxUIState());

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

		public void OnHide() { }
		public void OnShow() { }

		public void Dispose() {
			appSettings.PropertyChanged -= AppSettings_PropertyChanged;
			hexBoxUndoService.Uninitialize(this.dnHexBox);
		}
	}
}
