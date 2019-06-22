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
using System.ComponentModel.Composition;
using dnSpy.Contracts.Images;

namespace dnSpy.Contracts.Menus {
	/// <summary>
	/// A menu item command
	/// </summary>
	public interface IMenuItem {
		/// <summary>
		/// Returns true if the menu item is visible in the menu
		/// </summary>
		/// <param name="context">Context</param>
		/// <returns></returns>
		bool IsVisible(IMenuItemContext context);

		/// <summary>
		/// Returns true if the menu item is enabled and its <see cref="Execute(IMenuItemContext)"/>
		/// method can be called.
		/// </summary>
		/// <param name="context">Context</param>
		/// <returns></returns>
		bool IsEnabled(IMenuItemContext context);

		/// <summary>
		/// Executes the command
		/// </summary>
		/// <param name="context">Context</param>
		void Execute(IMenuItemContext context);

		/// <summary>
		/// Gets the menu item header or null if the default header from the attribute should be
		/// used.
		/// </summary>
		/// <param name="context">Context</param>
		/// <returns></returns>
		string? GetHeader(IMenuItemContext context);

		/// <summary>
		/// Gets the menu item input gesture text or null if the default input gesture text from the
		/// attribute should be used.
		/// </summary>
		/// <param name="context">Context</param>
		/// <returns></returns>
		string? GetInputGestureText(IMenuItemContext context);

		/// <summary>
		/// Gets the menu item icon or null if the default icon from the attribute should be used.
		/// </summary>
		/// <param name="context">Context</param>
		/// <returns></returns>
		ImageReference? GetIcon(IMenuItemContext context);

		/// <summary>
		/// Returns true if the menu item is checked
		/// </summary>
		/// <param name="context">Context</param>
		/// <returns></returns>
		bool IsChecked(IMenuItemContext context);
	}

	/// <summary>Metadata</summary>
	public interface IMenuItemMetadata {
		/// <summary>See <see cref="ExportMenuItemAttribute.OwnerGuid"/></summary>
		string? OwnerGuid { get; }
		/// <summary>See <see cref="ExportMenuItemAttribute.Guid"/></summary>
		string? Guid { get; }
		/// <summary>See <see cref="ExportMenuItemAttribute.Group"/></summary>
		string? Group { get; }
		/// <summary>See <see cref="ExportMenuItemAttribute.Order"/></summary>
		double Order { get; }
		/// <summary>See <see cref="ExportMenuItemAttribute.Header"/></summary>
		string? Header { get; }
		/// <summary>See <see cref="ExportMenuItemAttribute.InputGestureText"/></summary>
		string? InputGestureText { get; }
		/// <summary>See <see cref="ExportMenuItemAttribute.Icon"/></summary>
		string? Icon { get; }
	}

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
		public string? OwnerGuid { get; set; }

		/// <summary>
		/// Guid of this item or null if it can't contain any child items
		/// </summary>
		public string? Guid { get; set; }

		/// <summary>
		/// Group name, must be of the format "order,name" where order is a decimal number and the
		/// order of the group in this menu.
		/// </summary>
		public string? Group { get; set; }

		/// <summary>
		/// Order within the menu group (<see cref="Group"/>)
		/// </summary>
		public double Order { get; set; }

		/// <summary>
		/// Menu item header property value
		/// </summary>
		public string? Header { get; set; }

		/// <summary>
		/// Menu item input gesture text property value
		/// </summary>
		public string? InputGestureText { get; set; }

		/// <summary>
		/// Icon name
		/// </summary>
		public string? Icon { get; set; }
	}
}
