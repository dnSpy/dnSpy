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

namespace dnSpy.Contracts.Hex.Files.ToolTips {
	/// <summary>
	/// Creates <see cref="HexToolTipContent"/>
	/// </summary>
	public abstract class HexToolTipContentCreator {
		/// <summary>
		/// Constructor
		/// </summary>
		protected HexToolTipContentCreator() { }

		/// <summary>
		/// Image shown in the tooltip
		/// </summary>
		public abstract object Image { get; set; }

		/// <summary>
		/// Gets the current writer
		/// </summary>
		public abstract HexFieldFormatter Writer { get; }

		/// <summary>
		/// Creates and returns a new writer. The created text is shown on a new line.
		/// </summary>
		/// <returns></returns>
		public abstract HexFieldFormatter CreateNewWriter();

		/// <summary>
		/// Creates the content
		/// </summary>
		/// <returns></returns>
		public abstract HexToolTipContent Create();
	}
}
