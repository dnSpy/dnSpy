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

using System;

namespace dnSpy.Contracts.Hex.Editor.HexGroups {
	/// <summary>
	/// Creates hex views that are part of some hex view group
	/// </summary>
	public abstract class HexEditorGroupFactoryService {
		/// <summary>
		/// Constructor
		/// </summary>
		protected HexEditorGroupFactoryService() { }

		/// <summary>
		/// Creates a hex view host
		/// </summary>
		/// <param name="buffer">Buffer</param>
		/// <param name="group">Group, eg. <see cref="PredefinedHexViewRoles.HexEditorGroup"/></param>
		/// <param name="subGroup">Sub group, eg. <see cref="PredefinedHexViewRoles.HexEditorGroupDefault"/></param>
		/// <param name="menuGuid">Menu guid or null</param>
		/// <returns></returns>
		public abstract WpfHexViewHost Create(HexBuffer buffer, string group, string subGroup, Guid? menuGuid);

		/// <summary>
		/// Gets the default local options
		/// </summary>
		/// <param name="hexView">Hex view</param>
		/// <returns></returns>
		public abstract LocalGroupOptions GetDefaultLocalOptions(HexView hexView);
	}
}
