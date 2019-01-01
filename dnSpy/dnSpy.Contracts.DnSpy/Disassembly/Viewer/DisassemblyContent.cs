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
	/// Disassembled content shown in the disassembly viewer
	/// </summary>
	public readonly struct DisassemblyContent {
		/// <summary>
		/// Gets the content kind
		/// </summary>
		public DisassemblyContentKind Kind { get; }

		/// <summary>
		/// Gets the text
		/// </summary>
		public DisassemblyText[] Text { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="kind">Content kind</param>
		/// <param name="text">Disassembly</param>
		public DisassemblyContent(DisassemblyContentKind kind, DisassemblyText[] text) {
			Kind = kind;
			Text = text ?? throw new ArgumentNullException(nameof(text));
		}
	}

	/// <summary>
	/// Content kind
	/// </summary>
	public enum DisassemblyContentKind {
		/// <summary>
		/// Some other unknown kind
		/// </summary>
		Unknown,

		/// <summary>
		/// x86 masm syntax
		/// </summary>
		Masm,

		/// <summary>
		/// x86 nasm syntax
		/// </summary>
		Nasm,

		/// <summary>
		/// x86 AT&amp;T syntax
		/// </summary>
		ATT,
	}
}
