//
// FormatItem.cs
//
// Author:
//       Simon Lindgren <simon.n.lindgren@gmail.com>
//
// Copyright (c) 2012 Simon Lindgren
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

namespace ICSharpCode.NRefactory.Utils
{
	public class FormatItem : FormatStringSegmentBase
	{
		public FormatItem (int index, int? alignment = null, string formatString = null)
		{
			Index = index;
			Alignment = alignment;
			FormatString = formatString;
		}

		public int Index { get; private set; }

		public int? Alignment { get; private set; }

		public string FormatString { get; private set; }

		#region Equality
		public override bool Equals (object obj)
		{
			if (obj == null)
				return false;
			if (obj.GetType () != typeof(FormatItem))
				return false;
			var other = (FormatItem)obj;
				
			return FieldsEquals (other);
		}
			
		public bool Equals (FormatItem other)
		{
			if (other == null)
				return false;
				
			return FieldsEquals (other);
		}

		bool FieldsEquals (FormatItem other)
		{
			return Index == other.Index &&
				Alignment == other.Alignment &&
				FormatString == other.FormatString &&
				StartLocation == other.StartLocation &&
				EndLocation == other.EndLocation;
		}
			
		public override int GetHashCode ()
		{
			unchecked {
				int hash = 23;
				hash = hash * 37 + Index.GetHashCode ();
				hash = hash * 37 + Alignment.GetHashCode ();
				hash = hash * 37 + FormatString.GetHashCode ();
				hash = hash * 37 + StartLocation.GetHashCode ();
				hash = hash * 37 + EndLocation.GetHashCode ();
				return hash;
			}
		}
		#endregion

		public override string ToString ()
		{
			return string.Format ("[FormatItem: Index={0}, Alignment={1}, FormatString={2}, StartLocation={3}, EndLocation={4}]", Index, Alignment, FormatString, StartLocation, EndLocation);
		}
	}

}
