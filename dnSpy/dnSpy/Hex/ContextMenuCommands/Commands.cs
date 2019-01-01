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
using System.Collections.Generic;
using System.Diagnostics;
using dnSpy.Contracts.Command;
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.Hex.Editor;
using dnSpy.Contracts.Hex.Editor.OptionsExtensionMethods;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Menus;
using dnSpy.Hex.Commands;
using dnSpy.Properties;

namespace dnSpy.Hex.ContextMenuCommands {
	sealed class HexViewContext {
		public HexView HexView { get; }
		public HexViewContext(HexView hexView) => HexView = hexView ?? throw new ArgumentNullException(nameof(hexView));
	}

	abstract class HexViewCommandTargetMenuItemBase : CommandTargetMenuItemBase<HexViewContext> {
		protected sealed override object CachedContextKey => ContextKey;
		static readonly object ContextKey = new object();

		protected HexViewCommandTargetMenuItemBase(StandardIds cmdId)
			: base(CommandConstants.StandardGroup, (int)cmdId) {
		}

		protected HexViewCommandTargetMenuItemBase(HexCommandIds cmdId)
			: base(HexCommandConstants.HexCommandGroup, (int)cmdId) {
		}

		protected HexViewCommandTargetMenuItemBase(HexEditorIds cmdId)
			: base(CommandConstants.HexEditorGroup, (int)cmdId) {
		}

		protected HexViewCommandTargetMenuItemBase(Guid guid, int cmdId)
			: base(guid, cmdId) {
		}

		protected override HexViewContext CreateContext(IMenuItemContext context) {
			var hexView = context.Find<HexView>();
			if (hexView == null)
				return null;
			return new HexViewContext(hexView);
		}

		protected override ICommandTarget GetCommandTarget(HexViewContext context) => context.HexView.CommandTarget;
		protected bool IsReadOnly(HexViewContext context) => context.HexView.Buffer.IsReadOnly || context.HexView.Options.DoesViewProhibitUserInput();
	}

	abstract class HexViewCommandTargetMenuItemBase2 : HexViewCommandTargetMenuItemBase {
		static readonly Guid dummyGroupGuid = Guid.NewGuid();
		protected HexViewCommandTargetMenuItemBase2()
			: base(dummyGroupGuid, 0) {
		}
		public override bool IsEnabled(HexViewContext context) => true;
	}

	[ExportMenuItem(Header = "res:GoToOffsetCommand", InputGestureText = "res:ShortCutKeyCtrlG", Group = MenuConstants.GROUP_CTX_HEXVIEW_SHOW, Order = 0)]
	sealed class GoToPositionContextMenuEntry : HexViewCommandTargetMenuItemBase {
		GoToPositionContextMenuEntry()
			: base(HexCommandIds.GoToPositionAbsolute) {
		}
	}

	[ExportMenuItem(Header = "res:GoToMetadataCommand", InputGestureText = "res:ShortCutKeyCtrlM", Group = MenuConstants.GROUP_CTX_HEXVIEW_SHOW, Order = 10)]
	sealed class GoToMetadataTableContextMenuEntry : HexViewCommandTargetMenuItemBase {
		GoToMetadataTableContextMenuEntry()
			: base(HexCommandIds.GoToMetadataTable) {
		}
	}

	[ExportMenuItem(Header = "res:HexGoToCodeOrStructure", InputGestureText = "res:ShortCutKeyF12", Group = MenuConstants.GROUP_CTX_HEXVIEW_SHOW, Order = 20)]
	sealed class GoToCodeOrStructureContextMenuEntry : HexViewCommandTargetMenuItemBase {
		GoToCodeOrStructureContextMenuEntry()
			: base(HexEditorIds.GoToCodeOrStructure) {
		}
	}

