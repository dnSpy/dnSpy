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
using System.Collections.Generic;
using System.ComponentModel.Composition;
using dnSpy.Contracts.Settings;

namespace dnSpy.Contracts.App {
	/// <summary>
	/// Called at startup and exit. Use <see cref="ExportDnSpyLoaderAttribute"/> to export an
	/// instance.
	/// </summary>
	public interface IDnSpyLoader {
		/// <summary>
		/// Called when dnSpy exits
		/// </summary>
		/// <param name="settingsManager">Settings manager</param>
		void Save(ISettingsManager settingsManager);

		/// <summary>
		/// Called when dnSpy has just started. If the method takes too long to execute, give control
		/// back to dnSpy by using yield return. Only values in <see cref="LoaderConstants"/>
		/// are used by the loader, anything else is ignored.
		/// </summary>
		/// <param name="settingsManager">Settings manager</param>
		/// <returns></returns>
		IEnumerable<object> Load(ISettingsManager settingsManager);

		/// <summary>
		/// Called when everything has been loaded
		/// </summary>
		void OnAppLoaded();
	}

	/// <summary>Metadata</summary>
	public interface IDnSpyLoaderMetadata {
		/// <summary>See <see cref="ExportDnSpyLoaderAttribute.Order"/></summary>
		double Order { get; }
	}

	/// <summary>
	/// Exports a <see cref="IDnSpyLoader"/> instance
	/// </summary>
	[MetadataAttribute, AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
	public sealed class ExportDnSpyLoaderAttribute : ExportAttribute, IDnSpyLoaderMetadata {
		/// <summary>Constructor</summary>
		public ExportDnSpyLoaderAttribute()
			: base(typeof(IDnSpyLoader)) {
			Order = double.MaxValue;
		}

		/// <summary>
		/// Order of this instance
		/// </summary>
		public double Order { get; set; }
	}
}
