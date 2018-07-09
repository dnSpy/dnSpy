/*
    Copyright (C) 2014-2018 de4dot@gmail.com

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
using System.Diagnostics;

namespace dnSpy.Contracts.Debugger.StartDebugging {
	/// <summary>
	/// Starts a process without debugging
	/// </summary>
	public abstract class DbgProcessStarter {
		/// <summary>
		/// Checks if this instance supports starting the executable
		/// </summary>
		/// <param name="filename">Filename</param>
		/// <returns></returns>
		public abstract bool IsSupported(string filename);

		/// <summary>
		/// Starts the executable. Returns false and an error message if it failed or throws an exception
		/// </summary>
		/// <param name="filename">Filename</param>
		/// <param name="error">Updated with an error message</param>
		/// <returns></returns>
		public abstract bool TryStart(string filename, out string error);
	}

	/// <summary>Metadata</summary>
	public interface IDbgProcessStarterMetadata {
		/// <summary>See <see cref="ExportDbgProcessStarterAttribute.Order"/></summary>
		double Order { get; }
	}

	/// <summary>
	/// Exports a <see cref="DbgProcessStarter"/> instance
	/// </summary>
	[MetadataAttribute, AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
	public sealed class ExportDbgProcessStarterAttribute : ExportAttribute, IDbgProcessStarterMetadata {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="order">Order, see <see cref="PredefinedDbgProcessStarterOrders"/></param>
		public ExportDbgProcessStarterAttribute(double order)
			: base(typeof(DbgProcessStarter)) => Order = order;

		/// <summary>
		/// Order
		/// </summary>
		public double Order { get; }
	}

	/// <summary>
	/// Predefined <see cref="DbgProcessStarter"/> order constants
	/// </summary>
	public static class PredefinedDbgProcessStarterOrders {
		/// <summary>
		/// .NET Core
		/// </summary>
		public const double DotNetCore = 1000000;

		/// <summary>
		/// Default process starter that calls <see cref="Process.Start(string)"/>
		/// </summary>
		public const double DefaultExe = double.MaxValue;
	}
}
