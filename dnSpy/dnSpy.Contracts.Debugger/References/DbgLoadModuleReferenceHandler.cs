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
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using dnSpy.Contracts.Documents;

namespace dnSpy.Contracts.Debugger.References {
	/// <summary>
	/// Loads modules (eg. in the Assembly Explorer treeview). Use <see cref="ExportDbgLoadModuleReferenceHandlerAttribute"/>
	/// to export an instance.
	/// </summary>
	public abstract class DbgLoadModuleReferenceHandler {
		/// <summary>
		/// Returns true if it showed the reference, and false if the next handler should get called.
		/// This method is called on the UI thread.
		/// </summary>
		/// <param name="moduleRef">Module reference</param>
		/// <param name="options">Options, eg. <see cref="PredefinedReferenceNavigatorOptions"/></param>
		/// <returns></returns>
		public abstract bool GoTo(DbgLoadModuleReference moduleRef, ReadOnlyCollection<object> options);
	}

	/// <summary>Metadata</summary>
	public interface IDbgLoadModuleReferenceHandlerMetadata {
		/// <summary>See <see cref="ExportDbgLoadModuleReferenceHandlerAttribute.Order"/></summary>
		double Order { get; }
	}

	/// <summary>
	/// Exports a <see cref="DbgLoadModuleReferenceHandler"/> instance
	/// </summary>
	[MetadataAttribute, AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
	public sealed class ExportDbgLoadModuleReferenceHandlerAttribute : ExportAttribute, IDbgLoadModuleReferenceHandlerMetadata {
		/// <summary>Constructor</summary>
		public ExportDbgLoadModuleReferenceHandlerAttribute()
			: base(typeof(DbgLoadModuleReferenceHandler)) => Order = double.MaxValue;

		/// <summary>
		/// Order of this instance
		/// </summary>
		public double Order { get; set; }
	}
}
