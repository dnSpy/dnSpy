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
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using dnSpy.Debugger.DotNet.CorDebug.Utilities;

namespace dnSpy.Debugger.DotNet.CorDebug.Impl {
	static class AppHostUtils {
		static int CalculateMaxAppHostExeSize() {
			ulong maxSize = 0;
			foreach (var info in AppHostInfoData.KnownAppHostInfos) {
				maxSize = Math.Max(maxSize, (ulong)info.HashDataOffset + info.HashDataSize);
				maxSize = Math.Max(maxSize, (ulong)info.RelPathOffset + AppHostInfo.MaxAppHostRelPathLength);
			}
			return checked((int)maxSize);
		}

		static readonly int MaxAppHostExeSize = CalculateMaxAppHostExeSize();
		const string AppHostExeUnpatched = "c3ab8ff13720e8ad9047dd39466b3c89" + "74e592c2fa383d4a3960714caef0c4f2";
		static readonly byte[] AppHostExeUnpatchedSignature = Encoding.UTF8.GetBytes(AppHostExeUnpatched + "\0");
		static readonly byte[] AppHostExeSignature = Encoding.UTF8.GetBytes("c3ab8ff13720e8ad9047dd39466b3c89" + "\0");

		// .NET Core 2.x, older .NET Core 3.0 previews
		internal static bool IsDotNetAppHostV1(string filename, [NotNullWhen(true)] out string? dllFilename) {
			// We detect the apphost.exe like so:
			//	- must have an exe extension
			//	- must be a PE file and an EXE (DLL bit cleared)
			//	- must not have .NET metadata
			//	- must have a file with the same name but a dll extension
			//	- this dll file must be a PE file and have .NET metadata

			// .NET Core 1.x: the apphost is a renamed dotnet.exe and it assumes (unless overridden
			// on the command line) that the managed dll is apphostname with a dll extension.
			// .NET Core 2.x-3.x: the relative path of the managed dll is part of the exe, patched
			// by an MSBuild task. Max utf8 string length is 1024 bytes. It's currently not possible
			// to override this path so it should be identical to apphostname with a dll extension,
			// unless someone patched the apphost exe (eg. dnSpy).

			// Windows apphosts have an .exe extension. Don't call Path.ChangeExtension() unless it's guaranteed
			// to have an .exe extension, eg. 'some.file' => 'some.file.dll', not 'some.dll'
			if (filename.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
				dllFilename = Path.ChangeExtension(filename, ".dll");
			else if (!filename.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
				dllFilename = filename + ".dll";
			else {
				dllFilename = null;
				return false;
			}
			if (!File.Exists(dllFilename))
				return false;
			if (PortableExecutableFileHelpers.IsPE(filename, out bool isExe, out bool hasDotNetMetadata) && (!isExe || hasDotNetMetadata))
				return false;
			if (!PortableExecutableFileHelpers.IsPE(dllFilename, out _, out hasDotNetMetadata) || !hasDotNetMetadata)
				return false;

			return true;
		}

		// Older .NET Core 3.0 previews
		internal static bool IsDotNetBundleV1(string filename) {
			try {
				using (var stream = File.OpenRead(filename)) {
					if (stream.Length < bundleSig.Length)
						return false;
					stream.Position = stream.Length - bundleSig.Length;
					var sig = new byte[bundleSig.Length];
					stream.Read(sig, 0, sig.Length);
					for (int i = 0; i < sig.Length; i++) {
						if (bundleSig[i] != sig[i])
							return false;
					}
					return true;
				}
			}
			catch {
			}
			return false;
		}
		// "\x0E.NetCoreBundle"
		static readonly byte[] bundleSig = new byte[] { 0x0E, 0x2E, 0x4E, 0x65, 0x74, 0x43, 0x6F, 0x72, 0x65, 0x42, 0x75, 0x6E, 0x64, 0x6C, 0x65 };

		// .NET Core 3.0
		internal static bool IsDotNetBundleV2_or_AppHost(string filename) {
			try {
				var data = ReadBytes(filename, 512 * 1024);
				return GetOffset(data, apphostSigV2) >= 0;
			}
			catch {
			}
			return false;
		}
		// SHA256(".net core bundle"). The previous 8 bytes is 0 or offset of the bundle header-offset
		static readonly byte[] apphostSigV2 = new byte[] {
			0x8B, 0x12, 0x02, 0xB9, 0x6A, 0x61, 0x20, 0x38, 0x72, 0x7B, 0x93, 0x02, 0x14, 0xD7, 0xA0, 0x32,
			0x13, 0xF5, 0xB9, 0xE6, 0xEF, 0xAE, 0x33, 0x18, 0xEE, 0x3B, 0x2D, 0xCE, 0x24, 0xB3, 0x6A, 0xAE,
		};

		static byte[] ReadBytes(string filename, int maxBytes) {
			using (var file = File.OpenRead(filename)) {
				int size = (int)Math.Min(file.Seek(0, SeekOrigin.End), maxBytes);
				file.Position = 0;
				var data = new byte[size];
				int sizeRead = file.Read(data, 0, data.Length);
				if (sizeRead != data.Length)
					throw new IOException($"Wanted to read {data.Length} bytes, but could only read {sizeRead} bytes, file '{filename}'");
				return data;
			}
		}

		internal static bool TryGetAppHostEmbeddedDotNetDllPath(string apphostFilename, out bool couldBeAppHost, [NotNullWhen(true)] out string? dotNetDllPath) {
			dotNetDllPath = null;
			couldBeAppHost = false;
			if (!File.Exists(apphostFilename))
				return false;
			if (PortableExecutableFileHelpers.IsPE(apphostFilename, out _, out var hasDotNetMetadata) && hasDotNetMetadata)
				return false;
			try {
				var data = ReadBytes(apphostFilename, MaxAppHostExeSize);
				if (GetOffset(data, AppHostExeUnpatchedSignature) >= 0) {
					couldBeAppHost = true;
					return false;
				}
				if (GetOffset(data, AppHostExeSignature) < 0)
					return false;
				couldBeAppHost = true;
				if (!ExeUtils.TryGetTextSectionInfo(new BinaryReader(new MemoryStream(data)), out _, out _))
					return false;

				var basePath = Path.GetDirectoryName(apphostFilename)!;
				foreach (var info in GetAppHostInfos(data)) {
					if (!TryGetUtf8StringZ(data, (int)info.RelPathOffset, AppHostInfo.MaxAppHostRelPathLength, out var relPath))
						continue;
					if (relPath == AppHostExeUnpatched)
						continue;
					string dotnetFile;
					try {
						dotnetFile = Path.Combine(basePath, relPath);
					}
					catch (ArgumentException) {
						continue;
					}
					if (!PortableExecutableFileHelpers.IsPE(dotnetFile, out _, out hasDotNetMetadata))
						continue;
					if (!hasDotNetMetadata)
						continue;
					dotNetDllPath = dotnetFile;
					return true;
				}
			}
			catch (IOException) {
			}
			return false;
		}

		static bool TryGetUtf8StringZ(byte[] data, int index, int maxLength, [NotNullWhen(true)] out string? relPath) {
			for (int i = 0; i < maxLength && (uint)(index + i) < (uint)data.Length; i++) {
				if (data[index + i] == 0) {
					relPath = Encoding.UTF8.GetString(data, index, i);
					return true;
				}
			}
			relPath = null;
			return false;
		}

		static IEnumerable<AppHostInfo> GetAppHostInfos(byte[] data) {
			uint hashDataOffset = 0, hashDataSize = 0;
			byte[]? hash = null;
			foreach (var info in AppHostInfoData.KnownAppHostInfos) {
				if (hash is null || !(hashDataOffset == info.HashDataOffset && hashDataSize == info.HashDataSize)) {
					if (Hash(data, info.HashDataOffset, info.HashDataSize, info.LastByte) is byte[] newHash) {
						hash = newHash;
						hashDataOffset = info.HashDataOffset;
						hashDataSize = info.HashDataSize;
					}
				}
				if (hash is not null && ByteArrayEquals(hash, info.Hash))
					yield return info;
			}
		}

		static byte[]? Hash(byte[] data, uint offset, uint size, byte lastByte) {
			if ((ulong)offset + size > (ulong)data.Length)
				return null;
			if (data[(int)(offset + size - 1)] != lastByte)
				return null;
			using (var sha1 = new SHA1Managed())
				return sha1.ComputeHash(data, (int)offset, (int)size);
		}

		static bool ByteArrayEquals(byte[]? a, byte[]? b) {
			if (a is null || b is null)
				return false;
			if (a.Length != b.Length)
				return false;
			for (int i = 0; i < a.Length; i++) {
				if (a[i] != b[i])
					return false;
			}
			return true;
		}

		static int GetOffset(byte[] bytes, byte[] pattern) {
			int si = 0;
			var b = pattern[0];
			while (si < bytes.Length) {
				si = Array.IndexOf(bytes, b, si);
				if (si < 0)
					break;
				if (Match(bytes, si, pattern))
					return si;
				si++;
			}
			return -1;
		}

		static bool Match(byte[] bytes, int index, byte[] pattern) {
			if (index + pattern.Length > bytes.Length)
				return false;
			for (int i = 0; i < pattern.Length; i++) {
				if (bytes[index + i] != pattern[i])
					return false;
			}
			return true;
		}
	}
}
