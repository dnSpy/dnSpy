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

namespace dnSpy.Contracts.Debugger {
	/// <summary>
	/// This class gets created the first time <see cref="DbgManager.Start(StartDebuggingOptions)"/>
	/// gets called. Use <see cref="ExportDbgManagerStartListenerAttribute"/> to export an instance.
	/// </summary>
	public interface IDbgManagerStartListener {
		/// <summary>
		/// Called the first time <see cref="DbgManager.Start(StartDebuggingOptions)"/> gets called.
		/// The code has a chance to hook events and do other initialization before a program
		/// gets debugged.
		/// </summary>
		/// <param name="dbgManager">Debug manager instance</param>
		void OnStart(DbgManager dbgManager);
	}

	/// <summary>Metadata</summary>
	public interface IDbgManagerStartListenerMetadata {
		/// <summary>See <see cref="ExportDbgManagerStartListenerAttribute.Order"/></summary>
		double Order { get; }
	}

	/// <summary>
	/// Exports an <see cref="IDbgManagerStartListener"/> instance
	/// </summary>
	[MetadataAttribute, AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
	public sealed class ExportDbgManagerStartListenerAttribute : ExportAttribute, IDbgManagerStartListenerMetadata {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="order">Order</param>
		public ExportDbgManagerStartListenerAttribute(double order = double.MaxValue)
			: base(typeof(IDbgManagerStartListener)) => Order = order;

		/// <summary>
		/// Order
		/// </summary>
		public double Order { get; }
	}
}
