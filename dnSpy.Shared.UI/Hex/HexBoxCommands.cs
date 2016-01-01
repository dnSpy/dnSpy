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
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using dnSpy.Contracts.App;
using dnSpy.Contracts.Menus;
using dnSpy.Shared.UI.HexEditor;
using dnSpy.Shared.UI.Menus;
using dnSpy.Shared.UI.MVVM;
using dnSpy.Shared.UI.MVVM.Dialogs;
using dnSpy.Shared.UI.Properties;
using WF = System.Windows.Forms;

namespace dnSpy.Shared.UI.Hex {
	abstract class HexBoxCommand : MenuItemBase<DnHexBox> {
		protected sealed override object CachedContextKey {
			get { return ContextKey; }
		}
		static readonly object ContextKey = new object();

		protected sealed override DnHexBox CreateContext(IMenuItemContext context) {
			return context.CreatorObject.Object as DnHexBox;
		}

		public override bool IsEnabled(DnHexBox context) {
			return IsVisible(context);
		}
	}

	[ExportMenuItem(InputGestureText = "res:GoToOffsetKey", Group = MenuConstants.GROUP_CTX_HEXBOX_SHOW, Order = 0)]
	sealed class GoToOffsetHexBoxCtxMenuCommand : HexBoxCommand {
		readonly IAppWindow appWindow;

		[ImportingConstructor]
		GoToOffsetHexBoxCtxMenuCommand(IAppWindow appWindow) {
			this.appWindow = appWindow;
		}

		public override void Execute(DnHexBox dnHexBox) {
			Execute2(dnHexBox, appWindow.MainWindow);
		}

		public override bool IsVisible(DnHexBox dnHexBox) {
			return CanExecute(dnHexBox);
		}

		internal static bool CanExecute(DnHexBox dnHexBox) {
			return dnHexBox != null && dnHexBox.Document != null;
		}

		internal static void Execute2(DnHexBox dnHexBox, Window ownerWindow) {
			Debug.Assert(ownerWindow != null);
			if (!CanExecute(dnHexBox))
				return;

			var data = new GoToOffsetVM(dnHexBox.PhysicalToVisibleOffset(dnHexBox.CaretPosition.Offset), dnHexBox.PhysicalToVisibleOffset(dnHexBox.StartOffset), dnHexBox.PhysicalToVisibleOffset(dnHexBox.EndOffset));
			var win = new GoToOffsetDlg();
			win.DataContext = data;
			win.Owner = ownerWindow ?? Application.Current.MainWindow;
			if (dnHexBox.IsMemory) {
				win.Title = dnSpy_Shared_UI_Resources.GoToOffset_Title_Address;
				win.offsetLabel.Content = dnSpy_Shared_UI_Resources.GoToOffset_Address_Label;
			}
			else {
				win.Title = dnSpy_Shared_UI_Resources.GoToOffset_Title;
				win.offsetLabel.Content = dnSpy_Shared_UI_Resources.GoToOffset_Offset_Label;
			}
			if (win.ShowDialog() != true)
				return;

			dnHexBox.CaretPosition = new HexBoxPosition(dnHexBox.VisibleToPhysicalOffset(data.OffsetVM.Value), dnHexBox.CaretPosition.Kind, 0);
		}

		public override string GetHeader(DnHexBox context) {
			return context.IsMemory ? dnSpy_Shared_UI_Resources.GoToAddressCommand : dnSpy_Shared_UI_Resources.GoToOffsetCommand;
		}
	}

	[ExportMenuItem(Header = "res:HexEditorSelectCommand", InputGestureText = "res:HexEditorSelectKey", Group = MenuConstants.GROUP_CTX_HEXBOX_SHOW, Order = 10)]
	sealed class SelectRangeHexBoxCtxMenuCommand : HexBoxCommand {
		readonly IAppWindow appWindow;

		[ImportingConstructor]
		SelectRangeHexBoxCtxMenuCommand(IAppWindow appWindow) {
			this.appWindow = appWindow;
		}

