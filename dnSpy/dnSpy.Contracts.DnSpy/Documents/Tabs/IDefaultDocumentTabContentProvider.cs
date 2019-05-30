/*
    Copyright (C) 2014-2019 de4dot@gmail.com

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
	/// Creates default document tab content. Use <see cref="ExportDefaultDocumentTabContentProviderAttribute"/>
	/// to export an instance.
	/// </summary>
	public interface IDefaultDocumentTabContentProvider {
		/// <summary>
		/// Creates default content or returns null
		/// </summary>
		/// <param name="documentTabService">Owner</param>
		/// <returns></returns>
		DocumentTabContent? Create(IDocumentTabService documentTabService);
	}

	/// <summary>Metadata</summary>
	public interface IDefaultDocumentTabContentProviderMetadata {
		/// <summary>See <see cref="ExportDefaultDocumentTabContentProviderAttribute.Order"/></summary>
		double Order { get; }
	}

	/// <summary>
	/// Exports a <see cref="IDefaultDocumentTabContentProvider"/> instance
	/// </summary>
	[MetadataAttribute, AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
	public sealed class ExportDefaultDocumentTabContentProviderAttribute : ExportAttribute, IDefaultDocumentTabContentProviderMetadata {
		/// <summary>Constructor</summary>
		public ExportDefaultDocumentTabContentProviderAttribute()
			: base(typeof(IDefaultDocumentTabContentProvider)) => Order = double.MaxValue;

		/// <summary>
		/// Order of this instance, eg. <see cref="DefaultDocumentTabContentProviderConstants.DEFAULT_HANDLER"/>
		/// </summary>
		public double Order { get; set; }
	}
}
