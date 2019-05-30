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

namespace dnSpy.Contracts.AsmEditor.Compiler {
	/// <summary>
	/// Metadata reference
	/// </summary>
	public unsafe readonly struct CompilerMetadataReference {
		/// <summary>
		/// Raw bytes
		/// </summary>
		public void* Data { get; }

		/// <summary>
		/// Gets the size of <see cref="Data"/>
		/// </summary>
		public int Size { get; }

		/// <summary>
		/// Gets the assembly or null
		/// </summary>
		public IAssembly? Assembly { get; }

		/// <summary>
		/// Gets the filename or null if it doesn't exist on disk
		/// </summary>
		public string? Filename { get; }

		/// <summary>
		/// true if it's an assembly reference, false if it's a module reference
		/// </summary>
		public bool IsAssemblyReference { get; }

		CompilerMetadataReference(void* data, int size, IAssembly? assembly, string? filename, bool isAssemblyReference) {
			if (data is null)
				throw new ArgumentNullException(nameof(data));
			if (size <= 0)
				throw new ArgumentOutOfRangeException(nameof(size));
			Data = data;
			Size = size;
			Assembly = assembly;
			Filename = filename;
			IsAssemblyReference = isAssemblyReference;
		}

		/// <summary>
		/// Creates an assembly metadata reference
		/// </summary>
		/// <param name="data">File data</param>
		/// <param name="size">Size of data</param>
		/// <param name="assembly">Assembly owner or null</param>
		/// <param name="filename">Filename or null if it doesn't exist on disk</param>
		/// <returns></returns>
		public static CompilerMetadataReference CreateAssemblyReference(void* data, int size, IAssembly? assembly, string? filename = null) => new CompilerMetadataReference(data, size, assembly, filename, true);

		/// <summary>
		/// Creates a module metadata reference
		/// </summary>
		/// <param name="data">File data</param>
		/// <param name="size">Size of data</param>
		/// <param name="assembly">Assembly owner or null</param>
		/// <param name="filename">Filename or null if it doesn't exist on disk</param>
		/// <returns></returns>
		public static CompilerMetadataReference CreateModuleReference(void* data, int size, IAssembly? assembly, string? filename = null) => new CompilerMetadataReference(data, size, assembly, filename, false);
	}
}
