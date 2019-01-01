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

namespace dnSpy.Contracts.Debugger.AntiAntiDebug {
	/// <summary>
	/// Hooked function. The owner can write their own code by calling <see cref="WriteByte(byte)"/>
	/// </summary>
	public abstract class DbgHookedNativeFunction {
		/// <summary>
		/// Gets the next address that <see cref="WriteByte(byte)"/> will write to
		/// </summary>
		public abstract ulong CurrentAddress { get; }

		/// <summary>
		/// Gets the address of the new hooked function. The first written byte will be written to this location.
		/// The original function is patched to jump to this address.
		/// </summary>
		public abstract ulong NewCodeAddress { get; }

		/// <summary>
		/// Gets the address of the original function after it has been moved. This address can be used
		/// to call the original function from the new code.
		/// </summary>
		public abstract ulong NewFunctionAddress { get; }

		/// <summary>
		/// Gets the address of the function, which has been hooked by us. If you must call the real function
		/// from the new code, call <see cref="NewFunctionAddress"/> and not this address.
		/// </summary>
		public abstract ulong OriginalFunctionAddress { get; }

		/// <summary>
		/// Writes the next byte to the code section
		/// </summary>
		/// <param name="value">Byte to write</param>
		public abstract void WriteByte(byte value);
	}
}
