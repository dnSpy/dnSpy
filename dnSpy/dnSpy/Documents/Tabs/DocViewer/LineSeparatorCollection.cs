/*
    Copyright (C) 2014-2019 de4dot@gmail.com

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
using Microsoft.VisualStudio.Text;

namespace dnSpy.Documents.Tabs.DocViewer {
	sealed class LineSeparatorCollection {
		public static readonly LineSeparatorCollection Empty = new LineSeparatorCollection(Array.Empty<LineSeparator>());

		readonly LineSeparator[] lineSeparators;

		public LineSeparatorCollection(LineSeparator[] lineSeparators) {
			if (lineSeparators == null)
				throw new ArgumentNullException(nameof(lineSeparators));
#if DEBUG
			for (int i = 1; i < lineSeparators.Length; i++) {
				if (lineSeparators[i - 1].Position >= lineSeparators[i].Position)
					throw new ArgumentException("Line separators array isn't sorted or contains dupes");
			}
#endif
			this.lineSeparators = lineSeparators;
		}

		public IEnumerable<int> Find(Span span) => Find(span.Start, span.Length);

		public IEnumerable<int> Find(int position, int length) {
			if (position < 0)
				throw new ArgumentOutOfRangeException(nameof(position));
			if (length < 0)
				throw new ArgumentOutOfRangeException(nameof(length));
			int end = position + length;
			if (end < 0)
				throw new ArgumentOutOfRangeException(nameof(length));
			int index = GetStartIndex(position);
			if (index < 0)
				yield break;
			var array = lineSeparators;
			while (index < array.Length) {
				var lineSep = array[index++];
				if (end < lineSep.Position)
					break;
				Debug.Assert(position <= lineSep.Position && lineSep.Position <= position + length);
				yield return lineSep.Position;
			}
		}

		int GetStartIndex(int position) {
			var array = lineSeparators;
			int lo = 0, hi = array.Length - 1;
			while (lo <= hi) {
				int index = (lo + hi) / 2;

				var spanData = array[index];
				if (position < spanData.Position)
					hi = index - 1;
				else if (position > spanData.Position)
					lo = index + 1;
				else
					return index;
			}
			return lo < array.Length ? lo : -1;
		}
	}
}
