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

using dnSpy.Contracts.Text.Formatting;

namespace dnSpy.Contracts.Text.Tagging {
	/// <summary>
	/// Represents a tag for a space-negotiating adornment. The tag is used to provide space for positioning the adornment in a view.
	/// </summary>
	public class SpaceNegotiatingAdornmentTag : ITag {
		/// <summary>
		/// Gets the width of the adornment
		/// </summary>
		public double Width { get; }

		/// <summary>
		/// Gets the amount of space needed between the top of the text in the <see cref="ITextViewLine"/> and the top of the <see cref="ITextViewLine"/>
		/// </summary>
		public double TopSpace { get; }

		/// <summary>
		/// Gets the baseline of the space-negotiating adornment
		/// </summary>
		public double Baseline { get; }

		/// <summary>
		/// Gets the height of the text portion of the space-negotiating adornment
		/// </summary>
		public double TextHeight { get; }

		/// <summary>
		/// Gets the amount of space needed between the bottom of the text in the <see cref="ITextViewLine"/> and the bottom of the <see cref="ITextViewLine"/>
		/// </summary>
		public double BottomSpace { get; }

		/// <summary>
		/// Gets the <see cref="PositionAffinity"/> of the space-negotiating adornment
		/// </summary>
		public PositionAffinity Affinity { get; }

		/// <summary>
		/// Gets a unique object associated with the space-negotiating adornment, which is used by <see cref="ITextViewLine.GetAdornmentBounds(object)"/>
		/// </summary>
		public object IdentityTag { get; }

		/// <summary>
		/// Gets a unique object that identifies the provider of the adornment
		/// </summary>
		public object ProviderTag { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="width">Width of the adornment</param>
		/// <param name="topSpace">Amount of space needed between the top of the text in the <see cref="ITextViewLine"/> and the top of the <see cref="ITextViewLine"/></param>
		/// <param name="baseline">Baseline of the space-negotiating adornment</param>
		/// <param name="textHeight">Height of the text portion of the space-negotiating adornment</param>
		/// <param name="bottomSpace">Amount of space needed between the bottom of the text in the <see cref="ITextViewLine"/> and the bottom of the <see cref="ITextViewLine"/></param>
		/// <param name="affinity"><see cref="PositionAffinity"/> of the space-negotiating adornment</param>
		/// <param name="identityTag">A unique object associated with the space-negotiating adornment, which is used by <see cref="ITextViewLine.GetAdornmentBounds(object)"/></param>
		/// <param name="providerTag">A unique object that identifies the provider of the adornment</param>
		public SpaceNegotiatingAdornmentTag(double width, double topSpace, double baseline, double textHeight, double bottomSpace, PositionAffinity affinity, object identityTag, object providerTag) {
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
