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

using System;
using System.Windows;
using dnSpy.Contracts.Hex.Tagging;
using VST = Microsoft.VisualStudio.Text;
using VSTE = Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Contracts.Hex.Editor {
	/// <summary>
	/// Intra text adornment tag
	/// </summary>
	public class HexIntraTextAdornmentTag : HexTag {
		/// <summary>
		/// Gets the adornment element
		/// </summary>
		public UIElement Adornment { get; }

		/// <summary>
		/// Gets the removal callback or null if none
		/// </summary>
		public VSTE.AdornmentRemovedCallback RemovalCallback { get; }

		/// <summary>
		/// Gets the top space or null to use the default value
		/// </summary>
		public double? TopSpace { get; }

		/// <summary>
		/// Gets the base line or null to use the default value
		/// </summary>
		public double? Baseline { get; }

		/// <summary>
		/// Gets the text height or null to use the default value
		/// </summary>
		public double? TextHeight { get; }

		/// <summary>
		/// Gets the bottom space or null to use the default value
		/// </summary>
		public double? BottomSpace { get; }

		/// <summary>
		/// Gets the position affinity or null to use the default value
		/// </summary>
		public VST.PositionAffinity? Affinity { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="adornment">Adornment element</param>
		/// <param name="removalCallback">Called when the adornment is removed, may be null</param>
		/// <param name="topSpace">Top space or null to use the default value</param>
		/// <param name="baseline">Base line or null to use the default value</param>
		/// <param name="textHeight">Text height or null to use the default value</param>
		/// <param name="bottomSpace">Bottom space or null to use the default value</param>
		/// <param name="affinity">Position affinity or null to use the default value</param>
		public HexIntraTextAdornmentTag(UIElement adornment, VSTE.AdornmentRemovedCallback removalCallback, double? topSpace, double? baseline, double? textHeight, double? bottomSpace, VST.PositionAffinity? affinity) {
			if (adornment == null)
				throw new ArgumentNullException(nameof(adornment));
			Adornment = adornment;
			RemovalCallback = removalCallback;
			TopSpace = topSpace;
			Baseline = baseline;
			TextHeight = textHeight;
			BottomSpace = bottomSpace;
			Affinity = affinity;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="adornment">Adornment element</param>
		/// <param name="removalCallback">Called when the adornment is removed, may be null</param>
		/// <param name="affinity">Position affinity or null to use the default value</param>
		public HexIntraTextAdornmentTag(UIElement adornment, VSTE.AdornmentRemovedCallback removalCallback, VST.PositionAffinity? affinity)
			: this(adornment, removalCallback, null, null, null, null, affinity) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="adornment">Adornment element</param>
		/// <param name="removalCallback">Called when the adornment is removed, may be null</param>
		public HexIntraTextAdornmentTag(UIElement adornment, VSTE.AdornmentRemovedCallback removalCallback)
			: this(adornment, removalCallback, null, null, null, null, null) {
		}
	}
}
