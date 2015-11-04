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
using System.Diagnostics;
using System.IO;
using System.Windows.Controls;
using System.Windows.Threading;
using dnSpy.HexEditor;
using dnSpy.MVVM;
using dnSpy.MVVM.Dialogs;
using ICSharpCode.ILSpy;
using WF = System.Windows.Forms;

namespace dnSpy.Hex {
	abstract class HexBoxContextMenuEntry : IContextMenuEntry2 {
		public void Execute(ContextMenuEntryContext context) {
			var dnHexBox = GetDnHexBox(context.Element as HexBox);
			if (dnHexBox != null)
				Execute(dnHexBox);
		}

		public void Initialize(ContextMenuEntryContext context, MenuItem menuItem) {
			var dnHexBox = GetDnHexBox(context.Element as HexBox);
			if (dnHexBox != null)
				Initialize(dnHexBox, menuItem);
		}

		public bool IsEnabled(ContextMenuEntryContext context) {
			var dnHexBox = GetDnHexBox(context.Element as HexBox);
			if (dnHexBox != null)
				return IsEnabled(dnHexBox);
			return false;
		}

		public bool IsVisible(ContextMenuEntryContext context) {
			var dnHexBox = GetDnHexBox(context.Element as HexBox);
			if (dnHexBox != null)
				return IsVisible(dnHexBox);
			return false;
		}

		static DnHexBox GetDnHexBox(HexBox hexBox) {
			var dnHexBox = hexBox as DnHexBox;
			Debug.Assert(dnHexBox != null || hexBox == null);
			return dnHexBox;
		}

		protected abstract void Execute(DnHexBox dnHexBox);
		protected virtual void Initialize(DnHexBox dnHexBox, MenuItem menuItem) {
		}
		protected virtual bool IsEnabled(DnHexBox dnHexBox) {
			return IsVisible(dnHexBox);
		}
		protected abstract bool IsVisible(DnHexBox dnHexBox);
	}

	[ExportContextMenuEntry(Order = 100, Category = "Misc", InputGestureText = "Ctrl+G")]
	sealed class GoToOffsetHexBoxContextMenuEntry : HexBoxContextMenuEntry {
		protected override void Execute(DnHexBox dnHexBox) {
			Execute2(dnHexBox);
		}

		protected override bool IsVisible(DnHexBox dnHexBox) {
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

		protected override void Initialize(DnHexBox dnHexBox, MenuItem menuItem) {
			menuItem.Header = dnHexBox.IsMemory ? "Go to Address..." : "Go to Offset...";
		}
	}

	[ExportContextMenuEntry(Header = "Select...", Order = 110, Category = "Misc", InputGestureText = "Ctrl+L")]
	sealed class SelectRangeHexBoxContextMenuEntry : HexBoxContextMenuEntry {
		internal static void Execute2(DnHexBox dnHexBox) {
			ExecuteInternal(dnHexBox);
		}

		internal static bool CanExecute(DnHexBox dnHexBox) {
			return dnHexBox != null && dnHexBox.Document != null;
		}

