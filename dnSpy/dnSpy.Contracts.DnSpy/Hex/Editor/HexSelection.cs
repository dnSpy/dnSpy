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
using System.Collections.Generic;
using dnSpy.Contracts.Hex.Formatting;
using VST = Microsoft.VisualStudio.Text;

namespace dnSpy.Contracts.Hex.Editor {
	/// <summary>
	/// Hex selection
	/// </summary>
	public abstract class HexSelection {
		/// <summary>
		/// Constructor
		/// </summary>
		protected HexSelection() { }

		/// <summary>
		/// Gets the hex view
		/// </summary>
		public abstract HexView HexView { get; }

		/// <summary>
		/// Selects a span
		/// </summary>
		/// <param name="selectionSpan">Span</param>
		/// <param name="isReversed">true if the anchor point is the end point of <paramref name="selectionSpan"/></param>
		public abstract void Select(HexBufferSpan selectionSpan, bool isReversed);

		/// <summary>
		/// Select a span
		/// </summary>
		/// <param name="anchorPoint">Anchor point</param>
		/// <param name="activePoint">Active point</param>
		public abstract void Select(HexBufferPoint anchorPoint, HexBufferPoint activePoint);

		/// <summary>
		/// Gets all selected spans
		/// </summary>
		public abstract NormalizedHexBufferSpanCollection SelectedSpans { get; }

		/// <summary>
		/// Gets the slection on a line
		/// </summary>
		/// <param name="line">Line</param>
		/// <returns></returns>
		public abstract IEnumerable<VST.Span> GetSelectionOnHexViewLine(HexViewLine line);

		/// <summary>
		/// Gets the selected span
		/// </summary>
		public HexBufferSpan StreamSelectionSpan => new HexBufferSpan(Start, End);

		/// <summary>
		/// true if the selection is reversed
		/// </summary>
		public bool IsReversed => ActivePoint < AnchorPoint;

		/// <summary>
		/// Clears the selection
		/// </summary>
		public abstract void Clear();

		/// <summary>
		/// true if the selection is empty
		/// </summary>
		public abstract bool IsEmpty { get; }

		/// <summary>
		/// true if the selection is active, false if it's inactive
		/// </summary>
		public abstract bool IsActive { get; set; }

		/// <summary>
		/// true if <see cref="IsActive"/> gets updated when the hex view gets and loses focus
		/// </summary>
		public abstract bool ActivationTracksFocus { get; set; }

		/// <summary>
		/// Raised when the selection is changed
		/// </summary>
		public abstract event EventHandler SelectionChanged;

		/// <summary>
		/// Gets the active point
		/// </summary>
		public abstract HexBufferPoint ActivePoint { get; }

		/// <summary>
		/// Gets the anchor point
		/// </summary>
		public abstract HexBufferPoint AnchorPoint { get; }

		/// <summary>
		/// Gets the start position
		/// </summary>
		public HexBufferPoint Start => AnchorPoint < ActivePoint ? AnchorPoint : ActivePoint;

		/// <summary>
		/// Gets the end position
		/// </summary>
		public HexBufferPoint End => AnchorPoint < ActivePoint ? ActivePoint : AnchorPoint;
	}
}
