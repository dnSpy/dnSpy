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

namespace dnSpy.Contracts.Files.Tabs {
	/// <summary>
	/// Can cancel loading file lists. Use <see cref="ExportFileListListenerAttribute"/> to export
	/// an instance.
	/// </summary>
	public interface IFileListListener {
		/// <summary>
		/// true if we can load a new file list
		/// </summary>
		bool CanLoad { get; }

		/// <summary>
		/// true if we can reload the current file list
		/// </summary>
		bool CanReload { get; }

		/// <summary>
		/// Called before a new file list is loaded
		/// </summary>
		/// <param name="isReload">true if it's a reload, false if it's a load</param>
		void BeforeLoad(bool isReload);

		/// <summary>
		/// Called after a new file list has been loaded
		/// </summary>
		/// <param name="isReload">true if it's a reload, false if it's a load</param>
		void AfterLoad(bool isReload);
	}

	/// <summary>Metadata</summary>
	public interface IFileListListenerMetadata {
		/// <summary>See <see cref="ExportFileListListenerAttribute.Order"/></summary>
		double Order { get; }
	}

	/// <summary>
	/// Exports a <see cref="IFileListListener"/> instance
	/// </summary>
	[MetadataAttribute, AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
	public sealed class ExportFileListListenerAttribute : ExportAttribute, IFileListListenerMetadata {
		/// <summary>Constructor</summary>
		public ExportFileListListenerAttribute()
			: base(typeof(IFileListListener)) {
		}

		/// <summary>
		/// Order of this instance
		/// </summary>
		public double Order { get; set; }
	}
}
