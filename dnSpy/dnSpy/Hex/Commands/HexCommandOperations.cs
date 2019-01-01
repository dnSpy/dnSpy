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
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using dnSpy.Contracts.App;
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.Hex.Editor;
using dnSpy.Contracts.Hex.Editor.HexGroups;
using dnSpy.Contracts.Hex.Editor.OptionsExtensionMethods;
using dnSpy.Contracts.Hex.Files;
using dnSpy.Contracts.Hex.Files.DotNet;
using dnSpy.Contracts.Hex.Files.PE;
using dnSpy.Contracts.MVVM;
using dnSpy.Contracts.MVVM.Dialogs;
using dnSpy.Contracts.Utilities;
using dnSpy.Properties;
using VSTE = Microsoft.VisualStudio.Text.Editor;
using WF = System.Windows.Forms;

namespace dnSpy.Hex.Commands {
	abstract class HexCommandOperations {
		public abstract HexView HexView { get; }
		public abstract void GoToPosition(PositionKind positionKind);
		public abstract void GoToMetadata(GoToMetadataKind mdKind);
		public abstract void Select();
		public abstract void SaveSelection();
		public abstract void FillSelection();
		public abstract void EditLocalSettings();
		public abstract void ResetLocalSettings();
		public abstract void ToggleUseRelativePositions();
	}

	enum PositionKind {
		Absolute,
		File,
		RVA,
		CurrentPosition,
	}

	enum GoToMetadataKind {
		Blob,
		Strings,
		US,
		GUID,
		Table,
		MemberRva,
	}

	sealed class HexCommandOperationsImpl : HexCommandOperations {
		public override HexView HexView { get; }
		readonly IMessageBoxService messageBoxService;
		readonly Lazy<HexEditorGroupFactoryService> hexEditorGroupFactoryService;
		readonly Lazy<HexBufferFileServiceFactory> hexBufferFileServiceFactory;

		HexBufferFileService HexBufferFileService => __hexBufferFileService ?? (__hexBufferFileService = hexBufferFileServiceFactory.Value.Create(HexView.Buffer));
		HexBufferFileService __hexBufferFileService;

		public HexCommandOperationsImpl(IMessageBoxService messageBoxService, Lazy<HexEditorGroupFactoryService> hexEditorGroupFactoryService, Lazy<HexBufferFileServiceFactory> hexBufferFileServiceFactory, HexView hexView) {
			this.messageBoxService = messageBoxService ?? throw new ArgumentNullException(nameof(messageBoxService));
			this.hexEditorGroupFactoryService = hexEditorGroupFactoryService ?? throw new ArgumentNullException(nameof(hexEditorGroupFactoryService));
			this.hexBufferFileServiceFactory = hexBufferFileServiceFactory ?? throw new ArgumentNullException(nameof(hexBufferFileServiceFactory));
			HexView = hexView ?? throw new ArgumentNullException(nameof(hexView));
			hexView.Closed += HexView_Closed;
		}

		void HexView_Closed(object sender, EventArgs e) {
			HexView.Closed -= HexView_Closed;
			HexCommandOperationsFactoryServiceImpl.RemoveFromProperties(this);
		}

		Dispatcher CurrentDispatcher {
			get {
				var wpfHexView = HexView as WpfHexView;
				Debug.Assert(wpfHexView != null);
				return wpfHexView?.VisualElement.Dispatcher ?? Dispatcher.CurrentDispatcher;
			}
		}

		Window OwnerWindow {
			get {
				var wpfHexView = HexView as WpfHexView;
				Debug.Assert(wpfHexView != null);
				if (wpfHexView != null) {
					var window = Window.GetWindow(wpfHexView.VisualElement);
					if (window != null)
						return window;
				}
				return Application.Current.MainWindow;
			}
		}

