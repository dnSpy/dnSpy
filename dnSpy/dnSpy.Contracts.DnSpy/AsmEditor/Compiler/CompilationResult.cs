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
using System.Diagnostics;

namespace dnSpy.Contracts.AsmEditor.Compiler {
	/// <summary>
	/// Compilation result
	/// </summary>
	public struct CompilationResult {
		/// <summary>
		/// true if the compilation succeeded
		/// </summary>
		public bool Success => RawFile != null;

		/// <summary>
		/// Result of compilation or null if compilation failed
		/// </summary>
		public byte[] RawFile { get; }

		/// <summary>
		/// Debug file data (eg. PDB data)
		/// </summary>
		public DebugFileResult DebugFile { get; }

		/// <summary>
		/// Gets the diagnostics
		/// </summary>
		public CompilerDiagnostic[] Diagnostics { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="rawFile">Raw file data</param>
		/// <param name="debugFile">Debug file result or null</param>
		/// <param name="diagnostics">Diagnostics or null</param>
		public CompilationResult(byte[] rawFile, DebugFileResult? debugFile = null, CompilerDiagnostic[] diagnostics = null) {
			RawFile = rawFile ?? throw new ArgumentNullException(nameof(rawFile));
			DebugFile = debugFile ?? new DebugFileResult();
			Diagnostics = diagnostics ?? Array.Empty<CompilerDiagnostic>();
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="diagnostics">Diagnostics</param>
		public CompilationResult(CompilerDiagnostic[] diagnostics) {
			if (diagnostics == null)
				throw new ArgumentNullException(nameof(diagnostics));
			Debug.Assert(diagnostics.Length != 0);
			RawFile = null;
			DebugFile = new DebugFileResult();
			Diagnostics = diagnostics;
		}
	}
}
