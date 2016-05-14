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

using System;
using dnlib.DotNet;

namespace dnSpy.AsmEditor.Compile {
	struct CompilerMetadataReference {
		/// <summary>
		/// Raw bytes
		/// </summary>
		public byte[] Data { get; }

		/// <summary>
		/// Gets the assembly or null
		/// </summary>
		public IAssembly Assembly { get; }

		/// <summary>
		/// true if it's an assembly reference, false if it's a module reference
		/// </summary>
		public bool IsAssemblyReference { get; }

		CompilerMetadataReference(byte[] data, IAssembly assembly, bool isAssemblyReference) {
			if (data == null)
				throw new ArgumentNullException(nameof(data));
			Data = data;
			Assembly = assembly;
			IsAssemblyReference = isAssemblyReference;
		}

		public static CompilerMetadataReference CreateAssemblyReference(byte[] data, IAssembly assembly) => new CompilerMetadataReference(data, assembly, true);
		public static CompilerMetadataReference CreateModuleReference(byte[] data, IAssembly assembly) => new CompilerMetadataReference(data, assembly, false);
	}
}
