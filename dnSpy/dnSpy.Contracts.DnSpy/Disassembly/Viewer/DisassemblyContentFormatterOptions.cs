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

namespace dnSpy.Contracts.Disassembly.Viewer {
	/// <summary>
	/// Disassembly options
	/// </summary>
	[Flags]
	public enum DisassemblyContentFormatterOptions : uint {
		/// <summary>
		/// No option is enabled
		/// </summary>
		None							= 0,

		/// <summary>
		/// Add an empty line between basic blocks (overrides global options)
		/// </summary>
		EmptyLineBetweenBasicBlocks		= 0x00000001,

		/// <summary>
		/// Don't add an empty line between basic blocks (overrides global options)
		/// </summary>
		NoEmptyLineBetweenBasicBlocks	= 0x00000002,

		/// <summary>
		/// Show instruction addresses (overrides global options)
		/// </summary>
		InstructionAddresses			= 0x00000004,

		/// <summary>
		/// Don't show instruction addresses (overrides global options)
		/// </summary>
		NoInstructionAddresses			= 0x00000008,

		/// <summary>
		/// Show instruction bytes (overrides global options)
		/// </summary>
		InstructionBytes				= 0x00000010,

		/// <summary>
		/// Don't show instruction bytes (overrides global options)
		/// </summary>
		NoInstructionBytes				= 0x00000020,

		/// <summary>
		/// Add labels to the disassembled code (overrides global options)
		/// </summary>
		AddLabels						= 0x00000040,

		/// <summary>
		/// Don't add labels to the disassembled code (overrides global options)
		/// </summary>
		NoAddLabels						= 0x00000080,
	}
}
