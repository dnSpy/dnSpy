/*
    Copyright (C) 2014-2016 de4dot@gmail.com

    This file is part of dnSpy

    dnSpy is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    dnSpy is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with dnSpy.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Text;
using Microsoft.VisualStudio.Text;

namespace dnSpy.Files.Tabs.DocViewer {
	sealed class BracePairCollection {
		public static readonly BracePairCollection Empty = new BracePairCollection(Array.Empty<BracePair>());

		readonly SpanDataCollection<Span> leftSorted;
		readonly SpanDataCollection<Span> rightSorted;

		public BracePairCollection(BracePair[] pairs) {
			if (pairs == null)
				throw new ArgumentNullException(nameof(pairs));

			if (pairs.Length == 0) {
				leftSorted = SpanDataCollection<Span>.Empty;
				rightSorted = SpanDataCollection<Span>.Empty;
			}
			else {
				int prevEnd;
				var builder = SpanDataCollectionBuilder<Span>.CreateBuilder(pairs.Length);

				Array.Sort(pairs, LeftSorter.Instance);
				prevEnd = 0;
				foreach (var p in pairs) {
					if (prevEnd <= p.Left.Start) {
						builder.Add(new Span(p.Left.Start, p.Left.Length), new Span(p.Right.Start, p.Right.Length));
						prevEnd = p.Left.End;
					}
				}
				leftSorted = builder.Create();

				builder.Clear();
				Array.Sort(pairs, RightSorter.Instance);
				prevEnd = 0;
				foreach (var p in pairs) {
					if (prevEnd <= p.Right.Start) {
						builder.Add(new Span(p.Right.Start, p.Right.Length), new Span(p.Left.Start, p.Left.Length));
						prevEnd = p.Right.End;
					}
				}
				rightSorted = builder.Create();
			}
		}

		sealed class LeftSorter : IComparer<BracePair> {
			public static readonly LeftSorter Instance = new LeftSorter();
			public int Compare(BracePair x, BracePair y) => x.Left.Start - y.Left.Start;
		}

		sealed class RightSorter : IComparer<BracePair> {
			public static readonly RightSorter Instance = new RightSorter();
			public int Compare(BracePair x, BracePair y) => x.Right.Start - y.Right.Start;
		}

		public BracePairResult? FindBracePair(int position) {
			var left = leftSorted.Find(position);
			var right = rightSorted.Find(position);
			if (left != null && right != null) {
				// Example: >( as in SomeMethod<T>(int arg)
				// We should really return both results but our code can only handle one.
				// Visual Studio highlights both references.
				return new BracePairResult(left.Value.Span, left.Value.Data);
			}
			if (left != null)
				return new BracePairResult(left.Value.Span, left.Value.Data);
			if (right != null)
				return new BracePairResult(right.Value.Data, right.Value.Span);
			return null;
		}
	}

	struct BracePairResult : IEquatable<BracePairResult> {
		public Span Left { get; }
		public Span Right { get; }
		public BracePairResult(Span left, Span right) {
			Debug.Assert(left.Start < right.Start);
			Left = left;
			Right = right;
		}

		public static bool operator ==(BracePairResult left, BracePairResult right) => left.Equals(right);
		public static bool operator !=(BracePairResult left, BracePairResult right) => !left.Equals(right);
		public bool Equals(BracePairResult other) => Left == other.Left && Right == other.Right;
		public override bool Equals(object obj) => obj is BracePairResult && Equals((BracePairResult)obj);
		public override int GetHashCode() => Left.GetHashCode() ^ Right.GetHashCode();
		public override string ToString() => "[" + Left.ToString() + "," + Right.ToString() + "]";
	}
}
