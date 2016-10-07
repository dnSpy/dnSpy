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

namespace dnSpy.Contracts.Settings.Dialog {
	/// <summary>
	/// A tab with <see cref="ISimpleAppOption"/>s. Use <see cref="ExportDynamicAppSettingsTabAttribute"/>
	/// to export an instance.
	/// </summary>
	public interface IDynamicAppSettingsTab {
	}

	/// <summary>Metadata</summary>
	public interface IDynamicAppSettingsTabMetadata {
		/// <summary>See <see cref="ExportDynamicAppSettingsTabAttribute.Guid"/></summary>
		string Guid { get; }
		/// <summary>See <see cref="ExportDynamicAppSettingsTabAttribute.Order"/></summary>
		double Order { get; }
		/// <summary>See <see cref="ExportDynamicAppSettingsTabAttribute.Title"/></summary>
		string Title { get; }
	}

	/// <summary>
	/// Exports a <see cref="IDynamicAppSettingsTab"/> instance
	/// </summary>
	[MetadataAttribute, AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
	public sealed class ExportDynamicAppSettingsTabAttribute : ExportAttribute, IDynamicAppSettingsTabMetadata {
		/// <summary>Constructor</summary>
		public ExportDynamicAppSettingsTabAttribute()
			: base(typeof(IDynamicAppSettingsTab)) {
			Order = double.MaxValue;
		}

		/// <summary>
		/// Gets the guid, eg. <see cref="AppSettingsConstants.GUID_DYNTAB_MISC"/>
		/// </summary>
		public string Guid { get; set; }

		/// <summary>
		/// Gets the order, eg. <see cref="AppSettingsConstants.ORDER_TAB_DECOMPILER"/>
		/// </summary>
		public double Order { get; set; }

		/// <summary>
		/// Gets the title
		/// </summary>
		public string Title { get; set; }
	}
}
