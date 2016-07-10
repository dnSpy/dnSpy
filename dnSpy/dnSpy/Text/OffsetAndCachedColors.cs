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

using System.Diagnostics;
using dnSpy.Contracts.Text;

namespace dnSpy.Text {
	struct OffsetAndCachedColors {
		public int Offset { get; }
		public CachedTextTokenColors CachedColors { get; }

		static OffsetAndCachedColors() {
			var info = new CachedTextTokenColors();
			info.Finish();
			Default = new OffsetAndCachedColors(0, info);
		}
		public static readonly OffsetAndCachedColors Default;

		public OffsetAndCachedColors(int offset, CachedTextTokenColors cachedColors) {
			Offset = offset;
			CachedColors = cachedColors;
		}

		public int DocOffsetToRelativeOffset(int docOffset) {
			Debug.Assert(CachedColors == Default.CachedColors || Offset <= docOffset);
			return docOffset - Offset;
		}

		public bool FindByDocOffset(int docOffset, out int defaultTextLength, out object color, out int tokenLength) =>
			CachedColors.Find(DocOffsetToRelativeOffset(docOffset), out defaultTextLength, out color, out tokenLength);
	}
}
