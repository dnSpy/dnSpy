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
	/// Creates <see cref="IFileTabContent"/> instances. Use <see cref="ExportReferenceFileTabContentCreatorAttribute"/>
	/// to export an instance.
	/// </summary>
	public interface IReferenceFileTabContentCreator {
		/// <summary>
		/// Creates a new <see cref="FileTabReferenceResult"/> or returns null
		/// </summary>
		/// <param name="fileTabManager">Owner</param>
		/// <param name="sourceContent">Source content or null. It's used when showing the reference
		/// in a new tab. This would then be the older tab's content.</param>
		/// <param name="ref">Reference</param>
		/// <returns></returns>
		FileTabReferenceResult Create(IFileTabManager fileTabManager, IFileTabContent sourceContent, object @ref);
	}

	/// <summary>Metadata</summary>
	public interface IReferenceFileTabContentCreatorMetadata {
		/// <summary>See <see cref="ExportReferenceFileTabContentCreatorAttribute.Order"/></summary>
		double Order { get; }
	}

	/// <summary>
	/// Exports a <see cref="IReferenceFileTabContentCreator"/> instance
	/// </summary>
	[MetadataAttribute, AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
	public sealed class ExportReferenceFileTabContentCreatorAttribute : ExportAttribute, IReferenceFileTabContentCreatorMetadata {
		/// <summary>Constructor</summary>
		public ExportReferenceFileTabContentCreatorAttribute()
			: base(typeof(IReferenceFileTabContentCreator)) {
			Order = double.MaxValue;
		}

		/// <summary>
		/// Order of this instance
		/// </summary>
		public double Order { get; set; }
	}
}
