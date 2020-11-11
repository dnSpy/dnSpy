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
using System.ComponentModel.Composition;
using System.IO;
using System.Runtime.InteropServices;
using dnlib.DotNet;
using dnlib.IO;

namespace dnSpy.AsmEditor.Compiler {
	abstract class RawModuleBytesProvider {
		public abstract RawModuleBytes? GetRawModuleBytes(ModuleDef module);
	}

	[Export(typeof(RawModuleBytesProvider))]
	sealed unsafe class RawModuleBytesProviderImpl : RawModuleBytesProvider {
		readonly byte[] buffer;

		[ImportingConstructor]
		RawModuleBytesProviderImpl() => buffer = new byte[0x2000];

		public override RawModuleBytes? GetRawModuleBytes(ModuleDef module) {
			// Try to use the latest changes the user has saved to disk.

			// Try the file, if it still exists
			var rawData = TryReadFile(module.Location);
			if (rawData is not null)
				return rawData;

			// If there's no file, use the in-memory data
			if (module is ModuleDefMD m)
				return TryReadModule(m);

			return null;
		}

		RawModuleBytes? TryReadFile(string filename) {
			if (File.Exists(filename)) {
				try {
					using (var stream = File.OpenRead(filename))
						return TryReadStream(stream, isFileLayout: true);
				}
				catch {
				}
			}
			return null;
		}

		RawModuleBytes? TryReadStream(Stream stream, bool isFileLayout) {
			RawModuleBytes? rawModuleBytes = null;
			bool error = true;
			try {
				if (stream.Length > int.MaxValue)
					return null;
				rawModuleBytes = new NativeMemoryRawModuleBytes((int)stream.Length, isFileLayout);
				var p = (byte*)rawModuleBytes.Pointer;
				for (;;) {
					int bytesLeft = (int)(stream.Length - stream.Position);
					if (bytesLeft == 0) {
						error = false;
						return rawModuleBytes;
					}
					if (bytesLeft > buffer.Length)
						bytesLeft = buffer.Length;
					int readBytes = stream.Read(buffer, 0, bytesLeft);
					if (readBytes == 0)
						return default;
					Marshal.Copy(buffer, 0, (IntPtr)p, readBytes);
					p += readBytes;
				}
			}
			finally {
				if (error)
					rawModuleBytes?.Dispose();
			}
		}

		RawModuleBytes? TryReadModule(ModuleDefMD module) {
			var reader = module.Metadata.PEImage.CreateReader();
			return TryReadStream(ref reader, module.Metadata.PEImage.IsFileImageLayout);
		}

		RawModuleBytes? TryReadStream(ref DataReader reader, bool isFileLayout) {
			RawModuleBytes? rawModuleBytes = null;
			bool error = true;
			try {
				if (reader.Length > int.MaxValue)
					return null;
				rawModuleBytes = new NativeMemoryRawModuleBytes((int)reader.Length, isFileLayout);
				reader.ReadBytes(rawModuleBytes.Pointer, rawModuleBytes.Size);
				error = false;
				return rawModuleBytes;
			}
			finally {
				if (error)
					rawModuleBytes?.Dispose();
			}
		}
	}
}
