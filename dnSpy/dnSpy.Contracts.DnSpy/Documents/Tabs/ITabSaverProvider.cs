/*
    Copyright (C) 2014-2017 de4dot@gmail.com

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

namespace dnSpy.Contracts.Documents.Tabs {
	/// <summary>
	/// Creates <see cref="ITabSaver"/> instances. Use <see cref="ExportTabSaverProviderAttribute"/>
	/// to export an instance.
	/// </summary>
	public interface ITabSaverProvider {
		/// <summary>
		/// Creates a <see cref="ITabSaver"/> instance or returns null
		/// </summary>
		/// <param name="tab">Tab</param>
		/// <returns></returns>
		ITabSaver Create(IDocumentTab tab);
	}

	/// <summary>Metadata</summary>
	public interface ITabSaverProviderMetadata {
		/// <summary>See <see cref="ExportTabSaverProviderAttribute.Order"/></summary>
		double Order { get; }
	}

	/// <summary>
	/// Exports a <see cref="ITabSaverProvider"/> instance
	/// </summary>
	[MetadataAttribute, AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
	public sealed class ExportTabSaverProviderAttribute : ExportAttribute, ITabSaverProviderMetadata {
		/// <summary>Constructor</summary>
		public ExportTabSaverProviderAttribute()
			: base(typeof(ITabSaverProvider)) {
		}

		/// <summary>
		/// Order of this instance
		/// </summary>
		public double Order { get; set; }
	}
}
