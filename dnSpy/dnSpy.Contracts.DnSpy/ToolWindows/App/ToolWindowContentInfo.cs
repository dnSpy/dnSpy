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

namespace dnSpy.Contracts.ToolWindows.App {
	/// <summary>
	/// <see cref="ToolWindowContent"/> info
	/// </summary>
	public struct ToolWindowContentInfo {
		/// <summary>
		/// Guid of <see cref="ToolWindowContent"/>
		/// </summary>
		public Guid Guid { get; set; }

		/// <summary>
		/// Location
		/// </summary>
		public AppToolWindowLocation Location { get; set; }

		/// <summary>
		/// Order, used if <see cref="IsDefault"/> is true
		/// </summary>
		public double Order { get; set; }

		/// <summary>
		/// true if it's shown the first time dnSpy loads
		/// </summary>
		public bool IsDefault { get; set; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="guid">Guid</param>
		/// <param name="location">Location</param>
		/// <param name="order">Order</param>
		/// <param name="isDefault">true if default</param>
		public ToolWindowContentInfo(Guid guid, AppToolWindowLocation location = AppToolWindowLocation.DefaultHorizontal, double order = double.MaxValue, bool isDefault = false) {
			Guid = guid;
			Location = location;
			Order = order;
			IsDefault = isDefault;
		}
	}
}
