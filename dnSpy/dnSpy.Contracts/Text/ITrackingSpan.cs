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

namespace dnSpy.Contracts.Text {
	/// <summary>
	/// Tracking span
	/// </summary>
	public interface ITrackingSpan {
		/// <summary>
		/// Gets the text buffer
		/// </summary>
		ITextBuffer TextBuffer { get; }

		/// <summary>
		/// Gets the tracking fidelity mode
		/// </summary>
		TrackingFidelityMode TrackingFidelity { get; }

		/// <summary>
		/// Gets the span tracking mode
		/// </summary>
		SpanTrackingMode TrackingMode { get; }

		/// <summary>
		/// Gets the <see cref="Span"/>
		/// </summary>
		/// <param name="version">Version</param>
		/// <returns></returns>
		Span GetSpan(ITextVersion version);

		/// <summary>
		/// Gets the <see cref="SnapshotSpan"/>
		/// </summary>
		/// <param name="snapshot">Snapshot</param>
		/// <returns></returns>
		SnapshotSpan GetSpan(ITextSnapshot snapshot);

		/// <summary>
		/// Gets the start point
		/// </summary>
		/// <param name="snapshot">Snapshot</param>
		/// <returns></returns>
		SnapshotPoint GetStartPoint(ITextSnapshot snapshot);

		/// <summary>
		/// Gets the end point
		/// </summary>
		/// <param name="snapshot">Snapshot</param>
		/// <returns></returns>
		SnapshotPoint GetEndPoint(ITextSnapshot snapshot);

		/// <summary>
		/// Gets the text
		/// </summary>
		/// <param name="snapshot"></param>
		/// <returns></returns>
		string GetText(ITextSnapshot snapshot);
	}
}
