// Copyright (c) 2010-2013 AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using System.ComponentModel;
using System.Globalization;

namespace ICSharpCode.NRefactory
{
	/// <summary>
	/// A line/column position.
	/// Text editor lines/columns are counted started from one.
	/// </summary>
	/// <remarks>
	/// The document provides the methods <see cref="Editor.IDocument.GetLocation"/> and
	/// <see cref="Editor.IDocument.GetOffset(TextLocation)"/> to convert between offsets and TextLocations.
	/// </remarks>
	[Serializable]
	[TypeConverter(typeof(TextLocationConverter))]
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
	
	public class TextLocationConverter : TypeConverter
	{
		public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
		{
			return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
		}
		
		public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
		{
			return destinationType == typeof(TextLocation) || base.CanConvertTo(context, destinationType);
		}
		
		public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
		{
			if (value is string) {
				string[] parts = ((string)value).Split(';', ',');
				if (parts.Length == 2) {
					return new TextLocation(int.Parse(parts[0]), int.Parse(parts[1]));
				}
			}
			return base.ConvertFrom(context, culture, value);
		}
		
		public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
		{
			if (value is TextLocation) {
				var loc = (TextLocation)value;
				return loc.Line + ";" + loc.Column;
			}
			return base.ConvertTo(context, culture, value, destinationType);
		}
	}
}
