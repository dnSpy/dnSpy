/*
    Copyright (C) 2014-2017 de4dot@gmail.com

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
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using dnSpy.Contracts.Text.Editor.OptionsExtensionMethods;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.OptionsExtensionMethods;
using Microsoft.VisualStudio.Text.Formatting;

namespace dnSpy.Text.Editor {
	sealed class TextSelection : ITextSelection {
		ITextView ITextSelection.TextView => TextView;
		public IWpfTextView TextView { get; }
		public bool IsEmpty => AnchorPoint == ActivePoint;
		public bool IsReversed => ActivePoint < AnchorPoint;
		public VirtualSnapshotPoint AnchorPoint => anchorPoint;
		public VirtualSnapshotPoint ActivePoint => activePoint;
		public VirtualSnapshotPoint Start => AnchorPoint < ActivePoint ? AnchorPoint : ActivePoint;
		public VirtualSnapshotPoint End => AnchorPoint < ActivePoint ? ActivePoint : AnchorPoint;
		public VirtualSnapshotSpan StreamSelectionSpan => new VirtualSnapshotSpan(Start, End);
		public event EventHandler SelectionChanged;

		public bool IsActive {
			get { return textSelectionLayer.IsActive; }
			set { textSelectionLayer.IsActive = value; }
		}

		public bool ActivationTracksFocus {
			get { return activationTracksFocus; }
			set {
				if (activationTracksFocus == value)
					return;
				activationTracksFocus = value;
				if (value)
					IsActive = TextView.HasAggregateFocus;
			}
		}
		bool activationTracksFocus;

		public NormalizedSnapshotSpanCollection SelectedSpans => new NormalizedSnapshotSpanCollection(GetSelectedSpans().Select(a => a.SnapshotSpan));
		public ReadOnlyCollection<VirtualSnapshotSpan> VirtualSelectedSpans => new ReadOnlyCollection<VirtualSnapshotSpan>(GetSelectedSpans());

		List<VirtualSnapshotSpan> GetSelectedSpans() {
			var list = new List<VirtualSnapshotSpan>();

			if (Mode == TextSelectionMode.Stream)
				list.Add(StreamSelectionSpan);
			else {
				var helper = new BoxSelectionHelper(this);
				var start = Start;
				while (start <= End) {
					var line = TextView.GetTextViewLineContainingBufferPosition(start.Position);
					list.Add(helper.GetSpan(line));
					if (line.IsLastDocumentLine())
						break;
					start = new VirtualSnapshotPoint(line.GetPointAfterLineBreak());
				}
			}

			// At least one span must be included, even if the span's empty
			if (list.Count == 0)
				list.Add(StreamSelectionSpan);

			return list;
		}

		public TextSelectionMode Mode {
			get { return mode; }
			set {
				if (mode == value)
					return;
				if (mode != TextSelectionMode.Stream && mode != TextSelectionMode.Box)
					throw new ArgumentOutOfRangeException(nameof(mode));
				if (!TextView.Options.IsAllowBoxSelectionEnabled())
					return;
				mode = value;
				textSelectionLayer.OnModeUpdated();
			}
		}
		TextSelectionMode mode;

		readonly TextSelectionLayer textSelectionLayer;
		VirtualSnapshotPoint anchorPoint, activePoint;

		public TextSelection(IWpfTextView textView, IAdornmentLayer selectionLayer, IEditorFormatMap editorFormatMap) {
			if (selectionLayer == null)
				throw new ArgumentNullException(nameof(selectionLayer));
			if (editorFormatMap == null)
				throw new ArgumentNullException(nameof(editorFormatMap));
			TextView = textView ?? throw new ArgumentNullException(nameof(textView));
			Mode = TextSelectionMode.Stream;
			activePoint = anchorPoint = new VirtualSnapshotPoint(TextView.TextSnapshot, 0);
			TextView.TextBuffer.ChangedHighPriority += TextBuffer_ChangedHighPriority;
			TextView.Options.OptionChanged += Options_OptionChanged;
			TextView.GotAggregateFocus += TextView_GotAggregateFocus;
			TextView.LostAggregateFocus += TextView_LostAggregateFocus;
			textSelectionLayer = new TextSelectionLayer(this, selectionLayer, editorFormatMap);
			ActivationTracksFocus = true;
		}

		void TextBuffer_ChangedHighPriority(object sender, TextContentChangedEventArgs e) {
			var newAnchorPoint = anchorPoint.TranslateTo(TextView.TextSnapshot);
			var newActivePoint = activePoint.TranslateTo(TextView.TextSnapshot);
			Select(newAnchorPoint, newActivePoint);
		}

		void Options_OptionChanged(object sender, EditorOptionChangedEventArgs e) {
			if (e.OptionId == DefaultTextViewOptions.UseVirtualSpaceName) {
				if (Mode == TextSelectionMode.Stream && !TextView.Options.IsVirtualSpaceEnabled())
					Select(new VirtualSnapshotPoint(AnchorPoint.Position), new VirtualSnapshotPoint(ActivePoint.Position));
			}
		}

		void TextView_GotAggregateFocus(object sender, EventArgs e) {
			if (ActivationTracksFocus)
				IsActive = true;
		}

		void TextView_LostAggregateFocus(object sender, EventArgs e) {
			if (ActivationTracksFocus)
				IsActive = false;
		}

		public void Clear() {
			Mode = TextSelectionMode.Stream;
			ClearInternal();
		}

		void ClearInternal() {
			bool isEmpty = IsEmpty;
			ActivationTracksFocus = true;
			activePoint = activePoint.TranslateTo(TextView.TextSnapshot);
			anchorPoint = activePoint;
			if (!isEmpty)
				SelectionChanged?.Invoke(this, EventArgs.Empty);
		}

		public VirtualSnapshotSpan? GetSelectionOnTextViewLine(ITextViewLine line) {
			if (line == null)
				throw new ArgumentNullException(nameof(line));
			if (line.Snapshot != TextView.TextSnapshot)
				throw new ArgumentException();
			if (IsEmpty) {
				if (line.ContainsBufferPosition(ActivePoint.Position))
					return new VirtualSnapshotSpan(ActivePoint, ActivePoint);
				return null;
			}
			if (Mode == TextSelectionMode.Stream) {
				var spanTmp = line.ExtentIncludingLineBreak.Intersection(StreamSelectionSpan.SnapshotSpan);
				if (spanTmp == null)
					return null;
				var span = spanTmp.Value;
				if (End > new VirtualSnapshotPoint(line.End))
					span = new SnapshotSpan(span.Start, line.EndIncludingLineBreak);
				if (span.Length == 0)
					return null;
				return new VirtualSnapshotSpan(span);
			}
			else {
				Debug.Assert(Mode == TextSelectionMode.Box);
				if (!line.IntersectsBufferSpan(StreamSelectionSpan.SnapshotSpan))
					return null;
				var helper = new BoxSelectionHelper(this);
				return helper.GetSpan(line);
			}
		}

		public void Select(SnapshotSpan selectionSpan, bool isReversed) {
			if (isReversed)
				Select(new VirtualSnapshotPoint(selectionSpan.End), new VirtualSnapshotPoint(selectionSpan.Start));
			else
				Select(new VirtualSnapshotPoint(selectionSpan.Start), new VirtualSnapshotPoint(selectionSpan.End));
		}

		public void Select(VirtualSnapshotPoint anchorPoint, VirtualSnapshotPoint activePoint) {
			if (anchorPoint.Position.Snapshot != activePoint.Position.Snapshot)
				throw new ArgumentException();
			if (anchorPoint.Position.Snapshot != TextView.TextSnapshot)
				throw new ArgumentException();
			if (anchorPoint == activePoint) {
				ClearInternal();
				return;
			}
			ActivationTracksFocus = true;

			this.anchorPoint = this.anchorPoint.TranslateTo(TextView.TextSnapshot);
			this.activePoint = this.activePoint.TranslateTo(TextView.TextSnapshot);

			bool sameSelection = SamePoint(this.anchorPoint, anchorPoint) && SamePoint(this.activePoint, activePoint);
			if (!sameSelection) {
				this.anchorPoint = anchorPoint;
				this.activePoint = activePoint;
				SelectionChanged?.Invoke(this, EventArgs.Empty);
			}
		}

		static bool SamePoint(VirtualSnapshotPoint a, VirtualSnapshotPoint b) =>
			a.VirtualSpaces == b.VirtualSpaces && a.Position.Position == b.Position.Position;

		internal void Dispose() {
			TextView.TextBuffer.ChangedHighPriority -= TextBuffer_ChangedHighPriority;
			TextView.Options.OptionChanged -= Options_OptionChanged;
			TextView.GotAggregateFocus -= TextView_GotAggregateFocus;
			TextView.LostAggregateFocus -= TextView_LostAggregateFocus;
			textSelectionLayer.Dispose();
		}
	}
}
