// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT license (for details please see \doc\license.txt)

using System;
using System.Globalization;

namespace ICSharpCode.Editor
{
	/// <summary>
	/// A line/column position.
	/// Text editor lines/columns are counted started from one.
	/// </summary>
	/// <remarks>
	/// The document provides the methods <see cref="IDocument.GetLocation"/> and
	/// <see cref="IDocument.GetOffset(TextLocation)"/> to convert between offsets and TextLocations.
	/// </remarks>
	[Serializable]
	public struct TextLocation : IComparable<TextLocation>, IEquatable<TextLocation>
	{
		/// <summary>
		/// Represents no text location (0, 0).
		/// </summary>
		public static readonly TextLocation Empty = new TextLocation(0, 0);
		
		/// <summary>
		/// Constant of the minimum line.
		/// </summary>
		public const int MinLine   = 1;
		
		/// <summary>
		/// Constant of the minimum column.
		/// </summary>
		public const int MinColumn = 1;
		
		/// <summary>
		/// Creates a TextLocation instance.
		/// </summary>
		public TextLocation(int line, int column)
		{
			this.line = line;
			this.column = column;
		}
		
		int column, line;
		
		/// <summary>
		/// Gets the line number.
		/// </summary>
		public int Line {
			get { return line; }
		}
		
		/// <summary>
		/// Gets the column number.
		/// </summary>
		public int Column {
			get { return column; }
		}
		
		/// <summary>
		/// Gets whether the TextLocation instance is empty.
		/// </summary>
		public bool IsEmpty {
			get {
				return column < MinLine && line < MinColumn;
			}
		}
		
		/// <summary>
		/// Gets a string representation for debugging purposes.
		/// </summary>
		public override string ToString()
		{
			return string.Format(CultureInfo.InvariantCulture, "(Line {1}, Col {0})", this.column, this.line);
		}
		
		/// <summary>
		/// Gets a hash code.
		/// </summary>
		public override int GetHashCode()
		{
			return unchecked (191 * column.GetHashCode() ^ line.GetHashCode());
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
			return left.column == right.column && left.line == right.line;
		}
		
		/// <summary>
		/// Inequality test.
		/// </summary>
		public static bool operator !=(TextLocation left, TextLocation right)
		{
			return left.column != right.column || left.line != right.line;
		}
		
		/// <summary>
		/// Compares two text locations.
		/// </summary>
		public static bool operator <(TextLocation left, TextLocation right)
		{
			if (left.line < right.line)
				return true;
			else if (left.line == right.line)
				return left.column < right.column;
			else
				return false;
		}
		
		/// <summary>
		/// Compares two text locations.
		/// </summary>
		public static bool operator >(TextLocation left, TextLocation right)
		{
			if (left.line > right.line)
				return true;
			else if (left.line == right.line)
				return left.column > right.column;
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