		internal static void Execute2(DnHexBox dnHexBox, Window ownerWindow) {
			ExecuteInternal(dnHexBox, ownerWindow);
		}

		internal static bool CanExecute(DnHexBox dnHexBox) {
			return dnHexBox != null && dnHexBox.Document != null;
		}

		public override void Execute(DnHexBox dnHexBox) {
			ExecuteInternal(dnHexBox, appWindow.MainWindow);
		}

		static void ExecuteInternal(DnHexBox dnHexBox, Window ownerWindow) {
			Debug.Assert(ownerWindow != null);
			if (dnHexBox == null)
				return;
			ulong start = dnHexBox.CaretPosition.Offset;
			ulong end = start;
			if (dnHexBox.Selection != null) {
				start = dnHexBox.Selection.Value.StartOffset;
				end = dnHexBox.Selection.Value.EndOffset;
			}
			var data = new SelectVM(dnHexBox.PhysicalToVisibleOffset(start), dnHexBox.PhysicalToVisibleOffset(end), dnHexBox.PhysicalToVisibleOffset(dnHexBox.StartOffset), dnHexBox.PhysicalToVisibleOffset(dnHexBox.EndOffset));
			var win = new SelectDlg();
			win.DataContext = data;
			win.Owner = ownerWindow ?? Application.Current.MainWindow;
			if (win.ShowDialog() != true)
				return;

			dnHexBox.Selection = new HexSelection(dnHexBox.VisibleToPhysicalOffset(data.EndVM.Value), dnHexBox.VisibleToPhysicalOffset(data.StartVM.Value));
			dnHexBox.CaretPosition = new HexBoxPosition(dnHexBox.VisibleToPhysicalOffset(data.StartVM.Value), dnHexBox.CaretPosition.Kind, 0);
		}

		public override bool IsEnabled(DnHexBox dnHexBox) {
			return CanExecute(dnHexBox);
		}

		public override bool IsVisible(DnHexBox dnHexBox) {
			return true;
		}
	}

	sealed class HexDocumentDataSaver : IProgressTask {
		public bool IsIndeterminate {
			get { return false; }
		}

		public double ProgressMinimum {
			get { return 0; }
		}

		public double ProgressMaximum {
			get { return progressMaximum; }
		}

		readonly HexDocument doc;
		readonly long progressMaximum;
		readonly string filename;
		readonly ulong start, end;
		const int BUF_SIZE = 64 * 1024;

		public HexDocumentDataSaver(HexDocument doc, ulong start, ulong end, string filename) {
			this.doc = doc;
			this.start = start;
			this.end = end;
			this.filename = filename;
			ulong len = end - start + 1;
			if (len == 0 || len + BUF_SIZE - 1 < len)
				this.progressMaximum = (long)(0x8000000000000000UL / (BUF_SIZE / 2));
			else
				this.progressMaximum = (long)((len + BUF_SIZE - 1) / BUF_SIZE);
		}

		public void Execute(IProgress progress) {
			progress.SetDescription(filename);
			var file = File.Create(filename);
			try {
				var buf = new byte[BUF_SIZE];
				ulong offs = start;
				long currentProgress = 0;
				while (offs <= end) {
					progress.ThrowIfCancellationRequested();
					progress.SetTotalProgress(currentProgress);
					currentProgress++;
					ulong left = end - start + 1;
					if (left == 0)
						left = ulong.MaxValue;
					int size = left > (ulong)buf.Length ? buf.Length : (int)left;
					doc.Read(offs, buf, 0, size);
					file.Write(buf, 0, size);
					offs += (ulong)size;
					if (offs == 0)
						break;
				}
				progress.SetTotalProgress(currentProgress);
			}
			catch {
				file.Dispose();
				try { File.Delete(filename); }
				catch { }
				throw;
			}
			finally {
				file.Dispose();
			}
		}
	}

