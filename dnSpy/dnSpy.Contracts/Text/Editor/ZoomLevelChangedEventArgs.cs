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
using System.Windows.Media;

namespace dnSpy.Contracts.Text.Editor {
	/// <summary>
	/// Zoom level changed event args
	/// </summary>
	public sealed class ZoomLevelChangedEventArgs : EventArgs {
		/// <summary>
		/// New zoom level
		/// </summary>
		public double NewZoomLevel { get; }

		/// <summary>
		/// New zoom transform
		/// </summary>
		public Transform ZoomTransform { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="newZoomLevel">New zoom level</param>
		/// <param name="transform">New zoom transform</param>
		public ZoomLevelChangedEventArgs(double newZoomLevel, Transform transform) {
			if (transform == null)
				throw new ArgumentNullException(nameof(transform));
			NewZoomLevel = newZoomLevel;
			ZoomTransform = transform;
		}
	}
}
