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
using System.ComponentModel.Composition;

namespace dnSpy.Contracts.ToolBars {
	/// <summary>
	/// Exports a toolbar button (<see cref="IToolBarButton"/>)
	/// </summary>
	[MetadataAttribute, AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
	public sealed class ExportToolBarButtonAttribute : ExportToolBarItemAttribute, IToolBarButtonMetadata {
		/// <summary>Constructor</summary>
		public ExportToolBarButtonAttribute()
			: base(typeof(IToolBarButton)) {
		}

		/// <summary>
		/// (Optional) toolbar button header property value. If not set, you can implement
		/// <see cref="IToolBarButton2"/>
		/// </summary>
		public string Header { get; set; }

		/// <summary>
		/// (Optional) icon name. If not set, you must implement <see cref="IToolBarButton2"/>
		/// </summary>
		public string Icon { get; set; }

		/// <summary>
		/// (Optional) tooltip. If not set, you can implement <see cref="IToolBarButton2"/>
		/// </summary>
		public string ToolTip { get; set; }

		/// <summary>
		/// true if it's a toggle button. If true, you must implement <see cref="IToolBarToggleButton"/>.
		/// </summary>
		public bool IsToggleButton { get; set; }
	}
}
