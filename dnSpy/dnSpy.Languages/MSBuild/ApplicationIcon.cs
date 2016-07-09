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

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using dnlib.IO;
using dnlib.W32Resources;
using dnSpy.Languages.Properties;

namespace dnSpy.Languages.MSBuild {
	sealed class ApplicationIcon : IFileJob {
		const int RT_ICON = 3;
		const int RT_GROUP_ICON = 14;

		public string Description => dnSpy_Languages_Resources.MSBuild_CreateApplicationIcon;
		public string Filename { get; }

		readonly byte[] data;

		ApplicationIcon(string filename, byte[] data) {
			this.Filename = filename;
			this.data = data;
		}

		public static ApplicationIcon TryCreate(Win32Resources resources, string filenameNoExt, FilenameCreator filenameCreator) {
			if (resources == null)
				return null;

			var dir = resources.Find(new ResourceName(RT_GROUP_ICON));
			if (dir == null || dir.Directories.Count == 0)
				return null;
			dir = dir.Directories[0];
			if (dir.Data.Count == 0)
				return null;

			var iconDir = resources.Find(new ResourceName(RT_ICON));
			if (iconDir == null)
				return null;

			var iconData = TryCreateIcon(dir.Data[0].Data, iconDir);
			if (iconData == null)
				return null;

			return new ApplicationIcon(filenameCreator.CreateName(filenameNoExt + ".ico"), iconData);
		}

		static byte[] TryCreateIcon(IBinaryReader reader, ResourceDirectory iconDir) {
			try {
				reader.Position = 0;
				var outStream = new MemoryStream();
				var writer = new BinaryWriter(outStream);
				// Write GRPICONDIR
				writer.Write(reader.ReadUInt16());
				writer.Write(reader.ReadUInt16());
				ushort numImages;
				writer.Write(numImages = reader.ReadUInt16());

				var entries = new List<GrpIconDirEntry>();
				for (int i = 0; i < numImages; i++) {
					var e = new GrpIconDirEntry();
					entries.Add(e);
					e.bWidth = reader.ReadByte();
					e.bHeight = reader.ReadByte();
					e.bColorCount = reader.ReadByte();
					e.bReserved = reader.ReadByte();
					e.wPlanes = reader.ReadUInt16();
					e.wBitCount = reader.ReadUInt16();
					e.dwBytesInRes = reader.ReadUInt32();
					e.nID = reader.ReadUInt16();
				}

				uint dataOffset = 2 * 3 + (uint)entries.Count * 0x10;
				foreach (var e in entries) {
					writer.Write(e.bWidth);
					writer.Write(e.bHeight);
					writer.Write(e.bColorCount);
					writer.Write(e.bReserved);
					writer.Write(e.wPlanes);
					writer.Write(e.wBitCount);
					writer.Write(e.dwBytesInRes);
					writer.Write(dataOffset);
					dataOffset += e.dwBytesInRes;
				}

				foreach (var e in entries) {
					var d = iconDir.Directories.FirstOrDefault(a => a.Name == new ResourceName(e.nID));
					if (d == null || d.Data.Count == 0)
						return null;
					var r = d.Data[0].Data;
					Debug.Assert(r.Length == e.dwBytesInRes);
					if (r.Length < e.dwBytesInRes)
						return null;
					r.Position = 0;
					writer.Write(r.ReadBytes((int)e.dwBytesInRes), 0, (int)e.dwBytesInRes);
				}

				return outStream.ToArray();
			}
			catch (IOException) {
			}
			return null;
		}

		sealed class GrpIconDirEntry {
			public byte bWidth;
			public byte bHeight;
			public byte bColorCount;
			public byte bReserved;
			public ushort wPlanes;
			public ushort wBitCount;
			public uint dwBytesInRes;
			public ushort nID;
		}

		public void Create(DecompileContext ctx) {
			using (var stream = File.Create(Filename))
				stream.Write(data, 0, data.Length);
		}
	}
}
