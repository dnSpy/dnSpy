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
using System.IO;
using System.Text;
using dnlib.DotNet.MD;
using dnlib.PE;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace MakeEverythingPublic {
	public sealed class MakeEverythingPublic : Task {
		// Increment it if something changes so the files are re-created
		const string VERSION = "v1";

		[Required]
		public string IVTString { get; set; }

		[Required]
		public string DestinationDirectory { get; set; }

		[Required]
		public string AssembliesToMakePublic { get; set; }

		[Required]
		public ITaskItem[] ReferencePath { get; set; }

		[Output]
		public ITaskItem[] OutputReferencePath { get; private set; }

		public override bool Execute() {
			if (string.IsNullOrWhiteSpace(IVTString)) {
				Log.LogMessageFromText(nameof(IVTString) + " is an empty string", MessageImportance.High);
				return false;
			}

			if (string.IsNullOrWhiteSpace(DestinationDirectory)) {
				Log.LogMessageFromText(nameof(DestinationDirectory) + " is an empty string", MessageImportance.High);
				return false;
			}

			var assembliesToFix = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (var tmp in AssembliesToMakePublic.Split(';')) {
				var asmName = tmp.Trim();
				var asmSimpleName = asmName;
				int index = asmSimpleName.IndexOf(',');
				if (index >= 0)
					asmSimpleName = asmSimpleName.Substring(0, index).Trim();
				if (asmSimpleName.Length == 0)
					continue;
				assembliesToFix.Add(asmSimpleName);
			}

			OutputReferencePath = new ITaskItem[ReferencePath.Length];
			byte[] ivtBlob = null;
			for (int i = 0; i < ReferencePath.Length; i++) {
				var file = ReferencePath[i];
				OutputReferencePath[i] = file;
				var filename = file.ItemSpec;
				var fileExt = Path.GetExtension(filename);
				var asmSimpleName = Path.GetFileNameWithoutExtension(filename);
				if (!assembliesToFix.Contains(asmSimpleName))
					continue;
				if (!File.Exists(filename)) {
					Log.LogMessageFromText($"File does not exist: {filename}", MessageImportance.High);
					return false;
				}

				var patchDir = DestinationDirectory;
				Directory.CreateDirectory(patchDir);

				var fileInfo = new FileInfo(filename);
				long filesize = fileInfo.Length;
				long writeTime = fileInfo.LastWriteTimeUtc.ToBinary();

				var extraInfo = $"_{VERSION} {filesize} {writeTime}_";
				var patchedFilename = Path.Combine(patchDir, asmSimpleName + extraInfo + fileExt);
				if (StringComparer.OrdinalIgnoreCase.Equals(patchedFilename, filename))
					continue;

				if (!File.Exists(patchedFilename)) {
					if (ivtBlob == null)
						ivtBlob = CreateIVTBlob(IVTString);
					var data = File.ReadAllBytes(filename);
					try {
						using (var peImage = new PEImage(data, filename, ImageLayout.File, verify: true)) {
							using (var md = MetadataFactory.CreateMetadata(peImage, verify: true)) {
								var result = new IVTPatcher(data, md, ivtBlob).Patch();
								if (result != IVTPatcherResult.OK) {
									string errMsg;
									switch (result) {
									case IVTPatcherResult.NoCustomAttributes:
										errMsg = $"Assembly '{asmSimpleName}' has no custom attributes";
										break;
									case IVTPatcherResult.NoIVTs:
										errMsg = $"Assembly '{asmSimpleName}' has no InternalsVisibleToAttributes";
										break;
									case IVTPatcherResult.IVTBlobTooSmall:
										errMsg = $"Assembly '{asmSimpleName}' has no InternalsVisibleToAttribute blob that is big enough to store '{IVTString}'. Use a shorter assembly name and/or a shorter public key, or skip PublicKey=xxxx... altogether (if it's a C# assembly)";
										break;
									default:
										Debug.Fail($"Unknown error result: {result}");
										errMsg = "Unknown error";
										break;
									}
									Log.LogMessageFromText(errMsg, MessageImportance.High);
									return false;
								}
								try {
									File.WriteAllBytes(patchedFilename, data);
								}
								catch {
									try { File.Delete(patchedFilename); } catch { }
									throw;
								}
							}
						}
					}
					catch (Exception ex) when (ex is IOException || ex is BadImageFormatException) {
						Log.LogMessageFromText($"File '{filename}' is not a .NET file", MessageImportance.High);
						return false;
					}

					var xmlDocFile = Path.ChangeExtension(filename, "xml");
					if (File.Exists(xmlDocFile)) {
						var newXmlDocFile = Path.ChangeExtension(patchedFilename, "xml");
						if (File.Exists(newXmlDocFile))
							File.Delete(newXmlDocFile);
						File.Copy(xmlDocFile, newXmlDocFile);
					}
				}

				OutputReferencePath[i] = new TaskItem(patchedFilename);
			}

			return true;
		}

		static byte[] CreateIVTBlob(string newIVTString) {
			var caStream = new MemoryStream();
			var caWriter = new BinaryWriter(caStream);
			caWriter.Write((ushort)1);
			WriteString(caWriter, newIVTString);
			caWriter.Write((ushort)0);
			var newIVTBlob = caStream.ToArray();
			var compressedSize = GetCompressedUInt32Bytes((uint)newIVTBlob.Length);
			var blob = new byte[compressedSize + newIVTBlob.Length];
			var blobStream = new MemoryStream(blob);
			var blobWriter = new BinaryWriter(blobStream);
			WriteCompressedUInt32(blobWriter, (uint)newIVTBlob.Length);
			blobWriter.Write(newIVTBlob);
			if (blobWriter.BaseStream.Position != blob.Length)
				throw new InvalidOperationException();
			return blob;
		}

		static void WriteString(BinaryWriter writer, string s) {
			var bytes = Encoding.UTF8.GetBytes(s);
			WriteCompressedUInt32(writer, (uint)bytes.Length);
			writer.Write(bytes);
		}

		static void WriteCompressedUInt32(BinaryWriter writer, uint value) {
			if (value <= 0x7F)
				writer.Write((byte)value);
			else if (value <= 0x3FFF) {
				writer.Write((byte)((value >> 8) | 0x80));
				writer.Write((byte)value);
			}
			else if (value <= 0x1FFFFFFF) {
				writer.Write((byte)((value >> 24) | 0xC0));
				writer.Write((byte)(value >> 16));
				writer.Write((byte)(value >> 8));
				writer.Write((byte)value);
			}
			else
				throw new ArgumentOutOfRangeException("UInt32 value can't be compressed");
		}

		static uint GetCompressedUInt32Bytes(uint value) {
			if (value <= 0x7F)
				return 1;
			if (value <= 0x3FFF)
				return 2;
			else if (value <= 0x1FFFFFFF)
				return 4;
			throw new ArgumentOutOfRangeException("UInt32 value can't be compressed");
		}
	}
}
