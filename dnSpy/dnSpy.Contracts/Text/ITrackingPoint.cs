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
	/// Tracking point
	/// </summary>
	public interface ITrackingPoint {
		/// <summary>
		/// Gets the text buffer
		/// </summary>
		ITextBuffer TextBuffer { get; }

		/// <summary>
		/// Gets the tracking fidelity mode
		/// </summary>
		TrackingFidelityMode TrackingFidelity { get; }

		/// <summary>
		/// Gets the point tracking mode
		/// </summary>
		PointTrackingMode TrackingMode { get; }

		/// <summary>
		/// Gets the character at this position
		/// </summary>
		/// <param name="snapshot">Snapshot</param>
		/// <returns></returns>
		char GetCharacter(ITextSnapshot snapshot);

		/// <summary>
		/// Gets the <see cref="SnapshotPoint"/>
		/// </summary>
		/// <param name="snapshot">Snapshot</param>
		/// <returns></returns>
		SnapshotPoint GetPoint(ITextSnapshot snapshot);

		/// <summary>
		/// Gets the position in <paramref name="snapshot"/>
		/// </summary>
		/// <param name="snapshot">Snapshot</param>
		/// <returns></returns>
		int GetPosition(ITextSnapshot snapshot);

		/// <summary>
		/// Gets the position in <paramref name="version"/>
		/// </summary>
		/// <param name="version">Version</param>
		/// <returns></returns>
		int GetPosition(ITextVersion version);
	}
}
