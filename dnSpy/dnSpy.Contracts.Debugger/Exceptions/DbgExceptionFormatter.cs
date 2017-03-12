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
using dnSpy.Contracts.Debugger.Text;

namespace dnSpy.Contracts.Debugger.Exceptions {
	/// <summary>
	/// Formats exceptions in the Exception Settings window. Use <see cref="ExportDbgExceptionFormatterAttribute"/>
	/// to export an instance.
	/// </summary>
	public abstract class DbgExceptionFormatter {
		/// <summary>
		/// Writes the exception name. Returns true if it wrote the name.
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="definition">Exception definition</param>
		/// <returns></returns>
		public virtual bool WriteName(IDebugOutputWriter writer, DbgExceptionDefinition definition) => false;
	}

	/// <summary>Metadata</summary>
	public interface IDbgExceptionFormatterMetadata {
		/// <summary>See <see cref="ExportDbgExceptionFormatterAttribute.Order"/></summary>
		double Order { get; }
		/// <summary>See <see cref="ExportDbgExceptionFormatterAttribute.Group"/></summary>
		string Group { get; }
	}

	/// <summary>
	/// Exports a <see cref="DbgExceptionFormatter"/> instance
	/// </summary>
	[MetadataAttribute, AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
	public sealed class ExportDbgExceptionFormatterAttribute : ExportAttribute, IDbgExceptionFormatterMetadata {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="group">Group name, see <see cref="PredefinedExceptionGroups"/></param>
		/// <param name="order">Order</param>
		public ExportDbgExceptionFormatterAttribute(string group, double order = double.MaxValue)
			: base(typeof(DbgExceptionFormatter)) {
			Group = group ?? throw new ArgumentNullException(nameof(group));
			Order = order;
		}

		/// <summary>
		/// Group name, see <see cref="PredefinedExceptionGroups"/>
		/// </summary>
		public string Group { get; }

		/// <summary>
		/// Order
		/// </summary>
		public double Order { get; }
	}
}
