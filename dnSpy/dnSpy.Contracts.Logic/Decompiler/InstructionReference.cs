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
using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace dnSpy.Contracts.Decompiler {
	/// <summary>
	/// Instruction reference
	/// </summary>
	public sealed class InstructionReference : IEquatable<InstructionReference?> {
		/// <summary>
		/// Method
		/// </summary>
		public MethodDef Method { get; }

		/// <summary>
		/// Instruction
		/// </summary>
		public Instruction Instruction { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="method">Method</param>
		/// <param name="instruction">Instruction</param>
		public InstructionReference(MethodDef method, Instruction instruction) {
			Method = method ?? throw new ArgumentNullException(nameof(method));
			Instruction = instruction ?? throw new ArgumentNullException(nameof(instruction));
		}

		/// <summary>
		/// Equals()
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public bool Equals(InstructionReference? other) => !(other is null) && Method == other.Method && Instruction == other.Instruction;

		/// <summary>
		/// Equals()
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public override bool Equals(object? obj) => Equals(obj as InstructionReference);

		/// <summary>
		/// GetHashCode()
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode() => Method.GetHashCode() ^ Instruction.GetHashCode();
	}
}