	[ExportMenuItem(Header = "res:HexFollowFieldValueReference", InputGestureText = "res:ShortCutKeyCtrlF12", Group = MenuConstants.GROUP_CTX_HEXVIEW_SHOW, Order = 30)]
	sealed class FollowFieldValueReferenceContextMenuEntry : HexViewCommandTargetMenuItemBase {
		FollowFieldValueReferenceContextMenuEntry()
			: base(HexEditorIds.FollowFieldValueReference) {
		}
	}

	[ExportMenuItem(Header = "res:HexEditorSaveSelectionCommand", Icon = DsImagesAttribute.Save, InputGestureText = "res:ShortCutKeyCtrlAltS", Group = MenuConstants.GROUP_CTX_HEXVIEW_SHOW, Order = 100)]
	sealed class SaveSelectionContextMenuEntry : HexViewCommandTargetMenuItemBase {
		SaveSelectionContextMenuEntry()
			: base(HexCommandIds.SaveSelection) {
		}
		public override bool IsEnabled(HexViewContext context) => !context.HexView.Selection.IsEmpty;
	}

	[ExportMenuItem(Header = "res:ShowOnlySelectedBytesCommand", InputGestureText = "res:ShortCutKeyCtrlD", Group = MenuConstants.GROUP_CTX_HEXVIEW_SHOW, Order = 200)]
	sealed class ShowOnlySelectedBytesContextMenuEntry : HexViewCommandTargetMenuItemBase {
		ShowOnlySelectedBytesContextMenuEntry()
			: base(HexEditorIds.ShowOnlySelectedBytes) {
		}
		public override bool IsVisible(HexViewContext context) => !context.HexView.Selection.IsEmpty;
	}

	[ExportMenuItem(Header = "res:ShowAllBytesCommand", InputGestureText = "res:ShortCutKeyCtrlShiftD", Group = MenuConstants.GROUP_CTX_HEXVIEW_SHOW, Order = 210)]
	sealed class ShowAllBytesContextMenuEntry : HexViewCommandTargetMenuItemBase {
		ShowAllBytesContextMenuEntry()
			: base(HexEditorIds.ShowAllBytes) {
		}
		public override bool IsVisible(HexViewContext context) =>
			context.HexView.BufferLines.BufferSpan != new HexBufferSpan(context.HexView.Buffer, context.HexView.Buffer.Span);
	}

	[ExportMenuItem(Icon = DsImagesAttribute.Cancel, InputGestureText = "res:ShortCutKeyDelete", Group = MenuConstants.GROUP_CTX_HEXVIEW_EDIT, Order = 0)]
	sealed class DeleteContextMenuEntry : HexViewCommandTargetMenuItemBase {
		DeleteContextMenuEntry()
			: base(HexEditorIds.DELETE) {
		}
		public override bool IsVisible(HexViewContext context) => !IsReadOnly(context) && base.IsVisible(context);
		public override string GetHeader(HexViewContext context) => !context.HexView.Selection.IsEmpty ? dnSpy_Resources.ClearSelectedBytesCommand : dnSpy_Resources.ClearByteCommand;
	}

	[ExportMenuItem(Header = "res:FillSelectionCommand", Icon = DsImagesAttribute.Fill, Group = MenuConstants.GROUP_CTX_HEXVIEW_EDIT, Order = 10)]
	sealed class FillSelectionContextMenuEntry : HexViewCommandTargetMenuItemBase {
		FillSelectionContextMenuEntry()
			: base(HexCommandIds.FillSelection) {
		}
		public override bool IsVisible(HexViewContext context) => !IsReadOnly(context) && base.IsVisible(context);
		public override bool IsEnabled(HexViewContext context) => !context.HexView.Selection.IsEmpty;
	}

