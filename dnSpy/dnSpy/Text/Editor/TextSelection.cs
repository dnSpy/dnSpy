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
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Editor;
using dnSpy.Contracts.Text.Formatting;

namespace dnSpy.Text.Editor {
	sealed class TextSelection : ITextSelection {
		public ITextView TextView { get; }
		public bool IsActive { get; set; }
		public bool IsEmpty => AnchorPoint == ActivePoint;
		public bool IsReversed => ActivePoint < AnchorPoint;
		public VirtualSnapshotPoint AnchorPoint => anchorPoint;
		public VirtualSnapshotPoint ActivePoint => activePoint;
		public VirtualSnapshotPoint Start => AnchorPoint < ActivePoint ? AnchorPoint : ActivePoint;
		public VirtualSnapshotPoint End => AnchorPoint < ActivePoint ? ActivePoint : AnchorPoint;
		public VirtualSnapshotSpan StreamSelectionSpan => new VirtualSnapshotSpan(Start, End);
		public event EventHandler SelectionChanged;

		VirtualSnapshotPoint anchorPoint, activePoint;

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
				//TODO:
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
				mode = value;
				textSelectionLayer.OnModeUpdated();
			}
		}
		TextSelectionMode mode;

		readonly TextSelectionLayer textSelectionLayer;

		public TextSelection(ITextView textView, IAdornmentLayer selectionLayer) {
			if (textView == null)
				throw new ArgumentNullException(nameof(textView));
			if (selectionLayer == null)
				throw new ArgumentNullException(nameof(selectionLayer));
			this.textSelectionLayer = new TextSelectionLayer(this, selectionLayer);
			TextView = textView;
			TextView.TextBuffer.ChangedHighPriority += TextBuffer_ChangedHighPriority;
			Mode = TextSelectionMode.Stream;
			ActivationTracksFocus = true;
			TextView.Options.OptionChanged += TextView_Options_OptionChanged;
			anchorPoint = new VirtualSnapshotPoint(TextView.TextSnapshot, 0);
			activePoint = new VirtualSnapshotPoint(TextView.TextSnapshot, 0);
		}

		void TextBuffer_ChangedHighPriority(object sender, TextContentChangedEventArgs e) {
			var newAnchorPoint = anchorPoint.TranslateTo(TextView.TextSnapshot);
			var newActivePoint = activePoint.TranslateTo(TextView.TextSnapshot);
			Select(newAnchorPoint, newActivePoint);
		}

		void TextView_Options_OptionChanged(object sender, EditorOptionChangedEventArgs e) {
			if (e.OptionId == DefaultTextViewOptions.UseVirtualSpaceId.Name) {
				if (Mode == TextSelectionMode.Stream && !TextView.Options.GetOptionValue(DefaultTextViewOptions.UseVirtualSpaceId))
					Select(new VirtualSnapshotPoint(AnchorPoint.Position), new VirtualSnapshotPoint(ActivePoint.Position));
			}
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
			throw new NotImplementedException();//TODO:
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

		public void Select(int startLine, int startColumn, int endLine, int endColumn) {
			var l1 = TextView.TextSnapshot.GetLineFromLineNumber(startLine);
			var l2 = TextView.TextSnapshot.GetLineFromLineNumber(endLine);
			var vsp1 = new VirtualSnapshotPoint(new SnapshotPoint(TextView.TextSnapshot, l1.Start + startColumn));
			var vsp2 = new VirtualSnapshotPoint(new SnapshotPoint(TextView.TextSnapshot, l2.Start + endColumn));
			Select(vsp1, vsp2);
		}

		public string GetText() {
			if (Mode == TextSelectionMode.Stream)
				return StreamSelectionSpan.GetText();
			var sb = new StringBuilder();
			var snapshot = TextView.TextSnapshot;
			int i = 0;
			foreach (var s in SelectedSpans) {
				if (i++ > 0)
					sb.AppendLine();
				sb.Append(snapshot.GetText(s));
			}
			if (i > 1)
				sb.AppendLine();
			return sb.ToString();
		}

		public void Dispose() {
			TextView.TextBuffer.ChangedHighPriority -= TextBuffer_ChangedHighPriority;
			TextView.Options.OptionChanged -= TextView_Options_OptionChanged;
			textSelectionLayer.Dispose();
		}
	}
}
