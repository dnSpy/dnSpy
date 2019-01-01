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

namespace dnSpy.Contracts.Debugger.AntiAntiDebug {
	/// <summary>
	/// Hooks native functions when the debugged process starts.
	/// Use <see cref="ExportDbgNativeFunctionHookAttribute"/> to export an instance.
	/// </summary>
	public interface IDbgNativeFunctionHook {
		/// <summary>
		/// Returns true if it's enabled
		/// </summary>
		/// <param name="context">Context</param>
		/// <returns></returns>
		bool IsEnabled(DbgNativeFunctionHookContext context);

		/// <summary>
		/// Hooks the function
		/// </summary>
		/// <param name="context">Context</param>
		/// <param name="errorMessage">Updated with an error message if it failed or null if it was successful</param>
		void Hook(DbgNativeFunctionHookContext context, out string errorMessage);
	}

	/// <summary>Metadata</summary>
	public interface IDbgNativeFunctionHookMetadata {
		/// <summary>See <see cref="ExportDbgNativeFunctionHookAttribute.Dll"/></summary>
		string Dll { get; }
		/// <summary>See <see cref="ExportDbgNativeFunctionHookAttribute.Function"/></summary>
		string Function { get; }
		/// <summary>See <see cref="ExportDbgNativeFunctionHookAttribute.Architectures"/></summary>
		DbgArchitecture[] Architectures { get; }
		/// <summary>See <see cref="ExportDbgNativeFunctionHookAttribute.OperatingSystems"/></summary>
		DbgOperatingSystem[] OperatingSystems { get; }
		/// <summary>See <see cref="ExportDbgNativeFunctionHookAttribute.Order"/></summary>
		double Order { get; }
	}

	/// <summary>
	/// Exports a <see cref="IDbgNativeFunctionHook"/> instance
	/// </summary>
	[MetadataAttribute, AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
	public sealed class ExportDbgNativeFunctionHookAttribute : ExportAttribute, IDbgNativeFunctionHookMetadata {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="dll">DLL name including dll extension, case sensitive. It can be any name, it's only used to make sure only one handler patches a function.</param>
		/// <param name="function">Function name, case sensitive. It can be any name, it's only used to make sure only one handler patches a function.</param>
		/// <param name="architecture">Supported architecture</param>
		/// <param name="operatingSystem">Supported operating system</param>
		/// <param name="order">Order</param>
		public ExportDbgNativeFunctionHookAttribute(string dll, string function, DbgArchitecture architecture, DbgOperatingSystem operatingSystem, double order = double.MaxValue)
			: this(dll, function, new[] { architecture }, new[] { operatingSystem }, order) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="dll">DLL name including dll extension, case sensitive. It can be any name, it's only used to make sure only one handler patches a function.</param>
		/// <param name="function">Function name, case sensitive. It can be any name, it's only used to make sure only one handler patches a function.</param>
		/// <param name="architectures">Supported architectures or empty to support all available architectures</param>
		/// <param name="operatingSystems">Supported operating systems or empty to support all operating systems</param>
		/// <param name="order">Order</param>
		public ExportDbgNativeFunctionHookAttribute(string dll, string function, DbgArchitecture[] architectures, DbgOperatingSystem[] operatingSystems, double order = double.MaxValue)
			: base(typeof(IDbgNativeFunctionHook)) {
			Dll = dll ?? throw new ArgumentNullException(nameof(dll));
			Function = function ?? throw new ArgumentNullException(nameof(function));
			Architectures = architectures ?? throw new ArgumentNullException(nameof(architectures));
			OperatingSystems = operatingSystems ?? throw new ArgumentNullException(nameof(operatingSystems));
			Order = order;
		}

		/// <summary>
		/// DLL name including dll extension, case sensitive. It can be any name, it's only used to make sure only one handler patches a function.
		/// </summary>
		public string Dll { get; }

		/// <summary>
		/// Function name, case sensitive. It can be any name, it's only used to make sure only one handler patches a function.
		/// </summary>
		public string Function { get; }

		/// <summary>
		/// Supported architectures or empty to support all available architectures
		/// </summary>
		public DbgArchitecture[] Architectures { get; }

		/// <summary>
		/// Supported operating systems or empty to support all operating systems
		/// </summary>
		public DbgOperatingSystem[] OperatingSystems { get; }

		/// <summary>
		/// Gets the order
		/// </summary>
		public double Order { get; }
	}
}
