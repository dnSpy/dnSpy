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
using System.Diagnostics;
using System.IO;
using System.Windows.Threading;
using dnSpy.Contracts.Menus;
using dnSpy.MVVM;
using dnSpy.Shared.UI.HexEditor;
using dnSpy.Shared.UI.Menus;
using dnSpy.Shared.UI.MVVM;
using dnSpy.Shared.UI.MVVM.Dialogs;
using ICSharpCode.ILSpy;
using WF = System.Windows.Forms;

namespace dnSpy.Hex {
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

	[ExportMenuItem(InputGestureText = "Ctrl+G", Group = MenuConstants.GROUP_CTX_HEXBOX_SHOW, Order = 0)]
	sealed class GoToOffsetHexBoxCtxMenuCommand : HexBoxCommand {
		public override void Execute(DnHexBox dnHexBox) {
			Execute2(dnHexBox);
		}

		public override bool IsVisible(DnHexBox dnHexBox) {
			return CanExecute(dnHexBox);
		}

		internal static bool CanExecute(DnHexBox dnHexBox) {
			return dnHexBox != null && dnHexBox.Document != null;
		}

		internal static void Execute2(DnHexBox dnHexBox) {
			if (!CanExecute(dnHexBox))
				return;

			var data = new GoToOffsetVM(dnHexBox.PhysicalToVisibleOffset(dnHexBox.CaretPosition.Offset), dnHexBox.PhysicalToVisibleOffset(dnHexBox.StartOffset), dnHexBox.PhysicalToVisibleOffset(dnHexBox.EndOffset));
			var win = new GoToOffsetDlg();
			win.DataContext = data;
			win.Owner = MainWindow.Instance;
			if (dnHexBox.IsMemory) {
				win.Title = "Go to Address";
				win.offsetLabel.Content = "_Address";
			}
			else {
				win.Title = "Go to Offset";
				win.offsetLabel.Content = "O_ffset";
			}
			if (win.ShowDialog() != true)
				return;

			dnHexBox.CaretPosition = new HexBoxPosition(dnHexBox.VisibleToPhysicalOffset(data.OffsetVM.Value), dnHexBox.CaretPosition.Kind, 0);
		}

		public override string GetHeader(DnHexBox context) {
			return context.IsMemory ? "Go to Address..." : "Go to Offset...";
		}
	}

	[ExportMenuItem(Header = "Select...", InputGestureText = "Ctrl+L", Group = MenuConstants.GROUP_CTX_HEXBOX_SHOW, Order = 10)]
	sealed class SelectRangeHexBoxCtxMenuCommand : HexBoxCommand {
		internal static void Execute2(DnHexBox dnHexBox) {
			ExecuteInternal(dnHexBox);
		}

		internal static bool CanExecute(DnHexBox dnHexBox) {
			return dnHexBox != null && dnHexBox.Document != null;
		}

		public override void Execute(DnHexBox dnHexBox) {
			ExecuteInternal(dnHexBox);
		}

