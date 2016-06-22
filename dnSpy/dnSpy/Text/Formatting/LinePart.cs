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

using System.Windows.Media.TextFormatting;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Formatting;

namespace dnSpy.Text.Formatting {
	struct LinePart {
		/// <summary>
		/// Column (visible character index). This is usually equal to <see cref="Span"/>'s <see cref="Contracts.Text.Span.Start"/>
		/// property unless there's one or more hidden characters before this <see cref="LinePart"/>.
		/// </summary>
		public readonly int Column;

		/// <summary>
		/// Span relative to the start of the physical line (<see cref="LinePartsCollection.Span"/>)
		/// </summary>
		public readonly Span Span;

		/// <summary>
		/// Adornment element or null
		/// </summary>
		public readonly IAdornmentElement AdornmentElement;

		/// <summary>
		/// Text run properties if it's normal text or null if an adornment element is used instead
		/// </summary>
		public readonly TextRunProperties TextRunProperties;

		public LinePart(int column, Span span, IAdornmentElement adornmentElement) {
			this.Column = column;
			this.Span = span;
			this.AdornmentElement = adornmentElement;
			this.TextRunProperties = null;
		}

		public LinePart(int column, Span span, TextRunProperties textRunProperties) {
			this.Column = column;
			this.Span = span;
			this.AdornmentElement = null;
			this.TextRunProperties = textRunProperties;
		}

		public override string ToString() {
			if (AdornmentElement != null)
				return $"{Span.ToString()} {AdornmentElement.ToString()}";
			return Span.ToString();
		}
	}
}
