// Copyright (c) 2010 AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Globalization;
using ICSharpCode.NRefactory.CSharp;

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
		
		public readonly static DomRegion Empty = new DomRegion(null, -1, -1);
		
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
		
		public AstLocation Begin {
			get {
				return new AstLocation (beginLine, beginColumn);
			}
		}
		
		public AstLocation End {
			get {
				return new AstLocation (endLine, endColumn);
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
		
		public DomRegion (AstLocation begin, AstLocation end) : this (null, begin, end)
		{
		}
		
		public DomRegion (string fileName, AstLocation begin, AstLocation end)
		{
			this.fileName = fileName;
			this.beginLine = begin.Line;
			this.beginColumn = begin.Column;
			this.endLine = end.Line;
			this.endColumn = end.Column;
		}
		
		public DomRegion (AstLocation begin) : this (null, begin)
		{
		}
		
		public DomRegion (string fileName, AstLocation begin)
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
		
		public bool IsInside(AstLocation location)
		{
			return IsInside(location.Line, location.Column);
		}
		
		public override string ToString()
		{
			return string.Format(
				CultureInfo.InvariantCulture,
				"[DomRegion FileName={0}, BeginLine={1}, EndLine={2}, BeginColumn={3}, EndColumn={4}]",
				fileName, beginLine, endLine, beginColumn, endColumn);
		}
		
		public override bool Equals(object obj)
		{
			return obj is DomRegion && Equals((DomRegion)obj);
		}
		
		public override int GetHashCode()
		{
			unchecked {
				int hashCode = fileName != null ? fileName.GetHashCode() : 0;
				hashCode ^= BeginColumn + 1100009 * BeginLine + 1200007 * BeginColumn + 1300021 * EndColumn;
				return hashCode;
			}
		}
		
		public bool Equals(DomRegion other)
		{
			return BeginLine == other.BeginLine && BeginColumn == other.BeginColumn
				&& EndLine == other.EndLine && EndColumn == other.EndColumn
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