		public override void GoToPosition(PositionKind positionKind) {
			var origPos = HexView.Caret.Position.Position.ActivePosition.BufferPosition;
			var selectPos = HexView.Selection.IsEmpty ? origPos :
				HexView.Selection.AnchorPoint < HexView.Selection.ActivePoint ?
				HexView.Selection.AnchorPoint : HexView.Selection.AnchorPoint - 1;
			var data = new GoToPositionVM(HexView.Buffer.ReadUInt32(origPos));
			data.PositionKind = positionKind;
			var win = new GoToPositionDlg();
			win.DataContext = data;
			win.Owner = OwnerWindow;
			if (win.ShowDialog() != true)
				return;

			var pos = Filter(ToBufferPosition(origPos, data.OffsetVM.Value, data.PositionKind));
			var newPos = new HexBufferPoint(HexView.Buffer, pos);
			if (!data.SelectToNewPosition)
				MoveTo(newPos, newPos, newPos, select: false);
			else {
				var info = UserValueToSelection(selectPos, newPos);
				MoveTo(new HexBufferPoint(HexView.Buffer, info.Anchor), new HexBufferPoint(HexView.Buffer, info.Active), new HexBufferPoint(HexView.Buffer, info.Caret), select: true);
			}
		}

		HexPosition Filter(HexPosition position) {
			if (position < HexView.BufferLines.StartPosition)
				return HexView.BufferLines.StartPosition;
			else if (position > HexView.BufferLines.EndPosition)
				return HexView.BufferLines.EndPosition;
			return position;
		}

		HexPosition ToBufferPosition(HexPosition origPosition, ulong position, PositionKind positionKind) {
			switch (positionKind) {
			case PositionKind.Absolute:
				return HexView.BufferLines.ToPhysicalPosition(position);

			case PositionKind.File:
				return TryGetPeHeaders(origPosition)?.FilePositionToBufferPosition(position) ?? position;

			case PositionKind.RVA:
				return TryGetPeHeaders(origPosition)?.RvaToBufferPosition((uint)position) ?? position;

			case PositionKind.CurrentPosition:
				return (origPosition + position).ToUInt64();

			default:
				throw new ArgumentOutOfRangeException(nameof(positionKind));
			}
		}

		HexBufferFile TryGetFile(HexPosition position) => HexBufferFileService.GetFile(position, checkNestedFiles: false);
		PeHeaders TryGetPeHeaders(HexPosition position) => TryGetFile(position)?.GetHeaders<PeHeaders>();
		DotNetMetadataHeaders TryGetMetadataHeaders(HexPosition position) => TryGetFile(position)?.GetHeaders<DotNetMetadataHeaders>();

		public override void GoToMetadata(GoToMetadataKind mdKind) {
			var origPos = HexView.Caret.Position.Position.ActivePosition.BufferPosition;
			var mdHeaders = TryGetMetadataHeaders(origPos);
			if (mdHeaders == null)
				return;
			var peHeaders = TryGetPeHeaders(origPos);
			if (peHeaders == null && mdKind == GoToMetadataKind.MemberRva)
				mdKind = GoToMetadataKind.Table;
			var data = new GoToMetadataVM(HexView.Buffer, mdHeaders, peHeaders, HexView.Buffer.ReadUInt32(origPos));
			data.GoToMetadataKind = mdKind;
			var win = new GoToMetadataDlg();
			win.DataContext = data;
			win.Owner = OwnerWindow;
			if (win.ShowDialog() != true)
				return;

			var span = GetGoToMetadataSpan(mdHeaders, peHeaders, data.OffsetTokenValue, data.GoToMetadataKind);
			Debug.Assert(span != null);
			if (span == null)
				return;
			var info = UserValueToSelection(span.Value.End, span.Value.Start);
			MoveTo(new HexBufferPoint(HexView.Buffer, info.Anchor), new HexBufferPoint(HexView.Buffer, info.Active), new HexBufferPoint(HexView.Buffer, info.Caret), select: false);
		}

