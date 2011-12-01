// Copyright (c) AlphaSierraPapa for the SharpDevelop Team
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

namespace ICSharpCode.NRefactory.TypeSystem
{
	[Serializable]
	public struct DomRegion : IEquatable<DomRegion>
	{
		readonly string fileName;
		readonly int beginLine;
		readonly int endLine;
		readonly int beginColumn;
		readonly int endColumn;
		
		public readonly static DomRegion Empty = new DomRegion();
		
		public bool IsEmpty {
			get {
				return BeginLine <= 0;
			}
		}
		
		public string FileName {
			get { return fileName; }
		}
		
		public int BeginLine {
			get {
				return beginLine;
			}
		}
		
		/// <value>
		/// if the end line is == -1 the end column is -1 too
		/// this stands for an unknwon end
		/// </value>
		public int EndLine {
			get {
				return endLine;
			}
		}
		
		public int BeginColumn {
			get {
				return beginColumn;
			}
		}
		
		/// <value>
		/// if the end column is == -1 the end line is -1 too
		/// this stands for an unknown end
		/// </value>
		public int EndColumn {
			get {
				return endColumn;
			}
		}
		
		public TextLocation Begin {
			get {
				return new TextLocation (beginLine, beginColumn);
			}
		}
		
		public TextLocation End {
			get {
				return new TextLocation (endLine, endColumn);
			}
		}
		
		public DomRegion (int beginLine, int beginColumn, int endLine, int endColumn) : this (null, beginLine, beginColumn, endLine, endColumn)
		{
		}

		public DomRegion(string fileName, int beginLine, int beginColumn, int endLine, int endColumn)
		{
			this.fileName = fileName;
			this.beginLine   = beginLine;
			this.beginColumn = beginColumn;
			this.endLine     = endLine;
			this.endColumn   = endColumn;
		}
		
		public DomRegion (int beginLine, int beginColumn) : this (null, beginLine, beginColumn)
		{
		}
		
		public DomRegion (string fileName, int beginLine, int beginColumn)
		{
			this.fileName = fileName;
			this.beginLine = beginLine;
			this.beginColumn = beginColumn;
			this.endLine = -1;
			this.endColumn = -1;
		}
		
		public DomRegion (TextLocation begin, TextLocation end) : this (null, begin, end)
		{
		}
		
		public DomRegion (string fileName, TextLocation begin, TextLocation end)
		{
			this.fileName = fileName;
			this.beginLine = begin.Line;
			this.beginColumn = begin.Column;
			this.endLine = end.Line;
			this.endColumn = end.Column;
		}
		
		public DomRegion (TextLocation begin) : this (null, begin)
		{
		}
		
		public DomRegion (string fileName, TextLocation begin)
		{
			this.fileName = fileName;
			this.beginLine = begin.Line;
			this.beginColumn = begin.Column;
			this.endLine = -1;
			this.endColumn = -1;
		}
		
		/// <remarks>
		/// Returns true, if the given coordinates (line, column) are in the region.
		/// This method assumes that for an unknown end the end line is == -1
		/// </remarks>
		public bool IsInside(int line, int column)
		{
			if (IsEmpty)
				return false;
			return line >= BeginLine &&
				(line <= EndLine   || EndLine == -1) &&
				(line != BeginLine || column >= BeginColumn) &&
				(line != EndLine   || column <= EndColumn);
		}
		
		public bool IsInside(TextLocation location)
		{
			return IsInside(location.Line, location.Column);
		}
		
		public override string ToString()
		{
			return string.Format(
				CultureInfo.InvariantCulture,
				"[DomRegion FileName={0}, Begin=({1}, {2}), End=({3}, {4})]",
				fileName, beginLine, beginColumn, endLine, endColumn);
		}
		
		public override bool Equals(object obj)
		{
			return obj is DomRegion && Equals((DomRegion)obj);
		}
		
		public override int GetHashCode()
		{
			unchecked {
				int hashCode = fileName != null ? fileName.GetHashCode() : 0;
				hashCode ^= beginColumn + 1100009 * beginLine + 1200007 * endLine + 1300021 * endColumn;
				return hashCode;
			}
		}
		
		public bool Equals(DomRegion other)
		{
			return beginLine == other.beginLine && beginColumn == other.beginColumn
				&& endLine == other.endLine && endColumn == other.endColumn
				&& fileName == other.fileName;
		}
		
		public static bool operator ==(DomRegion left, DomRegion right)
		{
			return left.Equals(right);
		}
		
		public static bool operator !=(DomRegion left, DomRegion right)
		{
			return !left.Equals(right);
		}
	}
}
