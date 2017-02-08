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

using VSTA = Microsoft.VisualStudio.Text.Adornments;

namespace dnSpy.Contracts.Hex.Adornments {
	/// <summary>
	/// Shows tooltips
	/// </summary>
	public abstract class HexToolTipProvider {
		/// <summary>
		/// Constructor
		/// </summary>
		protected HexToolTipProvider() { }

		/// <summary>
		/// Shows a tooltip
		/// </summary>
		/// <param name="span">Span and selection flags</param>
		/// <param name="toolTipContent">Tooltip content</param>
		public void ShowToolTip(HexBufferSpanSelection span, object toolTipContent) =>
			ShowToolTip(span.BufferSpan, span.SelectionFlags, toolTipContent);

		/// <summary>
		/// Shows a tooltip
		/// </summary>
		/// <param name="span">Span and selection flags</param>
		/// <param name="toolTipContent">Tooltip content</param>
		/// <param name="style">Popup style</param>
		public void ShowToolTip(HexBufferSpanSelection span, object toolTipContent, VSTA.PopupStyles style) =>
			ShowToolTip(span.BufferSpan, span.SelectionFlags, toolTipContent, style);

		/// <summary>
		/// Shows a tooltip
		/// </summary>
		/// <param name="bufferSpan">Buffer span</param>
		/// <param name="flags">Selection flags</param>
		/// <param name="toolTipContent">Tooltip content</param>
		public void ShowToolTip(HexBufferSpan bufferSpan, HexSpanSelectionFlags flags, object toolTipContent) =>
			ShowToolTip(bufferSpan, flags, toolTipContent, VSTA.PopupStyles.None);

		/// <summary>
		/// Shows a tooltip
		/// </summary>
		/// <param name="bufferSpan">Buffer span</param>
		/// <param name="flags">Selection flags</param>
		/// <param name="toolTipContent">Tooltip content</param>
		/// <param name="style">Popup style</param>
		public abstract void ShowToolTip(HexBufferSpan bufferSpan, HexSpanSelectionFlags flags, object toolTipContent, VSTA.PopupStyles style);

		/// <summary>
		/// Closes the tooltip
		/// </summary>
		public abstract void ClearToolTip();
	}
}