		HexSpan? GetGoToMetadataSpan(DotNetMetadataHeaders mdHeaders, PeHeaders peHeaders, uint offsetTokenValue, GoToMetadataKind mdKind) {
			MDTable mdTable;
			switch (mdKind) {
			case GoToMetadataKind.Blob:
				if (mdHeaders.BlobStream == null)
					return null;
				return new HexSpan(mdHeaders.BlobStream.Span.Span.Start + offsetTokenValue, 0);

			case GoToMetadataKind.Strings:
				if (mdHeaders.StringsStream == null)
					return null;
				return new HexSpan(mdHeaders.StringsStream.Span.Span.Start + offsetTokenValue, 0);

			case GoToMetadataKind.US:
				if (mdHeaders.USStream == null)
					return null;
				return new HexSpan(mdHeaders.USStream.Span.Span.Start + (offsetTokenValue & 0x00FFFFFF), 0);

			case GoToMetadataKind.GUID:
				if (mdHeaders.GUIDStream == null)
					return null;
				return new HexSpan(mdHeaders.GUIDStream.Span.Span.Start + (offsetTokenValue - 1) * 16, 16);

			case GoToMetadataKind.Table:
				mdTable = GetMDTable(mdHeaders, offsetTokenValue);
				if (mdTable == null)
					return null;
				return new HexSpan(mdTable.Span.Start + ((offsetTokenValue & 0x00FFFFFF) - 1) * mdTable.RowSize, mdTable.RowSize);

			case GoToMetadataKind.MemberRva:
				if (peHeaders == null)
					return null;
				mdTable = GetMDTable(mdHeaders, offsetTokenValue);
				if (mdTable == null)
					return null;
				if (mdTable.Table != Table.Method && mdTable.Table != Table.FieldRVA)
					return null;
				// Column 0 is the RVA in both Method and FieldRVA tables
				var pos = mdTable.Span.Start + ((offsetTokenValue & 0x00FFFFFF) - 1) * mdTable.RowSize;
				var rva = HexView.Buffer.ReadUInt32(pos);
				return new HexSpan(peHeaders.RvaToBufferPosition(rva), 0);

			default: throw new InvalidOperationException();
			}
		}

		static MDTable GetMDTable(DotNetMetadataHeaders mdHeaders, uint token) {
			var tablesStream = mdHeaders.TablesStream;
			if (tablesStream == null)
				return null;
			var table = token >> 24;
			if (table >= (uint)tablesStream.MDTables.Count)
				return null;
			var mdTable = tablesStream.MDTables[(int)table];
			return mdTable?.IsValidRID(token & 0x00FFFFFF) == true ? mdTable : null;
		}

		bool MoveTo(HexBufferPoint start, HexBufferPoint end, HexBufferPoint caret, bool select) {
			if (!HexView.BufferLines.IsValidPosition(start) || !HexView.BufferLines.IsValidPosition(end) || !HexView.BufferLines.IsValidPosition(caret))
				return false;
			HexView.Caret.MoveTo(caret);
			var flags = HexView.Caret.Position.Position.ActiveColumn == HexColumnType.Values ? HexSpanSelectionFlags.Values : HexSpanSelectionFlags.Ascii;
			var span = start <= end ? new HexBufferSpan(start, end) : new HexBufferSpan(end, start);
			HexView.ViewScroller.EnsureSpanVisible(span, flags, VSTE.EnsureSpanVisibleOptions.ShowStart);
			HexView.Caret.EnsureVisible();
			if (select)
				HexView.Selection.Select(start, end, alignPoints: false);
			else
				HexView.Selection.Clear();
			return true;
		}

		readonly struct SelectionInfo {
			public HexPosition Anchor { get; }
			public HexPosition Active { get; }
			public HexPosition Caret { get; }
			public SelectionInfo(HexPosition anchor, HexPosition active, HexPosition caret) {
				Anchor = anchor;
				Active = active;
				Caret = caret;
			}
		}

