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

namespace dnSpy.Contracts.Documents.Tabs.DocViewer {
	/// <summary>
	/// Uses custom data created by the decompiler and transforms it to some other data. Use
	/// <see cref="ExportDocumentViewerCustomDataProviderAttribute"/> to export an instance.
	/// </summary>
	public interface IDocumentViewerCustomDataProvider {
		/// <summary>
		/// Gets called to create data that gets stored in an <see cref="DocumentViewerContent"/> instance
		/// </summary>
		/// <param name="context">Context</param>
		void OnCustomData(IDocumentViewerCustomDataContext context);
	}

	/// <summary>Metadata</summary>
	public interface IDocumentViewerCustomDataProviderMetadata {
		/// <summary>See <see cref="ExportDocumentViewerCustomDataProviderAttribute.Order"/></summary>
		double Order { get; }
	}

	/// <summary>
	/// Exports a <see cref="IDocumentViewerCustomDataProvider"/> instance
	/// </summary>
	[MetadataAttribute, AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
	public sealed class ExportDocumentViewerCustomDataProviderAttribute : ExportAttribute, IDocumentViewerCustomDataProviderMetadata {
		/// <summary>Constructor</summary>
		public ExportDocumentViewerCustomDataProviderAttribute()
			: this(double.MaxValue) {
		}

		/// <summary>Constructor</summary>
		/// <param name="order">Order of this instance</param>
		public ExportDocumentViewerCustomDataProviderAttribute(double order)
			: base(typeof(IDocumentViewerCustomDataProvider)) {
			Order = order;
		}

		/// <summary>
		/// Order of this instance
		/// </summary>
		public double Order { get; set; }
	}
}
