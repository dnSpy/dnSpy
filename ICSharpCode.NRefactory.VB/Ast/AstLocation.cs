//
// DomLocation.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;

namespace ICSharpCode.NRefactory.VB
{
	[Serializable]
	public struct AstLocation : IComparable<AstLocation>, IEquatable<AstLocation>
	{
		public static readonly AstLocation Empty = new AstLocation(0, 0);
		
		readonly int line, column;
		
		public AstLocation (int line, int column)
		{
			this.line   = line;
			this.column = column;
		}
		
		public bool IsEmpty {
			get {
				return line <= 0;
			}
		}
		
		public int Line {
			get { return line; }
		}
		
		public int Column {
			get { return column; }
		}
		
		public override bool Equals (object obj)
		{
			if (!(obj is AstLocation))
				return false;
			return (AstLocation)obj == this;
		}
		
		public override int GetHashCode ()
		{
			unchecked {
				return line + column * 5000;
			}
		}
		
		public bool Equals (AstLocation other)
		{
			return other == this;
		}
		
		public int CompareTo (AstLocation other)
		{
			if (this == other)
				return 0;
			if (this < other)
				return -1;
			return 1;
		}
		
		public override string ToString ()
		{
			return String.Format ("(Line {0}, Column {1})", Line, Column);
		}

		public static AstLocation FromInvariantString (string invariantString)
		{
			if (string.Equals(invariantString, "EMPTY", StringComparison.OrdinalIgnoreCase))
				return AstLocation.Empty;
			string[] splits = invariantString.Split (',', '/');
			if (splits.Length == 2)
				return new AstLocation (Int32.Parse (splits[0]), Int32.Parse (splits[1]));
			return AstLocation.Empty;
		}
		
		public string ToInvariantString ()
		{
			if (IsEmpty)
				return "Empty";
			return String.Format ("{0}/{1}", line, column);
		}
		
		public static bool operator==(AstLocation left, AstLocation right)
		{
			return left.line == right.line && left.column == right.column;
		}
		
		public static bool operator!=(AstLocation left, AstLocation right)
		{
			return left.line != right.line || left.column != right.column;
		}
		
		public static bool operator<(AstLocation left, AstLocation right)
		{
			return left.line < right.line || left.line == right.line && left.column < right.column;
		}
		public static bool operator>(AstLocation left, AstLocation right)
		{
			return left.line > right.line || left.line == right.line && left.column > right.column;
		}
		public static bool operator<=(AstLocation left, AstLocation right)
		{
			return !(left > right);
		}
		public static bool operator>=(AstLocation left, AstLocation right)
		{
			return !(left < right);
		}
	}
}
