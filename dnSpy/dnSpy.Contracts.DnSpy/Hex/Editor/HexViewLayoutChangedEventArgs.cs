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
using System.Collections.ObjectModel;
using dnSpy.Contracts.Hex.Formatting;

namespace dnSpy.Contracts.Hex.Editor {
	/// <summary>
	/// Hex view layout changed event args
	/// </summary>
	public sealed class HexViewLayoutChangedEventArgs : EventArgs {
		/// <summary>
		/// Gets the old view state
		/// </summary>
		public HexViewState OldViewState { get; }

		/// <summary>
		/// Gets the new view state
		/// </summary>
		public HexViewState NewViewState { get; }

		/// <summary>
		/// Gets all new or reformatted lines
		/// </summary>
		public ReadOnlyCollection<HexViewLine> NewOrReformattedLines { get; }

		/// <summary>
		/// Gets all translated lines
		/// </summary>
		public ReadOnlyCollection<HexViewLine> TranslatedLines { get; }

		/// <summary>
		/// true if the layout was translated vertically
		/// </summary>
		public bool VerticalTranslation => OldViewState.ViewportTop != NewViewState.ViewportTop;

		/// <summary>
		/// true if the layout was translated horizontally
		/// </summary>
		public bool HorizontalTranslation => OldViewState.ViewportLeft != NewViewState.ViewportLeft;

		/// <summary>
		/// Gets the old version
		/// </summary>
		public HexVersion OldVersion => OldViewState.Version;

		/// <summary>
		/// Gets the new version
		/// </summary>
		public HexVersion NewVersion => NewViewState.Version;

		/// <summary>
		/// Gets all new or reformatted spans
		/// </summary>
		public NormalizedHexBufferSpanCollection NewOrReformattedSpans => newOrReformattedSpans ?? (newOrReformattedSpans = CreateSpans(NewOrReformattedLines));
		NormalizedHexBufferSpanCollection newOrReformattedSpans;

		/// <summary>
		/// Gets all translated spans
		/// </summary>
		public NormalizedHexBufferSpanCollection TranslatedSpans => translatedSpans ?? (translatedSpans = CreateSpans(TranslatedLines));
		NormalizedHexBufferSpanCollection translatedSpans;

		static NormalizedHexBufferSpanCollection CreateSpans(ReadOnlyCollection<HexViewLine> lines) {
			if (lines.Count == 0)
				return NormalizedHexBufferSpanCollection.Empty;
			if (lines.Count == 1)
				return new NormalizedHexBufferSpanCollection(lines[0].BufferSpan);
			var array = new HexBufferSpan[lines.Count];
			for (int i = 0; i < array.Length; i++)
				array[i] = lines[i].BufferSpan;
			return new NormalizedHexBufferSpanCollection(array);
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="oldState">Old view state</param>
		/// <param name="newState">New view state</param>
		/// <param name="newOrReformattedLines">New or reformatted lines</param>
		/// <param name="translatedLines">Translated lines</param>
		public HexViewLayoutChangedEventArgs(HexViewState oldState, HexViewState newState, IList<HexViewLine> newOrReformattedLines, IList<HexViewLine> translatedLines) {
			if (newOrReformattedLines == null)
				throw new ArgumentNullException(nameof(newOrReformattedLines));
			if (translatedLines == null)
				throw new ArgumentNullException(nameof(translatedLines));
			OldViewState = oldState ?? throw new ArgumentNullException(nameof(oldState));
			NewViewState = newState ?? throw new ArgumentNullException(nameof(newState));
			NewOrReformattedLines = new ReadOnlyCollection<HexViewLine>(newOrReformattedLines);
			TranslatedLines = new ReadOnlyCollection<HexViewLine>(translatedLines);
		}
	}
}
