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
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace dnSpy.MainApp {
	sealed class CachedMefInfo {
		readonly Assembly[] assemblies;
		readonly Stream stream;

		const uint SIG = 0x577BC8FF;

		public CachedMefInfo(Assembly[] assemblies, Stream stream) {
			this.assemblies = assemblies;
			this.stream = stream;
		}

		static string GetPathToClrDll() {
#if NETFRAMEWORK
			const string clrDllFilename = "clr.dll";
#elif NETCOREAPP
			const string clrDllFilename = "coreclr.dll";
#else
#error Unknown target framework
#endif
			var basePath = Path.GetDirectoryName(typeof(void).Assembly.Location)!;
			var filename = Path.Combine(basePath, clrDllFilename);
			if (File.Exists(filename))
				return filename;
			throw new InvalidProgramException("Couldn't find " + clrDllFilename);
		}

		public void WriteFile(int[] tokens, out long resourceManagerTokensOffset) {
			Debug.Assert(assemblies.Length == tokens.Length);
			stream.Position = 0;
			var writer = new BinaryWriter(stream);
			writer.Write(SIG);

			// VS-MEF serializes metadata tokens. I don't know if eg. mscorlib tokens could be saved
			// in the file so record some extra info in this file so we won't load it if eg. clr.dll
			// or mscorlib.dll got updated.
			WriteFile(writer, GetPathToClrDll());
			WriteAssembly(writer, typeof(void).Assembly);

			writer.Write(assemblies.Length);
			foreach (var assembly in assemblies)
				WriteAssembly(writer, assembly);

			resourceManagerTokensOffset = writer.BaseStream.Position;
			foreach (var token in tokens)
				writer.Write(token);

			writer.Flush();
		}

		public void UpdateResourceManagerTokens(long resourceManagerTokensOffset, ResourceManagerTokenCacheImpl resourceManagerTokenCacheImpl) {
			if (resourceManagerTokensOffset < 0)
				return;
			stream.Position = resourceManagerTokensOffset;
			var writer = new BinaryWriter(stream);
			var tokens = resourceManagerTokenCacheImpl.GetTokens(assemblies);
			if (tokens.Length != assemblies.Length)
				throw new InvalidOperationException();
			foreach (var token in tokens)
				writer.Write(token);
		}

		void WriteFile(BinaryWriter writer, string filename) {
			Debug.Assert(File.Exists(filename));
			writer.Write(filename);
			if (File.Exists(filename)) {
				var fileInfo = new FileInfo(filename);
				writer.Write(fileInfo.LastWriteTimeUtc.ToFileTimeUtc());
				writer.Write(fileInfo.CreationTimeUtc.ToFileTimeUtc());
				writer.Write(fileInfo.Length);
			}
		}

		void WriteAssembly(BinaryWriter writer, Assembly assembly) {
			var filename = assembly.Location;
			writer.Write(filename);
			var fileInfo = new FileInfo(filename);
			writer.Write(fileInfo.LastWriteTimeUtc.ToFileTimeUtc());
			writer.Write(fileInfo.CreationTimeUtc.ToFileTimeUtc());
			writer.Write(fileInfo.Length);
			writer.Write(assembly.ManifestModule.ModuleVersionId.ToByteArray());
		}

		public bool CheckFile(ResourceManagerTokenCacheImpl resourceManagerTokenCacheImpl, out long resourceManagerTokensOffset) {
			resourceManagerTokensOffset = -1;
			stream.Position = 0;
			var reader = new BinaryReader(stream);
			if (reader.ReadUInt32() != SIG)
				return false;

			if (!CheckFile(reader, GetPathToClrDll()))
				return false;
			if (!CheckAssembly(reader, typeof(void).Assembly))
				return false;

			if (reader.ReadInt32() != assemblies.Length)
				return false;
			foreach (var assembly in assemblies) {
				if (!CheckAssembly(reader, assembly))
					return false;
			}

			var tokens = new int[assemblies.Length];
			resourceManagerTokensOffset = reader.BaseStream.Position;
			for (int i = 0; i < tokens.Length; i++) {
				var token = reader.ReadInt32();
				if (!(token == 0 || ((token >> 24) == 0x06 && (token & 0x00FFFFFF) != 0)))
					return false;
				tokens[i] = token;
			}

			resourceManagerTokenCacheImpl.SetTokens(assemblies, tokens);

			return true;
		}

		bool CheckFile(BinaryReader reader, string filename) {
			if (reader.ReadString() != filename)
				return false;
			Debug.Assert(File.Exists(filename));
			if (File.Exists(filename)) {
				var fileInfo = new FileInfo(filename);
				if (reader.ReadInt64() != fileInfo.LastWriteTimeUtc.ToFileTimeUtc())
					return false;
				if (reader.ReadInt64() != fileInfo.CreationTimeUtc.ToFileTimeUtc())
					return false;
				if (reader.ReadInt64() != fileInfo.Length)
					return false;
			}
			return true;
		}

		bool CheckAssembly(BinaryReader reader, Assembly assembly) {
			var filename = assembly.Location;
			if (reader.ReadString() != filename)
				return false;
			var fileInfo = new FileInfo(filename);
			if (reader.ReadInt64() != fileInfo.LastWriteTimeUtc.ToFileTimeUtc())
				return false;
			if (reader.ReadInt64() != fileInfo.CreationTimeUtc.ToFileTimeUtc())
				return false;
			if (reader.ReadInt64() != fileInfo.Length)
				return false;
			if (new Guid(reader.ReadBytes(16)) != assembly.ManifestModule.ModuleVersionId)
				return false;
			return true;
		}
	}
}
