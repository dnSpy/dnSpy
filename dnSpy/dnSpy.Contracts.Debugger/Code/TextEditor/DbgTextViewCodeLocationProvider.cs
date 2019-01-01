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
using dnSpy.Contracts.Documents.Tabs;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Contracts.Debugger.Code.TextEditor {
	/// <summary>
	/// Creates breakpoint locations in text views
	/// </summary>
	public abstract class DbgTextViewCodeLocationProvider {
		/// <summary>
		/// Creates a new <see cref="DbgCodeLocation"/> instance whose text view span is >= <paramref name="position"/>
		/// </summary>
		/// <param name="tab">Tab</param>
		/// <param name="textView">Text view</param>
		/// <param name="position">Position</param>
		/// <returns></returns>
		public abstract DbgTextViewBreakpointLocationResult? CreateLocation(IDocumentTab tab, ITextView textView, VirtualSnapshotPoint position);
	}

	/// <summary>
	/// Text view locations
	/// </summary>
	public readonly struct DbgTextViewBreakpointLocationResult {
		/// <summary>
		/// Gets all locations
		/// </summary>
		public DbgCodeLocation[] Locations { get; }

		/// <summary>
		/// Gets the span
		/// </summary>
		public VirtualSnapshotSpan Span { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="location">Location</param>
		/// <param name="span">Text view span</param>
		public DbgTextViewBreakpointLocationResult(DbgCodeLocation location, SnapshotSpan span)
			: this(location, new VirtualSnapshotSpan(span)) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="location">Location</param>
		/// <param name="span">Text view span</param>
		public DbgTextViewBreakpointLocationResult(DbgCodeLocation location, VirtualSnapshotSpan span) {
			if (span.Snapshot == null)
				throw new ArgumentException();
			Locations = new[] { location ?? throw new ArgumentNullException(nameof(location)) };
			Span = span;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="locations">Locations</param>
		/// <param name="span">Text view span</param>
		public DbgTextViewBreakpointLocationResult(DbgCodeLocation[] locations, SnapshotSpan span)
			: this(locations, new VirtualSnapshotSpan(span)) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="locations">Locations</param>
		/// <param name="span">Text view span</param>
		public DbgTextViewBreakpointLocationResult(DbgCodeLocation[] locations, VirtualSnapshotSpan span) {
			if (span.Snapshot == null)
				throw new ArgumentException();
			Locations = locations ?? throw new ArgumentNullException(nameof(locations));
			Span = span;
		}
	}
}
