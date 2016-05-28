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

namespace dnSpy.Contracts.Text.Editor {
	/// <summary>
	/// <see cref="ITextView"/> model
	/// </summary>
	public interface ITextViewModel : IPropertyOwner, IDisposable {
		/// <summary>
		/// Gets the nearest point in the visual buffer
		/// </summary>
		/// <param name="editBufferPoint">Edit buffer</param>
		/// <returns></returns>
		SnapshotPoint GetNearestPointInVisualBuffer(SnapshotPoint editBufferPoint);

		/// <summary>
		/// Gets the nearest point in visual snapshot
		/// </summary>
		/// <param name="editBufferPoint">Edit buffer point</param>
		/// <param name="targetVisualSnapshot">Target visual snapshot</param>
		/// <param name="trackingMode">Tracking mode</param>
		/// <returns></returns>
		SnapshotPoint GetNearestPointInVisualSnapshot(SnapshotPoint editBufferPoint, ITextSnapshot targetVisualSnapshot, PointTrackingMode trackingMode);

		/// <summary>
		/// Returns true if the editor point is in the visual buffer
		/// </summary>
		/// <param name="editBufferPoint">Edit buffer point</param>
		/// <param name="affinity">Affinity</param>
		/// <returns></returns>
		bool IsPointInVisualBuffer(SnapshotPoint editBufferPoint, PositionAffinity affinity);

		/// <summary>
		/// Gets the data model
		/// </summary>
		ITextDataModel DataModel { get; }

		/// <summary>
		/// Data level text buffer
		/// </summary>
		ITextBuffer DataBuffer { get; }

		/// <summary>
		/// Buffer used for editing text. It may be identical to <see cref="DataBuffer"/>
		/// </summary>
		ITextBuffer EditBuffer { get; }

		/// <summary>
		/// Buffer shown in the text view. It may be identical to <see cref="EditBuffer"/>
		/// </summary>
		ITextBuffer VisualBuffer { get; }
	}
}
