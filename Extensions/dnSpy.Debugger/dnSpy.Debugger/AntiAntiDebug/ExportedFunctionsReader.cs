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
using System.IO;
using dnlib.IO;
using dnlib.PE;

namespace dnSpy.Debugger.AntiAntiDebug {
	struct ExportedFunctionsReader : IDisposable {
		readonly ulong baseAddress;
		readonly PEImage peImage;

		public ExportedFunctionsReader(string filename, ulong baseAddress) {
			this.baseAddress = baseAddress;
			try {
				peImage = new PEImage(filename);
			}
			catch (IOException) {
				throw new DbgHookException($"Invalid PE file: {filename}");
			}
			catch (BadImageFormatException) {
				throw new DbgHookException($"Invalid PE file: {filename}");
			}
		}

		public ExportedFunctions ReadExports() {
			try {
				var exportedFuncs = new ExportedFunctions();
				ReadExports(exportedFuncs);
				return exportedFuncs;
			}
			catch (IOException) {
				throw new DbgHookException($"Invalid PE file: {peImage.Filename}");
			}
		}

		void ReadExports(ExportedFunctions exportedFuncs) {
			var exportHdr = peImage.ImageNTHeaders.OptionalHeader.DataDirectories[0];
			if (exportHdr.VirtualAddress == 0 || exportHdr.Size < 0x28)
				return;

			var reader = peImage.CreateReader();
			reader.Position = (uint)peImage.ToFileOffset(exportHdr.VirtualAddress);
			reader.Position += 16;
			uint ordinalBase = reader.ReadUInt32();
			int numFuncs = reader.ReadInt32();
			int numNames = reader.ReadInt32();
			uint offsetOfFuncs = (uint)peImage.ToFileOffset((RVA)reader.ReadUInt32());
			uint offsetOfNames = (uint)peImage.ToFileOffset((RVA)reader.ReadUInt32());
			uint offsetOfNameIndexes = (uint)peImage.ToFileOffset((RVA)reader.ReadUInt32());

			var names = ReadNames(ref reader, peImage, numNames, offsetOfNames, offsetOfNameIndexes);
			reader.Position = offsetOfFuncs;
			var allRvas = new uint[numFuncs];
			for (int i = 0; i < numFuncs; i++) {
				uint rva = reader.ReadUInt32();
				allRvas[i] = rva;
				if (rva != 0)
					exportedFuncs.Add((ushort)(ordinalBase + (uint)i), baseAddress + rva);
			}

			foreach (var info in names) {
				int index = info.index;
				if ((uint)index >= (uint)allRvas.Length)
					continue;
				uint rva = allRvas[index];
				if (rva == 0)
					continue;
				exportedFuncs.Add(info.name, baseAddress + rva);
			}
		}

		static (string name, int index)[] ReadNames(ref DataReader reader, IPEImage peImage, int numNames, uint offsetOfNames, uint offsetOfNameIndexes) {
			var names = new (string name, int index)[numNames];

			reader.Position = offsetOfNameIndexes;
			for (int i = 0; i < names.Length; i++)
				names[i].index = reader.ReadUInt16();

			var currentOffset = offsetOfNames;
			for (int i = 0; i < names.Length; i++, currentOffset += 4) {
				reader.Position = currentOffset;
				uint offsetOfName = (uint)peImage.ToFileOffset((RVA)reader.ReadUInt32());
				names[i].name = ReadMethodNameASCIIZ(ref reader, offsetOfName);
			}

			return names;
		}

		static string ReadMethodNameASCIIZ(ref DataReader reader, uint offset) {
			reader.Position = offset;
			return reader.TryReadZeroTerminatedUtf8String() ?? string.Empty;
		}

		public void Dispose() => peImage?.Dispose();
	}
}
