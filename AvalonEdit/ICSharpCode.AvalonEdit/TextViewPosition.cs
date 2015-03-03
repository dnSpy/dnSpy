// Copyright (c) 2014 AlphaSierraPapa for the SharpDevelop Team
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
using System.Globalization;
using ICSharpCode.NRefactory;
using ICSharpCode.AvalonEdit.Document;

namespace ICSharpCode.AvalonEdit
{
	/// <summary>
	/// Represents a text location with a visual column.
	/// </summary>
	public struct TextViewPosition : IEquatable<TextViewPosition>, IComparable<TextViewPosition>
	{
		int line, column, visualColumn;
		bool isAtEndOfLine;
		
		/// <summary>
		/// Gets/Sets Location.
		/// </summary>
		public TextLocation Location {
			get {
				return new TextLocation(line, column);
			}
			set {
				line = value.Line;
				column = value.Column;
			}
		}
		
		/// <summary>
		/// Gets/Sets the line number.
		/// </summary>
		public int Line {
			get { return line; }
			set { line = value; }
		}
		
		/// <summary>
		/// Gets/Sets the (text) column number.
		/// </summary>
		public int Column {
			get { return column; }
			set { column = value; }
		}
		
		/// <summary>
		/// Gets/Sets the visual column number.
		/// Can be -1 (meaning unknown visual column).
		/// </summary>
		public int VisualColumn {
			get { return visualColumn; }
			set { visualColumn = value; }
		}
		
		/// <summary>
		/// When word-wrap is enabled and a line is wrapped at a position where there is no space character;
		/// then both the end of the first TextLine and the beginning of the second TextLine
		/// refer to the same position in the document, and also have the same visual column.
		/// In this case, the IsAtEndOfLine property is used to distinguish between the two cases:
		/// the value <c>true</c> indicates that the position refers to the end of the previous TextLine;
		/// the value <c>false</c> indicates that the position refers to the beginning of the next TextLine.
		/// 
		/// If this position is not at such a wrapping position, the value of this property has no effect.
		/// </summary>
		public bool IsAtEndOfLine {
			get { return isAtEndOfLine; }
			set { isAtEndOfLine = value; }
		}
		
		/// <summary>
		/// Creates a new TextViewPosition instance.
		/// </summary>
		public TextViewPosition(int line, int column, int visualColumn)
		{
			this.line = line;
			this.column = column;
			this.visualColumn = visualColumn;
			this.isAtEndOfLine = false;
		}
		
		/// <summary>
		/// Creates a new TextViewPosition instance.
		/// </summary>
		public TextViewPosition(int line, int column)
			: this(line, column, -1)
		{
		}
		
		/// <summary>
		/// Creates a new TextViewPosition instance.
		/// </summary>
		public TextViewPosition(TextLocation location, int visualColumn)
		{
			this.line = location.Line;
			this.column = location.Column;
			this.visualColumn = visualColumn;
			this.isAtEndOfLine = false;
		}
		
		/// <summary>
		/// Creates a new TextViewPosition instance.
		/// </summary>
		public TextViewPosition(TextLocation location)
			: this(location, -1)
		{
		}
		
		/// <inheritdoc/>
		public override string ToString()
		{
			return string.Format(CultureInfo.InvariantCulture,
			                     "[TextViewPosition Line={0} Column={1} VisualColumn={2} IsAtEndOfLine={3}]",
			                     this.line, this.column, this.visualColumn, this.isAtEndOfLine);
		}
		
		#region Equals and GetHashCode implementation
		// The code in this region is useful if you want to use this structure in collections.
		// If you don't need it, you can just remove the region and the ": IEquatable<Struct1>" declaration.
		
		/// <inheritdoc/>
		public override bool Equals(object obj)
		{
			if (obj is TextViewPosition)
				return Equals((TextViewPosition)obj); // use Equals method below
			else
				return false;
		}
		
		/// <inheritdoc/>
		public override int GetHashCode()
		{
			int hashCode = isAtEndOfLine ? 115817 : 0;
			unchecked {
				hashCode += 1000000007 * Line.GetHashCode();
				hashCode += 1000000009 * Column.GetHashCode();
				hashCode += 1000000021 * VisualColumn.GetHashCode();
			}
			return hashCode;
		}
		
		/// <summary>
		/// Equality test.
		/// </summary>
		public bool Equals(TextViewPosition other)
		{
			return this.Line == other.Line && this.Column == other.Column && this.VisualColumn == other.VisualColumn && this.IsAtEndOfLine == other.IsAtEndOfLine;
		}
		
		/// <summary>
		/// Equality test.
		/// </summary>
		public static bool operator ==(TextViewPosition left, TextViewPosition right)
		{
			return left.Equals(right);
		}
		
		/// <summary>
		/// Inequality test.
		/// </summary>
		public static bool operator !=(TextViewPosition left, TextViewPosition right)
		{
			return !(left.Equals(right)); // use operator == and negate result
		}
		#endregion
		
		/// <inheritdoc/>
		public int CompareTo(TextViewPosition other)
		{
			int r = this.Location.CompareTo(other.Location);
			if (r != 0)
				return r;
			r = this.visualColumn.CompareTo(other.visualColumn);
			if (r != 0)
				return r;
			if (isAtEndOfLine && !other.isAtEndOfLine)
				return -1;
			else if (!isAtEndOfLine && other.isAtEndOfLine)
				return 1;
			return 0;
		}
	}
}
