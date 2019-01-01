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

using System.ComponentModel;

namespace dnSpy.Contracts.Disassembly.Viewer {
	/// <summary>
	/// Disassembly content settings
	/// </summary>
	public abstract class DisassemblyContentSettings : INotifyPropertyChanged {
		/// <summary>
		/// Raised when a property is changed
		/// </summary>
		public event PropertyChangedEventHandler PropertyChanged;

		/// <summary>
		/// Raises <see cref="PropertyChanged"/>
		/// </summary>
		/// <param name="propName">Name of property that changed</param>
		protected void OnPropertyChanged(string propName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));

		/// <summary>
		/// Show instruction address
		/// </summary>
		public abstract bool ShowInstructionAddress { get; set; }

		/// <summary>
		/// Show instruction bytes
		/// </summary>
		public abstract bool ShowInstructionBytes { get; set; }

		/// <summary>
		/// Add an empty line between basic blocks
		/// </summary>
		public abstract bool EmptyLineBetweenBasicBlocks { get; set; }

		/// <summary>
		/// Add labels to the disassembled code
		/// </summary>
		public abstract bool AddLabels { get; set; }

		/// <summary>
		/// Show IL code, if available
		/// </summary>
		public abstract bool ShowILCode { get; set; }

		/// <summary>
		/// Show source code or decompiled code, if available
		/// </summary>
		public abstract bool ShowCode { get; set; }

		/// <summary>
		/// x86 disassembler
		/// </summary>
		public abstract X86Disassembler X86Disassembler { get; set; }
	}

	/// <summary>
	/// x86 disassembler
	/// </summary>
	public enum X86Disassembler {
		/// <summary>
		/// masm disassembler
		/// </summary>
		Masm,

		/// <summary>
		/// nasm disassembler
		/// </summary>
		Nasm,

		/// <summary>
		/// GNU assembler (AT&amp;T) disassembler
		/// </summary>
		Gas,
	}
}