		static void ExecuteInternal(DnHexBox dnHexBox) {
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
			win.Owner = MainWindow.Instance;
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

	[ExportMenuItem(Header = "Save Se_lection...", InputGestureText = "Ctrl+Alt+S", Group = MenuConstants.GROUP_CTX_HEXBOX_SHOW, Order = 20)]
	sealed class SaveSelectionHexBoxCtxMenuCommand : HexBoxCommand {
		public override void Execute(DnHexBox dnHexBox) {
			Execute2(dnHexBox);
		}

		public override bool IsVisible(DnHexBox dnHexBox) {
			return CanExecute(dnHexBox);
		}

		internal static void Execute2(DnHexBox dnHexBox) {
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

			var data = new ProgressVM(MainWindow.Instance.Dispatcher, new HexDocumentDataSaver(doc, sel.Value.StartOffset, sel.Value.EndOffset, dialog.FileName));
			var win = new ProgressDlg();
			win.DataContext = data;
			win.Owner = MainWindow.Instance;
			win.Title = string.Format("Save Selection 0x{0:X}-0x{1:X}", sel.Value.StartOffset, sel.Value.EndOffset);
			var res = win.ShowDialog();
			if (res != true)
				return;
			if (!data.WasError)
				return;
			MainWindow.Instance.ShowMessageBox(string.Format("An error occurred:\n\n{0}", data.ErrorMessage));
		}

		internal static bool CanExecute(DnHexBox dnHexBox) {
			return dnHexBox.Document != null && dnHexBox.Selection != null;
		}
	}

	[ExportMenuItem(Header = "Show Only Selected Bytes", InputGestureText = "Ctrl+D", Group = MenuConstants.GROUP_CTX_HEXBOX_SHOW, Order = 30)]
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

	[ExportMenuItem(Header = "Show All Bytes", InputGestureText = "Ctrl+Shift+D", Group = MenuConstants.GROUP_CTX_HEXBOX_SHOW, Order = 40)]
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

	[ExportMenuItem(InputGestureText = "Del", Group = MenuConstants.GROUP_CTX_HEXBOX_EDIT, Order = 0)]
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
			return context.Selection != null ? "Clear Selected Bytes" : "Clear Byte";
		}
	}

	[ExportMenuItem(Header = "Fill Selection with Byte...", Icon = "Fill", Group = MenuConstants.GROUP_CTX_HEXBOX_EDIT, Order = 10)]
	sealed class WriteToSelectionSelectionHexBoxCtxMenuCommand : HexBoxCommand {
		public override void Execute(DnHexBox dnHexBox) {
			var sel = dnHexBox.Selection;
			if (sel == null)
				return;

			var ask = new AskForInput();
			ask.Owner = MainWindow.Instance;
			ask.Title = "Enter Value";
			ask.label.Content = "_Byte";
			ask.textBox.Text = "0xFF";
			ask.ShowDialog();
			if (ask.DialogResult != true)
				return;

			string error;
			byte b = NumberVMUtils.ParseByte(ask.textBox.Text, byte.MinValue, byte.MaxValue, out error);
			if (!string.IsNullOrEmpty(error)) {
				MainWindow.Instance.ShowMessageBox(error);
				return;
			}

			dnHexBox.FillBytes(sel.Value.StartOffset, sel.Value.EndOffset, b);
			dnHexBox.Selection = null;
		}

		public override bool IsEnabled(DnHexBox dnHexBox) {
			return dnHexBox.Selection != null;
		}

		public override bool IsVisible(DnHexBox dnHexBox) {
			return true;
		}
	}

	[ExportMenuItem(Header = "Use 0x Prefix (offset)", Group = MenuConstants.GROUP_CTX_HEXBOX_OPTS, Order = 0)]
	sealed class UseHexPrefixHexBoxCtxMenuCommand : HexBoxCommand {
		public override void Execute(DnHexBox dnHexBox) {
			dnHexBox.UseHexPrefix = !(dnHexBox.UseHexPrefix ?? HexSettings.Instance.UseHexPrefix);
		}

		public override bool IsChecked(DnHexBox context) {
			return context.UseHexPrefix ?? HexSettings.Instance.UseHexPrefix;
		}
	}

	[ExportMenuItem(Header = "Show ASCII", Group = MenuConstants.GROUP_CTX_HEXBOX_OPTS, Order = 10)]
	sealed class ShowAsciiHexBoxCtxMenuCommand : HexBoxCommand {
		public override void Execute(DnHexBox dnHexBox) {
			dnHexBox.ShowAscii = !(dnHexBox.ShowAscii ?? HexSettings.Instance.ShowAscii);
		}

		public override bool IsChecked(DnHexBox context) {
			return context.ShowAscii ?? HexSettings.Instance.ShowAscii;
		}
	}

