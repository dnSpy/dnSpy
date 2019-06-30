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
using System.Collections.Generic;
using System.ComponentModel.Composition;

namespace dnSpy.Contracts.Debugger.Exceptions {
	/// <summary>
	/// Provides exception category definitions and exception definitions. Use <see cref="ExportDbgExceptionDefinitionProviderAttribute"/>
	/// to export an instance.
	/// </summary>
	public abstract class DbgExceptionDefinitionProvider {
		/// <summary>
		/// Gets exception files (*.ex.xml) that define exceptions and exception categories. If a relative filename
		/// is returned, it's relative to the assembly of the called type.
		/// There's no need to return files already in the debug directory.
		/// </summary>
		/// <returns></returns>
		public virtual IEnumerable<string> GetExceptionFilenames() {
			yield break;
		}

		/// <summary>
		/// Returns all exception category definitions
		/// </summary>
		/// <returns></returns>
		public virtual IEnumerable<DbgExceptionCategoryDefinition> CreateCategories() {
			yield break;
		}

		/// <summary>
		/// Returns all exception definitions
		/// </summary>
		/// <returns></returns>
		public virtual IEnumerable<DbgExceptionDefinition> Create() {
			yield break;
		}
	}

	/// <summary>Metadata</summary>
	public interface IDbgExceptionDefinitionProviderMetadata {
		/// <summary>See <see cref="ExportDbgExceptionDefinitionProviderAttribute.Order"/></summary>
		double Order { get; }
	}

	/// <summary>
	/// Exports a <see cref="DbgExceptionDefinitionProvider"/> instance
	/// </summary>
	[MetadataAttribute, AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
	public sealed class ExportDbgExceptionDefinitionProviderAttribute : ExportAttribute, IDbgExceptionDefinitionProviderMetadata {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="order">Order</param>
		public ExportDbgExceptionDefinitionProviderAttribute(double order = double.MaxValue)
			: base(typeof(DbgExceptionDefinitionProvider)) => Order = order;

		/// <summary>
		/// Order
		/// </summary>
		public double Order { get; }
	}
}
