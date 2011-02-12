// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using Location = ICSharpCode.NRefactory.Location;

namespace ICSharpCode.SharpDevelop.Dom
{
	[Serializable]
	public struct DomRegion : IEquatable<DomRegion>
	{
		readonly int beginLine;
		readonly int endLine;
		readonly int beginColumn;
		readonly int endColumn;
		
		public readonly static DomRegion Empty = new DomRegion(-1, -1);
		
		public bool IsEmpty {
			get {
				return BeginLine <= 0;
			}
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
		
		public static DomRegion FromLocation(Location start, Location end)
		{
			return new DomRegion(start.Y, start.X, end.Y, end.X);
		}
		
		public DomRegion(int beginLine, int beginColumn, int endLine, int endColumn)
		{
			this.beginLine   = beginLine;
			this.beginColumn = beginColumn;
			this.endLine     = endLine;
			this.endColumn   = endColumn;
		}
		
		public DomRegion(int beginLine, int beginColumn)
		{
			this.beginLine   = beginLine;
			this.beginColumn = beginColumn;
			this.endLine = -1;
			this.endColumn = -1;
		}
		
		/// <remarks>
		/// Returns true, if the given coordinates (row, column) are in the region.
		/// This method assumes that for an unknown end the end line is == -1
		/// </remarks>
		public bool IsInside(int row, int column)
		{
			if (IsEmpty)
				return false;
			return row >= BeginLine &&
				(row <= EndLine   || EndLine == -1) &&
				(row != BeginLine || column >= BeginColumn) &&
				(row != EndLine   || column <= EndColumn);
		}

		public override string ToString()
		{
			return String.Format("[Region: BeginLine = {0}, EndLine = {1}, BeginColumn = {2}, EndColumn = {3}]",
			                     beginLine,
			                     endLine,
			                     beginColumn,
			                     endColumn);
		}
		
		public override bool Equals(object obj)
		{
			return obj is DomRegion && Equals((DomRegion)obj);
		}
		
		public override int GetHashCode()
		{
			unchecked {
				return BeginColumn + 1100009 * BeginLine + 1200007 * BeginColumn + 1300021 * EndColumn;
			}
		}
		
		public bool Equals(DomRegion other)
		{
			return BeginLine == other.BeginLine && BeginColumn == other.BeginColumn
				&& EndLine == other.EndLine && EndColumn == other.EndColumn;
		}
		
		public static bool operator ==(DomRegion lhs, DomRegion rhs)
		{
			return lhs.Equals(rhs);
		}
		
		public static bool operator !=(DomRegion lhs, DomRegion rhs)
		{
			return !lhs.Equals(rhs);
		}
	}
}
