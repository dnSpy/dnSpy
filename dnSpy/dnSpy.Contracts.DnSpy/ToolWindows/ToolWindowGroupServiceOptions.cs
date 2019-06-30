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
using dnSpy.Contracts.Menus;

namespace dnSpy.Contracts.ToolWindows {
	/// <summary>
	/// <see cref="IToolWindowService"/> options
	/// </summary>
	public sealed class ToolWindowGroupServiceOptions {
		/// <summary>
		/// A style or a resource key or null to use the default style
		/// </summary>
		public object? TabControlStyle { get; set; }

		/// <summary>
		/// A style or a resource key or null to use the default style
		/// </summary>
		public object? TabItemStyle { get; set; }

		/// <summary>
		/// Tool window group guid, eg. <see cref="MenuConstants.GUIDOBJ_TOOLWINDOW_TABCONTROL_GUID"/>
		/// </summary>
		public Guid ToolWindowGroupGuid { get; set; }

		/// <summary>
		/// Default constructor
		/// </summary>
		public ToolWindowGroupServiceOptions() {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="groupGuid">See <see cref="ToolWindowGroupGuid"/></param>
		public ToolWindowGroupServiceOptions(string groupGuid)
			: this(new Guid(groupGuid)) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="groupGuid">See <see cref="ToolWindowGroupGuid"/></param>
		public ToolWindowGroupServiceOptions(Guid groupGuid) => ToolWindowGroupGuid = groupGuid;

		/// <summary>
		/// Clones this instance
		/// </summary>
		/// <returns></returns>
		public ToolWindowGroupServiceOptions Clone() => new ToolWindowGroupServiceOptions {
			TabControlStyle = TabControlStyle,
			TabItemStyle = TabItemStyle,
			ToolWindowGroupGuid = ToolWindowGroupGuid,
		};
	}
}
