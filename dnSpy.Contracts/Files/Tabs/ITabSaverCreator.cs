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

namespace dnSpy.Contracts.Files.Tabs {
	/// <summary>
	/// Creates <see cref="ITabSaver"/> instances. Use <see cref="ExportTabSaverCreatorAttribute"/>
	/// to export an instance.
	/// </summary>
	public interface ITabSaverCreator {
		/// <summary>
		/// Creates a <see cref="ITabSaver"/> instance or returns null
		/// </summary>
		/// <param name="tab">Tab</param>
		/// <returns></returns>
		ITabSaver Create(IFileTab tab);
	}

	/// <summary>Metadata</summary>
	public interface ITabSaverCreatorMetadata {
		/// <summary>See <see cref="ExportTabSaverCreatorAttribute.Order"/></summary>
		double Order { get; }
	}

	/// <summary>
	/// Exports a <see cref="ITabSaverCreator"/> instance
	/// </summary>
	[MetadataAttribute, AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
	public sealed class ExportTabSaverCreatorAttribute : ExportAttribute, ITabSaverCreatorMetadata {
		/// <summary>Constructor</summary>
		public ExportTabSaverCreatorAttribute()
			: base(typeof(ITabSaverCreator)) {
		}

		/// <summary>
		/// Order of this instance
		/// </summary>
		public double Order { get; set; }
	}
}
