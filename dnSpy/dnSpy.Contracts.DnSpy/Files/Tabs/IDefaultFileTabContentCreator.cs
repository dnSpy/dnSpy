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
	/// Creates default file tab content. Use <see cref="ExportDefaultFileTabContentCreatorAttribute"/>
	/// to export an instance.
	/// </summary>
	public interface IDefaultFileTabContentCreator {
		/// <summary>
		/// Creates default content or returns null
		/// </summary>
		/// <param name="fileTabManager">Owner</param>
		/// <returns></returns>
		IFileTabContent Create(IFileTabManager fileTabManager);
	}

	/// <summary>Metadata</summary>
	public interface IDefaultFileTabContentCreatorMetadata {
		/// <summary>See <see cref="ExportDefaultFileTabContentCreatorAttribute.Order"/></summary>
		double Order { get; }
	}

	/// <summary>
	/// Exports a <see cref="IDefaultFileTabContentCreator"/> instance
	/// </summary>
	[MetadataAttribute, AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
	public sealed class ExportDefaultFileTabContentCreatorAttribute : ExportAttribute, IDefaultFileTabContentCreatorMetadata {
		/// <summary>Constructor</summary>
		public ExportDefaultFileTabContentCreatorAttribute()
			: base(typeof(IDefaultFileTabContentCreator)) {
			Order = double.MaxValue;
		}

		/// <summary>
		/// Order of this instance, eg. <see cref="DefaultFileTabContentCreatorConstants.DEFAULT_HANDLER"/>
		/// </summary>
		public double Order { get; set; }
	}
}
