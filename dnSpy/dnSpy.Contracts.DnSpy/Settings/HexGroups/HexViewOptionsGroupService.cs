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

namespace dnSpy.Contracts.Settings.HexGroups {
	/// <summary>
	/// <see cref="HexViewOptionsGroup"/> service
	/// </summary>
	public abstract class HexViewOptionsGroupService {
		/// <summary>
		/// Constructor
		/// </summary>
		protected HexViewOptionsGroupService() { }

		/// <summary>
		/// Gets a group
		/// </summary>
		/// <param name="name">Group name, eg. <see cref="PredefinedHexViewGroupNames.HexEditor"/></param>
		/// <returns></returns>
		public abstract HexViewOptionsGroup GetGroup(string name);
	}
}
