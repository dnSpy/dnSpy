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

using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Windows;
using dnSpy.Contracts.Menus;
using VSTE = Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Contracts.Hex.Editor {
	/// <summary>
	/// Creates <see cref="IHexMarginContextMenuHandler"/>s or returns null. You must <see cref="ExportAttribute"/>
	/// this interface and add a <see cref="HexMarginNameAttribute"/> with the name of the margin (eg.
	/// <see cref="PredefinedHexMarginNames.Glyph"/>). Optional attribute: <see cref="VSTE.TextViewRoleAttribute"/>.
	/// </summary>
	public abstract class HexMarginContextMenuHandlerProvider {
		/// <summary>
		/// Constructor
		/// </summary>
		protected HexMarginContextMenuHandlerProvider() { }

		/// <summary>
		/// Creates <see cref="IHexMarginContextMenuHandler"/>s or returns null
		/// </summary>
		/// <param name="wpfHexViewHost">Hex view host</param>
		/// <param name="margin">Margin</param>
		/// <returns></returns>
		public abstract IHexMarginContextMenuHandler Create(WpfHexViewHost wpfHexViewHost, WpfHexViewMargin margin);
	}

	/// <summary>
	/// Creates context menu objects
	/// </summary>
	public interface IHexMarginContextMenuHandler {
		/// <summary>
		/// Creates context menu objects
		/// </summary>
		/// <param name="marginRelativePoint">Position of the mouse pointer relative to the glyph margin</param>
		/// <returns></returns>
		IEnumerable<GuidObject> GetContextMenuObjects(Point marginRelativePoint);
	}
}
