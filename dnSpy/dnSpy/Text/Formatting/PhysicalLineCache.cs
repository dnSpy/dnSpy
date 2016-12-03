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
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Formatting;

namespace dnSpy.Text.Formatting {
	sealed class PhysicalLineCache  {
		readonly List<PhysicalLine> cache;
		readonly int maxCacheSize;
		bool isDisposed;

		public PhysicalLineCache(int maxCacheSize) {
			if (maxCacheSize < 1)
				throw new ArgumentOutOfRangeException(nameof(maxCacheSize));
			cache = new List<PhysicalLine>(maxCacheSize);
			this.maxCacheSize = maxCacheSize;
		}

		public IFormattedLine FindFormattedLineByBufferPosition(SnapshotPoint point) {
			if (isDisposed)
				throw new ObjectDisposedException(nameof(PhysicalLineCache));
			for (int i = 0; i < cache.Count; i++) {
				var physLine = cache[i];
				var line = physLine.FindFormattedLineByBufferPosition(point);
				if (line != null)
					return line;
			}
			return null;
		}

		public void Add(PhysicalLine line) {
			if (isDisposed)
				throw new ObjectDisposedException(nameof(PhysicalLineCache));
			if (line == null)
				throw new ArgumentNullException(nameof(line));
			if (cache.Count == maxCacheSize) {
				const int oldestIndex = 0;
				var oldest = cache[oldestIndex];
				oldest.Dispose();
				cache.RemoveAt(oldestIndex);
			}
			cache.Add(line);
		}

		void Clear() {
			foreach (var l in cache)
				l.Dispose();
			cache.Clear();
		}

		public PhysicalLine[] TakeOwnership() {
			var lines = cache.ToArray();
			cache.Clear();
			return lines;
		}

		public void Dispose() {
			isDisposed = true;
			Clear();
		}
	}
}
