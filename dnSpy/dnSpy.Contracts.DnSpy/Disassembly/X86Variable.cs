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

namespace dnSpy.Contracts.Disassembly {
	/// <summary>
	/// A variable (argument or local) in a function
	/// </summary>
	public readonly struct X86Variable {
		/// <summary>
		/// Name or null
		/// </summary>
		public string Name { get; }

		/// <summary>
		/// Argument/local index or &lt; 0
		/// </summary>
		public int Index { get; }

		/// <summary>
		/// true if it's a local variable
		/// </summary>
		public bool IsLocal { get; }

		/// <summary>
		/// true if it's an argument
		/// </summary>
		public bool IsArgument => !IsLocal;

		/// <summary>
		/// Start address in the code where this variable is live
		/// </summary>
		public ulong LiveAddress { get; }

		/// <summary>
		/// Length of the live range
		/// </summary>
		public uint LiveLength { get; }

		/// <summary>
		/// Variable location kind
		/// </summary>
		public X86VariableLocationKind LocationKind { get; }

		/// <summary>
		/// Register or memory base register
		/// </summary>
		public X86Register Register { get; }

		/// <summary>
		/// Offset relative to <see cref="Register"/> if it's a memory location (<see cref="X86VariableLocationKind.Memory"/>)
		/// </summary>
		public int MemoryOffset { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Name or null</param>
		/// <param name="index">Argument/local index or &lt; 0</param>
		/// <param name="isLocal">true if it's a local, false if it's an argument</param>
		/// <param name="liveAddress">Start address in the code where this variable is live</param>
		/// <param name="liveLength">Length of the live range</param>
		/// <param name="locationKind">Variable kind</param>
		/// <param name="register">Register or memory base register</param>
		/// <param name="memoryOffset">Offset relative to <paramref name="register"/> if it's a memory location (<see cref="X86VariableLocationKind.Memory"/>)</param>
		public X86Variable(string name, int index, bool isLocal, ulong liveAddress, uint liveLength, X86VariableLocationKind locationKind, X86Register register, int memoryOffset) {
			Name = name;
			Index = index;
			IsLocal = isLocal;
			LiveAddress = liveAddress;
			LiveLength = liveLength;
			LocationKind = locationKind;
			Register = register;
			MemoryOffset = memoryOffset;
		}
	}

	/// <summary>
	/// Variable location kind
	/// </summary>
	public enum X86VariableLocationKind {
		/// <summary>
		/// The variable is stored somewhere else
		/// </summary>
		Other,

		/// <summary>
		/// The variable is stored in a register
		/// </summary>
		Register,

		/// <summary>
		/// The variable is stored in a memory location (register + offset)
		/// </summary>
		Memory,
	}
}
