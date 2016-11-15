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

using Microsoft.VisualStudio.Text;

namespace dnSpy.Contracts.Hex.Formatting {
	/// <summary>
	/// Adornment sequence element
	/// </summary>
	public abstract class HexAdornmentElement : HexSequenceElement {
		/// <summary>
		/// Constructor
		/// </summary>
		protected HexAdornmentElement() { }

		/// <summary>
		/// Gets the width
		/// </summary>
		public abstract double Width { get; }

		/// <summary>
		/// Gets the top space
		/// </summary>
		public abstract double TopSpace { get; }

		/// <summary>
		/// Gets the base line
		/// </summary>
		public abstract double Baseline { get; }

		/// <summary>
		/// Gets the text height
		/// </summary>
		public abstract double TextHeight { get; }

		/// <summary>
		/// Gets the bottom space
		/// </summary>
		public abstract double BottomSpace { get; }

		/// <summary>
		/// Gets the affinity
		/// </summary>
		public abstract PositionAffinity Affinity { get; }

		/// <summary>
		/// Gets the identity tag
		/// </summary>
		public abstract object IdentityTag { get; }

		/// <summary>
		/// Gets the provider tag
		/// </summary>
		public abstract object ProviderTag { get; }
	}
}
