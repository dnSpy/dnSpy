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

namespace dnSpy.Contracts.Menus {
	/// <summary>Metadata</summary>
	public interface IMenuItemMetadata {
		/// <summary>See <see cref="ExportMenuItemAttribute.OwnerGuid"/></summary>
		string OwnerGuid { get; }
		/// <summary>See <see cref="ExportMenuItemAttribute.Guid"/></summary>
		string Guid { get; }
		/// <summary>See <see cref="ExportMenuItemAttribute.Group"/></summary>
		string Group { get; }
		/// <summary>See <see cref="ExportMenuItemAttribute.Order"/></summary>
		double Order { get; }
		/// <summary>See <see cref="ExportMenuItemAttribute.Header"/></summary>
		string Header { get; }
		/// <summary>See <see cref="ExportMenuItemAttribute.InputGestureText"/></summary>
		string InputGestureText { get; }
		/// <summary>See <see cref="ExportMenuItemAttribute.Icon"/></summary>
		string Icon { get; }
	}
}