	static class Constants {
		public const string BYTES_PER_LINE_GUID = "785AF553-F3BE-42B1-B6C3-DCD6A9F6C755";
		public const string GROUP_BYTES_PER_LINE = "0,203B617C-77CD-4DD9-B4C2-E5D62CF94C3B";
		public const string VALUE_FORMAT_GUID = "556259F0-AFF4-4F25-A87A-A7275E4C807C";
		public const string GROUP_VALUE_FORMAT = "0,91DF749C-8C92-4FF7-B713-78A73AA72D41";
		public const string COPY_SPECIAL_GUID = "D7D8B565-DC1A-411C-B236-480A2953F82E";
		public const string GROUP_COPY_SPECIAL = "0,9BD31DFF-6415-46C5-87B9-84B082F41C0F";
		public const string PASTE_SPECIAL_GUID = "48DD26BA-2A11-444C-BE29-355BFA72C97D";
		public const string GROUP_PASTE_SPECIAL = "0,4EA9BCF4-A2E1-4D46-B17F-B61787CEF6FF";
		public const string SELECT_SPECIAL_GUID = "2DD0F02F-8DB4-40AF-A03A-1EE06849FEEA";
		public const string GROUP_SELECT_SPECIAL = "0,004AC2AF-7261-4004-B2D2-A809961B3A74";
	}

	sealed class MyMenuItem : MenuItemBase {
		readonly Action<IMenuItemContext> action;
		readonly bool isChecked;

		public MyMenuItem(Action<IMenuItemContext> action, bool isChecked = false) {
			this.action = action;
			this.isChecked = isChecked;
		}

		public override void Execute(IMenuItemContext context) => action(context);
		public override bool IsChecked(IMenuItemContext context) => isChecked;
	}

	[ExportMenuItem(Header = "res:BytesPerLineCommand", Guid = Constants.BYTES_PER_LINE_GUID, Group = MenuConstants.GROUP_CTX_HEXVIEW_OPTS, Order = 0)]
	sealed class BytesPerLineContextMenuEntry : HexViewCommandTargetMenuItemBase2 {
		public override void Execute(HexViewContext context) { }
	}

	[ExportMenuItem(OwnerGuid = Constants.BYTES_PER_LINE_GUID, Group = Constants.GROUP_BYTES_PER_LINE, Order = 0)]
	sealed class BytesPerLineSubContextMenuEntry : HexViewCommandTargetMenuItemBase2, IMenuItemProvider {
		public override void Execute(HexViewContext context) { }

		static readonly (int bits, string header)[] subMenus = new (int, string)[] {
			(0, dnSpy_Resources.HexEditor_BytesPerLine_FitToWidth),
			(8, dnSpy_Resources.HexEditor_BytesPerLine_8),
			(16, dnSpy_Resources.HexEditor_BytesPerLine_16),
			(32, dnSpy_Resources.HexEditor_BytesPerLine_32),
			(48, dnSpy_Resources.HexEditor_BytesPerLine_48),
			(64, dnSpy_Resources.HexEditor_BytesPerLine_64),
		};

		public IEnumerable<CreatedMenuItem> Create(IMenuItemContext context) {
			var ctx = CreateContext(context);
			Debug.Assert(ctx != null);
			if (ctx == null)
				yield break;
			var hexView = ctx.HexView;

			for (int i = 0; i < subMenus.Length; i++) {
				var info = subMenus[i];
				var attr = new ExportMenuItemAttribute { Header = info.header };
				bool isChecked = info.bits == hexView.Options.GetOptionValue(DefaultHexViewOptions.BytesPerLineId);
				var item = new MyMenuItem(ctx2 => hexView.Options.SetOptionValue(DefaultHexViewOptions.BytesPerLineId, info.bits), isChecked);
				yield return new CreatedMenuItem(attr, item);
			}
		}
	}

	[ExportMenuItem(Header = "res:ValueFormatCommand", Guid = Constants.VALUE_FORMAT_GUID, Group = MenuConstants.GROUP_CTX_HEXVIEW_OPTS, Order = 10)]
	sealed class ValueFormatContextMenuEntry : HexViewCommandTargetMenuItemBase2 {
		public override void Execute(HexViewContext context) { }
	}

	[ExportMenuItem(OwnerGuid = Constants.VALUE_FORMAT_GUID, Group = Constants.GROUP_VALUE_FORMAT, Order = 0)]
	sealed class ValueFormatSubContextMenuEntry : HexViewCommandTargetMenuItemBase2, IMenuItemProvider {
		public override void Execute(HexViewContext context) { }

