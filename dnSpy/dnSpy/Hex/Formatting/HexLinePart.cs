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
using System.Windows.Media.TextFormatting;
using dnSpy.Contracts.Hex.Formatting;
using VST = Microsoft.VisualStudio.Text;

namespace dnSpy.Hex.Formatting {
	readonly struct HexLinePart {
		/// <summary>
		/// Column (visible character index). This is usually equal to <see cref="Span"/>'s <see cref="VST.Span.Start"/>
		/// property unless there's one or more hidden characters before this <see cref="HexLinePart"/> or if there's a <see cref="HexLinePart"/>
		/// with a non-null <see cref="AdornmentElement"/>.
		/// </summary>
		public readonly int Column;

		/// <summary>
		/// Length in column characters. This is never zero.
		/// </summary>
		public int ColumnLength => AdornmentElement is not null ? 1 : Span.Length;

		/// <summary>
		/// Span relative to the start of the physical line (<see cref="HexLinePartsCollection.Span"/>)
		/// </summary>
		public readonly VST.Span Span;

		/// <summary>
		/// Adornment element or null
		/// </summary>
		public readonly HexAdornmentElement? AdornmentElement;

		/// <summary>
		/// Text run properties if it's normal text or null if an adornment element is used instead
		/// </summary>
		public readonly TextRunProperties? TextRunProperties;

		/// <summary>
		/// Index of this instance in the collection
		/// </summary>
		public readonly int Index;

		public HexLinePart(int index, int column, VST.Span span, HexAdornmentElement adornmentElement, TextRunProperties textRunProperties) {
			Debug2.Assert(adornmentElement is not null);
			Debug2.Assert(textRunProperties is not null);
			Index = index;
			Column = column;
			Span = span;
			AdornmentElement = adornmentElement;
			TextRunProperties = textRunProperties;
		}

		public HexLinePart(int index, int column, VST.Span span, TextRunProperties textRunProperties) {
			Debug.Assert(!span.IsEmpty);
			Debug2.Assert(textRunProperties is not null);
			Index = index;
			Column = column;
			Span = span;
			AdornmentElement = null;
			TextRunProperties = textRunProperties;
		}

		public bool BelongsTo(int lineIndex) {
			if (AdornmentElement is null || Span.Length != 0)
				return Span.Start <= lineIndex && lineIndex < Span.End;

			switch (AdornmentElement.Affinity) {
			case VST.PositionAffinity.Predecessor:
				if (Span.Start == 0 && lineIndex == 0)
					return true;
				return Span.Start == lineIndex + 1;

			case VST.PositionAffinity.Successor:
				return Span.Start == lineIndex;

			default: throw new InvalidOperationException($"Invalid {nameof(VST.PositionAffinity)}: {AdornmentElement.Affinity}");
			}
		}

		public override string ToString() {
			if (AdornmentElement is not null)
				return $"{Span.ToString()} {AdornmentElement.ToString()}";
			return Span.ToString();
		}
	}
}
