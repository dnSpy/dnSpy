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

namespace dnSpy.Contracts.Settings.Dialog {
	/// <summary>
	/// Creates <see cref="IAppSettingsTab"/> instances. Use <see cref="ExportAppSettingsTabCreatorAttribute"/>
	/// to export an instance.
	/// </summary>
	public interface IAppSettingsTabCreator {
		/// <summary>
		/// Creates a new <see cref="IAppSettingsTab"/> instance
		/// </summary>
		/// <returns></returns>
		IAppSettingsTab Create();
	}

	/// <summary>Metadata</summary>
	public interface IAppSettingsTabCreatorMetadata {
		/// <summary>See <see cref="ExportAppSettingsTabCreatorAttribute.Order"/></summary>
		double Order { get; }
	}

	/// <summary>
	/// Exports a <see cref="IAppSettingsTabCreator"/> instance
	/// </summary>
	[MetadataAttribute, AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
	public sealed class ExportAppSettingsTabCreatorAttribute : ExportAttribute, IAppSettingsTabCreatorMetadata {
		/// <summary>Constructor</summary>
		public ExportAppSettingsTabCreatorAttribute()
			: base(typeof(IAppSettingsTabCreator)) {
			Order = double.MaxValue;
		}

		/// <summary>
		/// Order of this instance
		/// </summary>
		public double Order { get; set; }
	}
}
