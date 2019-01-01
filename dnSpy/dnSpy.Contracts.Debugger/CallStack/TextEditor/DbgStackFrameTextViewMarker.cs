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

using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Contracts.Debugger.CallStack.TextEditor {
	/// <summary>
	/// Creates stack frame spans in <see cref="ITextView"/>s
	/// </summary>
	public abstract class DbgStackFrameTextViewMarker {
		/// <summary>
		/// Called when there's new frames. It's always called before <see cref="GetFrameSpans(ITextView, NormalizedSnapshotSpanCollection)"/>.
		/// The first frame (if it exists) should not be marked by this class.
		/// </summary>
		/// <param name="frames">New frames. These frames are owned by the caller and should not be closed by the callee.</param>
		public abstract void OnNewFrames(ReadOnlyCollection<DbgStackFrame> frames);

		/// <summary>
		/// Returns spans of the active statements set by <see cref="OnNewFrames(ReadOnlyCollection{DbgStackFrame})"/>.
		/// It shouldn't return duplicate spans.
		/// The first frame (if it exists) should not be marked by this class.
		/// </summary>
		/// <param name="textView">Text view</param>
		/// <param name="spans">Spans</param>
		/// <returns></returns>
		public abstract IEnumerable<SnapshotSpan> GetFrameSpans(ITextView textView, NormalizedSnapshotSpanCollection spans);
	}
}
