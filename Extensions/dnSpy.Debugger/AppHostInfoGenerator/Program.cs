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
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using dnSpy.Debugger.DotNet.CorDebug.Impl;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AppHostInfoGenerator {
	sealed class Program : IDisposable {
		// *** .NET Core 3.0 apphosts now have a signature (SHA256(".net core bundle")) so this table doesn't need to be updated anymore.
		// Add new versions from: https://www.nuget.org/packages/Microsoft.NETCore.DotNetAppHost/
		// The code ignores known versions so all versions can be added.
		//	^(\S+)\s.*		=>		\t\t\t"\1",
		static readonly string[] DotNetAppHost_Versions_ToCheck = new string[] {
			"3.0.0",
			"3.0.0-rc1-19456-20",
			"3.0.0-preview9-19423-09",
			"3.0.0-preview8-28405-07",
			"3.0.0-preview7-27912-14",
			"3.0.0-preview6-27804-01",
			"3.0.0-preview5-27626-15",
			"3.0.0-preview4-27615-11",
			"3.0.0-preview3-27503-5",
			"3.0.0-preview-27324-5",
			"3.0.0-preview-27122-01",
			"2.2.7",
			"2.2.6",
			"2.2.5",
			"2.2.4",
			"2.2.3",
			"2.2.2",
			"2.2.1",
			"2.2.0",
			"2.2.0-preview3-27014-02",
			"2.2.0-preview2-26905-02",
			"2.2.0-preview-26820-02",
			"2.1.13",
			"2.1.12",
			"2.1.11",
			"2.1.10",
			"2.1.9",
			"2.1.8",
			"2.1.7",
			"2.1.6",
			"2.1.5",
			"2.1.4",
			"2.1.3",
			"2.1.2",
			"2.1.1",
			"2.1.0",
			"2.1.0-rc1",
			"2.1.0-preview2-26406-04",
			"2.1.0-preview1-26216-03",
			"2.0.9",
			"2.0.7",
			"2.0.6",
			"2.0.5",
			"2.0.4",
			"2.0.3",
			"2.0.0",
			"2.0.0-preview2-25407-01",
			"2.0.0-preview1-002111-00",
		};
		const string NuGetPackageDownloadUrlFormatString = "https://www.nuget.org/api/v2/package/{0}/{1}";
		const string DotNetMyGetPackageDownloadUrlFormatString = "https://dotnet.myget.org/F/dotnet-core/api/v2/package/{0}/{1}";
		const string TizenNuGetPackageDownloadUrlFormatString = "https://tizen.myget.org/F/dotnet-core/api/v2/package/{0}/{1}";
		static readonly byte[] appHostRelPathHash = Encoding.UTF8.GetBytes("c3ab8ff13720e8ad9047dd39466b3c89" + "74e592c2fa383d4a3960714caef0c4f2" + "\0");
		static readonly byte[] appHostSignature = Encoding.UTF8.GetBytes("c3ab8ff13720e8ad9047dd39466b3c89" + "\0");
		const int MinHashSize = 0x800;

		enum NuGetSource {
			NuGet,
			DotNetMyGet,
			TizenMyGet,
		}

		static readonly NuGetSource[] dotnetNugetSources = new[] { NuGetSource.NuGet, NuGetSource.DotNetMyGet };
		static readonly NuGetSource[] tizenNugetSources = new[] { NuGetSource.TizenMyGet, NuGetSource.NuGet };

		const string defaultFilename = nameof(AppHostInfoData) + ".g.cs";
		static void Usage() =>
			Console.WriteLine("Usage: AppHostInfoGenerator [path-to-" + defaultFilename + "]");

		static int Main(string[] args) {
			string filename;
			switch (args.Length) {
			case 0:
				filename = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(typeof(Program).Assembly.Location)!, "..", "..", "..", "..", "dnSpy.Debugger.DotNet.CorDebug", "Impl", defaultFilename));
				break;
			case 1:
				filename = args[0];
				break;
			default:
				Usage();
				return 1;
			}
			using (var p = new Program(filename))
				return p.DoIt();
		}

		readonly TextWriter output;

		Program(string filename) =>
			output = new StreamWriter(filename, append: false, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true));

		public void Dispose() => output.Dispose();

		int DoIt() {
			try {
				output.WriteLine("/*");
				output.WriteLine("    Copyright (C) 2014-2019 de4dot@gmail.com");
				output.WriteLine();
				output.WriteLine("    This file is part of dnSpy");
				output.WriteLine();
				output.WriteLine("    dnSpy is free software: you can redistribute it and/or modify");
				output.WriteLine("    it under the terms of the GNU General Public License as published by");
				output.WriteLine("    the Free Software Foundation, either version 3 of the License, or");
				output.WriteLine("    (at your option) any later version.");
				output.WriteLine();
				output.WriteLine("    dnSpy is distributed in the hope that it will be useful,");
				output.WriteLine("    but WITHOUT ANY WARRANTY; without even the implied warranty of");
				output.WriteLine("    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the");
				output.WriteLine("    GNU General Public License for more details.");
				output.WriteLine();
				output.WriteLine("    You should have received a copy of the GNU General Public License");
				output.WriteLine("    along with dnSpy.  If not, see <http://www.gnu.org/licenses/>.");
				output.WriteLine("*/");
				output.WriteLine();
				output.WriteLine("// This is a generated file, use the AppHostInfoGenerator project to update it");
				output.WriteLine("#nullable enable");
				output.WriteLine();
				output.WriteLine("namespace dnSpy.Debugger.DotNet.CorDebug.Impl {");
				output.WriteLine("\tstatic partial class AppHostInfoData {");

				var knownVersions = new HashSet<string>(AppHostInfoData.KnownAppHostInfos.Select(a => a.Version), StringComparer.Ordinal);
				var newInfos = new List<AppHostInfo>();
				var errors = new List<string>();
				foreach (var version in DotNetAppHost_Versions_ToCheck) {
					if (knownVersions.Contains(version))
						continue;

					Console.WriteLine();
					Console.WriteLine($"Runtime version: {version}");

					var fileData = DownloadNuGetPackage("Microsoft.NETCore.DotNetAppHost", version, NuGetSource.NuGet);
					using (var zip = new ZipArchive(new MemoryStream(fileData), ZipArchiveMode.Read, leaveOpen: false)) {
						var runtimeJsonString = GetFileAsString(zip, "runtime.json");
						var runtimeJson = (JObject)JsonConvert.DeserializeObject(runtimeJsonString)!;
						foreach (JProperty runtime in runtimeJson["runtimes"]!) {
							var runtimeName = runtime.Name;
							if (runtime.Count != 1)
								throw new InvalidOperationException("Expected 1 child");
							var dotNetAppHostObject = (JObject)runtime.First!;
							var dotNetAppHostObject2 = (JObject)dotNetAppHostObject["Microsoft.NETCore.DotNetAppHost"]!;
							if (dotNetAppHostObject2.Count != 1)
								throw new InvalidOperationException("Expected 1 child");
							var dotNetAppHostProperty = (JProperty)dotNetAppHostObject2.First!;
							if (dotNetAppHostProperty.Count != 1)
								throw new InvalidOperationException("Expected 1 child");
							var runtimePackageName = dotNetAppHostProperty.Name;
							var runtimePackageVersion = GetNuGetVersion((string)((JValue)dotNetAppHostProperty.Value).Value!);
							Console.WriteLine();
							Console.WriteLine($"{runtimePackageName} {runtimePackageVersion}");
							NuGetSource[] nugetSources;
							if (runtimeName.StartsWith("tizen.", StringComparison.Ordinal))
								nugetSources = tizenNugetSources;
							else
								nugetSources = dotnetNugetSources;
							bool couldDownload = false;
							byte[]? ridData = null;
							foreach (var nugetSource in nugetSources) {
								if (TryDownloadNuGetPackage(runtimePackageName, runtimePackageVersion, nugetSource, out ridData)) {
									couldDownload = true;
									break;
								}
							}
							if (!couldDownload) {
								var error = $"***ERROR: 404 NOT FOUND: Couldn't download {runtimePackageName} = {runtimePackageVersion}";
								errors.Add(error);
								Console.WriteLine(error);
								continue;
							}
							Debug.Assert(!(ridData is null));
							if (ridData is null)
								throw new InvalidOperationException();
							using (var ridZip = new ZipArchive(new MemoryStream(ridData), ZipArchiveMode.Read, leaveOpen: false)) {
								var appHostEntries = GetAppHostEntries(ridZip).ToArray();
								if (appHostEntries.Length == 0)
									throw new InvalidOperationException("Expected at least one apphost");
								foreach (var info in appHostEntries) {
									if (info.rid != runtimeName)
										throw new InvalidOperationException($"Expected rid='{runtimeName}' but got '{info.rid}' from the zip file");
									var appHostData = GetData(info.entry);
									int relPathOffset = GetOffset(appHostData, appHostRelPathHash);
									if (relPathOffset < 0)
										throw new InvalidOperationException($"Couldn't get offset of hash in apphost: '{info.entry.FullName}'");
									int sigOffset = GetOffset(appHostData, appHostSignature);
									if (sigOffset < 0)
										throw new InvalidOperationException($"Couldn't get offset of sig in apphost: '{info.entry.FullName}'");
									bool mustBeZero = false;
									for (int i = 0; i < AppHostInfo.MaxAppHostRelPathLength; i++) {
										byte b = appHostData[relPathOffset + i];
										if (mustBeZero) {
											if (b != 0)
												throw new InvalidOperationException($"Not zero padded or the string data is smaller");
										}
										else
											mustBeZero = b == 0;
									}
									var exeReader = new BinaryReader(new MemoryStream(appHostData));
									if (!ExeUtils.TryGetTextSectionInfo(exeReader, out var textOffset, out var textSize))
										throw new InvalidOperationException("Could not get .text offset/size");
									if (!TryHashData(appHostData, relPathOffset, textOffset, textSize, out var hashDataOffset, out var hashDataSize, out var hash, out var lastByte))
										throw new InvalidOperationException("Failed to hash the .text section");
									newInfos.Add(new AppHostInfo(info.rid, runtimePackageVersion, (uint)relPathOffset, (uint)hashDataOffset, (uint)hashDataSize, hash, lastByte));
								}
							}
						}
					}
				}

				errors.AddRange(AppHostInfoData.GetErrors());
#if !APPHOSTINFO_ERROR_STRINGS
#error APPHOSTINFO_ERROR_STRINGS must be defined at the project level so error strings can be restored
#endif
				output.WriteLine("#if APPHOSTINFO_ERROR_STRINGS");
				output.WriteLine("\t\tpublic static string[] GetErrors() =>");
				output.WriteLine("\t\t\tnew string[] {");
				foreach (var error in errors)
					output.WriteLine(serializeIndent + "\"" + error + "\",");
				output.WriteLine("\t\t\t};");
				output.WriteLine("#endif");
				output.WriteLine();

				var addedInfos = new HashSet<AppHostInfo>(AppHostInfoDupeEqualityComparer.Instance);

				var allInfos = new List<AppHostInfo>(newInfos);
				allInfos.AddRange(AppHostInfoData.KnownAppHostInfos);

				var stringsTable = new Dictionary<string, uint>(StringComparer.Ordinal);
				var serializedData = AppHostInfoData.GetSerializedAppHostInfos();
				if (serializedData.Length > 0) {
					int o = 0;
					uint numStrings = AppHostInfoData.DeserializeCompressedUInt32(serializedData, ref o);
					for (uint i = 0; i < numStrings; i++)
						stringsTable.Add(AppHostInfoData.DeserializeString(serializedData, ref o), i);
#if !APPHOSTINFO_STRINGS
#error APPHOSTINFO_STRINGS must be defined at the project level so strings can be restored
#endif
					if (numStrings == 0)
						throw new InvalidOperationException("No strings");
				}

				foreach (var info in allInfos) {
					if (!stringsTable.ContainsKey(info.Rid))
						stringsTable.Add(info.Rid, (uint)stringsTable.Count);
				}
				foreach (var info in allInfos) {
					if (!stringsTable.ContainsKey(info.Version))
						stringsTable.Add(info.Version, (uint)stringsTable.Count);
				}

				int expectedStringIndex = 0;
				output.WriteLine("\t\tpublic static byte[] GetSerializedAppHostInfos() =>");
				output.WriteLine("\t\t\tnew byte[] {");
				output.WriteLine("#if APPHOSTINFO_STRINGS");
				SerializeCompressedUInt32((uint)stringsTable.Count, "StringsTableCount");
				foreach (var kv in stringsTable.OrderBy(a => a.Value)) {
					if (kv.Value != expectedStringIndex)
						throw new InvalidOperationException();
					SerializeString(kv.Key, null);
					expectedStringIndex++;
				}
				output.WriteLine("#endif");
				int numDupes = 0;
				foreach (var info in allInfos) {
					if (info.Rid.Length == 0 || info.Version.Length == 0)
						throw new InvalidOperationException();
					bool dupe = !addedInfos.Add(info);
					if (dupe)
						numDupes++;
					Serialize(info, stringsTable, dupe);
				}
				output.WriteLine("\t\t\t};");
				output.WriteLine("\t\tpublic const int SerializedAppHostInfosCount =");
				output.WriteLine("#if APPHOSTINFO_DUPES");
				output.WriteLine($"\t\t\t{allInfos.Count};");
				output.WriteLine("#else");
				output.WriteLine($"\t\t\t{allInfos.Count - numDupes};");
				output.WriteLine("#endif");
				output.WriteLine("\t}");
				output.WriteLine("}");

				Console.WriteLine($"{newInfos.Count} new infos");

				var hashes = new Dictionary<AppHostInfo, List<AppHostInfo>>(AppHostInfoEqualityComparer.Instance);
				foreach (var info in AppHostInfoData.KnownAppHostInfos.Concat(newInfos)) {
					if (!hashes.TryGetValue(info, out var list))
						hashes.Add(info, list = new List<AppHostInfo>());
					list.Add(info);
				}
				foreach (var kv in hashes) {
					var list = kv.Value;
					var info = list[0];
					bool bad = false;
					for (int i = 1; i < list.Count; i++) {
						// If all hash fields are the same, then we require that RelPathOffset also be
						// the same. If this is a problem, hash more data, or allow RelPathOffset to be
						// different (need to add code to verify the string at that location and try
						// the other offset if it's not a valid file).
						if (info.RelPathOffset != list[i].RelPathOffset) {
							bad = true;
							break;
						}
					}
					if (bad) {
						Console.WriteLine($"*** ERROR: The following apphosts have the same hash but different RelPathOffset:");
						foreach (var info2 in list)
							Console.WriteLine($"\t{info2.Rid} {info2.Version} RelPathOffset=0x{info2.RelPathOffset.ToString("X8")}");
					}
				}

				return 0;
			}
			catch (Exception ex) {
				Console.WriteLine(ex.ToString());
				return 1;
			}
		}

		static string GetNuGetVersion(string version) {
			if (version.StartsWith("[")) {
				var parts = version.Substring(1).Split(',');
				if (parts.Length != 2)
					throw new InvalidOperationException();
				if (!parts[1].EndsWith(" )"))
					throw new InvalidOperationException();
				return parts[0];
			}
			return version;
		}

		sealed class AppHostInfoDupeEqualityComparer : IEqualityComparer<AppHostInfo> {
			public static readonly AppHostInfoDupeEqualityComparer Instance = new AppHostInfoDupeEqualityComparer();
			AppHostInfoDupeEqualityComparer() { }

			public bool Equals([AllowNull] AppHostInfo x, [AllowNull] AppHostInfo y) =>
				x.RelPathOffset == y.RelPathOffset &&
				x.HashDataOffset == y.HashDataOffset &&
				x.HashDataSize == y.HashDataSize &&
				AppHostInfoEqualityComparer.ByteArrayEquals(x.Hash, y.Hash);

			public int GetHashCode([DisallowNull] AppHostInfo obj) =>
				(int)(obj.RelPathOffset ^ obj.HashDataOffset ^ obj.HashDataSize) ^ AppHostInfoEqualityComparer.ByteArrayGetHashCode(obj.Hash);
		}

		sealed class AppHostInfoEqualityComparer : IEqualityComparer<AppHostInfo> {
			public static readonly AppHostInfoEqualityComparer Instance = new AppHostInfoEqualityComparer();
			AppHostInfoEqualityComparer() { }

			public bool Equals([AllowNull] AppHostInfo x, [AllowNull] AppHostInfo y) =>
				x.HashDataOffset == y.HashDataOffset &&
				x.HashDataSize == y.HashDataSize &&
				ByteArrayEquals(x.Hash, y.Hash);

			public int GetHashCode([DisallowNull] AppHostInfo obj) =>
				(int)(obj.HashDataOffset ^ obj.HashDataSize) ^ ByteArrayGetHashCode(obj.Hash);

			internal static bool ByteArrayEquals(byte[] a, byte[] b) {
				if (a.Length != b.Length)
					return false;
				for (int i = 0; i < a.Length; i++) {
					if (a[i] != b[i])
						return false;
				}
				return true;
			}

			// It's a sha1 hash, return the 1st 4 bytes
			internal static int ByteArrayGetHashCode(byte[] a) => BitConverter.ToInt32(a, 0);
		}

		void Serialize(in AppHostInfo info, Dictionary<string, uint> stringsTable, bool dupe) {
			output.WriteLine();
#if !APPHOSTINFO_DUPES
#error APPHOSTINFO_DUPES must be defined at the project level so all data can be restored
#endif
			if (dupe)
				output.WriteLine("#if APPHOSTINFO_DUPES");
			output.WriteLine("#if APPHOSTINFO_STRINGS");
			SerializeString(info.Rid, nameof(info.Rid), stringsTable);
			SerializeString(info.Version, nameof(info.Version), stringsTable);
			output.WriteLine("#endif");
			SerializeCompressedUInt32(info.RelPathOffset, nameof(info.RelPathOffset));
			SerializeCompressedUInt32(info.HashDataOffset, nameof(info.HashDataOffset));
			if (info.HashDataSize != AppHostInfo.DefaultHashSize)
				throw new InvalidOperationException($"{nameof(info.HashDataSize)} = 0x{info.HashDataSize:X} != 0x{AppHostInfo.DefaultHashSize:X}");
			SerializeByteArray(info.Hash, nameof(info.Hash), null, needLength: false);
			SerializeByte(info.LastByte, nameof(info.LastByte));
			if (dupe)
				output.WriteLine("#endif");
		}
		const string serializeIndent = "\t\t\t\t";

		void WriteComment(string? name, string? origValue) {
			if (name is null) {
				if (!(origValue is null))
					output.Write($"// {origValue}");
			}
			else if (origValue is null)
				output.Write($"// {name}");
			else
				output.Write($"// {name} = {origValue}");
		}

		void SerializeString(string value, string name, Dictionary<string, uint> stringsTable) {
			uint index = stringsTable[value];
			var commentValue = $"string({index}) = {value}";
			SerializeCompressedUInt32(index, name, commentValue);
		}

		void SerializeString(string value, string? name) {
			var encoding = AppHostInfoData.StringEncoding;
			var data = encoding.GetBytes(value);
			if (encoding.GetString(data) != value)
				throw new InvalidOperationException();
			SerializeByteArray(data, name, value, needLength: true);
		}

		void SerializeCompressedUInt32(uint value, string name, string? commentValue = null) {
			output.Write(serializeIndent);

			bool needSpace = false;
			var currentValue = value;
			for (;;) {
				if (needSpace)
					output.Write(" ");
				needSpace = true;
				uint v = currentValue;
				if (v < 0x80)
					output.Write($"0x{((byte)currentValue).ToString("X2")},");
				else
					output.Write($"0x{((byte)(currentValue | 0x80)).ToString("X2")},");
				currentValue >>= 7;
				if (currentValue == 0)
					break;
			}

			if (commentValue is null)
				commentValue = "0x" + value.ToString("X8");
			WriteComment(name, commentValue);
			output.WriteLine();
		}

		void SerializeByte(byte value, string name) {
			output.Write(serializeIndent);

			output.Write($"0x{value.ToString("X2")},");

			WriteComment(name, null);
			output.WriteLine();
		}

		void SerializeByteArray(byte[] value, string? name, string? origValue, bool needLength) {
			output.Write(serializeIndent);
			if (value.Length > byte.MaxValue)
				throw new InvalidOperationException();

			bool needComma = false;
			if (needLength) {
				output.Write("0x");
				output.Write(value.Length.ToString("X2"));
				needComma = true;
			}
			for (int i = 0; i < value.Length; i++) {
				if (needComma)
					output.Write(", ");
				output.Write("0x");
				output.Write(value[i].ToString("X2"));
				needComma = true;
			}
			output.Write(',');

			WriteComment(name, origValue);
			output.WriteLine();
		}

		static bool TryHashData(byte[] appHostData, int relPathOffset, int textOffset, int textSize, out int hashDataOffset, out int hashDataSize, [NotNullWhen(true)] out byte[]? hash, out byte lastByte) {
			hashDataOffset = textOffset;
			hashDataSize = Math.Min(textSize, AppHostInfo.DefaultHashSize);
			int hashDataSizeEnd = hashDataOffset + hashDataSize;
			int relPathOffsetEnd = relPathOffset + AppHostInfo.MaxAppHostRelPathLength;
			if ((hashDataOffset >= relPathOffsetEnd || hashDataSizeEnd <= relPathOffset) && hashDataSize >= MinHashSize) {
				using (var sha1 = new SHA1Managed())
					hash = sha1.ComputeHash(appHostData, hashDataOffset, hashDataSize);
				lastByte = appHostData[hashDataOffset + hashDataSize - 1];
				return true;
			}
			hash = null;
			lastByte = 0;
			return false;
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

		const string runtimesDir = "runtimes";
		const string nativeDir = "native";
		static readonly HashSet<string> apphostNames = new HashSet<string>(StringComparer.Ordinal) {
			"apphost",
			"apphost.exe",
		};
		static readonly HashSet<string> ignoredNames = new HashSet<string>(StringComparer.Ordinal) {
			"comhost.dll",
			"ijwhost.dll",
			"ijwhost.lib",
			"libnethost.dylib",
			"libnethost.so",
			"nethost.dll",
			"nethost.h",
			"nethost.lib",
		};
		static IEnumerable<(ZipArchiveEntry entry, string rid)> GetAppHostEntries(ZipArchive zip) {
			foreach (var entry in zip.Entries) {
				var fullName = entry.FullName;
				if (!TryGetRid(fullName, out var rid, out var filename)) {
					if (fullName.StartsWith(runtimesDir + "/")) {
						Debug.Assert(false);
						throw new InvalidOperationException($"Unknown {runtimesDir} dir filename, not an apphost: '{filename}'");
					}
					continue;
				}
				if (ignoredNames.Contains(filename))
					continue;
				if (!apphostNames.Contains(filename)) {
					Debug.Assert(false);
					throw new InvalidOperationException($"Unknown apphost filename: '{filename}', fullName = '{fullName}'");
				}
				yield return (entry, rid);
			}
		}

		static bool TryGetRid(string fullName, [NotNullWhen(true)] out string? rid, [NotNullWhen(true)] out string? filename) {
			rid = null;
			filename = null;
			var parts = fullName.Split('/');
			if (parts.Length != 4)
				return false;
			if (parts[0] != runtimesDir)
				return false;
			if (parts[2] != nativeDir)
				return false;
			rid = parts[1];
			filename = parts[3];
			return true;
		}

		static byte[] DownloadNuGetPackage(string packageName, string version, NuGetSource nugetSource) {
			string formatString;
			switch (nugetSource) {
			case NuGetSource.NuGet:
				formatString = NuGetPackageDownloadUrlFormatString;
				break;
			case NuGetSource.DotNetMyGet:
				formatString = DotNetMyGetPackageDownloadUrlFormatString;
				break;
			case NuGetSource.TizenMyGet:
				formatString = TizenNuGetPackageDownloadUrlFormatString;
				break;
			default:
				throw new ArgumentOutOfRangeException(nameof(nugetSource));
			}
			var url = string.Format(formatString, packageName, version);
			Console.WriteLine($"Downloading {url}");
			using (var wc = new WebClient())
				return wc.DownloadData(url);
		}

		static bool TryDownloadNuGetPackage(string packageName, string version, NuGetSource nugetSource, [NotNullWhen(true)] out byte[]? data) {
			try {
				data = DownloadNuGetPackage(packageName, version, nugetSource);
				return true;
			}
			catch (WebException wex) when (wex.Response is HttpWebResponse responce && responce.StatusCode == HttpStatusCode.NotFound) {
				data = null;
				return false;
			}
		}

		static byte[] GetData(ZipArchive zip, string name) {
			var entry = zip.GetEntry(name);
			if (entry is null)
				throw new InvalidOperationException($"Couldn't find {name} in zip file");
			return GetData(entry);
		}

		static byte[] GetData(ZipArchiveEntry entry) {
			var data = new byte[entry.Length];
			using (var runtimeJsonStream = entry.Open()) {
				if (runtimeJsonStream.Read(data, 0, data.Length) != data.Length)
					throw new InvalidOperationException($"Could not read all bytes from compressed '{entry.FullName}'");
			}
			return data;
		}

		static string GetFileAsString(ZipArchive zip, string name) =>
			Encoding.UTF8.GetString(GetData(zip, name));
	}
}
