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
using dnSpy.Contracts.Text.Formatting;

namespace dnSpy.Text.Editor {
	static class TextViewLineExtensions {
		public static bool IsVisible(this ITextViewLine line) {
			if (line == null)
				throw new ArgumentNullException(nameof(line));
			return line.VisibilityState == VisibilityState.FullyVisible || line.VisibilityState == VisibilityState.PartiallyVisible;
		}

		public static bool IsFirstDocumentLine(this ITextViewLine line) {
			if (line == null)
				throw new ArgumentNullException(nameof(line));
			return line.IsFirstTextViewLineForSnapshotLine && line.Start.Position == 0;
		}

		public static bool IsLastDocumentLine(this ITextViewLine line) {
			if (line == null)
				throw new ArgumentNullException(nameof(line));
			return line.IsLastTextViewLineForSnapshotLine && line.LineBreakLength == 0;
		}
	}
}
