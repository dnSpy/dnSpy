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
using System.Windows;
using System.Windows.Media;
using Microsoft.VisualStudio.Text.Formatting;

namespace dnSpy.Contracts.Hex.Formatting {
	/// <summary>
	/// A formatted line
	/// </summary>
	public abstract class HexFormattedLine : WpfHexViewLine, IDisposable {
		/// <summary>
		/// Constructor
		/// </summary>
		protected HexFormattedLine() { }

		/// <summary>
		/// true if there's at least one adornment
		/// </summary>
		public abstract bool HasAdornments { get; }

		/// <summary>
		/// Gets or creates the visual
		/// </summary>
		/// <returns></returns>
		public abstract Visual GetOrCreateVisual();

		/// <summary>
		/// Removes the visual
		/// </summary>
		public abstract void RemoveVisual();

		/// <summary>
		/// Sets the change
		/// </summary>
		/// <param name="change">New value</param>
		public abstract void SetChange(TextViewLineChange change);

		/// <summary>
		/// Sets a new delta Y
		/// </summary>
		/// <param name="deltaY">New delta Y</param>
		public abstract void SetDeltaY(double deltaY);

		/// <summary>
		/// Sets a new line transform
		/// </summary>
		/// <param name="transform">New line transform</param>
		public abstract void SetLineTransform(LineTransform transform);

		/// <summary>
		/// Sets a new top
		/// </summary>
		/// <param name="top">New value</param>
		public abstract void SetTop(double top);

		/// <summary>
		/// Sets the visible area
		/// </summary>
		/// <param name="visibleArea">Visible area</param>
		public abstract void SetVisibleArea(Rect visibleArea);

		/// <summary>
		/// Disposes this instance
		/// </summary>
		public void Dispose() => DisposeCore();

		/// <summary>
		/// Disposes this instance
		/// </summary>
		protected virtual void DisposeCore() { }
	}
}
