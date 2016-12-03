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

namespace dnSpy.Text.AvalonEdit {
	/// <summary>
	/// Represents a simple segment (Offset,Length pair) that is not automatically updated
	/// on document changes.
	/// </summary>
	struct SimpleSegment : IEquatable<SimpleSegment> {
		public static readonly SimpleSegment Invalid = new SimpleSegment(-1, -1);
		public readonly int Offset, Length;

		public SimpleSegment(int offset, int length) {
			Offset = offset;
			Length = length;
		}

		public override int GetHashCode() {
			unchecked {
				return Offset + 10301 * Length;
			}
		}

		public override bool Equals(object obj) {
			return (obj is SimpleSegment) && Equals((SimpleSegment)obj);
		}

		public bool Equals(SimpleSegment other) {
			return Offset == other.Offset && Length == other.Length;
		}

		public static bool operator ==(SimpleSegment left, SimpleSegment right) {
			return left.Equals(right);
		}

		public static bool operator !=(SimpleSegment left, SimpleSegment right) {
			return !left.Equals(right);
		}

		/// <inheritdoc/>
		public override string ToString() {
			return "[Offset=" + Offset.ToString(CultureInfo.InvariantCulture) + ", Length=" + Length.ToString(CultureInfo.InvariantCulture) + "]";
		}
	}
}