		static SelectionInfo UserValueToSelection(HexPosition anchor, HexPosition active) {
			if (anchor <= active)
				return new SelectionInfo(anchor, HexPosition.Min(active + 1, HexPosition.MaxEndPosition), active);
			return new SelectionInfo(HexPosition.Min(anchor + 1, HexPosition.MaxEndPosition), active, active);
		}

		static SelectionInfo SelectionToUserValue(HexPosition anchor, HexPosition active) {
			if (anchor == active)
				return new SelectionInfo(anchor, active, active);
			if (anchor < active)
				return new SelectionInfo(anchor, active - 1, active - 1);
			return new SelectionInfo(anchor - 1, active, active);
		}

		public override void Select() {
			HexPosition start, end;
			if (HexView.Selection.IsEmpty) {
				start = HexView.Caret.Position.Position.ActivePosition.BufferPosition.Position;
				end = start;
			}
			else {
				var info2 = SelectionToUserValue(HexView.Selection.AnchorPoint, HexView.Selection.ActivePoint);
				start = info2.Anchor;
				end = info2.Active;
			}
			var logStart = HexView.BufferLines.ToLogicalPosition(start);
			var logEnd = HexView.BufferLines.ToLogicalPosition(end);
			var data = new SelectVM(logStart, logEnd);
			data.PositionKind = PositionKind.Absolute;
			data.PositionLengthKind = SelectPositionLengthKind.Position;
			var win = new SelectDlg();
			win.DataContext = data;
			win.Owner = OwnerWindow;
			if (win.ShowDialog() != true)
				return;

			var newStart = ToBufferPosition(logStart, data.StartVM.Value.ToUInt64(), data.PositionKind);
			var newEnd = GetEndPosition(logStart, newStart, data.EndVM.Value, data.PositionKind, data.PositionLengthKind);
			var info = UserValueToSelection(newStart, newEnd);

			MoveTo(new HexBufferPoint(HexView.Buffer, info.Anchor), new HexBufferPoint(HexView.Buffer, info.Active), new HexBufferPoint(HexView.Buffer, info.Caret), select: true);
		}

		HexPosition GetEndPosition(HexPosition origPos, HexPosition startPos, HexPosition pos, PositionKind positionKind, SelectPositionLengthKind selectPosKind) {
			switch (selectPosKind) {
			case SelectPositionLengthKind.Position:
				switch (positionKind) {
				case PositionKind.Absolute:
				case PositionKind.File:
				case PositionKind.RVA:
					return ToBufferPosition(origPos, pos.ToUInt64(), positionKind);

				case PositionKind.CurrentPosition:
					return (origPos + pos).ToUInt64();

				default: throw new InvalidOperationException();
				}

			case SelectPositionLengthKind.Length:
				if (pos == HexPosition.Zero)
					return startPos;
				return (startPos + pos - 1).ToUInt64();

			default: throw new InvalidOperationException();
			}
		}

		public override void SaveSelection() {
			if (HexView.Selection.IsEmpty)
				return;

			var dialog = new WF.SaveFileDialog() {
				Filter = PickFilenameConstants.AnyFilenameFilter,
				RestoreDirectory = true,
				ValidateNames = true,
			};
			if (dialog.ShowDialog() != WF.DialogResult.OK)
				return;

			var selectionSpan = HexView.Selection.StreamSelectionSpan;
			var data = new ProgressVM(CurrentDispatcher, new HexBufferDataSaver(HexView.Buffer, selectionSpan, dialog.FileName));
			var win = new ProgressDlg();
			win.DataContext = data;
			win.Owner = OwnerWindow;
			var info = SelectionToUserValue(selectionSpan.Start, selectionSpan.End);
			win.Title = string.Format(dnSpy_Resources.HexEditorSaveSelection_Title, info.Anchor.ToUInt64(), info.Active.ToUInt64());
			var res = win.ShowDialog();
			if (res != true)
				return;
			if (!data.WasError)
				return;
			messageBoxService.Show(string.Format(dnSpy_Resources.AnErrorOccurred, data.ErrorMessage));
		}

