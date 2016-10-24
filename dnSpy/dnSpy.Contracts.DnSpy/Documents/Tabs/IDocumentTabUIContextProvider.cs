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

namespace dnSpy.Contracts.Documents.Tabs {
	/// <summary>
	/// Creates <see cref="DocumentTabUIContext"/> instances. Use <see cref="ExportDocumentTabUIContextProviderAttribute"/>
	/// to export an instance.
	/// </summary>
	public interface IDocumentTabUIContextProvider {
		/// <summary>
		/// Creates a new <see cref="DocumentTabUIContext"/> instance or returns null if someone else
		/// should create it.
		/// </summary>
		/// <typeparam name="T">Type</typeparam>
		/// <returns></returns>
		DocumentTabUIContext Create<T>() where T : class;
	}

	/// <summary>Metadata</summary>
	public interface IDocumentTabUIContextProviderMetadata {
		/// <summary>See <see cref="ExportDocumentTabUIContextProviderAttribute.Order"/></summary>
		double Order { get; }
		/// <summary>See <see cref="ExportDocumentTabUIContextProviderAttribute.UseStrongReference"/></summary>
		bool UseStrongReference { get; }
	}

	/// <summary>
	/// Exports a <see cref="IDocumentTabUIContextProvider"/> instance
	/// </summary>
	[MetadataAttribute, AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
	public sealed class ExportDocumentTabUIContextProviderAttribute : ExportAttribute, IDocumentTabUIContextProviderMetadata {
		/// <summary>Constructor</summary>
		public ExportDocumentTabUIContextProviderAttribute()
			: base(typeof(IDocumentTabUIContextProvider)) {
		}

		/// <summary>
		/// Order of this instance
		/// </summary>
		public double Order { get; set; }

		/// <summary>
		/// true to store the created instance in a strong reference
		/// </summary>
		public bool UseStrongReference { get; set; }
	}
}