		public IEnumerable<CreatedMenuItem> Create(IMenuItemContext context) {
			var ctx = CreateContext(context);
			Debug.Assert(ctx != null);
			if (ctx == null)
				yield break;
			var hexView = ctx.HexView;

			foreach (var info in SettingsConstants.ValueFormatList) {
				var attr = new ExportMenuItemAttribute { Header = info.text };
				bool isChecked = info.displayFormat == hexView.BufferLines.ValuesFormat;
				var item = new MyMenuItem(ctx2 => hexView.Options.SetOptionValue(DefaultHexViewOptions.HexValuesDisplayFormatId, info.displayFormat), isChecked);
				yield return new CreatedMenuItem(attr, item);
			}
		}
	}

	[ExportMenuItem(Header = "res:HexEditorSettingsCommand", Icon = DsImagesAttribute.Settings, Group = MenuConstants.GROUP_CTX_HEXVIEW_OPTS, Order = 1000000)]
	sealed class EditLocalSettingsContextMenuEntry : HexViewCommandTargetMenuItemBase {
		EditLocalSettingsContextMenuEntry()
			: base(HexCommandIds.EditLocalSettings) {
		}
	}

	[ExportMenuItem(Header = "res:CopySpecialCommand", Icon = DsImagesAttribute.Copy, Guid = Constants.COPY_SPECIAL_GUID, Group = MenuConstants.GROUP_CTX_HEXVIEW_COPY, Order = 0)]
	sealed class CopySpecialContextMenuEntry : HexViewCommandTargetMenuItemBase2 {
		public override void Execute(HexViewContext context) { }
	}

	abstract class CopyHexViewCommandTargetMenuItemBase : HexViewCommandTargetMenuItemBase {
		protected CopyHexViewCommandTargetMenuItemBase(StandardIds cmdId)
			: base(cmdId) {
		}

		protected CopyHexViewCommandTargetMenuItemBase(HexEditorIds cmdId)
			: base(cmdId) {
		}

		public override bool IsEnabled(HexViewContext context) => !context.HexView.Selection.IsEmpty;
	}

	[ExportMenuItem(OwnerGuid = Constants.COPY_SPECIAL_GUID, Header = "res:CopyCommand", Icon = DsImagesAttribute.Copy, InputGestureText = "res:CopyKey", Group = Constants.GROUP_COPY_SPECIAL, Order = 0)]
	sealed class CopyContextMenuEntry : CopyHexViewCommandTargetMenuItemBase {
		CopyContextMenuEntry()
			: base(StandardIds.Copy) {
		}
	}

	[ExportMenuItem(OwnerGuid = Constants.COPY_SPECIAL_GUID, Header = "res:CopyUTF8StringCommand", InputGestureText = "res:ShortCutKeyCtrlShift8", Group = Constants.GROUP_COPY_SPECIAL, Order = 10)]
	sealed class CopyUtf8StringContextMenuEntry : CopyHexViewCommandTargetMenuItemBase {
		CopyUtf8StringContextMenuEntry()
			: base(HexEditorIds.CopyUtf8String) {
		}
	}

	[ExportMenuItem(OwnerGuid = Constants.COPY_SPECIAL_GUID, Header = "res:CopyUnicodeStringCommand", InputGestureText = "res:ShortCutKeyCtrlShiftU", Group = Constants.GROUP_COPY_SPECIAL, Order = 20)]
	sealed class CopyUnicodeStringContextMenuEntry : CopyHexViewCommandTargetMenuItemBase {
		CopyUnicodeStringContextMenuEntry()
			: base(HexEditorIds.CopyUnicodeString) {
		}
	}

