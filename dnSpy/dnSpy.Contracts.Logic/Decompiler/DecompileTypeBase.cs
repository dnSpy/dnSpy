/*
    Copyright (C) 2014-2017 de4dot@gmail.com

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

namespace dnSpy.Contracts.Decompiler {
	/// <summary>
	/// Base class of <see cref="DecompilationType"/> data
	/// </summary>
	public abstract class DecompileTypeBase {
		/// <summary>
		/// Output
		/// </summary>
		public IDecompilerOutput Output { get; }

		/// <summary>
		/// Options
		/// </summary>
		public DecompilationContext Context { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="output">Output</param>
		/// <param name="ctx">Context</param>
		protected DecompileTypeBase(IDecompilerOutput output, DecompilationContext ctx) {
			if (output == null)
				throw new ArgumentNullException(nameof(output));
			if (ctx == null)
				throw new ArgumentNullException(nameof(ctx));
			Output = output;
			Context = ctx;
		}
	}
}