	[ExportMenuItem(Header = "res:HexEditorSaveSelectionCommand", InputGestureText = "res:HexEditorSaveSelectionKey", Group = MenuConstants.GROUP_CTX_HEXBOX_SHOW, Order = 20)]
	sealed class SaveSelectionHexBoxCtxMenuCommand : HexBoxCommand {
		readonly IAppWindow appWindow;

		[ImportingConstructor]
		SaveSelectionHexBoxCtxMenuCommand(IAppWindow appWindow) {
			this.appWindow = appWindow;
		}

		public override void Execute(DnHexBox dnHexBox) {
			Execute2(dnHexBox, appWindow.MainWindow);
		}

		public override bool IsVisible(DnHexBox dnHexBox) {
			return CanExecute(dnHexBox);
		}

		internal static void Execute2(DnHexBox dnHexBox, Window ownerWindow) {
			Debug.Assert(ownerWindow != null);
			var doc = dnHexBox.Document;
			if (doc == null)
				return;
			var sel = dnHexBox.Selection;
			if (sel == null)
				return;

			var dialog = new WF.SaveFileDialog() {
				Filter = PickFilenameConstants.AnyFilenameFilter,
				RestoreDirectory = true,
				ValidateNames = true,
			};

			if (dialog.ShowDialog() != WF.DialogResult.OK)
				return;

			var data = new ProgressVM(Dispatcher.CurrentDispatcher, new HexDocumentDataSaver(doc, sel.Value.StartOffset, sel.Value.EndOffset, dialog.FileName));
			var win = new ProgressDlg();
			win.DataContext = data;
			win.Owner = ownerWindow ?? Application.Current.MainWindow;
			win.Title = string.Format(dnSpy_Shared_UI_Resources.HexEditorSaveSelection_Title, sel.Value.StartOffset, sel.Value.EndOffset);
			var res = win.ShowDialog();
			if (res != true)
				return;
			if (!data.WasError)
				return;
			App.MsgBox.Instance.Show(string.Format(dnSpy_Shared_UI_Resources.AnErrorOccurred, data.ErrorMessage));
		}

		internal static bool CanExecute(DnHexBox dnHexBox) {
			return dnHexBox.Document != null && dnHexBox.Selection != null;
		}
	}

	[ExportMenuItem(Header = "res:ShowOnlySelectedBytesCommand", InputGestureText = "res:ShowOnlySelectedBytesKey", Group = MenuConstants.GROUP_CTX_HEXBOX_SHOW, Order = 30)]
	sealed class ShowSelectionHexBoxCtxMenuCommand : HexBoxCommand {
		public override void Execute(DnHexBox dnHexBox) {
			Execute2(dnHexBox);
		}

		public override bool IsVisible(DnHexBox dnHexBox) {
			return CanExecute(dnHexBox);
		}

		internal static void Execute2(DnHexBox dnHexBox) {
			var sel = dnHexBox.Selection;
			if (sel == null)
				return;

			dnHexBox.StartOffset = sel.Value.StartOffset;
			dnHexBox.EndOffset = sel.Value.EndOffset;
		}

		internal static bool CanExecute(DnHexBox dnHexBox) {
			return dnHexBox.Selection != null &&
				(dnHexBox.StartOffset != dnHexBox.Selection.Value.StartOffset ||
				dnHexBox.EndOffset != dnHexBox.Selection.Value.EndOffset);
		}
	}

	[ExportMenuItem(Header = "res:ShowAllBytesCommand", InputGestureText = "res:ShowAllBytesKey", Group = MenuConstants.GROUP_CTX_HEXBOX_SHOW, Order = 40)]
	sealed class ShowWholeDocumentHexBoxCtxMenuCommand : HexBoxCommand {
		public override void Execute(DnHexBox dnHexBox) {
			Execute2(dnHexBox);
		}

		public override bool IsVisible(DnHexBox dnHexBox) {
			return CanExecute(dnHexBox);
		}