	[ExportMenuItem(OwnerGuid = Constants.COPY_SPECIAL_GUID, Header = "res:CopyCSharpArrayCommand", InputGestureText = "res:ShortCutKeyCtrlShiftH", Group = Constants.GROUP_COPY_SPECIAL, Order = 30)]
	sealed class CopyCSharpArrayContextMenuEntry : CopyHexViewCommandTargetMenuItemBase {
		CopyCSharpArrayContextMenuEntry()
			: base(HexEditorIds.CopyCSharpArray) {
		}
	}

	[ExportMenuItem(OwnerGuid = Constants.COPY_SPECIAL_GUID, Header = "res:CopyVisualBasicArrayCommand", InputGestureText = "res:ShortCutKeyCtrlShiftB", Group = Constants.GROUP_COPY_SPECIAL, Order = 40)]
	sealed class CopyVisualBasicArrayContextMenuEntry : CopyHexViewCommandTargetMenuItemBase {
		CopyVisualBasicArrayContextMenuEntry()
			: base(HexEditorIds.CopyVisualBasicArray) {
		}
	}

	[ExportMenuItem(OwnerGuid = Constants.COPY_SPECIAL_GUID, Header = "res:CopyTextCommand", InputGestureText = "res:ShortCutKeyCtrlShiftC", Group = Constants.GROUP_COPY_SPECIAL, Order = 50)]
	sealed class CopyTextContextMenuEntry : CopyHexViewCommandTargetMenuItemBase {
		CopyTextContextMenuEntry()
			: base(HexEditorIds.CopyText) {
		}
	}

	[ExportMenuItem(OwnerGuid = Constants.COPY_SPECIAL_GUID, Header = "res:CopyOffsetCommand", InputGestureText = "res:ShortCutKeyCtrlAltO", Group = Constants.GROUP_COPY_SPECIAL, Order = 60)]
	sealed class CopyOffsetContextMenuEntry : HexViewCommandTargetMenuItemBase {
		CopyOffsetContextMenuEntry()
			: base(HexEditorIds.CopyOffset) {
		}
	}

	[ExportMenuItem(OwnerGuid = Constants.COPY_SPECIAL_GUID, Header = "res:CopyValueCommand", InputGestureText = "res:ShortCutKeyCtrlShiftV", Group = Constants.GROUP_COPY_SPECIAL, Order = 70)]
	sealed class CopyValueContextMenuEntry : HexViewCommandTargetMenuItemBase {
		CopyValueContextMenuEntry()
			: base(HexEditorIds.CopyValue) {
		}
	}

	[ExportMenuItem(OwnerGuid = Constants.COPY_SPECIAL_GUID, Group = Constants.GROUP_COPY_SPECIAL, Order = 80)]
	sealed class CopyUInt16ContextMenuEntry : HexViewCommandTargetMenuItemBase {
		CopyUInt16ContextMenuEntry()
			: base(HexEditorIds.CopyUInt16) {
		}

		public override string GetHeader(HexViewContext context) => string.Format(dnSpy_Resources.CopyDataCommand, "UInt16");
	}

	[ExportMenuItem(OwnerGuid = Constants.COPY_SPECIAL_GUID, Group = Constants.GROUP_COPY_SPECIAL, Order = 90)]
	sealed class CopyUInt16BigEndianContextMenuEntry : HexViewCommandTargetMenuItemBase {
		CopyUInt16BigEndianContextMenuEntry()
			: base(HexEditorIds.CopyUInt16BigEndian) {
		}

		public override string GetHeader(HexViewContext context) => string.Format(dnSpy_Resources.CopyDataCommand, "UInt16" + " (" + dnSpy_Resources.BigEndian + ")");
	}

	[ExportMenuItem(OwnerGuid = Constants.COPY_SPECIAL_GUID, InputGestureText = "res:ShortCutKeyCtrlShiftQ", Group = Constants.GROUP_COPY_SPECIAL, Order = 100)]
	sealed class CopyUInt32ContextMenuEntry : HexViewCommandTargetMenuItemBase {
		CopyUInt32ContextMenuEntry()
			: base(HexEditorIds.CopyUInt32) {
		}

