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

namespace dnSpy.Contracts.ToolBars {
	/// <summary>
	/// ToolBar export attribute base class
	/// </summary>
	public abstract class ExportToolBarItemAttribute : ExportAttribute, IToolBarItemMetadata {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="contractType">Contract type</param>
		protected ExportToolBarItemAttribute(Type contractType)
			: base(contractType) {
		}

		/// <summary>
		/// Guid of owner toolbar or null to use <see cref="ToolBarConstants.APP_TOOLBAR_GUID"/>
		/// </summary>
		public string OwnerGuid { get; set; }

		/// <summary>
		/// Group name, must be of the format "order,name" where order is a decimal number and the
		/// order of the group in this toolbar.
		/// </summary>
		public string Group { get; set; }

		/// <summary>
		/// Order within the toolbar group (<see cref="Group"/>)
		/// </summary>
		public double Order { get; set; }
	}
}
