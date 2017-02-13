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
using System.Diagnostics;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.OptionsExtensionMethods;
using Microsoft.VisualStudio.Text.Formatting;

namespace dnSpy.Text.Editor {
	sealed class ScrollMap : IScrollMap, IDisposable {
		public event EventHandler MappingChanged;
		public ITextView TextView { get; }
		public bool AreElisionsExpanded { get; }
		public double Start => 0;
		public double End { get; private set; }
		public double ThumbSize { get; private set; }
		bool IsWordWrap => (TextView.Options.WordWrapStyle() & WordWrapStyles.WordWrap) != 0;

		public ScrollMap(ITextView textView, bool areElisionsExpanded) {
			TextView = textView ?? throw new ArgumentNullException(nameof(textView));
			//TODO: Support AreElisionsExpanded == false (use visual snapshot instead of text snapshot)
			AreElisionsExpanded = areElisionsExpanded;
			TextView.Options.OptionChanged += Options_OptionChanged;
			TextView.TextBuffer.ChangedLowPriority += TextBuffer_ChangedLowPriority;
			TextView.LayoutChanged += TextView_LayoutChanged;
			TextView.Closed += TextView_Closed;
		}

		void TextView_LayoutChanged(object sender, TextViewLayoutChangedEventArgs e) {
			bool update = false;
			if (e.OldViewState.ViewportHeight != e.NewViewState.ViewportHeight)
				update = true;
			if (IsWordWrap && e.OldViewState.ViewportWidth != e.NewViewState.ViewportWidth)
				update = true;
			if (update)
				UpdateCachedState();
		}

		void TextView_Closed(object sender, EventArgs e) => Dispose();
		void TextBuffer_ChangedLowPriority(object sender, TextContentChangedEventArgs e) => UpdateCachedState();

		void Options_OptionChanged(object sender, EditorOptionChangedEventArgs e) {
			if (e.OptionId == DefaultTextViewOptions.WordWrapStyleName)
				UpdateCachedState();
		}

		void UpdateCachedState() {
			End = GetCoordinateAtBufferPosition(new SnapshotPoint(TextView.TextSnapshot, TextView.TextSnapshot.Length));
			ThumbSize = TextView.ViewportHeight / TextView.LineHeight;
			MappingChanged?.Invoke(this, EventArgs.Empty);
		}

		public SnapshotPoint GetBufferPositionAtCoordinate(double coordinate) {
			coordinate -= Start;
			if (coordinate < 0)
				coordinate = 0;
			int lineNumber = (int)Math.Min(int.MaxValue, coordinate);
			if (lineNumber >= TextView.TextSnapshot.LineCount)
				lineNumber = TextView.TextSnapshot.LineCount - 1;
			var line = TextView.TextSnapshot.GetLineFromLineNumber(lineNumber);
			if (!IsWordWrap)
				return line.Start;
			double fraction = coordinate - (int)coordinate;
			var viewLine = TextView.GetTextViewLineContainingBufferPosition(line.Start);
			int logicalLines = GetNumberOfLogicalLines(viewLine, out int logicalLineNumber);
			logicalLineNumber = (int)Math.Round(fraction * logicalLines, MidpointRounding.AwayFromZero);
			while (!viewLine.IsLastTextViewLineForSnapshotLine && logicalLineNumber-- > 0)
				viewLine = TextView.GetTextViewLineContainingBufferPosition(viewLine.GetPointAfterLineBreak());
			return viewLine.Start;
		}

		public double GetCoordinateAtBufferPosition(SnapshotPoint bufferPosition) {
			if (bufferPosition.Snapshot != TextView.TextSnapshot)
				throw new ArgumentException();
			ITextSnapshotLine line;
			if (!IsWordWrap) {
				line = bufferPosition.GetContainingLine();
				return Start + line.LineNumber;
			}
			else {
				var viewLine = TextView.GetTextViewLineContainingBufferPosition(bufferPosition);
				int logicalLines = GetNumberOfLogicalLines(viewLine, out int logicalLineNumber);
				double fraction = (double)logicalLineNumber / logicalLines;
				line = viewLine.Start.GetContainingLine();
				return Start + line.LineNumber + fraction;
			}
		}

		int GetNumberOfLogicalLines(ITextViewLine line, out int logicalLineNumber) {
			int count = 1;
			logicalLineNumber = 0;

			var l = line;
			while (!l.IsFirstTextViewLineForSnapshotLine) {
				l = TextView.GetTextViewLineContainingBufferPosition(l.Start - 1);
				count++;
				logicalLineNumber++;
			}

			l = line;
			while (!l.IsLastTextViewLineForSnapshotLine) {
				l = TextView.GetTextViewLineContainingBufferPosition(l.GetPointAfterLineBreak());
				count++;
			}

			return count;
		}

		public SnapshotPoint GetBufferPositionAtFraction(double fraction) {
			if (fraction < 0 || fraction > 1)
				throw new ArgumentOutOfRangeException(nameof(fraction));
			double length = End - Start;
			var coord = Start + fraction * length;
			return GetBufferPositionAtCoordinate(coord);
		}

		public double GetFractionAtBufferPosition(SnapshotPoint bufferPosition) {
			if (bufferPosition.Snapshot != TextView.TextSnapshot)
				throw new ArgumentException();
			double length = End - Start;
			if (length == 0)
				return 0;
			var coord = GetCoordinateAtBufferPosition(bufferPosition);
			Debug.Assert(Start <= coord && coord <= End);
			return Math.Min(Math.Max(0, (coord - Start) / length), 1);
		}

		public void Dispose() {
			TextView.Options.OptionChanged -= Options_OptionChanged;
			TextView.TextBuffer.ChangedLowPriority -= TextBuffer_ChangedLowPriority;
			TextView.LayoutChanged -= TextView_LayoutChanged;
			TextView.Closed -= TextView_Closed;
		}
	}
}
