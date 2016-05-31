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
using dnSpy.Contracts.Text.Formatting;

namespace dnSpy.Contracts.Text.Editor {
	/// <summary>
	/// Text view layout changed event args
	/// </summary>
	public sealed class TextViewLayoutChangedEventArgs : EventArgs {
		/// <summary>
		/// true if it was translated horizontally since last layout
		/// </summary>
		public bool HorizontalTranslation => OldViewState.ViewportLeft != NewViewState.ViewportLeft;

		/// <summary>
		/// true if it was translated vertically since last layout
		/// </summary>
		public bool VerticalTranslation => OldViewState.ViewportTop != NewViewState.ViewportTop;

		/// <summary>
		/// New or reformatted lines
		/// </summary>
		public ReadOnlyCollection<ITextViewLine> NewOrReformattedLines { get; }

		/// <summary>
		/// New edit snapshot
		/// </summary>
		public ITextSnapshot NewSnapshot => NewViewState.EditSnapshot;

		/// <summary>
		/// New view state
		/// </summary>
		public ViewState NewViewState { get; }

		/// <summary>
		/// Old edit snapshot
		/// </summary>
		public ITextSnapshot OldSnapshot => OldViewState.EditSnapshot;

		/// <summary>
		/// Old view state
		/// </summary>
		public ViewState OldViewState { get; }

		/// <summary>
		/// Translated lines
		/// </summary>
		public ReadOnlyCollection<ITextViewLine> TranslatedLines { get; }

		/// <summary>
		/// A normalized collection of new or reformatted spans
		/// </summary>
		public NormalizedSnapshotSpanCollection NewOrReformattedSpans => CreateSpanCollection(NewOrReformattedLines);

		/// <summary>
		/// A normalized collection of translated spans
		/// </summary>
		public NormalizedSnapshotSpanCollection TranslatedSpans => CreateSpanCollection(TranslatedLines);

		NormalizedSnapshotSpanCollection CreateSpanCollection(ReadOnlyCollection<ITextViewLine> lines) {
			if (lines.Count == 0)
				return NormalizedSnapshotSpanCollection.Empty;
			var spans = new List<Span>();
			foreach (ITextViewLine current in lines)
				spans.Add(current.ExtentIncludingLineBreak.Span);
			return new NormalizedSnapshotSpanCollection(lines[0].Snapshot, spans);
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="oldState">Old view state</param>
		/// <param name="newState">New view state</param>
		/// <param name="newOrReformattedLines">New or reformatted lines</param>
		/// <param name="translatedLines">Translated lines</param>
		public TextViewLayoutChangedEventArgs(ViewState oldState, ViewState newState, IList<ITextViewLine> newOrReformattedLines, IList<ITextViewLine> translatedLines) {
			if (oldState == null)
				throw new ArgumentNullException(nameof(oldState));
			if (newState == null)
				throw new ArgumentNullException(nameof(newState));
			if (newOrReformattedLines == null)
				throw new ArgumentNullException(nameof(newOrReformattedLines));
			if (translatedLines == null)
				throw new ArgumentNullException(nameof(translatedLines));
			OldViewState = oldState;
			NewViewState = newState;
			NewOrReformattedLines = new ReadOnlyCollection<ITextViewLine>(newOrReformattedLines);
			TranslatedLines = new ReadOnlyCollection<ITextViewLine>(translatedLines);
		}
	}
}
