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
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Windows.Threading;
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.Hex.Editor;
using dnSpy.Contracts.Hex.Editor.OptionsExtensionMethods;
using dnSpy.Contracts.Hex.Tagging;
using CTC = dnSpy.Contracts.Text.Classification;
using VST = Microsoft.VisualStudio.Text;
using VSTE = Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Hex.Editor {
	abstract class CurrentValueHighlighterProvider {
		public abstract CurrentValueHighlighter Get(WpfHexView wpfHexView);
	}

	[Export(typeof(CurrentValueHighlighterProvider))]
	sealed class CurrentValueHighlighterProviderImpl : CurrentValueHighlighterProvider {
		public override CurrentValueHighlighter Get(WpfHexView wpfHexView) {
			if (wpfHexView is null)
				throw new ArgumentNullException(nameof(wpfHexView));
			return wpfHexView.Properties.GetOrCreateSingletonProperty(typeof(CurrentValueHighlighter), () => new CurrentValueHighlighter(wpfHexView));
		}
	}

	[Export(typeof(HexViewTaggerProvider))]
	[HexTagType(typeof(HexMarkerTag))]
	sealed class CurrentValueHighlighterHexViewTaggerProvider : HexViewTaggerProvider {
		readonly CurrentValueHighlighterProvider currentValueHighlighterProvider;

		[ImportingConstructor]
		CurrentValueHighlighterHexViewTaggerProvider(CurrentValueHighlighterProvider currentValueHighlighterProvider) => this.currentValueHighlighterProvider = currentValueHighlighterProvider;

		public override IHexTagger<T>? CreateTagger<T>(HexView hexView, HexBuffer buffer) {
			var wpfHexView = hexView as WpfHexView;
			Debug2.Assert(!(wpfHexView is null));
			if (!(wpfHexView is null)) {
				return wpfHexView.Properties.GetOrCreateSingletonProperty(typeof(CurrentValueHighlighterTagger), () =>
					new CurrentValueHighlighterTagger(currentValueHighlighterProvider.Get(wpfHexView))) as IHexTagger<T>;
			}
			return null;
		}
	}

	sealed class CurrentValueHighlighterTagger : HexTagger<HexMarkerTag> {
		public override event EventHandler<HexBufferSpanEventArgs>? TagsChanged;
		readonly CurrentValueHighlighter currentValueHighlighter;

		public CurrentValueHighlighterTagger(CurrentValueHighlighter currentValueHighlighter) {
			this.currentValueHighlighter = currentValueHighlighter ?? throw new ArgumentNullException(nameof(currentValueHighlighter));
			currentValueHighlighter.Register(this);
		}

		public override IEnumerable<IHexTextTagSpan<HexMarkerTag>> GetTags(HexTaggerContext context) =>
			currentValueHighlighter.GetTags(context);
		public override IEnumerable<IHexTagSpan<HexMarkerTag>> GetTags(NormalizedHexBufferSpanCollection spans) =>
			currentValueHighlighter.GetTags(spans);
		internal void RaiseTagsChanged(HexBufferSpan hexBufferSpan) => TagsChanged?.Invoke(this, new HexBufferSpanEventArgs(hexBufferSpan));
	}

	sealed class CurrentValueHighlighter {
		readonly WpfHexView wpfHexView;
		bool enabled;

		public CurrentValueHighlighter(WpfHexView wpfHexView) {
			this.wpfHexView = wpfHexView ?? throw new ArgumentNullException(nameof(wpfHexView));
			wpfHexView.Closed += WpfHexView_Closed;
			wpfHexView.Selection.SelectionChanged += Selection_SelectionChanged;
			wpfHexView.Options.OptionChanged += Options_OptionChanged;
			UpdateEnabled();
		}

		void Selection_SelectionChanged(object? sender, EventArgs e) => UpdateEnabled();

		void Options_OptionChanged(object? sender, VSTE.EditorOptionChangedEventArgs e) {
			if (e.OptionId == DefaultHexViewOptions.HighlightCurrentValueName)
				UpdateEnabled();
		}

		void UpdateEnabled() {
			var newEnabled = wpfHexView.Options.HighlightCurrentValue() && wpfHexView.Selection.IsEmpty;
			if (newEnabled == enabled)
				return;
			enabled = newEnabled;
			if (enabled) {
				HookEvents();
				ReinitializeCurrentValue();
			}
			else {
				UnhookEvents();
				UninitializeCurrentValue();
				StopTimer();
			}
			RefreshAll();
		}

		void HookEvents() {
			wpfHexView.Caret.PositionChanged += Caret_PositionChanged;
			wpfHexView.BufferLinesChanged += WpfHexView_BufferLinesChanged;
			wpfHexView.Buffer.ChangedLowPriority += Buffer_ChangedLowPriority;
			wpfHexView.Buffer.BufferSpanInvalidated += Buffer_BufferSpanInvalidated;
		}

		void UnhookEvents() {
			wpfHexView.Caret.PositionChanged -= Caret_PositionChanged;
			wpfHexView.BufferLinesChanged -= WpfHexView_BufferLinesChanged;
			wpfHexView.Buffer.ChangedLowPriority -= Buffer_ChangedLowPriority;
			wpfHexView.Buffer.BufferSpanInvalidated -= Buffer_BufferSpanInvalidated;
		}

		void WpfHexView_BufferLinesChanged(object? sender, BufferLinesChangedEventArgs e) =>
			wpfHexView.VisualElement.Dispatcher.BeginInvoke(DispatcherPriority.Send, new Action(ReinitializeCurrentValue));

		void RefreshAll() => currentValueHighlighterTagger?.RaiseTagsChanged(new HexBufferSpan(new HexBufferPoint(wpfHexView.Buffer, 0), new HexBufferPoint(wpfHexView.Buffer, HexPosition.MaxEndPosition)));

		void Caret_PositionChanged(object? sender, HexCaretPositionChangedEventArgs e) => DelayUpdateCurrentValue();

		void ReinitializeCurrentValue() {
			if (wpfHexView.IsClosed)
				return;
			if (!enabled)
				return;
			savedValue = null;
			UpdateCurrentValue();
		}
		SavedValue? savedValue;

		sealed class SavedValue {
			public byte[] Data { get; }
			byte[] TempData { get; }
			public HexBufferSpan BufferSpan { get; private set; }
			public HexColumnType Column { get; }
			public HexValuesDisplayFormat ValuesFormat { get; }

			public SavedValue(HexValuesDisplayFormat valuesFormat, int size, HexCellPosition cellPosition, HexBufferSpan cellBufferSpan) {
				ValuesFormat = valuesFormat;
				Data = new byte[size];
				TempData = new byte[size];
				BufferSpan = cellBufferSpan;
				Column = cellPosition.Column;

				// Note that BufferSpan.Length could be less than Data.Length if cell
				// byte size > 1 and there's not enough bytes at the end of the buffer
				// for the full cell.
				Debug.Assert(BufferSpan.Length <= Data.Length);

				BufferSpan.Buffer.ReadBytes(BufferSpan.Start, Data);
			}

			public bool TryUpdate(HexCellPosition cellPosition, HexBufferLine line, HexCell? cell) {
				if (cell is null)
					return false;
				var oldBufferSpan = BufferSpan;
				Debug.Assert(cell.BufferSpan.Length <= Data.Length);
				BufferSpan = cell.BufferSpan;

				bool dataDifferent;
				if (oldBufferSpan != BufferSpan) {
					BufferSpan.Buffer.ReadBytes(BufferSpan.Start, TempData);
					dataDifferent = !CompareArrays(TempData, Data);
					if (dataDifferent)
						Array.Copy(TempData, Data, Data.Length);
				}
				else
					dataDifferent = false;

				return dataDifferent;
			}

			static bool CompareArrays(byte[] a, byte[] b) {
				if (a.Length != b.Length)
					return false;
				for (int i = 0; i < a.Length; i++) {
					if (a[i] != b[i])
						return false;
				}
				return true;
			}

			public bool UpdateValue() {
				Debug.Assert(!BufferSpan.IsDefault);
				Array.Copy(Data, TempData, Data.Length);
				BufferSpan.Buffer.ReadBytes(BufferSpan.Start, Data);
				return CompareArrays(Data, TempData);
			}

			public bool HasSameValueAs(HexBufferLine line, HexCell cell) {
				var index = (long)(cell.BufferStart - line.BufferStart).ToUInt64();
				if (line.HexBytes.AllValid == true) {
					// Nothing to do
				}
				else if (line.HexBytes.AllValid == false) {
					// Never mark non-existent data
					return false;
				}
				else {
					for (long i = 0; i < cell.BufferSpan.Length; i++) {
						if (!line.HexBytes.IsValid(index + i))
							return false;
					}
				}

				line.HexBytes.ReadBytes(index, TempData, 0, TempData.Length);
				return CompareArrays(TempData, Data);
			}
		}

		void UninitializeCurrentValue() => savedValue = null;

		// PERF: delay refreshing the changed value to speed up scrolling up/down
		// by eg. holding down PgDown/PgUp.
		void DelayUpdateCurrentValue() {
			int delayMs = wpfHexView.Options.GetHighlightCurrentValueDelayMilliSeconds();
			if (delayMs <= 0) {
				StopTimer();
				UpdateCurrentValue();
				return;
			}

			if (delayDispatcherTimer is null) {
				if (!(savedValue is null) && !TryUpdateCurrentValue())
					return;
				savedValue = null;
				// Make sure old highlighting gets cleared immediately
				RefreshAll();
			}
			// Always stop and restart the timer so nothing is highlighted if eg. PgDown is held down
			StopTimer();
			delayDispatcherTimer = new DispatcherTimer(DispatcherPriority.Normal, wpfHexView.VisualElement.Dispatcher);
			if (delayMs < 20 || delayMs > 2000)
				delayMs = DefaultHexViewOptions.DefaultHighlightCurrentValueDelayMilliSeconds;
			delayDispatcherTimer.Interval = TimeSpan.FromMilliseconds(delayMs);
			delayDispatcherTimer.Tick += Timer_Tick;
			delayDispatcherTimer.Start();
		}
		DispatcherTimer? delayDispatcherTimer;

		void StopTimer() {
			var timer = delayDispatcherTimer;
			delayDispatcherTimer = null;
			if (!(timer is null)) {
				timer.Stop();
				timer.Tick -= Timer_Tick;
			}
		}

		void Timer_Tick(object? sender, EventArgs e) {
			var timer = (DispatcherTimer)sender!;
			timer.Tick -= Timer_Tick;
			timer.Stop();
			if (delayDispatcherTimer != timer)
				return;
			delayDispatcherTimer = null;

			UpdateCurrentValue();
		}

		void UpdateCurrentValue() {
			if (TryUpdateCurrentValue())
				RefreshAll();
		}

		bool TryUpdateCurrentValue() {
			if (wpfHexView.IsClosed)
				return false;
			if (!enabled)
				return false;

			var bufferLines = wpfHexView.BufferLines;
			var pos = wpfHexView.Caret.Position.Position;
			bool isValues = pos.ActiveColumn == HexColumnType.Values;
			var bufferPos = bufferLines.FilterAndVerify(pos.ActivePosition.BufferPosition);
			var line = wpfHexView.Caret.ContainingHexViewLine.BufferLine;
			var cell = isValues ? line.ValueCells.GetCell(bufferPos) : line.AsciiCells.GetCell(bufferPos);

			if (savedValue is null || savedValue.Column != pos.ActiveColumn || savedValue.ValuesFormat != bufferLines.ValuesFormat)
				savedValue = new SavedValue(bufferLines.ValuesFormat, isValues ? bufferLines.BytesPerValue : 1, pos.ActivePosition, cell?.BufferSpan ?? new HexBufferSpan(bufferLines.BufferSpan.Start, bufferLines.BufferSpan.Start));
			else if (!savedValue.TryUpdate(pos.ActivePosition, line, cell))
				return false;
			return true;
		}

		void Buffer_BufferSpanInvalidated(object? sender, HexBufferSpanInvalidatedEventArgs e) {
			if (!(savedValue is null) && savedValue.BufferSpan.Span.OverlapsWith(e.Span)) {
				if (!savedValue.UpdateValue())
					RefreshAll();
			}
		}

		void Buffer_ChangedLowPriority(object? sender, HexContentChangedEventArgs e) {
			if (!(savedValue is null)) {
				foreach (var change in e.Changes) {
					if (savedValue.BufferSpan.Span.OverlapsWith(change.OldSpan)) {
						if (!savedValue.UpdateValue())
							RefreshAll();
						break;
					}
				}
			}
		}

		internal IEnumerable<IHexTextTagSpan<HexMarkerTag>> GetTags(HexTaggerContext context) {
			if (wpfHexView.IsClosed)
				yield break;
			if (!enabled)
				yield break;
			if (savedValue is null)
				yield break;
			var cells = (savedValue.Column == HexColumnType.Values ? context.Line.ValueCells : context.Line.AsciiCells).GetVisibleCells();
			var markerTag = savedValue.Column == HexColumnType.Values ? valueCellMarkerTag : asciiCellMarkerTag;

			// PERF: Select more than one cell if there are multiple consecutive cells with the same value.
			// Improves perf when selecting a common value, eg. 00.
			HexCell? startCell = null;
			HexCell? lastCell = null;
			foreach (var cell in cells) {
				if (!savedValue.HasSameValueAs(context.Line, cell))
					continue;
				if (startCell is null)
					startCell = lastCell = cell;
				else if (lastCell!.Index + 1 != cell.Index) {
					yield return new HexTextTagSpan<HexMarkerTag>(VST.Span.FromBounds(startCell.CellSpan.Start, lastCell.CellSpan.End), markerTag);
					startCell = lastCell = cell;
				}
				else
					lastCell = cell;
			}
			if (!(startCell is null))
				yield return new HexTextTagSpan<HexMarkerTag>(VST.Span.FromBounds(startCell.CellSpan.Start, lastCell!.CellSpan.End), markerTag);
		}
		static readonly HexMarkerTag valueCellMarkerTag = new HexMarkerTag(CTC.ThemeClassificationTypeNameKeys.HexCurrentValueCell);
		static readonly HexMarkerTag asciiCellMarkerTag = new HexMarkerTag(CTC.ThemeClassificationTypeNameKeys.HexCurrentAsciiCell);

		internal IEnumerable<IHexTagSpan<HexMarkerTag>> GetTags(NormalizedHexBufferSpanCollection spans) {
			yield break;
		}

		internal void Register(CurrentValueHighlighterTagger currentValueHighlighterTagger) {
			if (!(this.currentValueHighlighterTagger is null))
				throw new InvalidOperationException();
			this.currentValueHighlighterTagger = currentValueHighlighterTagger ?? throw new ArgumentNullException(nameof(currentValueHighlighterTagger));
		}
		CurrentValueHighlighterTagger? currentValueHighlighterTagger;

		void WpfHexView_Closed(object? sender, EventArgs e) {
			wpfHexView.Closed -= WpfHexView_Closed;
			wpfHexView.Selection.SelectionChanged -= Selection_SelectionChanged;
			wpfHexView.Options.OptionChanged -= Options_OptionChanged;
			UninitializeCurrentValue();
			UnhookEvents();
			StopTimer();
		}
	}
}
