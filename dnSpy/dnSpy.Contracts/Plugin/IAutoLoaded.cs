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

namespace dnSpy.Contracts.Plugin {
	/// <summary>
	/// All classes that export this type automatically get loaded at startup.
	/// Use <see cref="ExportAutoLoadedAttribute"/> to export it.
	/// </summary>
	public interface IAutoLoaded {
	}

	/// <summary>
	/// <see cref="IAutoLoaded"/> load type
	/// </summary>
	public enum AutoLoadedLoadType {
		/// <summary>
		/// Loaded before plugins are created
		/// </summary>
		BeforePlugins,

		/// <summary>
		/// Loaded after plugins have been created
		/// </summary>
		AfterPlugins,

		/// <summary>
		/// Loaded after all plugins have been created and loaded
		/// </summary>
		AfterPluginsLoaded,

		/// <summary>
		/// Loaded when the app has been loaded
		/// </summary>
		AppLoaded,
	}

	/// <summary>Metadata</summary>
	public interface IAutoLoadedMetadata {
		/// <summary>See <see cref="ExportAutoLoadedAttribute.LoadType"/></summary>
		AutoLoadedLoadType LoadType { get; }
		/// <summary>See <see cref="ExportAutoLoadedAttribute.Order"/></summary>
		double Order { get; }
	}

	/// <summary>
	/// Exports a <see cref="IAutoLoaded"/> instance
	/// </summary>
	[MetadataAttribute, AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
	public sealed class ExportAutoLoadedAttribute : ExportAttribute, IAutoLoadedMetadata {
		/// <summary>Constructor</summary>
		public ExportAutoLoadedAttribute()
			: base(typeof(IAutoLoaded)) {
			LoadType = AutoLoadedLoadType.AppLoaded;
		}

		/// <summary>
		/// Order of this instance
		/// </summary>
		public double Order { get; set; }

		/// <summary>
		/// Default is <see cref="AutoLoadedLoadType.AppLoaded"/>
		/// </summary>
		public AutoLoadedLoadType LoadType { get; set; }
	}
}
