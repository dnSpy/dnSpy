// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Globalization;

namespace ICSharpCode.AvalonEdit.Document
{
	/// <summary>
	/// A line/column position.
	/// Text editor lines/columns are counted started from one.
	/// </summary>
	/// <remarks>
	/// The document provides the methods <see cref="TextDocument.GetLocation"/> and
	/// <see cref="TextDocument.GetOffset(TextLocation)"/> to convert between offsets and TextLocations.
	/// </remarks>
	public struct TextLocation : IComparable<TextLocation>, IEquatable<TextLocation>
	{
		/// <summary>
		/// Represents no text location (0, 0).
		/// </summary>
		public static readonly TextLocation Empty = new TextLocation(0, 0);
		
		/// <summary>
		/// Creates a TextLocation instance.
		/// <para>
		/// Warning: the parameters are (line, column).
		/// Not (column, line) as in ICSharpCode.TextEditor!
		/// </para>
		/// </summary>
		public TextLocation(int line, int column)
		{
			y = line;
			x = column;
		}
		
		int x, y;
		
		/// <summary>
		/// Gets the line number.
		/// </summary>
		public int Line {
			get { return y; }
		}
		
		/// <summary>
		/// Gets the column number.
		/// </summary>
		public int Column {
			get { return x; }
		}
		
		/// <summary>
		/// Gets whether the TextLocation instance is empty.
		/// </summary>
		public bool IsEmpty {
			get {
				return x <= 0 && y <= 0;
			}
		}
		
		/// <summary>
		/// Gets a string representation for debugging purposes.
		/// </summary>
		public override string ToString()
		{
			return string.Format(CultureInfo.InvariantCulture, "(Line {1}, Col {0})", this.x, this.y);
		}
		
		/// <summary>
		/// Gets a hash code.
		/// </summary>
		public override int GetHashCode()
		{
			return unchecked (87 * x.GetHashCode() ^ y.GetHashCode());
		}
		
		/// <summary>
		/// Equality test.
		/// </summary>
		public override bool Equals(object obj)
		{
			if (!(obj is TextLocation)) return false;
			return (TextLocation)obj == this;
		}
		
		/// <summary>
		/// Equality test.
		/// </summary>
		public bool Equals(TextLocation other)
		{
			return this == other;
		}
		
		/// <summary>
		/// Equality test.
		/// </summary>
		public static bool operator ==(TextLocation left, TextLocation right)
		{
			return left.x == right.x && left.y == right.y;
		}
		
		/// <summary>
		/// Inequality test.
		/// </summary>
		public static bool operator !=(TextLocation left, TextLocation right)
		{
			return left.x != right.x || left.y != right.y;
		}
		
		/// <summary>
		/// Compares two text locations.
		/// </summary>
		public static bool operator <(TextLocation left, TextLocation right)
		{
			if (left.y < right.y)
				return true;
			else if (left.y == right.y)
				return left.x < right.x;
			else
				return false;
		}
		
		/// <summary>
		/// Compares two text locations.
		/// </summary>
		public static bool operator >(TextLocation left, TextLocation right)
		{
			if (left.y > right.y)
				return true;
			else if (left.y == right.y)
				return left.x > right.x;
			else
				return false;
		}
		
		/// <summary>
		/// Compares two text locations.
		/// </summary>
		public static bool operator <=(TextLocation left, TextLocation right)
		{
			return !(left > right);
		}
		
		/// <summary>
		/// Compares two text locations.
		/// </summary>
		public static bool operator >=(TextLocation left, TextLocation right)
		{
			return !(left < right);
		}
		
		/// <summary>
		/// Compares two text locations.
		/// </summary>
		public int CompareTo(TextLocation other)
		{
			if (this == other)
				return 0;
			if (this < other)
				return -1;
			else
				return 1;
		}
	}
}
