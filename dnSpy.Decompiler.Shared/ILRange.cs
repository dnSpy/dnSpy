// Copyright (c) 2011 AlphaSierraPapa for the SharpDevelop Team
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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace dnSpy.Decompiler.Shared {
	public struct ILRange : IEquatable<ILRange> {
		readonly uint from, to;
		public uint From {
			get { return from; }
		}
		public uint To {    // Exlusive
			get { return to; }
		}

		public static bool operator ==(ILRange a, ILRange b) {
			return a.Equals(b);
		}

		public static bool operator !=(ILRange a, ILRange b) {
			return !a.Equals(b);
		}

		public bool IsDefault {
			get { return from == 0 && to == 0; }
		}

		public ILRange(uint from, uint to) {
			this.from = from;
			this.to = to;
		}

		public bool Equals(ILRange other) {
			return from == other.from && to == other.to;
		}

		public override bool Equals(object obj) {
			if (!(obj is ILRange))
				return false;
			return Equals((ILRange)obj);
		}

		public override int GetHashCode() {
			return (int)(((from << 16) | from >> 32) | to);
		}

		public override string ToString() {
			return string.Format("{0}-{1}", from.ToString("X"), to.ToString("X"));
		}

		public static List<ILRange> OrderAndJoin(IEnumerable<ILRange> input) {
			if (input == null)
				throw new ArgumentNullException("Input is null!");

			List<ILRange> ranges = input.ToList();
			if (ranges.Count <= 1)
				return ranges;

			ranges.Sort(Sort);
			var result = new List<ILRange>();
			var curr = ranges[0];
			result.Add(curr);
			for (int i = 1; i < ranges.Count; i++) {
				var next = ranges[i];
				if (curr.to == next.from)
					result[result.Count - 1] = curr = new ILRange(curr.from, next.to);
				else if (next.from > curr.to) {
					result.Add(next);
					curr = next;
				}
				else if (next.to > curr.to)
					result[result.Count - 1] = curr = new ILRange(curr.from, next.to);
			}

			return result;
		}

		static int Sort(ILRange a, ILRange b) {
			int c = unchecked((int)a.from - (int)b.from);
			if (c != 0)
				return c;
			return unchecked((int)b.to - (int)a.to);
		}

		public static List<ILRange> Invert(IEnumerable<ILRange> input, int codeSize) {
			if (input == null)
				throw new ArgumentNullException("Input is null!");

			if (codeSize <= 0)
				throw new ArgumentException("Code size must be grater than 0");

			List<ILRange> ordered = OrderAndJoin(input);
			List<ILRange> result = new List<ILRange>(ordered.Count + 1);
			if (ordered.Count == 0) {
				result.Add(new ILRange(0, (uint)codeSize));
			}
			else {
				// Gap before the first element
				if (ordered.First().From != 0)
					result.Add(new ILRange(0, ordered.First().From));

				// Gaps between elements
				for (int i = 0; i < ordered.Count - 1; i++)
					result.Add(new ILRange(ordered[i].To, ordered[i + 1].From));

				// Gap after the last element
				Debug.Assert(ordered.Last().To <= codeSize);
				if (ordered.Last().To != codeSize)
					result.Add(new ILRange(ordered.Last().To, (uint)codeSize));
			}
			return result;
		}
	}
}