		public override void FillSelection() {
			if (HexView.Selection.IsEmpty)
				return;

			var res = messageBoxService.Ask<byte?>(dnSpy_Resources.FillSelection_Label, "0xFF", dnSpy_Resources.FillSelection_Title, s => {
				byte b = SimpleTypeConverter.ParseByte(s, byte.MinValue, byte.MaxValue, out string error);
				return string.IsNullOrEmpty(error) ? b : (byte?)null;
			}, s => {
				byte b = SimpleTypeConverter.ParseByte(s, byte.MinValue, byte.MaxValue, out string error);
				return error;
			});
			if (res == null)
				return;

			try {
				var span = HexView.Selection.StreamSelectionSpan;
				var data = new byte[span.IsFull ? ulong.MaxValue : span.Length.ToUInt64()];
				byte b = res.Value;
				if (b != 0) {
					if (data.LongLength <= int.MaxValue) {
						for (int i = 0; i < data.Length; i++)
							data[i] = b;
					}
					else {
						for (long i = 0; i < data.LongLength; i++)
							data[i] = b;
					}
				}
				HexView.Buffer.Replace(span.Start, data);
			}
			catch (ArithmeticException) {
				messageBoxService.Show("Out of memory");
			}
			catch (OutOfMemoryException) {
				messageBoxService.Show("Out of memory");
			}
			HexView.Selection.Clear();
		}

		public override void EditLocalSettings() {
			var vm = new LocalSettingsVM(new LocalGroupOptions(HexView), hexEditorGroupFactoryService.Value.GetDefaultLocalOptions(HexView));
			var win = new LocalSettingsDlg();
			win.DataContext = vm;
			win.Owner = OwnerWindow;
			if (win.ShowDialog() != true)
				return;

			vm.TryGetLocalGroupOptions().WriteTo(HexView);
		}

		public override void ResetLocalSettings() =>
			hexEditorGroupFactoryService.Value.GetDefaultLocalOptions(HexView).WriteTo(HexView);

		public override void ToggleUseRelativePositions() =>
			HexView.Options.SetOptionValue(DefaultHexViewOptions.UseRelativePositionsId, !HexView.Options.UseRelativePositions());
	}

	sealed class HexBufferDataSaver : IProgressTask {
		public bool IsIndeterminate => false;
		public double ProgressMinimum => 0;
		public double ProgressMaximum => progressMaximum;

		readonly HexBuffer buffer;
		readonly long progressMaximum;
		readonly string filename;
		readonly HexSpan span;
		const int BUF_SIZE = 64 * 1024;

		public HexBufferDataSaver(HexBuffer buffer, HexSpan span, string filename) {
			this.buffer = buffer;
			this.span = span;
			this.filename = filename;
			ulong len = span.IsFull ? ulong.MaxValue : span.Length.ToUInt64();
			if (len + BUF_SIZE - 1 < len)
				progressMaximum = (long)(0x8000000000000000UL / (BUF_SIZE / 2));
			else
				progressMaximum = (long)((len + BUF_SIZE - 1) / BUF_SIZE);
		}

		public void Execute(IProgress progress) {
			progress.SetDescription(filename);
			var file = File.Create(filename);
			try {
				var buf = new byte[BUF_SIZE];
				var pos = span.Start;
				long currentProgress = 0;
				while (pos < span.End) {
					progress.ThrowIfCancellationRequested();
					progress.SetTotalProgress(currentProgress);
					currentProgress++;
					ulong left = (span.End - pos).ToUInt64();
					if (left == 0)
						left = ulong.MaxValue;
					int size = left > (ulong)buf.Length ? buf.Length : (int)left;
					buffer.ReadBytes(pos, buf, 0, size);
					file.Write(buf, 0, size);
					pos += (ulong)size;
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
}
