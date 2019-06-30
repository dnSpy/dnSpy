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
using System.Windows;
using dnSpy.Contracts.Hex.Editor;
using VSLI = Microsoft.VisualStudio.Language.Intellisense;
using VSTA = Microsoft.VisualStudio.Text.Adornments;

namespace dnSpy.Contracts.Hex.Intellisense {
	/// <summary>
	/// Popup <see cref="HexIntellisensePresenter"/>
	/// </summary>
	public interface IHexPopupIntellisensePresenter : IHexIntellisensePresenter {
		/// <summary>
		/// Gets the UI element
		/// </summary>
		UIElement SurfaceElement { get; }

		/// <summary>
		/// Raised after <see cref="SurfaceElement"/> is changed
		/// </summary>
		event EventHandler SurfaceElementChanged;

		/// <summary>
		/// Gets the presentation span
		/// </summary>
		HexBufferSpanSelection PresentationSpan { get; }

		/// <summary>
		/// Raised after <see cref="PresentationSpan"/> is changed
		/// </summary>
		event EventHandler PresentationSpanChanged;

		/// <summary>
		/// Gets the popup style
		/// </summary>
		VSTA.PopupStyles PopupStyles { get; }

		/// <summary>
		/// Raised after <see cref="PopupStyles"/> is changed
		/// </summary>
		event EventHandler<VSLI.ValueChangedEventArgs<VSTA.PopupStyles>> PopupStylesChanged;

		/// <summary>
		/// Gets the name of the <see cref="HexSpaceReservationManager"/>
		/// </summary>
		string SpaceReservationManagerName { get; }

		/// <summary>
		/// Gets/sets the opacity
		/// </summary>
		double Opacity { get; set; }
	}
}