	[ExportMenuItem(Header = "Lower Case Hex", Group = MenuConstants.GROUP_CTX_HEXBOX_OPTS, Order = 20)]
	sealed class LowerCaseHexHexBoxCtxMenuCommand : HexBoxCommand {
		public override void Execute(DnHexBox dnHexBox) {
			dnHexBox.LowerCaseHex = !(dnHexBox.LowerCaseHex ?? HexSettings.Instance.LowerCaseHex);
		}

		public override bool IsChecked(DnHexBox context) {
			return context.LowerCaseHex ?? HexSettings.Instance.LowerCaseHex;
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

	[ExportMenuItem(Header = "Bytes per Line", Guid = Constants.BYTES_PER_LINE_GUID, Group = MenuConstants.GROUP_CTX_HEXBOX_OPTS, Order = 30)]
	sealed class BytesPerLineHexBoxCtxMenuCommand : HexBoxCommand {
		public override void Execute(DnHexBox dnHexBox) {
		}
	}

	[ExportMenuItem(OwnerGuid = Constants.BYTES_PER_LINE_GUID, Group = Constants.GROUP_BYTES_PER_LINE, Order = 0)]
	sealed class BytesPerLineSubCtxMenuCommand : HexBoxCommand, IMenuItemCreator {
		public override void Execute(DnHexBox context) {
		}

		static readonly Tuple<int?, string>[] subMenus = new Tuple<int?, string>[] {
			Tuple.Create((int?)0, "_Fit to Width"),
			Tuple.Create((int?)8, "_8 Bytes"),
			Tuple.Create((int?)16, "_16 Bytes"),
			Tuple.Create((int?)32, "_32 Bytes"),
			Tuple.Create((int?)48, "_48 Bytes"),
			Tuple.Create((int?)64, "_64 Bytes"),
			Tuple.Create((int?)null, "_Default"),
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

	[ExportMenuItem(Header = "Encoding", Guid = Constants.ENCODING_GUID, Group = MenuConstants.GROUP_CTX_HEXBOX_OPTS, Order = 40)]
	sealed class EncodingHexBoxCtxMenuCommand : HexBoxCommand {
		public override void Execute(DnHexBox dnHexBox) {
		}
	}

	[ExportMenuItem(OwnerGuid = Constants.ENCODING_GUID, Group = Constants.GROUP_ENCODING, Order = 0)]
	sealed class EncodingSubCtxMenuCommand : HexBoxCommand, IMenuItemCreator {
		public override void Execute(DnHexBox context) {
		}

		static readonly Tuple<AsciiEncoding?, string>[] subMenus = new Tuple<AsciiEncoding?, string>[] {
			Tuple.Create((AsciiEncoding?)AsciiEncoding.ASCII, "A_SCII"),
			Tuple.Create((AsciiEncoding?)AsciiEncoding.ANSI, "_ANSI"),
			Tuple.Create((AsciiEncoding?)AsciiEncoding.UTF7, "UTF_7"),
			Tuple.Create((AsciiEncoding?)AsciiEncoding.UTF8, "UTF_8"),
			Tuple.Create((AsciiEncoding?)AsciiEncoding.UTF32, "UTF_32"),
			Tuple.Create((AsciiEncoding?)AsciiEncoding.Unicode, "_Unicode"),
			Tuple.Create((AsciiEncoding?)AsciiEncoding.BigEndianUnicode, "_BE Unicode"),
			Tuple.Create((AsciiEncoding?)null, "_Default"),
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

	[ExportMenuItem(Header = "Settings...", Group = MenuConstants.GROUP_CTX_HEXBOX_OPTS, Order = 1000000)]
	sealed class LocalSettingsHexBoxCtxMenuCommand : HexBoxCommand {
		public override void Execute(DnHexBox dnHexBox) {
			var data = new LocalSettingsVM(new LocalHexSettings(dnHexBox));
			var win = new LocalSettingsDlg();
			win.DataContext = data;
			win.Owner = MainWindow.Instance;
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

	[ExportMenuItem(Header = "Cop_y", Icon = "Copy", InputGestureText = "Ctrl+C", Group = MenuConstants.GROUP_CTX_HEXBOX_COPY, Order = 0)]
	sealed class CopyHexBoxCtxMenuCommand : CopyBaseHexBoxCtxMenuCommand {
		public override void Execute(DnHexBox dnHexBox) {
			dnHexBox.Copy();
		}
	}

	[ExportMenuItem(Header = "Copy UTF-8 String", InputGestureText = "Ctrl+Shift+8", Group = MenuConstants.GROUP_CTX_HEXBOX_COPY, Order = 10)]
	sealed class CopyUtf8StringHexBoxCtxMenuCommand : CopyBaseHexBoxCtxMenuCommand {
		public override void Execute(DnHexBox dnHexBox) {
			dnHexBox.CopyUTF8String();
		}
	}

	[ExportMenuItem(Header = "Copy Unicode String", InputGestureText = "Ctrl+Shift+U", Group = MenuConstants.GROUP_CTX_HEXBOX_COPY, Order = 20)]
	sealed class CopyUnicodeStringHexBoxCtxMenuCommand : CopyBaseHexBoxCtxMenuCommand {
		public override void Execute(DnHexBox dnHexBox) {
			dnHexBox.CopyUnicodeString();
		}
	}

	[ExportMenuItem(Header = "Copy C# Array", InputGestureText = "Ctrl+Shift+P", Group = MenuConstants.GROUP_CTX_HEXBOX_COPY, Order = 30)]
	sealed class CopyCSharpArrayHexBoxCtxMenuCommand : CopyBaseHexBoxCtxMenuCommand {
		public override void Execute(DnHexBox dnHexBox) {
			dnHexBox.CopyCSharpArray();
		}
	}

	[ExportMenuItem(Header = "Copy VB Array", InputGestureText = "Ctrl+Shift+B", Group = MenuConstants.GROUP_CTX_HEXBOX_COPY, Order = 40)]
	sealed class CopyVBArrayHexBoxCtxMenuCommand : CopyBaseHexBoxCtxMenuCommand {
		public override void Execute(DnHexBox dnHexBox) {
			dnHexBox.CopyVBArray();
		}
	}

	[ExportMenuItem(Header = "Copy UI Contents", InputGestureText = "Ctrl+Shift+C", Group = MenuConstants.GROUP_CTX_HEXBOX_COPY, Order = 50)]
	sealed class CopyUIContentsHexBoxCtxMenuCommand : CopyBaseHexBoxCtxMenuCommand {
		public override void Execute(DnHexBox dnHexBox) {
			dnHexBox.CopyUIContents();
		}
	}

	[ExportMenuItem(Header = "Copy Offset", InputGestureText = "Ctrl+Alt+O", Group = MenuConstants.GROUP_CTX_HEXBOX_COPY, Order = 60)]
	sealed class CopyOffsetHexBoxCtxMenuCommand : HexBoxCommand {
		public override void Execute(DnHexBox dnHexBox) {
			dnHexBox.CopyOffset();
		}
	}

	[ExportMenuItem(Header = "_Paste", Icon = "Paste", InputGestureText = "Ctrl+V", Group = MenuConstants.GROUP_CTX_HEXBOX_COPY, Order = 70)]
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

	[ExportMenuItem(Header = "_Paste (UTF-8)", InputGestureText = "Ctrl+8", Group = MenuConstants.GROUP_CTX_HEXBOX_COPY, Order = 80)]
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

	[ExportMenuItem(Header = "_Paste (Unicode)", InputGestureText = "Ctrl+U", Group = MenuConstants.GROUP_CTX_HEXBOX_COPY, Order = 90)]
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

	[ExportMenuItem(Header = "_Paste (#Blob Data)", InputGestureText = "Ctrl+B", Group = MenuConstants.GROUP_CTX_HEXBOX_COPY, Order = 100)]
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