		internal static void Execute2(DnHexBox dnHexBox) {
			dnHexBox.StartOffset = dnHexBox.DocumentStartOffset;
			dnHexBox.EndOffset = dnHexBox.DocumentEndOffset;
			var sel = dnHexBox.Selection;
			dnHexBox.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(delegate {
				if (sel != null && sel == dnHexBox.Selection)
					dnHexBox.SetCaretPositionAndMakeVisible(sel.Value.StartOffset, sel.Value.EndOffset);
				else
					dnHexBox.BringCaretIntoView();
			}));
		}

		internal static bool CanExecute(DnHexBox dnHexBox) {
			return dnHexBox.StartOffset != dnHexBox.DocumentStartOffset ||
				dnHexBox.EndOffset != dnHexBox.DocumentEndOffset;
		}
	}

	[ExportMenuItem(InputGestureText = "res:ClearSelectionKey", Group = MenuConstants.GROUP_CTX_HEXBOX_EDIT, Order = 0)]
	sealed class ClearSelectionHexBoxCtxMenuCommand : HexBoxCommand {
		public override void Execute(DnHexBox dnHexBox) {
			dnHexBox.ClearBytes();
		}

		public override bool IsEnabled(DnHexBox dnHexBox) {
			return dnHexBox.Document != null;
		}

		public override bool IsVisible(DnHexBox dnHexBox) {
			return true;
		}

		public override string GetHeader(DnHexBox context) {
			return context.Selection != null ? dnSpy_Shared_UI_Resources.ClearSelectedBytesCommand : dnSpy_Shared_UI_Resources.ClearByteCommand;
		}
	}

	[ExportMenuItem(Header = "res:FillSelectionCommand", Icon = "Fill", Group = MenuConstants.GROUP_CTX_HEXBOX_EDIT, Order = 10)]
	sealed class WriteToSelectionSelectionHexBoxCtxMenuCommand : HexBoxCommand {
		readonly IMessageBoxManager messageBoxManager;

		[ImportingConstructor]
		WriteToSelectionSelectionHexBoxCtxMenuCommand(IMessageBoxManager messageBoxManager) {
			this.messageBoxManager = messageBoxManager;
		}

		public override void Execute(DnHexBox dnHexBox) {
			var sel = dnHexBox.Selection;
			if (sel == null)
				return;

			var res = messageBoxManager.Ask<byte?>(dnSpy_Shared_UI_Resources.FillSelection_Label, "0xFF", dnSpy_Shared_UI_Resources.FillSelection_Title, s => {
				string error;
				byte b = NumberVMUtils.ParseByte(s, byte.MinValue, byte.MaxValue, out error);
				return string.IsNullOrEmpty(error) ? b : (byte?)null;
			}, s => {
				string error;
				byte b = NumberVMUtils.ParseByte(s, byte.MinValue, byte.MaxValue, out error);
				return error;
			});
			if (res == null)
				return;

			dnHexBox.FillBytes(sel.Value.StartOffset, sel.Value.EndOffset, res.Value);
			dnHexBox.Selection = null;
		}

		public override bool IsEnabled(DnHexBox dnHexBox) {
			return dnHexBox.Selection != null;
		}

		public override bool IsVisible(DnHexBox dnHexBox) {
			return true;
		}
	}

	[ExportMenuItem(Header = "res:UseHexPrefixCommand", Group = MenuConstants.GROUP_CTX_HEXBOX_OPTS, Order = 0)]
	sealed class UseHexPrefixHexBoxCtxMenuCommand : HexBoxCommand {
		readonly IHexEditorSettings hexEditorSettings;

		[ImportingConstructor]
		UseHexPrefixHexBoxCtxMenuCommand(IHexEditorSettings hexEditorSettings) {
			this.hexEditorSettings = hexEditorSettings;
		}

		public override void Execute(DnHexBox dnHexBox) {
			dnHexBox.UseHexPrefix = !(dnHexBox.UseHexPrefix ?? hexEditorSettings.UseHexPrefix);
		}

		public override bool IsChecked(DnHexBox context) {
			return context.UseHexPrefix ?? hexEditorSettings.UseHexPrefix;
		}
	}

	[ExportMenuItem(Header = "res:HexEditorShowAsciiCommand", Group = MenuConstants.GROUP_CTX_HEXBOX_OPTS, Order = 10)]
	sealed class ShowAsciiHexBoxCtxMenuCommand : HexBoxCommand {
		readonly IHexEditorSettings hexEditorSettings;

		[ImportingConstructor]
		ShowAsciiHexBoxCtxMenuCommand(IHexEditorSettings hexEditorSettings) {
			this.hexEditorSettings = hexEditorSettings;
		}

		public override void Execute(DnHexBox dnHexBox) {
			dnHexBox.ShowAscii = !(dnHexBox.ShowAscii ?? hexEditorSettings.ShowAscii);
		}

		public override bool IsChecked(DnHexBox context) {
			return context.ShowAscii ?? hexEditorSettings.ShowAscii;
		}
	}

	[ExportMenuItem(Header = "res:LowerCaseHexCommand", Group = MenuConstants.GROUP_CTX_HEXBOX_OPTS, Order = 20)]
	sealed class LowerCaseHexHexBoxCtxMenuCommand : HexBoxCommand {
		readonly IHexEditorSettings hexEditorSettings;

		[ImportingConstructor]
		LowerCaseHexHexBoxCtxMenuCommand(IHexEditorSettings hexEditorSettings) {
			this.hexEditorSettings = hexEditorSettings;
		}

		public override void Execute(DnHexBox dnHexBox) {
			dnHexBox.LowerCaseHex = !(dnHexBox.LowerCaseHex ?? hexEditorSettings.LowerCaseHex);
		}

		public override bool IsChecked(DnHexBox context) {
			return context.LowerCaseHex ?? hexEditorSettings.LowerCaseHex;
		}
	}

	static class Constants {
		public const string BYTES_PER_LINE_GUID = "9356A88B-3850-4B07-A9F4-DE995042D14E";
		public const string GROUP_BYTES_PER_LINE = "0,C6BED368-103D-4928-A299-7508B50999D1";
		public const string ENCODING_GUID = "F29D6F73-1BB0-4651-BF22-611E1409BC37";
		public const string GROUP_ENCODING = "0,C2511110-0701-4D57-AD04-5E5294D7AE89";
	}

	sealed class MyMenuItem : MenuItemBase {
		readonly Action<IMenuItemContext> action;
		readonly bool isChecked;

		public MyMenuItem(Action<IMenuItemContext> action, bool isChecked = false) {
			this.action = action;
			this.isChecked = isChecked;
		}

		public override void Execute(IMenuItemContext context) {
			action(context);
		}

		public override bool IsChecked(IMenuItemContext context) {
			return isChecked;
		}
	}

	[ExportMenuItem(Header = "res:BytesPerLineCommand", Guid = Constants.BYTES_PER_LINE_GUID, Group = MenuConstants.GROUP_CTX_HEXBOX_OPTS, Order = 30)]
	sealed class BytesPerLineHexBoxCtxMenuCommand : HexBoxCommand {
		public override void Execute(DnHexBox dnHexBox) {
		}
	}

	[ExportMenuItem(OwnerGuid = Constants.BYTES_PER_LINE_GUID, Group = Constants.GROUP_BYTES_PER_LINE, Order = 0)]
	sealed class BytesPerLineSubCtxMenuCommand : HexBoxCommand, IMenuItemCreator {
		public override void Execute(DnHexBox context) {
		}

		static readonly Tuple<int?, string>[] subMenus = new Tuple<int?, string>[] {
			Tuple.Create((int?)0, dnSpy_Shared_UI_Resources.HexEditor_BytesPerLine_FitToWidth),
			Tuple.Create((int?)8, dnSpy_Shared_UI_Resources.HexEditor_BytesPerLine_8),
			Tuple.Create((int?)16, dnSpy_Shared_UI_Resources.HexEditor_BytesPerLine_16),
			Tuple.Create((int?)32, dnSpy_Shared_UI_Resources.HexEditor_BytesPerLine_32),
			Tuple.Create((int?)48, dnSpy_Shared_UI_Resources.HexEditor_BytesPerLine_48),
			Tuple.Create((int?)64, dnSpy_Shared_UI_Resources.HexEditor_BytesPerLine_64),
			Tuple.Create((int?)null, dnSpy_Shared_UI_Resources.HexEditor_Default),
		};

		public IEnumerable<CreatedMenuItem> Create(IMenuItemContext context) {
			var dnHexBox = CreateContext(context);
			Debug.Assert(dnHexBox != null);
			if (dnHexBox == null)
				yield break;

			for (int i = 0; i < subMenus.Length; i++) {
				var info = subMenus[i];
				var attr = new ExportMenuItemAttribute { Header = info.Item2 };
				bool isChecked = info.Item1 == dnHexBox.BytesPerLine;
				var item = new MyMenuItem(ctx => dnHexBox.BytesPerLine = info.Item1, isChecked);
				yield return new CreatedMenuItem(attr, item);
			}
		}
	}

	[ExportMenuItem(Header = "res:HexEditorCharacterEncodingCommand", Guid = Constants.ENCODING_GUID, Group = MenuConstants.GROUP_CTX_HEXBOX_OPTS, Order = 40)]
	sealed class EncodingHexBoxCtxMenuCommand : HexBoxCommand {
		public override void Execute(DnHexBox dnHexBox) {
		}
	}

	[ExportMenuItem(OwnerGuid = Constants.ENCODING_GUID, Group = Constants.GROUP_ENCODING, Order = 0)]
	sealed class EncodingSubCtxMenuCommand : HexBoxCommand, IMenuItemCreator {
		public override void Execute(DnHexBox context) {
		}

		static readonly Tuple<AsciiEncoding?, string>[] subMenus = new Tuple<AsciiEncoding?, string>[] {
			Tuple.Create((AsciiEncoding?)AsciiEncoding.ASCII, dnSpy_Shared_UI_Resources.HexEditor_CharacterEncoding_ASCII),
			Tuple.Create((AsciiEncoding?)AsciiEncoding.ANSI, dnSpy_Shared_UI_Resources.HexEditor_CharacterEncoding_ANSI),
			Tuple.Create((AsciiEncoding?)AsciiEncoding.UTF7, dnSpy_Shared_UI_Resources.HexEditor_CharacterEncoding_UTF7),
			Tuple.Create((AsciiEncoding?)AsciiEncoding.UTF8, dnSpy_Shared_UI_Resources.HexEditor_CharacterEncoding_UTF8),
			Tuple.Create((AsciiEncoding?)AsciiEncoding.UTF32, dnSpy_Shared_UI_Resources.HexEditor_CharacterEncoding_UTF32),
			Tuple.Create((AsciiEncoding?)AsciiEncoding.Unicode, dnSpy_Shared_UI_Resources.HexEditor_CharacterEncoding_UNICODE),
			Tuple.Create((AsciiEncoding?)AsciiEncoding.BigEndianUnicode, dnSpy_Shared_UI_Resources.HexEditor_CharacterEncoding_BIG_ENDIAN_UNICODE),
			Tuple.Create((AsciiEncoding?)null, dnSpy_Shared_UI_Resources.HexEditor_Default),
		};

		public IEnumerable<CreatedMenuItem> Create(IMenuItemContext context) {
			var dnHexBox = CreateContext(context);
			Debug.Assert(dnHexBox != null);
			if (dnHexBox == null)
				yield break;

			for (int i = 0; i < subMenus.Length; i++) {
				var info = subMenus[i];
				var attr = new ExportMenuItemAttribute { Header = info.Item2 };
				bool isChecked = info.Item1 == dnHexBox.AsciiEncoding;
				var item = new MyMenuItem(ctx => dnHexBox.AsciiEncoding = info.Item1, isChecked);
				yield return new CreatedMenuItem(attr, item);
			}
		}
	}

	[ExportMenuItem(Header = "res:HexEditorSettingsCommand", Group = MenuConstants.GROUP_CTX_HEXBOX_OPTS, Order = 1000000)]
	sealed class LocalSettingsHexBoxCtxMenuCommand : HexBoxCommand {
		readonly IAppWindow appWindow;

		[ImportingConstructor]
		LocalSettingsHexBoxCtxMenuCommand(IAppWindow appWindow) {
			this.appWindow = appWindow;
		}

		public override void Execute(DnHexBox dnHexBox) {
			var data = new LocalSettingsVM(new LocalHexSettings(dnHexBox));
			var win = new LocalSettingsDlg();
			win.DataContext = data;
			win.Owner = appWindow.MainWindow;
			if (win.ShowDialog() != true)
				return;

			data.CreateLocalHexSettings().CopyTo(dnHexBox);
		}
	}

	abstract class CopyBaseHexBoxCtxMenuCommand : HexBoxCommand {
		public override bool IsVisible(DnHexBox dnHexBox) {
			return true;
		}

		public override bool IsEnabled(DnHexBox dnHexBox) {
			return dnHexBox.Selection != null;
		}
	}

	[ExportMenuItem(Header = "res:CopyCommand", Icon = "Copy", InputGestureText = "res:CopyKey", Group = MenuConstants.GROUP_CTX_HEXBOX_COPY, Order = 0)]
	sealed class CopyHexBoxCtxMenuCommand : CopyBaseHexBoxCtxMenuCommand {
		public override void Execute(DnHexBox dnHexBox) {
			dnHexBox.Copy();
		}
	}

	[ExportMenuItem(Header = "res:CopyUTF8StringCommand", InputGestureText = "res:CopyUTF8StringKey", Group = MenuConstants.GROUP_CTX_HEXBOX_COPY, Order = 10)]
	sealed class CopyUtf8StringHexBoxCtxMenuCommand : CopyBaseHexBoxCtxMenuCommand {
		public override void Execute(DnHexBox dnHexBox) {
			dnHexBox.CopyUTF8String();
		}
	}

	[ExportMenuItem(Header = "res:CopyUnicodeStringCommand", InputGestureText = "res:CopyUnicodeStringKey", Group = MenuConstants.GROUP_CTX_HEXBOX_COPY, Order = 20)]
	sealed class CopyUnicodeStringHexBoxCtxMenuCommand : CopyBaseHexBoxCtxMenuCommand {
		public override void Execute(DnHexBox dnHexBox) {
			dnHexBox.CopyUnicodeString();
		}
	}

	[ExportMenuItem(Header = "res:CopyCSharpArrayCommand", InputGestureText = "res:CopyCSharpArrayKey", Group = MenuConstants.GROUP_CTX_HEXBOX_COPY, Order = 30)]
	sealed class CopyCSharpArrayHexBoxCtxMenuCommand : CopyBaseHexBoxCtxMenuCommand {
		public override void Execute(DnHexBox dnHexBox) {
			dnHexBox.CopyCSharpArray();
		}
	}

	[ExportMenuItem(Header = "res:CopyVBArrayCommand", InputGestureText = "res:CopyVBArrayKey", Group = MenuConstants.GROUP_CTX_HEXBOX_COPY, Order = 40)]
	sealed class CopyVBArrayHexBoxCtxMenuCommand : CopyBaseHexBoxCtxMenuCommand {
		public override void Execute(DnHexBox dnHexBox) {
			dnHexBox.CopyVBArray();
		}
	}

	[ExportMenuItem(Header = "res:CopyUIContentsCommand", InputGestureText = "res:CopyUIContentsKey", Group = MenuConstants.GROUP_CTX_HEXBOX_COPY, Order = 50)]
	sealed class CopyUIContentsHexBoxCtxMenuCommand : CopyBaseHexBoxCtxMenuCommand {
		public override void Execute(DnHexBox dnHexBox) {
			dnHexBox.CopyUIContents();
		}
	}

	[ExportMenuItem(Header = "res:CopyOffsetCommand", InputGestureText = "res:CopyOffsetKey", Group = MenuConstants.GROUP_CTX_HEXBOX_COPY, Order = 60)]
	sealed class CopyOffsetHexBoxCtxMenuCommand : HexBoxCommand {
		public override void Execute(DnHexBox dnHexBox) {
			dnHexBox.CopyOffset();
		}
	}

	[ExportMenuItem(Header = "res:PasteCommand", Icon = "Paste", InputGestureText = "res:PasteKey", Group = MenuConstants.GROUP_CTX_HEXBOX_COPY, Order = 70)]
	sealed class PasteHexBoxCtxMenuCommand : HexBoxCommand {
		public override void Execute(DnHexBox dnHexBox) {
			dnHexBox.Paste();
		}

		public override bool IsVisible(DnHexBox dnHexBox) {
			return true;
		}

		public override bool IsEnabled(DnHexBox dnHexBox) {
			return dnHexBox.CanPaste();
		}
	}

	[ExportMenuItem(Header = "res:PasteUTF8Command", InputGestureText = "res:PasteUTF8Key", Group = MenuConstants.GROUP_CTX_HEXBOX_COPY, Order = 80)]
	sealed class PasteUtf8HexBoxCtxMenuCommand : HexBoxCommand {
		public override void Execute(DnHexBox dnHexBox) {
			dnHexBox.PasteUtf8();
		}

		public override bool IsVisible(DnHexBox dnHexBox) {
			return true;
		}

		public override bool IsEnabled(DnHexBox dnHexBox) {
			return dnHexBox.CanPasteUtf8();
		}
	}

	[ExportMenuItem(Header = "res:PasteUnicodeCommand", InputGestureText = "res:PasteUnicodeKey", Group = MenuConstants.GROUP_CTX_HEXBOX_COPY, Order = 90)]
	sealed class PasteUnicodeHexBoxCtxMenuCommand : HexBoxCommand {
		public override void Execute(DnHexBox dnHexBox) {
			dnHexBox.PasteUnicode();
		}

		public override bool IsVisible(DnHexBox dnHexBox) {
			return true;
		}

		public override bool IsEnabled(DnHexBox dnHexBox) {
			return dnHexBox.CanPasteUnicode();
		}
	}

	[ExportMenuItem(Header = "res:PasteDotNetMetaDataBlobCommand", InputGestureText = "res:PasteDotNetMetaDataBlobKey", Group = MenuConstants.GROUP_CTX_HEXBOX_COPY, Order = 100)]
	sealed class PasteBlobDataHexBoxCtxMenuCommand : HexBoxCommand {
		internal static bool CanExecute(DnHexBox dnHexBox) {
			return dnHexBox != null && GetBlobData(ClipboardUtils.GetData()) != null;
		}

		internal static void Execute2(DnHexBox dnHexBox) {
			if (!CanExecute(dnHexBox))
				return;

			var data = GetBlobData(ClipboardUtils.GetData());
			if (data != null)
				dnHexBox.Paste(data);
		}

		public override void Execute(DnHexBox dnHexBox) {
			Execute2(dnHexBox);
		}

		static byte[] GetBlobData(byte[] data) {
			if (data == null)
				return null;
			uint len = (uint)data.Length;
			int extraLen = MDUtils.GetCompressedUInt32Length(len);
			if (extraLen < 0)
				return null;
			var d = new byte[data.Length + extraLen];
			MDUtils.WriteCompressedUInt32(d, 0, len);
			Array.Copy(data, 0, d, extraLen, data.Length);
			return d;
		}

		public override bool IsVisible(DnHexBox dnHexBox) {
			return true;
		}

		public override bool IsEnabled(DnHexBox dnHexBox) {
			return CanExecute(dnHexBox);
		}
	}
}
