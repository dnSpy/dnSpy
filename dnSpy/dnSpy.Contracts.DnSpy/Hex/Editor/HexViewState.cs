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

namespace dnSpy.Contracts.Hex.Editor {
	/// <summary>
	/// Hex view state
	/// </summary>
	public sealed class HexViewState {
		/// <summary>
		/// Gets viewport top
		/// </summary>
		public double ViewportTop { get; }

		/// <summary>
		/// Gets viewport bottom
		/// </summary>
		public double ViewportBottom => ViewportTop + ViewportHeight;

		/// <summary>
		/// Gets viewport left
		/// </summary>
		public double ViewportLeft { get; }

		/// <summary>
		/// Gets viewport right
		/// </summary>
		public double ViewportRight => ViewportLeft + ViewportWidth;

		/// <summary>
		/// Gets viewport width
		/// </summary>
		public double ViewportWidth { get; }

		/// <summary>
		/// Gets viewport height
		/// </summary>
		public double ViewportHeight { get; }

		/// <summary>
		/// Gets the buffer
		/// </summary>
		public HexBuffer Buffer { get; }

		/// <summary>
		/// Gets the version
		/// </summary>
		public HexVersion Version { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="view">Hex view</param>
		public HexViewState(HexView view)
			: this(view, view?.ViewportWidth ?? 0, view?.ViewportHeight ?? 0) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="view">Hex view</param>
		/// <param name="effectiveViewportWidth">Viewport width</param>
		/// <param name="effectiveViewportHeight">Viewport height</param>
		public HexViewState(HexView view, double effectiveViewportWidth, double effectiveViewportHeight) {
			if (view == null)
				throw new ArgumentNullException(nameof(view));
			ViewportTop = view.ViewportTop;
			ViewportLeft = view.ViewportLeft;
			ViewportWidth = effectiveViewportWidth;
			ViewportHeight = effectiveViewportHeight;
			Buffer = view.Buffer;
			Version = view.Buffer.Version;
		}
	}
}
