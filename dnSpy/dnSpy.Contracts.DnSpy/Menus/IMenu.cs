/*
    Copyright (C) 2014-2017 de4dot@gmail.com

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
	/// A menu
	/// </summary>
	public interface IMenu {
	}

	/// <summary>Metadata</summary>
	public interface IMenuMetadata {
		/// <summary>See <see cref="ExportMenuAttribute.OwnerGuid"/></summary>
		string OwnerGuid { get; }
		/// <summary>See <see cref="ExportMenuAttribute.Guid"/></summary>
		string Guid { get; }
		/// <summary>See <see cref="ExportMenuAttribute.Order"/></summary>
		double Order { get; }
		/// <summary>See <see cref="ExportMenuAttribute.Header"/></summary>
		string Header { get; }
	}

	/// <summary>
	/// Exports a menu (<see cref="IMenu"/>)
	/// </summary>
	[MetadataAttribute, AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
	public sealed class ExportMenuAttribute : ExportAttribute, IMenuMetadata {
		/// <summary>Constructor</summary>
		public ExportMenuAttribute()
			: base(typeof(IMenu)) {
		}

		/// <summary>
		/// Guid of menu or null to use <see cref="MenuConstants.APP_MENU_GUID"/>
		/// </summary>
		public string OwnerGuid { get; set; }

		/// <summary>
		/// Guid of this item
		/// </summary>
		public string Guid { get; set; }

		/// <summary>
		/// Order within the menu, eg. <see cref="MenuConstants.ORDER_APP_MENU_FILE"/>
		/// </summary>
		public double Order { get; set; }

		/// <summary>
		/// Menu header, eg. "_File"
		/// </summary>
		public string Header { get; set; }
	}
}
