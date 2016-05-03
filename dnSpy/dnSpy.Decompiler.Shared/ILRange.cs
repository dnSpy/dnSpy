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
		public uint From { get; }
		public uint To { get; }

		public static bool operator ==(ILRange a, ILRange b) => a.Equals(b);
		public static bool operator !=(ILRange a, ILRange b) => !a.Equals(b);
		public bool IsDefault => From == 0 && To == 0;

		public ILRange(uint from, uint to) {
			this.From = from;
			this.To = to;
		}

		public bool Equals(ILRange other) => From == other.From && To == other.To;

		public override bool Equals(object obj) {
			if (!(obj is ILRange))
				return false;
			return Equals((ILRange)obj);
		}

		public override int GetHashCode() => (int)(((From << 16) | From >> 32) | To);
		public override string ToString() => string.Format("{0}-{1}", From.ToString("X"), To.ToString("X"));
		public static List<ILRange> OrderAndJoin(IEnumerable<ILRange> input) => OrderAndJoinList(input.ToList());

		public static List<ILRange> OrderAndJoinList(List<ILRange> ranges) {// Don't rename to OrderAndJoin() since some pass in a list that shouldn't be modified
			if (ranges.Count <= 1)
				return ranges;

			ranges.Sort(Sort);
			var result = new List<ILRange>();
			var curr = ranges[0];
			result.Add(curr);
			for (int i = 1; i < ranges.Count; i++) {
				var next = ranges[i];
				if (curr.To == next.From)
					result[result.Count - 1] = curr = new ILRange(curr.From, next.To);
				else if (next.From > curr.To) {
					result.Add(next);
					curr = next;
				}
				else if (next.To > curr.To)
					result[result.Count - 1] = curr = new ILRange(curr.From, next.To);
			}

			return result;
		}

		static int Sort(ILRange a, ILRange b) {
			int c = unchecked((int)a.From - (int)b.From);
			if (c != 0)
				return c;
			return unchecked((int)b.To - (int)a.To);
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
