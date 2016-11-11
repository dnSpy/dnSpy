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

using Microsoft.VisualStudio.Text;

namespace dnSpy.Contracts.Hex.Classification {
	/// <summary>
	/// Hex classification context
	/// </summary>
	public abstract class HexClassificationContext {
		/// <summary>
		/// Constructor
		/// </summary>
		protected HexClassificationContext() { }

		/// <summary>
		/// Line span, some of the bytes could be hidden
		/// </summary>
		public abstract HexBufferSpan LineSpan { get; }

		/// <summary>
		/// The visible bytes shown in the UI
		/// </summary>
		public abstract HexBufferSpan VisibleBytesSpan { get; }

		/// <summary>
		/// All raw visible bytes
		/// </summary>
		public abstract HexBytes VisibleHexBytes { get; }

		/// <summary>
		/// Text shown in the UI
		/// </summary>
		public abstract string Text { get; }

		/// <summary>
		/// Gets the span in <see cref="Text"/> of the offset. This can be an empty span if
		/// the offset isn't shown.
		/// </summary>
		/// <returns></returns>
		public abstract Span GetOffsetSpan();

		/// <summary>
		/// Gets the span of a value in <see cref="Text"/>
		/// </summary>
		/// <param name="position">Position</param>
		/// <returns></returns>
		public abstract TextAndHexSpan GetBytesSpan(HexPosition position);

		/// <summary>
		/// Gets the span of values in <see cref="Text"/>
		/// </summary>
		/// <param name="span">Span</param>
		/// <returns></returns>
		public abstract TextAndHexSpan GetBytesSpan(HexSpan span);

		/// <summary>
		/// Gets the span of an ASCII character in <see cref="Text"/>. This can be an empty span
		/// if the ASCII isn't shown.
		/// </summary>
		/// <param name="position">Position</param>
		/// <returns></returns>
		public abstract Span GetAsciiSpan(HexPosition position);

		/// <summary>
		/// Gets the span of ASCII characters in <see cref="Text"/>. This can be an empty span
		/// if the ASCII isn't shown.
		/// </summary>
		/// <param name="span">Span</param>
		/// <returns></returns>
		public abstract Span GetAsciiSpan(HexSpan span);
	}
}