		public override string GetHeader(HexViewContext context) => string.Format(dnSpy_Resources.CopyDataCommand, "UInt32");
	}

	[ExportMenuItem(OwnerGuid = Constants.COPY_SPECIAL_GUID, Group = Constants.GROUP_COPY_SPECIAL, Order = 110)]
	sealed class CopyUInt32BigEndianContextMenuEntry : HexViewCommandTargetMenuItemBase {
		CopyUInt32BigEndianContextMenuEntry()
			: base(HexEditorIds.CopyUInt32BigEndian) {
		}

		public override string GetHeader(HexViewContext context) => string.Format(dnSpy_Resources.CopyDataCommand, "UInt32" + " (" + dnSpy_Resources.BigEndian + ")");
	}

	[ExportMenuItem(OwnerGuid = Constants.COPY_SPECIAL_GUID, Group = Constants.GROUP_COPY_SPECIAL, Order = 120)]
	sealed class CopyUInt64ContextMenuEntry : HexViewCommandTargetMenuItemBase {
		CopyUInt64ContextMenuEntry()
			: base(HexEditorIds.CopyUInt64) {
		}

		public override string GetHeader(HexViewContext context) => string.Format(dnSpy_Resources.CopyDataCommand, "UInt64");
	}

	[ExportMenuItem(OwnerGuid = Constants.COPY_SPECIAL_GUID, Group = Constants.GROUP_COPY_SPECIAL, Order = 130)]
	sealed class CopyUInt64BigEndianContextMenuEntry : HexViewCommandTargetMenuItemBase {
		CopyUInt64BigEndianContextMenuEntry()
			: base(HexEditorIds.CopyUInt64BigEndian) {
		}

		public override string GetHeader(HexViewContext context) => string.Format(dnSpy_Resources.CopyDataCommand, "UInt64" + " (" + dnSpy_Resources.BigEndian + ")");
	}

	[ExportMenuItem(OwnerGuid = Constants.COPY_SPECIAL_GUID, Header = "res:CopyFileOffsetCommand", InputGestureText = "res:ShortCutKeyCtrlShiftO", Group = Constants.GROUP_COPY_SPECIAL, Order = 140)]
	sealed class CopyFileOffsetContextMenuEntry : HexViewCommandTargetMenuItemBase {
		CopyFileOffsetContextMenuEntry()
			: base(HexEditorIds.CopyFileOffset) {
		}
	}

	[ExportMenuItem(OwnerGuid = Constants.COPY_SPECIAL_GUID, Header = "res:CopyAbsoluteFileOffsetCommand", InputGestureText = "res:ShortCutKeyCtrlShiftA", Group = Constants.GROUP_COPY_SPECIAL, Order = 150)]
	sealed class CopyAbsoluteFileOffsetContextMenuEntry : HexViewCommandTargetMenuItemBase {
		CopyAbsoluteFileOffsetContextMenuEntry()
			: base(HexEditorIds.CopyAbsoluteFileOffset) {
		}
	}

	[ExportMenuItem(OwnerGuid = Constants.COPY_SPECIAL_GUID, Header = "res:CopyRVACommand", InputGestureText = "res:ShortCutKeyCtrlShiftR", Group = Constants.GROUP_COPY_SPECIAL, Order = 160)]
	sealed class CopyRVAContextMenuEntry : HexViewCommandTargetMenuItemBase {
		CopyRVAContextMenuEntry()
			: base(HexEditorIds.CopyRVA) {
		}
	}

	[ExportMenuItem(Header = "res:PasteSpecialCommand", Icon = DsImagesAttribute.Paste, Guid = Constants.PASTE_SPECIAL_GUID, Group = MenuConstants.GROUP_CTX_HEXVIEW_COPY, Order = 10)]
	sealed class PasteSpecialContextMenuEntry : HexViewCommandTargetMenuItemBase2 {
		public override bool IsVisible(HexViewContext context) => !IsReadOnly(context) && base.IsVisible(context);
		public override void Execute(HexViewContext context) { }
	}

