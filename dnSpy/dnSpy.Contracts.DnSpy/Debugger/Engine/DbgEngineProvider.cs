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

namespace dnSpy.Contracts.Debugger.Engine {
	/// <summary>
	/// Creates <see cref="DbgEngine"/> instances. Use <see cref="ExportDbgEngineProviderAttribute"/>
	/// to export an instance.
	/// </summary>
	public abstract class DbgEngineProvider {
		/// <summary>
		/// Creates a <see cref="DbgEngine"/> and starts debugging or returns null if
		/// <paramref name="options"/> is unsupported
		/// </summary>
		/// <param name="options">Options</param>
		/// <returns></returns>
		public abstract DbgEngine Start(StartDebuggingOptions options);
	}

	/// <summary>Metadata</summary>
	public interface IDbgEngineProviderMetadata {
		/// <summary>See <see cref="ExportDbgEngineProviderAttribute.Order"/></summary>
		double Order { get; }
	}

	/// <summary>
	/// Exports a <see cref="DbgEngineProvider"/> instance
	/// </summary>
	[MetadataAttribute, AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
	public sealed class ExportDbgEngineProviderAttribute : ExportAttribute, IDbgEngineProviderMetadata {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="order">Order</param>
		public ExportDbgEngineProviderAttribute(double order = double.MaxValue)
			: base(typeof(DbgEngineProvider)) {
		}

		/// <summary>
		/// Order
		/// </summary>
		public double Order { get; }
	}
}
