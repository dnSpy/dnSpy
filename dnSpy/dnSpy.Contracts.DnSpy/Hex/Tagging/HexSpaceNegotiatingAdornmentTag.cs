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

using VST = Microsoft.VisualStudio.Text;

namespace dnSpy.Contracts.Hex.Tagging {
	/// <summary>
	/// Space negotiating adornment tag
	/// </summary>
	public sealed class HexSpaceNegotiatingAdornmentTag : HexTag {
		/// <summary>
		/// Gets the width
		/// </summary>
		public double Width { get; }

		/// <summary>
		/// Gets the top space
		/// </summary>
		public double TopSpace { get; }

		/// <summary>
		/// Gets the base line
		/// </summary>
		public double Baseline { get; }

		/// <summary>
		/// Gets the text height
		/// </summary>
		public double TextHeight { get; }

		/// <summary>
		/// Gets the bottom space
		/// </summary>
		public double BottomSpace { get; }

		/// <summary>
		/// Gets the affinity
		/// </summary>
		public VST.PositionAffinity Affinity { get; }

		/// <summary>
		/// Gets the identity tag
		/// </summary>
		public object IdentityTag { get; }

		/// <summary>
		/// Gets the provider tag
		/// </summary>
		public object ProviderTag { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="width">Width</param>
		/// <param name="topSpace">Top space</param>
		/// <param name="baseline">Base line</param>
		/// <param name="textHeight">Text height</param>
		/// <param name="bottomSpace">Bottom space</param>
		/// <param name="affinity">Affinity</param>
		/// <param name="identityTag">Identity tag</param>
		/// <param name="providerTag">Provider tag</param>
		public HexSpaceNegotiatingAdornmentTag(double width, double topSpace, double baseline, double textHeight, double bottomSpace, VST.PositionAffinity affinity, object identityTag, object providerTag) {
			Width = width;
			TopSpace = topSpace;
			Baseline = baseline;
			TextHeight = textHeight;
			BottomSpace = bottomSpace;
			Affinity = affinity;
			IdentityTag = identityTag;
			ProviderTag = providerTag;
		}
	}
}
