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
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using dnSpy.Contracts.App;
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.Hex.Editor;
using dnSpy.Contracts.Hex.Editor.HexGroups;
using dnSpy.Contracts.Hex.Editor.OptionsExtensionMethods;
using dnSpy.Contracts.MVVM;
using dnSpy.Contracts.MVVM.Dialogs;
using dnSpy.Contracts.Utilities;
using dnSpy.Properties;
using VSTE = Microsoft.VisualStudio.Text.Editor;
using WF = System.Windows.Forms;

namespace dnSpy.Hex.Commands {
	abstract class HexCommandOperations {
		public abstract HexView HexView { get; }
		public abstract void GoToPosition();
		public abstract void Select();
		public abstract void SaveSelection();
		public abstract void FillSelection();
		public abstract void EditLocalSettings();
		public abstract void ResetLocalSettings();
		public abstract void ToggleUseRelativePositions();
		public abstract void GoToOwnerMember();
	}

	sealed class HexCommandOperationsImpl : HexCommandOperations {
		public override HexView HexView { get; }
		readonly IMessageBoxService messageBoxService;
		readonly Lazy<HexEditorGroupFactoryService> hexEditorGroupFactoryService;
		readonly Lazy<HexStructureInfoAggregatorFactory> hexStructureInfoAggregatorFactory;
		readonly Lazy<HexReferenceHandlerService> hexReferenceHandlerService;

		HexStructureInfoAggregator HexStructureInfoAggregator => __hexStructureInfoAggregator ?? (__hexStructureInfoAggregator = hexStructureInfoAggregatorFactory.Value.Create(HexView));
		HexStructureInfoAggregator __hexStructureInfoAggregator;

		public HexCommandOperationsImpl(IMessageBoxService messageBoxService, Lazy<HexEditorGroupFactoryService> hexEditorGroupFactoryService, Lazy<HexStructureInfoAggregatorFactory> hexStructureInfoAggregatorFactory, Lazy<HexReferenceHandlerService> hexReferenceHandlerService, HexView hexView) {
			if (messageBoxService == null)
				throw new ArgumentNullException(nameof(messageBoxService));
			if (hexEditorGroupFactoryService == null)
				throw new ArgumentNullException(nameof(hexEditorGroupFactoryService));
			if (hexStructureInfoAggregatorFactory == null)
				throw new ArgumentNullException(nameof(hexStructureInfoAggregatorFactory));
			if (hexReferenceHandlerService == null)
				throw new ArgumentNullException(nameof(hexReferenceHandlerService));
			if (hexView == null)
				throw new ArgumentNullException(nameof(hexView));
			this.messageBoxService = messageBoxService;
			this.hexEditorGroupFactoryService = hexEditorGroupFactoryService;
			this.hexStructureInfoAggregatorFactory = hexStructureInfoAggregatorFactory;
			this.hexReferenceHandlerService = hexReferenceHandlerService;
			HexView = hexView;
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

		public override void GoToPosition() {
			var curr = HexView.BufferLines.ToLogicalPosition(HexView.Caret.Position.Position.ActivePosition.BufferPosition);
			var minPos = HexView.BufferLines.ToLogicalPosition(HexView.BufferLines.StartPosition);
			var maxPos = HexView.BufferLines.ToLogicalPosition(HexView.BufferLines.EndPosition);
			if (HexView.BufferLines.BufferSpan.IsEmpty) {
			}
			else if (maxPos == HexPosition.Zero)
				maxPos = HexPosition.MaxEndPosition - 1;
			else
				maxPos = maxPos - 1;
			var data = new GoToPositionVM(curr, minPos, maxPos);
			var win = new GoToPositionDlg();
			win.DataContext = data;
			win.Owner = OwnerWindow;
			if (HexView.Buffer.IsMemory) {
				win.Title = dnSpy_Resources.GoToOffset_Title_Address;
				win.offsetLabel.Content = dnSpy_Resources.GoToOffset_Address_Label;
			}
			else {
				win.Title = dnSpy_Resources.GoToOffset_Title;
				win.offsetLabel.Content = dnSpy_Resources.GoToOffset_Offset_Label;
			}
			if (win.ShowDialog() != true)
				return;

			var newPos = new HexBufferPoint(HexView.Buffer, HexView.BufferLines.ToPhysicalPosition(data.OffsetVM.Value));
			MoveTo(newPos, newPos, newPos);
		}

		bool MoveTo(HexBufferPoint start, HexBufferPoint end, HexBufferPoint caret) {
			if (!HexView.BufferLines.IsValidPosition(start) || !HexView.BufferLines.IsValidPosition(end) || !HexView.BufferLines.IsValidPosition(caret))
				return false;
			HexView.Caret.MoveTo(caret);
			var flags = HexView.Caret.Position.Position.ActiveColumn == HexColumnType.Values ? HexSpanSelectionFlags.Values : HexSpanSelectionFlags.Ascii;
			var span = start <= end ? new HexBufferSpan(start, end) : new HexBufferSpan(end, start);
			HexView.ViewScroller.EnsureSpanVisible(span, flags, VSTE.EnsureSpanVisibleOptions.ShowStart);
			HexView.Selection.Clear();
			return true;
		}

		struct SelectionInfo {
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
			var minPos = HexView.BufferLines.ToLogicalPosition(HexView.BufferLines.StartPosition);
			var maxPos = HexView.BufferLines.ToLogicalPosition(HexView.BufferLines.EndPosition > HexView.BufferLines.StartPosition ? HexView.BufferLines.EndPosition - 1 : HexView.BufferLines.EndPosition);
			var logStart = HexView.BufferLines.ToLogicalPosition(start);
			var logEnd = HexView.BufferLines.ToLogicalPosition(end);
			var data = new SelectVM(logStart, logEnd, minPos, maxPos);
			var win = new SelectDlg();
			win.DataContext = data;
			win.Owner = OwnerWindow;
			if (win.ShowDialog() != true)
				return;

			var newStart = HexView.BufferLines.ToPhysicalPosition(data.StartVM.Value);
			var newEnd = HexView.BufferLines.ToPhysicalPosition(data.EndVM.Value);
			var info = UserValueToSelection(newStart, newEnd);

			if (MoveTo(new HexBufferPoint(HexView.Buffer, info.Anchor), new HexBufferPoint(HexView.Buffer, info.Active), new HexBufferPoint(HexView.Buffer, info.Caret)))
				HexView.Selection.Select(new HexBufferPoint(HexView.Buffer, info.Anchor), new HexBufferPoint(HexView.Buffer, info.Active), alignPoints: false);
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
				string error;
				byte b = SimpleTypeConverter.ParseByte(s, byte.MinValue, byte.MaxValue, out error);
				return string.IsNullOrEmpty(error) ? b : (byte?)null;
			}, s => {
				string error;
				byte b = SimpleTypeConverter.ParseByte(s, byte.MinValue, byte.MaxValue, out error);
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

		public override void GoToOwnerMember() {
			var pos = HexView.Caret.Position.Position.ActivePosition.BufferPosition;
			foreach (var info in HexStructureInfoAggregator.GetReferences(pos)) {
				if (info.Value == null)
					continue;
				if (hexReferenceHandlerService.Value.Handle(HexView, info.Value))
					return;
			}
		}
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
