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
	/// Text version
	/// </summary>
	public interface ITextVersion {
		/// <summary>
		/// Gets the owner buffer
		/// </summary>
		ITextBuffer TextBuffer { get; }

		/// <summary>
		/// Gets the version number
		/// </summary>
		int VersionNumber { get; }

		/// <summary>
		/// Gets the oldest version number for which all text changes between that version and this version have been canceled out by corresponding undo/redo operations
		/// </summary>
		int ReiteratedVersionNumber { get; }

		/// <summary>
		/// Gets the next version or null if this is the latest version
		/// </summary>
		ITextVersion Next { get; }

		/// <summary>
		/// Gets all the changes to the next version or null if this is the latest version
		/// </summary>
		INormalizedTextChangeCollection Changes { get; }

		/// <summary>
		/// Gets the length in characters of the text
		/// </summary>
		int Length { get; }

		/// <summary>
		/// Creates a tracking point
		/// </summary>
		/// <param name="position">Position</param>
		/// <param name="trackingMode">Tracking mode</param>
		/// <returns></returns>
		ITrackingPoint CreateTrackingPoint(int position, PointTrackingMode trackingMode);

		/// <summary>
		/// Creates a tracking point
		/// </summary>
		/// <param name="position">Position</param>
		/// <param name="trackingMode">Tracking mode</param>
		/// <param name="trackingFidelity">Tracking fidelity</param>
		/// <returns></returns>
		ITrackingPoint CreateTrackingPoint(int position, PointTrackingMode trackingMode, TrackingFidelityMode trackingFidelity);

		/// <summary>
		/// Creates a tracking span
		/// </summary>
		/// <param name="span">Span</param>
		/// <param name="trackingMode">Tracking mode</param>
		/// <returns></returns>
		ITrackingSpan CreateTrackingSpan(Span span, SpanTrackingMode trackingMode);

		/// <summary>
		/// Creates a tracking span
		/// </summary>
		/// <param name="span">Span</param>
		/// <param name="trackingMode">Tracking mode</param>
		/// <param name="trackingFidelity">Tracking fidelity</param>
		/// <returns></returns>
		ITrackingSpan CreateTrackingSpan(Span span, SpanTrackingMode trackingMode, TrackingFidelityMode trackingFidelity);

		/// <summary>
		/// Creates a tracking span
		/// </summary>
		/// <param name="start">Start</param>
		/// <param name="length">Length</param>
		/// <param name="trackingMode">Tracking mode</param>
		/// <returns></returns>
		ITrackingSpan CreateTrackingSpan(int start, int length, SpanTrackingMode trackingMode);

		/// <summary>
		/// Creates a tracking span
		/// </summary>
		/// <param name="start">Start</param>
		/// <param name="length">Length</param>
		/// <param name="trackingMode">Tracking mode</param>
		/// <param name="trackingFidelity">Tracking fidelity</param>
		/// <returns></returns>
		ITrackingSpan CreateTrackingSpan(int start, int length, SpanTrackingMode trackingMode, TrackingFidelityMode trackingFidelity);
	}
}