	abstract class PasteHexViewCommandTargetMenuItemBase : HexViewCommandTargetMenuItemBase {
		protected PasteHexViewCommandTargetMenuItemBase(StandardIds cmdId)
			: base(cmdId) {
		}

		protected PasteHexViewCommandTargetMenuItemBase(HexEditorIds cmdId)
			: base(cmdId) {
		}

		public override bool IsVisible(HexViewContext context) => !IsReadOnly(context) && base.IsVisible(context);
	}

	[ExportMenuItem(OwnerGuid = Constants.PASTE_SPECIAL_GUID, Header = "res:PasteCommand", Icon = DsImagesAttribute.Paste, InputGestureText = "res:ShortCutKeyCtrlV", Group = Constants.GROUP_PASTE_SPECIAL, Order = 0)]
	sealed class PasteContextMenuEntry : PasteHexViewCommandTargetMenuItemBase {
		PasteContextMenuEntry()
			: base(StandardIds.Paste) {
		}
	}

	[ExportMenuItem(OwnerGuid = Constants.PASTE_SPECIAL_GUID, Header = "res:PasteUTF8Command", InputGestureText = "res:ShortCutKeyCtrl8", Group = Constants.GROUP_PASTE_SPECIAL, Order = 10)]
	sealed class PasteUtf8StringContextMenuEntry : PasteHexViewCommandTargetMenuItemBase {
		PasteUtf8StringContextMenuEntry()
			: base(HexEditorIds.PasteUtf8String) {
		}
	}

	[ExportMenuItem(OwnerGuid = Constants.PASTE_SPECIAL_GUID, Header = "res:PasteUTF8AndLength7Command", InputGestureText = "res:ShortCutKeyCtrlKCtrl8", Group = Constants.GROUP_PASTE_SPECIAL, Order = 20)]
	sealed class PasteUtf8String7BitEncodedLengthPrefixContextMenuEntry : PasteHexViewCommandTargetMenuItemBase {
		PasteUtf8String7BitEncodedLengthPrefixContextMenuEntry()
			: base(HexEditorIds.PasteUtf8String7BitEncodedLengthPrefix) {
		}
	}

	[ExportMenuItem(OwnerGuid = Constants.PASTE_SPECIAL_GUID, Header = "res:PasteUnicodeCommand", InputGestureText = "res:ShortCutKeyCtrlU", Group = Constants.GROUP_PASTE_SPECIAL, Order = 30)]
	sealed class PasteUnicodeStringContextMenuEntry : PasteHexViewCommandTargetMenuItemBase {
		PasteUnicodeStringContextMenuEntry()
			: base(HexEditorIds.PasteUnicodeString) {
		}
	}

	[ExportMenuItem(OwnerGuid = Constants.PASTE_SPECIAL_GUID, Header = "res:PasteUnicodeAndLength7Command", InputGestureText = "res:ShortCutKeyCtrlKCtrlU", Group = Constants.GROUP_PASTE_SPECIAL, Order = 40)]
	sealed class PasteUnicodeString7BitEncodedLengthPrefixContextMenuEntry : PasteHexViewCommandTargetMenuItemBase {
		PasteUnicodeString7BitEncodedLengthPrefixContextMenuEntry()
			: base(HexEditorIds.PasteUnicodeString7BitEncodedLengthPrefix) {
		}
	}

	[ExportMenuItem(OwnerGuid = Constants.PASTE_SPECIAL_GUID, Header = "res:PasteDotNetMetaDataBlobCommand", InputGestureText = "res:ShortCutKeyCtrlB", Group = Constants.GROUP_PASTE_SPECIAL, Order = 50)]
	sealed class PasteBlobContextMenuEntry : PasteHexViewCommandTargetMenuItemBase {
		PasteBlobContextMenuEntry()
			: base(HexEditorIds.PasteBlob) {
		}
	}

