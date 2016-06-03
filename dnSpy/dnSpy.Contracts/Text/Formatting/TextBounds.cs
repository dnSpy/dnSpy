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

namespace dnSpy.Contracts.Text.Formatting {
	/// <summary>
	/// Text bounds
	/// </summary>
	public struct TextBounds : IEquatable<TextBounds> {
		/// <summary>
		/// Gets the position of the top edge of the rectangle in the text rendering coordinate system.
		/// </summary>
		public double Top { get; }

		/// <summary>
		/// Gets the position of the bottom edge of the rectangle in the text rendering coordinate system
		/// </summary>
		public double Bottom => Top + Height;

		/// <summary>
		/// Gets the position of the left edge of the rectangle in the text rendering coordinate system.
		/// </summary>
		public double Left => bidiWidth >= 0 ? Leading : Trailing;

		/// <summary>
		/// Gets the position of the right edge of the rectangle in the text rendering coordinate system.
		/// </summary>
		public double Right => bidiWidth < 0 ? Leading : Trailing;

		/// <summary>
		/// Gets the distance between the leading and trailing edges of the rectangle in the text rendering coordinate system.
		/// </summary>
		public double Width => Math.Abs(bidiWidth);

		/// <summary>
		/// Gets the distance between the top and bottom edges of the rectangle in the text rendering coordinate system.
		/// </summary>
		public double Height { get; }

		/// <summary>
		/// Gets the top of the text on the line containing the text.
		/// </summary>
		public double TextTop { get; }

		/// <summary>
		/// Gets the bottom of the text on the line containing the characters.
		/// </summary>
		public double TextBottom => TextTop + TextHeight;

		/// <summary>
		/// Gets the height of the text on the line containing the characters.
		/// </summary>
		public double TextHeight { get; }

		/// <summary>
		/// Gets the position of the leading edge of the rectangle in the text rendering coordinate system.
		/// </summary>
		public double Leading { get; }

		/// <summary>
		/// Gets the position of the trailing edge of the rectangle in the text rendering coordinate system.
		/// </summary>
		public double Trailing => Leading + bidiWidth;

		/// <summary>
		/// Determines whether the character is a right-to-left character.
		/// </summary>
		public bool IsRightToLeft => bidiWidth < 0;

		readonly double bidiWidth;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="leading">The x-coordinate of the leading edge of the bounding rectangle</param>
		/// <param name="top">The y-coordinate of the top edge of the bounding rectangle</param>
		/// <param name="bidiWidth">The distance between the leading and trailing edges of the bounding rectangle. This can be negative for right-to-left text</param>
		/// <param name="height">The height of the rectangle. The height must be non-negative</param>
		/// <param name="textTop">The top of the text, measured from the line that contains the text</param>
		/// <param name="textHeight">The height of the text, measured from the line that contains the text</param>
		public TextBounds(double leading, double top, double bidiWidth, double height, double textTop, double textHeight) {
			if (height < 0)
				throw new ArgumentOutOfRangeException(nameof(height));
			if (textHeight < 0)
				throw new ArgumentOutOfRangeException(nameof(textHeight));
			Leading = leading;
			Top = top;
			this.bidiWidth = bidiWidth;
			Height = height;
			TextTop = textTop;
			TextHeight = textHeight;
		}

		/// <summary>
		/// operator ==()
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		public static bool operator ==(TextBounds left, TextBounds right) => left.Equals(right);

		/// <summary>
		/// operator !=()
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		public static bool operator !=(TextBounds left, TextBounds right) => !left.Equals(right);

		/// <summary>
		/// Equals()
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public bool Equals(TextBounds other) =>
			Leading == other.Leading &&
			Top == other.Top &&
			bidiWidth == other.bidiWidth &&
			Height == other.Height &&
			TextTop == other.TextTop &&
			TextHeight == other.TextHeight;

		/// <summary>
		/// Equals()
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public override bool Equals(object obj) => obj is TextBounds && Equals((TextBounds)obj);

		/// <summary>
		/// GetHashCode()
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode() =>
			Leading.GetHashCode() ^
			Top.GetHashCode() ^
			bidiWidth.GetHashCode() ^
			Height.GetHashCode() ^
			TextTop.GetHashCode() ^
			TextHeight.GetHashCode();

		/// <summary>
		/// ToString()
		/// </summary>
		/// <returns></returns>
		public override string ToString() => $"[{Leading},{Top},{Trailing},{Bottom}]";
	}
}
