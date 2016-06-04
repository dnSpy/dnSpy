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
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Editing;

namespace dnSpy.Text.Editor {
	sealed class TextSelection : ITextSelection {
		public ITextView TextView { get; }
		public bool IsActive { get; set; }
		public bool IsEmpty => AnchorPoint == ActivePoint;
		public bool IsReversed => ActivePoint < AnchorPoint;
		public VirtualSnapshotPoint AnchorPoint => ToVirtualSnapshotPoint(dnSpyTextEditor.TextArea.Selection.StartPosition);
		public VirtualSnapshotPoint ActivePoint => ToVirtualSnapshotPoint(dnSpyTextEditor.TextArea.Selection.EndPosition);
		public VirtualSnapshotPoint Start => AnchorPoint < ActivePoint ? AnchorPoint : ActivePoint;
		public VirtualSnapshotPoint End => AnchorPoint < ActivePoint ? ActivePoint : AnchorPoint;
		public VirtualSnapshotSpan StreamSelectionSpan => new VirtualSnapshotSpan(Start, End);
		public event EventHandler SelectionChanged;

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

			foreach (var s in dnSpyTextEditor.TextArea.Selection.Segments)
				list.Add(new VirtualSnapshotSpan(new SnapshotSpan(TextView.TextSnapshot, s.StartOffset, s.Length)));

			// At least one span must be included, even if the span's empty
			if (list.Count == 0)
				list.Add(StreamSelectionSpan);

			return list;
		}

		public TextSelectionMode Mode {
			get { return dnSpyTextEditor.TextArea.Selection.GetType().ToString().Contains("RectangleSelection") ? TextSelectionMode.Box : TextSelectionMode.Stream; }
			set {
				switch (value) {
				case TextSelectionMode.Stream:
				case TextSelectionMode.Box:
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(value));
				}
			}
		}

		readonly DnSpyTextEditor dnSpyTextEditor;

		public TextSelection(ITextView textView, DnSpyTextEditor dnSpyTextEditor) {
			this.dnSpyTextEditor = dnSpyTextEditor;
			dnSpyTextEditor.TextArea.SelectionChanged += AvalonEdit_TextArea_SelectionChanged;
			TextView = textView;
			Mode = TextSelectionMode.Stream;
			ActivationTracksFocus = true;
			dnSpyTextEditor.TextArea.Selection = Selection.Create(dnSpyTextEditor.TextArea, 0, 0);
			TextView.Options.OptionChanged += TextView_Options_OptionChanged;
		}

		void AvalonEdit_TextArea_SelectionChanged(object sender, EventArgs e) {
			ActivationTracksFocus = true;
			SelectionChanged?.Invoke(this, EventArgs.Empty);
		}

		void TextView_Options_OptionChanged(object sender, EditorOptionChangedEventArgs e) {
			if (e.OptionId == DefaultTextViewOptions.UseVirtualSpaceId.Name) {
				if (Mode == TextSelectionMode.Stream && !TextView.Options.GetOptionValue(DefaultTextViewOptions.UseVirtualSpaceId))
					Select(new VirtualSnapshotPoint(AnchorPoint.Position), new VirtualSnapshotPoint(ActivePoint.Position));
			}
		}

		VirtualSnapshotPoint ToVirtualSnapshotPoint(TextViewPosition textViewPosition) {
			if (textViewPosition.Line == 0 && textViewPosition.Column == 0)
				return new VirtualSnapshotPoint(TextView.TextSnapshot, 0);
			int offset = dnSpyTextEditor.TextArea.TextView.Document.GetOffset(textViewPosition.Location);
			return new VirtualSnapshotPoint(TextView.TextSnapshot, offset);
		}

		public void Clear() {
			Mode = TextSelectionMode.Stream;
			ClearInternal();
		}

		void ClearInternal() {
			bool isEmpty = IsEmpty;
			ActivationTracksFocus = true;
			if (!isEmpty)
				dnSpyTextEditor.TextArea.Selection = Selection.Create(dnSpyTextEditor.TextArea, ActivePoint.Position, ActivePoint.Position);
		}

		public VirtualSnapshotSpan? GetSelectionOnTextViewLine(ITextViewLine line) {
			//TODO:
			return null;
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
			dnSpyTextEditor.TextArea.Selection = Selection.Create(dnSpyTextEditor.TextArea, anchorPoint.Position, activePoint.Position);
		}

		public void Select(int startLine, int startColumn, int endLine, int endColumn) {
			var l1 = dnSpyTextEditor.TextArea.TextView.Document.GetLineByNumber(startLine + 1);
			var l2 = dnSpyTextEditor.TextArea.TextView.Document.GetLineByNumber(endLine + 1);
			var vsp1 = new VirtualSnapshotPoint(new SnapshotPoint(TextView.TextSnapshot, l1.Offset + startColumn));
			var vsp2 = new VirtualSnapshotPoint(new SnapshotPoint(TextView.TextSnapshot, l2.Offset + endColumn));
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
	}
}
