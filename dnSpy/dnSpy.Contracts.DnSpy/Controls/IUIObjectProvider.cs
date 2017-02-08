/*
    Copyright (C) 2014-2017 de4dot@gmail.com

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

using System.Windows;
using System.Windows.Media;

namespace dnSpy.Contracts.Controls {
	/// <summary>
	/// UI object provider
	/// </summary>
	public interface IUIObjectProvider {
		/// <summary>
		/// UI object
		/// </summary>
		object UIObject { get; }

		/// <summary>
		/// Focused element
		/// </summary>
		IInputElement FocusedElement { get; }

		/// <summary>
		/// Gets the element that gets the <see cref="ScaleTransform"/> or null if none
		/// </summary>
		FrameworkElement ZoomElement { get; }
	}

	/// <summary>
	/// UI object provider
	/// </summary>
	public interface IUIObjectProvider2 : IUIObjectProvider {
		/// <summary>
		/// Can be set to any value by the owner
		/// </summary>
		object Tag { get; set; }
	}
}
