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
		/// <summary>See <see cref="ExportDbgNativeFunctionHookAttribute.Machines"/></summary>
		DbgMachine[] Machines { get; }
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
		/// <param name="machine">Supported machine</param>
		/// <param name="order">Order</param>
		public ExportDbgNativeFunctionHookAttribute(string dll, string function, DbgMachine machine, double order = double.MaxValue)
			: this(dll, function, new[] { machine }, order) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="dll">DLL name including dll extension, case sensitive. It can be any name, it's only used to make sure only one handler patches a function.</param>
		/// <param name="function">Function name, case sensitive. It can be any name, it's only used to make sure only one handler patches a function.</param>
		/// <param name="machines">Supported machines or empty to support all available machines</param>
		/// <param name="order">Order</param>
		public ExportDbgNativeFunctionHookAttribute(string dll, string function, DbgMachine[] machines, double order = double.MaxValue)
			: base(typeof(IDbgNativeFunctionHook)) {
			Dll = dll ?? throw new ArgumentNullException(nameof(dll));
			Function = function ?? throw new ArgumentNullException(nameof(function));
			Machines = machines ?? throw new ArgumentNullException(nameof(machines));
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
		/// Supported machines or empty to support all available machines
		/// </summary>
		public DbgMachine[] Machines { get; }

		/// <summary>
		/// Gets the order
		/// </summary>
		public double Order { get; }
	}
}
