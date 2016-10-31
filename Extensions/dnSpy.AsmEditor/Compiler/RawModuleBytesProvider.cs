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

using System.ComponentModel.Composition;
using System.IO;
using dnlib.DotNet;
using dnlib.IO;

namespace dnSpy.AsmEditor.Compiler {
	interface IRawModuleBytesProvider {
		byte[] GetRawModuleBytes(ModuleDef module);
	}

	[Export(typeof(IRawModuleBytesProvider))]
	sealed class RawModuleBytesProvider : IRawModuleBytesProvider {
		public byte[] GetRawModuleBytes(ModuleDef module) {
			// Try to use the latest changes the user has saved to disk.

			// Try the file, if it still exists
			var fileBytes = TryReadFile(module.Location);
			if (fileBytes != null)
				return fileBytes;

			// If there's no file, use the in-memory data
			var m = module as ModuleDefMD;
			if (m != null)
				return m.MetaData.PEImage.CreateFullStream().ReadAllBytes();

			return null;
		}

		byte[] TryReadFile(string filename) {
			if (File.Exists(filename)) {
				try {
					return File.ReadAllBytes(filename);
				}
				catch { }
			}
			return null;
		}
	}
}
