/*
    Copyright (C) 2014-2016 de4dot@gmail.com

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

using dnlib.DotNet;
using dnSpy.Contracts.Decompiler;

namespace dnSpy.Contracts.Languages {
	/// <summary>
	/// Simple IL printer. Only used by the asm editor
	/// </summary>
	public interface ISimpleILPrinter {
		/// <summary>
		/// Gets the order
		/// </summary>
		double Order { get; }

		/// <summary>
		/// Writes a line to <paramref name="output"/>
		/// </summary>
		/// <param name="output">Output</param>
		/// <param name="member">Member</param>
		/// <returns></returns>
		bool Write(IDecompilerOutput output, IMemberRef member);

		/// <summary>
		/// Writes a method signature
		/// </summary>
		/// <param name="output">Output</param>
		/// <param name="sig">Signature</param>
		void Write(IDecompilerOutput output, MethodSig sig);

		/// <summary>
		/// Writes a type
		/// </summary>
		/// <param name="output">Output</param>
		/// <param name="type">Type</param>
		void Write(IDecompilerOutput output, TypeSig type);
	}
}
