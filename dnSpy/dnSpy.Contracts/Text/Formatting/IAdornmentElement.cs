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

namespace dnSpy.Contracts.Text.Formatting {
	/// <summary>
	/// Represents a sequence element that consists of an adornment
	/// </summary>
	public interface IAdornmentElement : ISequenceElement {
		/// <summary>
		/// Gets the width of the adornment (in logical pixels)
		/// </summary>
		double Width { get; }

		/// <summary>
		/// Gets the amount of space (in logical pixels) to reserve above top of the text for the <see cref="ITextViewLine"/>
		/// </summary>
		double TopSpace { get; }

		/// <summary>
		/// Gets the distance (in logical pixels) between the top of the adornment text and the baseline of the <see cref="ITextViewLine"/>
		/// </summary>
		double Baseline { get; }

		/// <summary>
		/// Gets the height of the adornment text
		/// </summary>
		double TextHeight { get; }

		/// <summary>
		/// Gets the amount of space (in logical pixels) to reserve below the bottom of the text in the <see cref="ITextViewLine"/>
		/// </summary>
		double BottomSpace { get; }

		/// <summary>
		/// Gets the <see cref="PositionAffinity"/> of the adornment
		/// </summary>
		PositionAffinity Affinity { get; }

		/// <summary>
		/// Gets the unique identifier associated with this adornment (which is used by <see cref="ITextViewLine.GetAdornmentBounds(object)"/>)
		/// </summary>
		object IdentityTag { get; }

		/// <summary>
		/// Gets the unique identifier associated with the provider of the adornment
		/// </summary>
		object ProviderTag { get; }
	}
}
