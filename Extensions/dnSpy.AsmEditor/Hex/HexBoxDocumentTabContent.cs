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
using System.IO;
using System.Windows;
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.Documents.Tabs;
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.Hex.Editor;
using dnSpy.Contracts.Hex.Editor.HexGroups;
using dnSpy.Contracts.Hex.Editor.OptionsExtensionMethods;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.Settings;
using VSTE = Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.AsmEditor.Hex {
	[ExportDocumentTabContentFactory(Order = TabConstants.ORDER_ASMED_HEXVIEWDOCUMENTTABCONTENTFACTORY)]
	sealed class HexViewDocumentTabContentFactory : IDocumentTabContentFactory {
		readonly Lazy<IHexViewDocumentTabContentCreator> hexViewDocumentTabContentCreator;

		[ImportingConstructor]
		HexViewDocumentTabContentFactory(Lazy<IHexViewDocumentTabContentCreator> hexViewDocumentTabContentCreator) {
			this.hexViewDocumentTabContentCreator = hexViewDocumentTabContentCreator;
		}

		public DocumentTabContent Create(IDocumentTabContentFactoryContext context) => null;

		static readonly Guid GUID_SerializedContent = new Guid("3125CEDA-98DE-447E-9363-8583A45BDE8C");

		public Guid? Serialize(DocumentTabContent content, ISettingsSection section) {
			var hb = content as HexViewDocumentTabContent;
			if (hb == null)
				return null;

			section.Attribute("filename", hb.Filename);
			return GUID_SerializedContent;
		}

		public DocumentTabContent Deserialize(Guid guid, ISettingsSection section, IDocumentTabContentFactoryContext context) {
			if (guid != GUID_SerializedContent)
				return null;

			var filename = section.Attribute<string>("filename");
			return hexViewDocumentTabContentCreator.Value.TryCreate(filename);
		}
	}

	interface IHexViewDocumentTabContentCreator {
		HexViewDocumentTabContent TryCreate(string filename);
	}

	[Export(typeof(IHexViewDocumentTabContentCreator))]
	sealed class HexViewDocumentTabContentCreator : IHexViewDocumentTabContentCreator {
		readonly Lazy<IHexBufferService> hexBufferService;
		readonly Lazy<HexEditorGroupFactoryService> hexEditorGroupFactoryService;

		[ImportingConstructor]
		HexViewDocumentTabContentCreator(Lazy<IHexBufferService> hexBufferService, Lazy<HexEditorGroupFactoryService> hexEditorGroupFactoryService) {
			this.hexBufferService = hexBufferService;
			this.hexEditorGroupFactoryService = hexEditorGroupFactoryService;
		}

		public HexViewDocumentTabContent TryCreate(string filename) {
			var buffer = hexBufferService.Value.GetOrCreate(filename);
			if (buffer == null)
				return null;

			return new HexViewDocumentTabContent(hexEditorGroupFactoryService, buffer);
		}
	}

	sealed class HexViewDocumentTabContent : DocumentTabContent {
		public override string Title {
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

		public override object ToolTip => Filename;
		public string Filename => buffer.Name;

		readonly HexBuffer buffer;
		readonly Lazy<HexEditorGroupFactoryService> hexEditorGroupFactoryService;

		public HexViewDocumentTabContent(Lazy<HexEditorGroupFactoryService> hexEditorGroupFactoryService, HexBuffer buffer) {
			if (buffer == null)
				throw new ArgumentNullException(nameof(buffer));
			this.buffer = buffer;
			this.hexEditorGroupFactoryService = hexEditorGroupFactoryService;
		}

		public override DocumentTabContent Clone() =>
			new HexViewDocumentTabContent(hexEditorGroupFactoryService, buffer);
		public override DocumentTabUIContext CreateUIContext(IDocumentTabUIContextLocator locator) =>
			locator.Get(buffer, useStrongReference: true, creator: () => new HexViewDocumentTabUIContext(hexEditorGroupFactoryService.Value, buffer));
	}

	sealed class HexViewDocumentTabUIContext : DocumentTabUIContext, IDisposable, IZoomable {
		public override IInputElement FocusedElement => hexViewHost.HexView.VisualElement;
		public override FrameworkElement ZoomElement => null;
		public override object UIObject => hexViewHost.HostControl;
		public WpfHexView HexView => hexViewHost.HexView;
		double IZoomable.ZoomValue => hexViewHost.HexView.ZoomLevel / 100;

		readonly WpfHexViewHost hexViewHost;

		public HexViewDocumentTabUIContext(HexEditorGroupFactoryService hexEditorGroupFactoryService, HexBuffer buffer) {
			hexViewHost = hexEditorGroupFactoryService.Create(buffer, PredefinedHexViewRoles.HexEditorGroup, PredefinedHexViewRoles.HexEditorGroupDefault, new Guid(MenuConstants.GUIDOBJ_ASMEDITOR_HEXVIEW_GUID));
		}

		public override object CreateUIState() {
			if (cachedHexViewUIState != null)
				return cachedHexViewUIState;
			var state = new HexViewUIState(HexView);
			state.ShowOffsetColumn = HexView.Options.ShowOffsetColumn();
			state.ShowValuesColumn = HexView.Options.ShowValuesColumn();
			state.ShowAsciiColumn = HexView.Options.ShowAsciiColumn();
			state.StartPosition = HexView.Options.GetStartPosition();
			state.EndPosition = HexView.Options.GetEndPosition();
			state.BasePosition = HexView.Options.GetBasePosition();
			state.UseRelativePositions = HexView.Options.UseRelativePositions();
			state.OffsetBitSize = HexView.Options.GetOffsetBitSize();
			state.HexValuesDisplayFormat = HexView.Options.GetValuesDisplayFormat();
			state.BytesPerLine = HexView.Options.GetBytesPerLine();
			return state;
		}

		public override void RestoreUIState(object obj) {
			var state = obj as HexViewUIState;
			if (state == null)
				return;

			if (!HexView.VisualElement.IsLoaded) {
				bool start = cachedHexViewUIState == null;
				cachedHexViewUIState = state;
				if (start)
					HexView.VisualElement.Loaded += VisualElement_Loaded;
			}
			else
				InitializeState(state);
		}
		HexViewUIState cachedHexViewUIState;

		void InitializeState(HexViewUIState state) {
			if (IsValid(state)) {
				HexView.Options.SetOptionValue(DefaultHexViewOptions.ShowOffsetColumnId, state.ShowOffsetColumn);
				HexView.Options.SetOptionValue(DefaultHexViewOptions.ShowValuesColumnId, state.ShowValuesColumn);
				HexView.Options.SetOptionValue(DefaultHexViewOptions.ShowAsciiColumnId, state.ShowAsciiColumn);
				HexView.Options.SetOptionValue(DefaultHexViewOptions.StartPositionId, state.StartPosition);
				HexView.Options.SetOptionValue(DefaultHexViewOptions.EndPositionId, state.EndPosition);
				HexView.Options.SetOptionValue(DefaultHexViewOptions.BasePositionId, state.BasePosition);
				HexView.Options.SetOptionValue(DefaultHexViewOptions.UseRelativePositionsId, state.UseRelativePositions);
				HexView.Options.SetOptionValue(DefaultHexViewOptions.OffsetBitSizeId, state.OffsetBitSize);
				HexView.Options.SetOptionValue(DefaultHexViewOptions.HexValuesDisplayFormatId, state.HexValuesDisplayFormat);
				HexView.Options.SetOptionValue(DefaultHexViewOptions.BytesPerLineId, state.BytesPerLine);

				HexView.ViewportLeft = state.ViewportLeft;
				HexView.DisplayHexLineContainingBufferPosition(new HexBufferPoint(HexView.Buffer, state.TopLinePosition), state.TopLineVerticalDistance, VSTE.ViewRelativePosition.Top, null, null, DisplayHexLineOptions.CanRecreateBufferLines);

				var valuesPos = new HexCellPosition(HexColumnType.Values, new HexBufferPoint(HexView.Buffer, state.ValuesPosition), state.ValuesCellPosition);
				var asciiPos = new HexCellPosition(HexColumnType.Ascii, new HexBufferPoint(HexView.Buffer, state.AsciiPosition), 0);
				var newPos = new HexColumnPosition(state.ActiveColumn, valuesPos, asciiPos);
				// BufferLines could've been recreated, re-verify the new position
				if (HexView.BufferLines.IsValidPosition(newPos.ValuePosition.BufferPosition) && HexView.BufferLines.IsValidPosition(newPos.AsciiPosition.BufferPosition))
					HexView.Caret.MoveTo(newPos);

				var anchorPoint = new HexBufferPoint(HexView.Buffer, state.AnchorPoint);
				var activePoint = new HexBufferPoint(HexView.Buffer, state.ActivePoint);
				if (HexView.BufferLines.IsValidPosition(anchorPoint) && HexView.BufferLines.IsValidPosition(activePoint))
					HexView.Selection.Select(anchorPoint, activePoint, alignPoints: false);
				else
					HexView.Selection.Clear();
			}
			else {
				HexView.Caret.MoveTo(HexView.BufferLines.BufferStart);
				HexView.Selection.Clear();
			}
		}

		bool IsValid(HexViewUIState state) {
			if (state.ActiveColumn != HexColumnType.Values && state.ActiveColumn != HexColumnType.Ascii)
				return false;
			if (state.StartPosition >= HexPosition.MaxEndPosition)
				return false;
			if (state.EndPosition > HexPosition.MaxEndPosition)
				return false;
			if (state.BasePosition >= HexPosition.MaxEndPosition)
				return false;
			if (state.EndPosition < state.StartPosition)
				return false;
			if (state.OffsetBitSize < HexBufferLineFormatterOptions.MinOffsetBitSize || state.OffsetBitSize > HexBufferLineFormatterOptions.MaxOffsetBitSize)
				return false;
			if (state.HexValuesDisplayFormat < HexBufferLineFormatterOptions.HexValuesDisplayFormat_First || state.HexValuesDisplayFormat > HexBufferLineFormatterOptions.HexValuesDisplayFormat_Last)
				return false;
			if (state.BytesPerLine < HexBufferLineFormatterOptions.MinBytesPerLine || state.BytesPerLine > HexBufferLineFormatterOptions.MaxBytesPerLine)
				return false;
			if (state.ValuesPosition >= HexPosition.MaxEndPosition)
				return false;
			if (state.AsciiPosition >= HexPosition.MaxEndPosition)
				return false;
			if (state.TopLinePosition >= HexPosition.MaxEndPosition)
				return false;
			if (state.ValuesPosition < state.StartPosition || state.ValuesPosition > state.EndPosition)
				return false;
			if (state.AsciiPosition < state.StartPosition || state.AsciiPosition > state.EndPosition)
				return false;
			if (state.ValuesCellPosition < 0 || state.ValuesCellPosition > 1000)
				return false;
			if (state.TopLinePosition < state.StartPosition || state.TopLinePosition > state.EndPosition)
				return false;
			if (state.AnchorPoint < state.ActivePoint) {
				if (state.AnchorPoint >= HexPosition.MaxEndPosition)
					return false;
				if (state.ActivePoint > HexPosition.MaxEndPosition)
					return false;
			}
			else {
				if (state.AnchorPoint > HexPosition.MaxEndPosition)
					return false;
				if (state.ActivePoint >= HexPosition.MaxEndPosition)
					return false;
			}
			if (double.IsNaN(state.ViewportLeft) || state.ViewportLeft < 0 || state.ViewportLeft > 100000)
				return false;
			if (double.IsNaN(state.TopLineVerticalDistance) || Math.Abs(state.TopLineVerticalDistance) > 10000)
				return false;
			return true;
		}

		void VisualElement_Loaded(object sender, RoutedEventArgs e) {
			HexView.VisualElement.Loaded -= VisualElement_Loaded;
			if (cachedHexViewUIState == null)
				return;
			InitializeState(cachedHexViewUIState);
			cachedHexViewUIState = null;
		}

		public override object DeserializeUIState(ISettingsSection section) => HexViewUIStateSerializer.Read(section, new HexViewUIState());

		public override void SerializeUIState(ISettingsSection section, object obj) {
			var state = obj as HexViewUIState;
			if (state == null)
				return;
			HexViewUIStateSerializer.Write(section, state);
		}

		public void Dispose() => hexViewHost.Close();
	}
}
