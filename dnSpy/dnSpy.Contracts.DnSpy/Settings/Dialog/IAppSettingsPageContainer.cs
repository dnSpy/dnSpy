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

namespace dnSpy.Contracts.Settings.Dialog {
	/// <summary>
	/// Creates an empty app settings page that only contains children settings pages.
	/// If it has zero children, it won't be shown in the dialog box. Use
	/// <see cref="ExportAppSettingsPageContainerAttribute"/> to export an instance.
	/// </summary>
	public interface IAppSettingsPageContainer {
	}

	/// <summary>Metadata</summary>
	public interface IAppSettingsPageContainerMetadata {
		/// <summary>See <see cref="ExportAppSettingsPageContainerAttribute.ParentGuid"/></summary>
		string? ParentGuid { get; }
		/// <summary>See <see cref="ExportAppSettingsPageContainerAttribute.Guid"/></summary>
		string Guid { get; }
		/// <summary>See <see cref="ExportAppSettingsPageContainerAttribute.Order"/></summary>
		double Order { get; }
		/// <summary>See <see cref="ExportAppSettingsPageContainerAttribute.Title"/></summary>
		string Title { get; }
		/// <summary>See <see cref="ExportAppSettingsPageContainerAttribute.Icon"/></summary>
		string? Icon { get; }
	}

	/// <summary>
	/// Exports a <see cref="IAppSettingsPageContainer"/> instance
	/// </summary>
	[MetadataAttribute, AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
	public sealed class ExportAppSettingsPageContainerAttribute : ExportAttribute, IAppSettingsPageContainerMetadata {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="title">Title shown in the UI</param>
		/// <param name="guid">Unique <see cref="System.Guid"/> of this app settings instance</param>
		/// <param name="order">Order of this instance relative to other instances with the same parent, eg. <see cref="AppSettingsConstants.ORDER_DECOMPILER"/></param>
		/// <param name="parentGuid"><see cref="System.Guid"/> of the parent or null if the root element is the parent, eg. <see cref="AppSettingsConstants.GUID_DECOMPILER"/></param>
		/// <param name="icon">Icon shown in the UI or null</param>
		public ExportAppSettingsPageContainerAttribute(string title, string guid, double order = double.MaxValue, string? parentGuid = null, string? icon = null)
			: base(typeof(IAppSettingsPageContainer)) {
			ParentGuid = parentGuid;
			Guid = guid ?? throw new ArgumentNullException(nameof(guid));
			Order = order;
			Title = title ?? throw new ArgumentNullException(nameof(title));
			Icon = icon;
		}

		/// <summary>
		/// Parent <see cref="System.Guid"/> or <see cref="System.Guid.Empty"/> if the root element is the parent
		/// </summary>
		public string? ParentGuid { get; }

		/// <summary>
		/// Gets the <see cref="System.Guid"/>
		/// </summary>
		public string Guid { get; }

		/// <summary>
		/// Gets the order, eg. <see cref="AppSettingsConstants.ORDER_DECOMPILER"/>
		/// </summary>
		public double Order { get; }

		/// <summary>
		/// Gets the title shown in the UI
		/// </summary>
		public string Title { get; }

		/// <summary>
		/// Gets the icon shown in the UI or null
		/// </summary>
		public string? Icon { get; }
	}
}
