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
using dnSpy.Contracts.Debugger.Breakpoints.Code;

namespace dnSpy.Contracts.Debugger.Engine {
	/// <summary>
	/// A class that can update a <see cref="DbgBoundCodeBreakpoint"/>
	/// </summary>
	public abstract class DbgEngineBoundCodeBreakpoint {
		/// <summary>
		/// Gets the bound breakpoint
		/// </summary>
		public abstract DbgBoundCodeBreakpoint BoundCodeBreakpoint { get; }

		/// <summary>
		/// Removes this bound breakpoint
		/// </summary>
		public abstract void Remove();

		/// <summary>
		/// Removes bound breakpoints
		/// </summary>
		/// <param name="breakpoints">Breakpoints to remove</param>
		public abstract void Remove(DbgEngineBoundCodeBreakpoint[] breakpoints);

		/// <summary>
		/// Properties to update
		/// </summary>
		[Flags]
		public enum UpdateOptions {
			/// <summary>
			/// No option is enabled
			/// </summary>
			None				= 0,

			/// <summary>
			/// Update <see cref="DbgBoundCodeBreakpoint.Module"/>
			/// </summary>
			Module				= 0x00000001,

			/// <summary>
			/// Update <see cref="DbgBoundCodeBreakpoint.Address"/>
			/// </summary>
			Address				= 0x00000002,

			/// <summary>
			/// Update <see cref="DbgBoundCodeBreakpoint.Message"/>
			/// </summary>
			Message				= 0x00000004,
		}

		/// <summary>
		/// Updates <see cref="DbgBoundCodeBreakpoint.Module"/>
		/// </summary>
		/// <param name="module">New value</param>
		public void UpdateModule(DbgModule module) => Update(UpdateOptions.Module, module: module);

		/// <summary>
		/// Updates <see cref="DbgBoundCodeBreakpoint.Address"/>
		/// </summary>
		/// <param name="address">New value</param>
		public void UpdateAddress(ulong address) => Update(UpdateOptions.Address, address: address);

		/// <summary>
		/// Updates <see cref="DbgBoundCodeBreakpoint.Message"/>
		/// </summary>
		/// <param name="message">New value</param>
		public void UpdateMessage(DbgEngineBoundCodeBreakpointMessage message) => Update(UpdateOptions.Message, message: message);

		/// <summary>
		/// Updates <see cref="DbgBoundCodeBreakpoint"/> properties
		/// </summary>
		/// <param name="options">Options</param>
		/// <param name="module">New <see cref="DbgBoundCodeBreakpoint.Module"/> value</param>
		/// <param name="address">New <see cref="DbgBoundCodeBreakpoint.Address"/> value</param>
		/// <param name="message">New <see cref="DbgBoundCodeBreakpoint.Message"/> value</param>
		public abstract void Update(UpdateOptions options, DbgModule? module = null, ulong address = 0, DbgEngineBoundCodeBreakpointMessage message = default);
	}

	/// <summary>
	/// Bound breakpoint message kind
	/// </summary>
	public enum DbgEngineBoundCodeBreakpointMessageKind {
		/// <summary>
		/// No warning or error, the breakpoint will break when hit
		/// </summary>
		None,

		/// <summary>
		/// Custom warning message
		/// </summary>
		CustomWarning,

		/// <summary>
		/// Custom error message
		/// </summary>
		CustomError,

		/// <summary>
		/// Function wasn't found
		/// </summary>
		FunctionNotFound,

		/// <summary>
		/// A breakpoint could not be created
		/// </summary>
		CouldNotCreateBreakpoint,
	}

	/// <summary>
	/// Bound breakpoint message
	/// </summary>
	public readonly struct DbgEngineBoundCodeBreakpointMessage {
		/// <summary>
		/// Message kind
		/// </summary>
		public DbgEngineBoundCodeBreakpointMessageKind Kind { get; }

		/// <summary>
		/// Arguments
		/// </summary>
		public string[] Arguments { get; }

		DbgEngineBoundCodeBreakpointMessage(DbgEngineBoundCodeBreakpointMessageKind kind, string[] arguments) {
			Kind = kind;
			Arguments = arguments ?? throw new ArgumentNullException(nameof(arguments));
		}

		/// <summary>
		/// Creates a no-error message
		/// </summary>
		/// <returns></returns>
		public static DbgEngineBoundCodeBreakpointMessage CreateNoError() =>
			new DbgEngineBoundCodeBreakpointMessage(DbgEngineBoundCodeBreakpointMessageKind.None, Array.Empty<string>());

		/// <summary>
		/// Creates a custom warning message
		/// </summary>
		/// <param name="message">Message</param>
		/// <returns></returns>
		public static DbgEngineBoundCodeBreakpointMessage CreateCustomWarning(string message) =>
			new DbgEngineBoundCodeBreakpointMessage(DbgEngineBoundCodeBreakpointMessageKind.CustomWarning, new[] { message ?? throw new ArgumentNullException(nameof(message)) });

		/// <summary>
		/// Creates a custom error message
		/// </summary>
		/// <param name="message">Message</param>
		/// <returns></returns>
		public static DbgEngineBoundCodeBreakpointMessage CreateCustomError(string message) =>
			new DbgEngineBoundCodeBreakpointMessage(DbgEngineBoundCodeBreakpointMessageKind.CustomError, new[] { message ?? throw new ArgumentNullException(nameof(message)) });

		/// <summary>
		/// Creates a function-not-found message
		/// </summary>
		/// <param name="function">Name of function</param>
		/// <returns></returns>
		public static DbgEngineBoundCodeBreakpointMessage CreateFunctionNotFound(string function) =>
			new DbgEngineBoundCodeBreakpointMessage(DbgEngineBoundCodeBreakpointMessageKind.FunctionNotFound, new[] { function ?? throw new ArgumentNullException(nameof(function)) });

		/// <summary>
		/// Creates a could-not-create-breakpoint message
		/// </summary>
		/// <returns></returns>
		public static DbgEngineBoundCodeBreakpointMessage CreateCouldNotCreateBreakpoint() =>
			new DbgEngineBoundCodeBreakpointMessage(DbgEngineBoundCodeBreakpointMessageKind.CouldNotCreateBreakpoint, Array.Empty<string>());
	}
}
