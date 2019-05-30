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

using dnSpy.Contracts.Hex.Editor;
using VSUTIL = Microsoft.VisualStudio.Utilities;

namespace dnSpy.Contracts.Hex.Files {
	/// <summary>
	/// Creates <see cref="HexFileStructureInfoProvider"/> instances. Export an instance with a
	/// <see cref="VSUTIL.NameAttribute"/> and an optional <see cref="VSUTIL.OrderAttribute"/>.
	/// See <see cref="PredefinedHexFileStructureInfoProviderFactoryNames"/>/
	/// </summary>
	public abstract class HexFileStructureInfoProviderFactory {
		/// <summary>
		/// Constructor
		/// </summary>
		protected HexFileStructureInfoProviderFactory() { }

		/// <summary>
		/// Creates a <see cref="HexFileStructureInfoProvider"/> or returns null
		/// </summary>
		/// <param name="hexView">Hex view</param>
		/// <returns></returns>
		public abstract HexFileStructureInfoProvider? Create(HexView hexView);
	}
}
