//
// TextSegment.cs
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
	public class TextSegment : FormatStringSegmentBase
	{
		public TextSegment (string text, int startLocation = 0, int? endLocation = null)
		{
			Text = text;
			StartLocation = startLocation;
			EndLocation = endLocation ?? startLocation + text.Length;
		}

		public string Text { get; set; }

		#region Equality
		public override bool Equals (object obj)
		{
			if (obj == null)
				return false;
			if (obj.GetType () != typeof(TextSegment))
				return false;
			var other = (TextSegment)obj;
			
			return Equals (Text, other.Text);
		}
		
		public bool Equals (TextSegment other)
		{
			if (other == null)
				return false;
			
			return Equals (Text, other.Text) &&
				StartLocation == other.StartLocation &&
				EndLocation == other.EndLocation;
		}
		
		public override int GetHashCode ()
		{
			unchecked {
				int hash = 23;
				hash = hash * 37 + Text.GetHashCode ();
				hash = hash * 37 + StartLocation.GetHashCode ();
				hash = hash * 37 + EndLocation.GetHashCode ();
				return hash;
			}
		}
		#endregion

		public override string ToString ()
		{
			return string.Format ("[TextSegment: Text={0}, StartLocation={1}, EndLocation={2}]", Text, StartLocation, EndLocation);
		}
	}
}
