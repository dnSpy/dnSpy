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

namespace dnSpy.Contracts.Extension {
	/// <summary>
	/// All extensions should export exactly one type that implements this interface. Use
	/// <see cref="ExportExtensionAttribute"/> to export a extension.
	/// </summary>
	public interface IExtension {
		/// <summary>
		/// Called at various times
		/// </summary>
		/// <param name="event">Event</param>
		/// <param name="obj">Data, see <see cref="ExtensionEvent"/></param>
		void OnEvent(ExtensionEvent @event, object obj);

		/// <summary>
		/// Gets the <see cref="Extension.ExtensionInfo"/> instance
		/// </summary>
		ExtensionInfo ExtensionInfo { get; }

		/// <summary>
		/// Gets relative paths of all resource dictionaries that will be added to the app's resources
		/// </summary>
		IEnumerable<string> MergedResourceDictionaries { get; }
	}

	/// <summary>Metadata</summary>
	public interface IExtensionMetadata {
		/// <summary>See <see cref="ExportExtensionAttribute.Order"/></summary>
		double Order { get; }
	}

	/// <summary>
	/// Exports a <see cref="IExtension"/> instance
	/// </summary>
	[MetadataAttribute, AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
	public sealed class ExportExtensionAttribute : ExportAttribute, IExtensionMetadata {
		/// <summary>Constructor</summary>
		public ExportExtensionAttribute()
			: base(typeof(IExtension)) {
			Order = double.MaxValue;
		}

		/// <summary>
		/// Order of this instance
		/// </summary>
		public double Order { get; set; }
	}
}
