/*
    Copyright (C) 2014-2015 de4dot@gmail.com

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
using System.ComponentModel.Composition;

namespace dnSpy.Contracts.Menus {
	/// <summary>
	/// Exports a menu item (<see cref="IMenuItem"/>)
	/// </summary>
	[MetadataAttribute, AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
	public sealed class ExportMenuItemAttribute : ExportAttribute, IMenuItemMetadata {
		/// <summary>Constructor</summary>
		public ExportMenuItemAttribute()
			: base(typeof(IMenuItem)) {
		}

		/// <summary>
		/// Guid of owner menu or menu item. <c>null</c> if it's a context menu (<see cref="MenuConstants.CTX_MENU_GUID"/>)
		/// </summary>
		public string OwnerGuid { get; set; }

		/// <summary>
		/// Guid of this item or null if it can't contain any child items
		/// </summary>
		public string Guid { get; set; }

		/// <summary>
		/// Group name, must be of the format "order,name" where order is a decimal number and the
		/// order of the group in this menu.
		/// </summary>
		public string Group { get; set; }

		/// <summary>
		/// Order within the menu group (<see cref="Group"/>)
		/// </summary>
		public double Order { get; set; }

		/// <summary>
		/// (Optional) menu item header property value. If not set, you should implement
		/// <see cref="IMenuItem2"/>
		/// </summary>
		public string Header { get; set; }

		/// <summary>
		/// (Optional) menu item input gesture text property value. If not set, you should implement
		/// <see cref="IMenuItem2"/>
		/// </summary>
		public string InputGestureText { get; set; }

		/// <summary>
		/// (Optional) icon name. If not set, you should implement <see cref="IMenuItem2"/>
		/// </summary>
		public string Icon { get; set; }
	}
}