		protected override void Execute(DnHexBox dnHexBox) {
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

		protected override bool IsEnabled(DnHexBox dnHexBox) {
			return CanExecute(dnHexBox);
		}

		protected override bool IsVisible(DnHexBox dnHexBox) {
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

	[ExportContextMenuEntry(Header = "Save Se_lection...", Order = 120, Category = "Misc", InputGestureText = "Ctrl+Alt+S")]
	sealed class SaveSelectionHexBoxContextMenuEntry : HexBoxContextMenuEntry {
		protected override void Execute(DnHexBox dnHexBox) {
			Execute2(dnHexBox);
		}

		protected override bool IsVisible(DnHexBox dnHexBox) {
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

	[ExportContextMenuEntry(Header = "Show Only Selected Bytes", Order = 130, Category = "Misc", InputGestureText = "Ctrl+D")]
	sealed class ShowSelectionHexBoxContextMenuEntry : HexBoxContextMenuEntry {
		protected override void Execute(DnHexBox dnHexBox) {
			Execute2(dnHexBox);
		}

		protected override bool IsVisible(DnHexBox dnHexBox) {
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

	[ExportContextMenuEntry(Header = "Show All Bytes", Order = 140, Category = "Misc", InputGestureText = "Ctrl+Shift+D")]
	sealed class ShowWholeDocumentHexBoxContextMenuEntry : HexBoxContextMenuEntry {
		protected override void Execute(DnHexBox dnHexBox) {
			Execute2(dnHexBox);
		}

		protected override bool IsVisible(DnHexBox dnHexBox) {
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

	[ExportContextMenuEntry(Order = 200, Category = "Edit", InputGestureText = "Del")]
	sealed class ClearSelectionHexBoxContextMenuEntry : HexBoxContextMenuEntry {
		protected override void Execute(DnHexBox dnHexBox) {
			dnHexBox.ClearBytes();
		}

		protected override bool IsEnabled(DnHexBox dnHexBox) {
			return dnHexBox.Document != null;
		}

		protected override bool IsVisible(DnHexBox dnHexBox) {
			return true;
		}

		protected override void Initialize(DnHexBox dnHexBox, MenuItem menuItem) {
			menuItem.Header = dnHexBox.Selection != null ? "Clear Selected Bytes" : "Clear Byte";
		}
	}

	[ExportContextMenuEntry(Header = "Fill Selection with Byte...", Order = 210, Category = "Edit", Icon = "Fill")]
	sealed class WriteToSelectionSelectionHexBoxContextMenuEntry : HexBoxContextMenuEntry {
		protected override void Execute(DnHexBox dnHexBox) {
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

		protected override bool IsEnabled(DnHexBox dnHexBox) {
			return dnHexBox.Selection != null;
		}

		protected override bool IsVisible(DnHexBox dnHexBox) {
			return true;
		}
	}

	[ExportContextMenuEntry(Header = "Use 0x Prefix (offset)", Order = 500, Category = "Options")]
	sealed class UseHexPrefixHexBoxContextMenuEntry : HexBoxContextMenuEntry {
		protected override void Execute(DnHexBox dnHexBox) {
			dnHexBox.UseHexPrefix = !(dnHexBox.UseHexPrefix ?? HexSettings.Instance.UseHexPrefix);
		}

		protected override bool IsVisible(DnHexBox dnHexBox) {
			return true;
		}

		protected override void Initialize(DnHexBox dnHexBox, MenuItem menuItem) {
			menuItem.IsChecked = dnHexBox.UseHexPrefix ?? HexSettings.Instance.UseHexPrefix;
		}
	}

	[ExportContextMenuEntry(Header = "Show ASCII", Order = 510, Category = "Options")]
	sealed class ShowAsciiHexBoxContextMenuEntry : HexBoxContextMenuEntry {
		protected override void Execute(DnHexBox dnHexBox) {
			dnHexBox.ShowAscii = !(dnHexBox.ShowAscii ?? HexSettings.Instance.ShowAscii);
		}

		protected override bool IsVisible(DnHexBox dnHexBox) {
			return true;
		}

		protected override void Initialize(DnHexBox dnHexBox, MenuItem menuItem) {
			menuItem.IsChecked = dnHexBox.ShowAscii ?? HexSettings.Instance.ShowAscii;
		}
	}

	[ExportContextMenuEntry(Header = "Lower Case Hex", Order = 520, Category = "Options")]
	sealed class LowerCaseHexHexBoxContextMenuEntry : HexBoxContextMenuEntry {
		protected override void Execute(DnHexBox dnHexBox) {
			dnHexBox.LowerCaseHex = !(dnHexBox.LowerCaseHex ?? HexSettings.Instance.LowerCaseHex);
		}

		protected override bool IsVisible(DnHexBox dnHexBox) {
			return true;
		}

		protected override void Initialize(DnHexBox dnHexBox, MenuItem menuItem) {
			menuItem.IsChecked = dnHexBox.LowerCaseHex ?? HexSettings.Instance.LowerCaseHex;
		}
	}

	[ExportContextMenuEntry(Header = "Bytes per Line", Order = 530, Category = "Options")]
	sealed class BytesPerLineHexBoxContextMenuEntry : HexBoxContextMenuEntry {
		protected override void Execute(DnHexBox dnHexBox) {
		}

		protected override bool IsVisible(DnHexBox dnHexBox) {
			return true;
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

		protected override void Initialize(DnHexBox dnHexBox, MenuItem menuItem) {
			foreach (var info in subMenus) {
				var mi = new MenuItem {
					Header = info.Item2,
					IsChecked = info.Item1 == dnHexBox.BytesPerLine,
				};
				var tmpInfo = info;
				mi.Click += (s, e) => dnHexBox.BytesPerLine = tmpInfo.Item1;
				menuItem.Items.Add(mi);
			}
		}
	}

	[ExportContextMenuEntry(Header = "Encoding", Order = 540, Category = "Options")]
	sealed class EncodingHexBoxContextMenuEntry : HexBoxContextMenuEntry {
		protected override void Execute(DnHexBox dnHexBox) {
		}

		protected override bool IsVisible(DnHexBox dnHexBox) {
			return true;
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

		protected override void Initialize(DnHexBox dnHexBox, MenuItem menuItem) {
			foreach (var info in subMenus) {
				var mi = new MenuItem {
					Header = info.Item2,
					IsChecked = info.Item1 == dnHexBox.AsciiEncoding,
				};
				var tmpInfo = info;
				mi.Click += (s, e) => dnHexBox.AsciiEncoding = tmpInfo.Item1;
				menuItem.Items.Add(mi);
			}
		}
	}

	[ExportContextMenuEntry(Header = "Settings...", Order = 599, Category = "Options")]
	sealed class LocalSettingsHexBoxContextMenuEntry : HexBoxContextMenuEntry {
		protected override void Execute(DnHexBox dnHexBox) {
			var data = new LocalSettingsVM(new LocalHexSettings(dnHexBox));
			var win = new LocalSettingsDlg();
			win.DataContext = data;
			win.Owner = MainWindow.Instance;
			if (win.ShowDialog() != true)
				return;

			data.CreateLocalHexSettings().CopyTo(dnHexBox);
		}

		protected override bool IsVisible(DnHexBox dnHexBox) {
			return true;
		}
	}

	abstract class CopyBaseHexBoxContextMenuEntry : HexBoxContextMenuEntry {
		protected override bool IsVisible(DnHexBox dnHexBox) {
			return true;
		}

		protected override bool IsEnabled(DnHexBox dnHexBox) {
			return dnHexBox.Selection != null;
		}
	}

	[ExportContextMenuEntry(Header = "Cop_y", Order = 600, Category = "HexCopy2", Icon = "Copy", InputGestureText = "Ctrl+C")]
	sealed class CopyHexBoxContextMenuEntry : CopyBaseHexBoxContextMenuEntry {
		protected override void Execute(DnHexBox dnHexBox) {
			dnHexBox.Copy();
		}
	}

	[ExportContextMenuEntry(Header = "Copy UTF-8 String", Order = 610, Category = "HexCopy2", InputGestureText = "Ctrl+Shift+8")]
	sealed class CopyUtf8StringHexBoxContextMenuEntry : CopyBaseHexBoxContextMenuEntry {
		protected override void Execute(DnHexBox dnHexBox) {
			dnHexBox.CopyUTF8String();
		}
	}

	[ExportContextMenuEntry(Header = "Copy Unicode String", Order = 620, Category = "HexCopy2", InputGestureText = "Ctrl+Shift+U")]
	sealed class CopyUnicodeStringHexBoxContextMenuEntry : CopyBaseHexBoxContextMenuEntry {
		protected override void Execute(DnHexBox dnHexBox) {
			dnHexBox.CopyUnicodeString();
		}
	}

	[ExportContextMenuEntry(Header = "Copy C# Array", Order = 630, Category = "HexCopy2", InputGestureText = "Ctrl+Shift+P")]
	sealed class CopyCSharpArrayHexBoxContextMenuEntry : CopyBaseHexBoxContextMenuEntry {
		protected override void Execute(DnHexBox dnHexBox) {
			dnHexBox.CopyCSharpArray();
		}
	}

	[ExportContextMenuEntry(Header = "Copy VB Array", Order = 640, Category = "HexCopy2", InputGestureText = "Ctrl+Shift+B")]
	sealed class CopyVBArrayHexBoxContextMenuEntry : CopyBaseHexBoxContextMenuEntry {
		protected override void Execute(DnHexBox dnHexBox) {
			dnHexBox.CopyVBArray();
		}
	}

	[ExportContextMenuEntry(Header = "Copy UI Contents", Order = 650, Category = "HexCopy2", InputGestureText = "Ctrl+Shift+C")]
	sealed class CopyUIContentsHexBoxContextMenuEntry : CopyBaseHexBoxContextMenuEntry {
		protected override void Execute(DnHexBox dnHexBox) {
			dnHexBox.CopyUIContents();
		}
	}

	[ExportContextMenuEntry(Header = "Copy Offset", Order = 660, Category = "HexCopy2", InputGestureText = "Ctrl+Alt+O")]
	sealed class CopyOffsetHexBoxContextMenuEntry : HexBoxContextMenuEntry {
		protected override void Execute(DnHexBox dnHexBox) {
			dnHexBox.CopyOffset();
		}

		protected override bool IsVisible(DnHexBox dnHexBox) {
			return true;
		}
	}

	[ExportContextMenuEntry(Header = "_Paste", Order = 670, Category = "HexCopy2", Icon = "Paste", InputGestureText = "Ctrl+V")]
	sealed class PasteHexBoxContextMenuEntry : HexBoxContextMenuEntry {
		protected override void Execute(DnHexBox dnHexBox) {
			dnHexBox.Paste();
		}

		protected override bool IsVisible(DnHexBox dnHexBox) {
			return true;
		}

		protected override bool IsEnabled(DnHexBox dnHexBox) {
			return dnHexBox.CanPaste();
		}
	}

	[ExportContextMenuEntry(Header = "_Paste (UTF-8)", Order = 680, Category = "HexCopy2", InputGestureText = "Ctrl+8")]
	sealed class PasteUtf8HexBoxContextMenuEntry : HexBoxContextMenuEntry {
		protected override void Execute(DnHexBox dnHexBox) {
			dnHexBox.PasteUtf8();
		}

		protected override bool IsVisible(DnHexBox dnHexBox) {
			return true;
		}

		protected override bool IsEnabled(DnHexBox dnHexBox) {
			return dnHexBox.CanPasteUtf8();
		}
	}

	[ExportContextMenuEntry(Header = "_Paste (Unicode)", Order = 690, Category = "HexCopy2", InputGestureText = "Ctrl+U")]
	sealed class PasteUnicodeHexBoxContextMenuEntry : HexBoxContextMenuEntry {
		protected override void Execute(DnHexBox dnHexBox) {
			dnHexBox.PasteUnicode();
		}

		protected override bool IsVisible(DnHexBox dnHexBox) {
			return true;
		}

		protected override bool IsEnabled(DnHexBox dnHexBox) {
			return dnHexBox.CanPasteUnicode();
		}
	}

	[ExportContextMenuEntry(Header = "_Paste (#Blob Data)", Order = 7000, Category = "HexCopy2", InputGestureText = "Ctrl+B")]
	sealed class PasteBlobDataHexBoxContextMenuEntry : HexBoxContextMenuEntry {
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

		protected override void Execute(DnHexBox dnHexBox) {
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

		protected override bool IsVisible(DnHexBox dnHexBox) {
			return true;
		}

		protected override bool IsEnabled(DnHexBox dnHexBox) {
			return CanExecute(dnHexBox);
		}
	}
}