	[ExportMenuItem(Header = "res:SelectSpecialCommand", Icon = DsImagesAttribute.Select, Guid = Constants.SELECT_SPECIAL_GUID, Group = MenuConstants.GROUP_CTX_HEXVIEW_COPY, Order = 10)]
	sealed class SelectSpecialContextMenuEntry : HexViewCommandTargetMenuItemBase2 {
		public override void Execute(HexViewContext context) { }
	}

	[ExportMenuItem(OwnerGuid = Constants.SELECT_SPECIAL_GUID, Header = "res:HexEditorSelectCommand", Icon = DsImagesAttribute.Select, InputGestureText = "res:ShortCutKeyCtrlL", Group = MenuConstants.GROUP_CTX_HEXVIEW_SHOW, Order = 0)]
	sealed class SelectContextMenuEntry : HexViewCommandTargetMenuItemBase {
		SelectContextMenuEntry()
			: base(HexCommandIds.Select) {
		}
	}

	[ExportMenuItem(OwnerGuid = Constants.SELECT_SPECIAL_GUID, Header = "res:HexEditorSelectFileCommand", InputGestureText = "res:ShortCutKeyCtrlECtrlF", Group = MenuConstants.GROUP_CTX_HEXVIEW_SHOW, Order = 10)]
	sealed class SelectFileContextMenuEntry : HexViewCommandTargetMenuItemBase {
		SelectFileContextMenuEntry()
			: base(HexEditorIds.SelectFile) {
		}
	}

	[ExportMenuItem(OwnerGuid = Constants.SELECT_SPECIAL_GUID, Header = "res:HexEditorSelectNestedFileCommand", InputGestureText = "res:ShortCutKeyCtrlECtrlN", Group = MenuConstants.GROUP_CTX_HEXVIEW_SHOW, Order = 20)]
	sealed class SelectNestedFileContextMenuEntry : HexViewCommandTargetMenuItemBase {
		SelectNestedFileContextMenuEntry()
			: base(HexEditorIds.SelectNestedFile) {
		}
	}

	[ExportMenuItem(OwnerGuid = Constants.SELECT_SPECIAL_GUID, Header = "res:HexEditorSelectStructureCommand", InputGestureText = "res:ShortCutKeyCtrlECtrlS", Group = MenuConstants.GROUP_CTX_HEXVIEW_SHOW, Order = 30)]
	sealed class SelectStructureContextMenuEntry : HexViewCommandTargetMenuItemBase {
		SelectStructureContextMenuEntry()
			: base(HexEditorIds.SelectStructure) {
		}
	}

	[ExportMenuItem(Header = "res:Refresh", Icon = DsImagesAttribute.Refresh, InputGestureText = "res:ShortCutKeyF5", Group = MenuConstants.GROUP_CTX_HEXVIEW_MISC, Order = 1000)]
	sealed class RefreshContextMenuEntry : HexViewCommandTargetMenuItemBase {
		RefreshContextMenuEntry()
			: base(HexEditorIds.Refresh) {
		}
		public override bool IsVisible(HexViewContext context) => context.HexView.Buffer.IsVolatile;
	}

	[ExportMenuItem(Header = "res:FindCommand", Icon = DsImagesAttribute.Search, InputGestureText = "res:ShortCutKeyCtrlF", Group = MenuConstants.GROUP_CTX_HEXVIEW_FIND, Order = 0)]
	sealed class FindCommandContextMenuEntry : HexViewCommandTargetMenuItemBase {
		FindCommandContextMenuEntry()
			: base(StandardIds.Find) {
		}
	}

	[ExportMenuItem(Header = "res:IncrementalSearchCommand", Icon = DsImagesAttribute.Search, InputGestureText = "res:ShortCutKeyCtrlI", Group = MenuConstants.GROUP_CTX_HEXVIEW_FIND, Order = 10)]
	sealed class IncrementalSearchForwardContextMenuEntry : HexViewCommandTargetMenuItemBase {
		IncrementalSearchForwardContextMenuEntry()
			: base(StandardIds.IncrementalSearchForward) {
		}
	}
}
