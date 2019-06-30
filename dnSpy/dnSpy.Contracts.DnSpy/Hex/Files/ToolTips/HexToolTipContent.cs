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
using dnSpy.Contracts.Hex.Text;

namespace dnSpy.Contracts.Hex.Files.ToolTips {
	/// <summary>
	/// Contains data used to create a tooltip
	/// </summary>
	public sealed class HexToolTipContent {
		/// <summary>
		/// Image shown in the tooltip or null
		/// </summary>
		public object? Image { get; }

		/// <summary>
		/// Gets all classified text
		/// </summary>
		public HexClassifiedTextCollection[] Text { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="text">Text</param>
		/// <param name="image">Image shown in the tooltip or null</param>
		public HexToolTipContent(HexClassifiedTextCollection[] text, object? image) {
			Text = text ?? throw new ArgumentNullException(nameof(text));
			Image = image;
		}
	}
}
