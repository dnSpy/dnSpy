/*
    Copyright (C) 2014-2017 de4dot@gmail.com

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
using Microsoft.VisualStudio.Text;

namespace dnSpy.Contracts.Text {
	sealed class CachedTextColorsCollection {
		public static readonly CachedTextColorsCollection Empty = new CachedTextColorsCollection().Freeze();
		public int Count => colorsList.Count;
		public int TextLength => currentOffset;
		public SpanData<object> this[int index] => colorsList[index];

		readonly List<SpanData<object>> colorsList;
		int currentOffset;
		bool frozen;

		public CachedTextColorsCollection() => colorsList = new List<SpanData<object>>();

		public CachedTextColorsCollection Freeze() {
			frozen = true;
			return this;
		}

		public void Append(object color, string text) {
			if (frozen)
				throw new InvalidOperationException("Instance is frozen");
			if (text == null)
				throw new ArgumentNullException(nameof(text));
			Append(color, text, 0, text.Length);
		}

		public void Append(object color, string text, int index, int length) {
			if (frozen)
				throw new InvalidOperationException("Instance is frozen");
			if (text == null)
				throw new ArgumentNullException(nameof(text));
			if (index < 0)
				throw new ArgumentOutOfRangeException(nameof(index));
			if (length < 0)
				throw new ArgumentOutOfRangeException(nameof(length));
			if ((uint)(index + length) > (uint)text.Length)
				throw new ArgumentOutOfRangeException(nameof(length));

			if (length == 0)
				return;
			colorsList.Add(new SpanData<object>(new Span(currentOffset, length), color));
			currentOffset += length;
		}

		public int GetStartIndex(int position) {
			var list = colorsList;
			int lo = 0, hi = list.Count - 1;
			while (lo <= hi) {
				int index = (lo + hi) / 2;

				var spanData = list[index];
				if (position < spanData.Span.Start)
					hi = index - 1;
				else if (position >= spanData.Span.End)
					lo = index + 1;
				else {
					if (index > 0 && list[index - 1].Span.End == position)
						return index - 1;
					return index;
				}
			}
			if ((uint)hi < (uint)list.Count && list[hi].Span.End == position)
				return hi;
			return lo < list.Count ? lo : -1;
		}
	}
}
