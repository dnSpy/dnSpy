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

namespace dnSpy.Contracts.Controls {
	/// <summary>
	/// Zoomable control
	/// </summary>
	public interface IZoomable {
		/// <summary>
		/// Gets the current scale value (1.0 == 100%)
		/// </summary>
		double ZoomValue { get; }
	}

	/// <summary>
	/// <see cref="IZoomable"/> provider
	/// </summary>
	public interface IZoomableProvider {
		/// <summary>
		/// Gets the <see cref="IZoomable"/> instance or null
		/// </summary>
		IZoomable Zoomable { get; }
	}
}
