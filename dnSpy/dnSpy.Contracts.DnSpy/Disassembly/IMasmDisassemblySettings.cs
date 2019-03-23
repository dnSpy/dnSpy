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
	/// masm disassembly settings
	/// </summary>
	public interface IMasmDisassemblySettings : IX86DisassemblySettings {
		/// <summary>
		/// Add a DS segment override even if it's not present. Used if it's 16/32-bit code and mem op is a displ, eg. 'mov eax,[12345678]' vs 'mov eax,ds:[12345678]'
		/// </summary>
		bool AddDsPrefix32 { get; set; }

		/// <summary>
		/// Show symbols in brackets, eg. '[ecx+symbol]' vs 'symbol[ecx]' and '[symbol]' vs 'symbol'
		/// </summary>
		bool SymbolDisplInBrackets { get; set; }

		/// <summary>
		/// Show displacements in brackets, eg. '[ecx+1234h]' vs '1234h[ecx]'
		/// </summary>
		bool DisplInBrackets { get; set; }
	}
}
