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

namespace dnSpy.Debugger.DotNet.Interpreter {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
	public enum InterpreterExceptionKind {
		TooManyInstructions,
		InvalidMethodBody,
		InstructionNotSupported,
	}
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

	/// <summary>
	/// Interpreter exception
	/// </summary>
	[Serializable]
	public abstract class InterpreterException : Exception {
		/// <summary>
		/// Gets the exception kind
		/// </summary>
		public abstract InterpreterExceptionKind Kind { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="message">Message</param>
		protected InterpreterException(string message) : base(message) { }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="message">Message</param>
		/// <param name="innerException">Inner exception</param>
		protected InterpreterException(string message, Exception innerException) : base(message, innerException) { }
	}

	/// <summary>
	/// Thrown when too many instructions have been interpreted
	/// </summary>
	[Serializable]
	public class TooManyInstructionsInterpreterException : InterpreterException {
		/// <summary>
		/// Returns <see cref="InterpreterExceptionKind.TooManyInstructions"/>
		/// </summary>
		public override InterpreterExceptionKind Kind => InterpreterExceptionKind.TooManyInstructions;

		/// <summary>
		/// Constructor
		/// </summary>
		public TooManyInstructionsInterpreterException() : base("Too many instructions have been executed") { }
	}

	/// <summary>
	/// Invalid method body, eg. last instruction isn't an unconditional branch instruction (eg. ret/throw)
	/// </summary>
	[Serializable]
	public class InvalidMethodBodyInterpreterException : InterpreterException {
		/// <summary>
		/// Returns <see cref="InterpreterExceptionKind.InvalidMethodBody"/>
		/// </summary>
		public override InterpreterExceptionKind Kind => InterpreterExceptionKind.InvalidMethodBody;

		const string MESSAGE = "Invalid method body";

		/// <summary>
		/// Constructor
		/// </summary>
		public InvalidMethodBodyInterpreterException() : base(MESSAGE) { }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="innerException">Inner exception</param>
		public InvalidMethodBodyInterpreterException(Exception innerException) : base(MESSAGE, innerException) { }
	}

	/// <summary>
	/// Unsupported IL instruction
	/// </summary>
	[Serializable]
	public class InstructionNotSupportedInterpreterException : InterpreterException {
		/// <summary>
		/// Returns <see cref="InterpreterExceptionKind.InstructionNotSupported"/>
		/// </summary>
		public override InterpreterExceptionKind Kind => InterpreterExceptionKind.InstructionNotSupported;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="message">Message</param>
		public InstructionNotSupportedInterpreterException(string message) : base(message) { }
	}
}
