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

namespace dnSpy.Contracts.Hex.Classification.DnSpy {
	/// <summary>
	/// Creates <see cref="HexTextElementCreator"/> instances that can be used to create text
	/// elements shown in tooltips
	/// </summary>
	public abstract class HexTextElementCreatorProvider {
		/// <summary>
		/// Constructor
		/// </summary>
		protected HexTextElementCreatorProvider() { }

		/// <summary>
		/// Creates a <see cref="HexTextElementCreator"/> using the default content type
		/// </summary>
		/// <returns></returns>
		public abstract HexTextElementCreator Create();

		/// <summary>
		/// Creates a <see cref="HexTextElementCreator"/>
		/// </summary>
		/// <param name="contentType">Content type</param>
		/// <returns></returns>
		public abstract HexTextElementCreator Create(string contentType);
	}
}
