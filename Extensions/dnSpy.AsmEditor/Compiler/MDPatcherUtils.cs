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
using dnlib.DotNet;
using dnlib.DotNet.MD;
using dnlib.PE;

namespace dnSpy.AsmEditor.Compiler {
	static unsafe class MDPatcherUtils {
		public sealed class InvalidMetadataException : Exception { }

		public static IEnumerable<TypeDef> GetMetadataTypes(TypeDef type) {
			if (!ExistsInMetadata(type))
				yield break;
			yield return type;
			foreach (var nested in type.GetTypes()) {
				if (ExistsInMetadata(nested))
					yield return nested;
			}
		}

		public static int GetCompressedUInt32Length(uint value) {
			if (value <= 0x7F)
				return 1;
			if (value <= 0x3FFF)
				return 2;
			Debug.Assert(value <= 0x1FFFFFFF);
			return 4;
		}

		public unsafe static uint ReadCompressedUInt32(ref byte* data, byte* end) {
			if (data >= end)
				throw new InvalidMetadataException();
			byte b = *data++;
			if ((b & 0x80) == 0)
				return b;

			if ((b & 0xC0) == 0x80) {
				if (data >= end)
					throw new InvalidMetadataException();
				return (uint)(((b & 0x3F) << 8) | *data++);
			}

			if (data + 2 >= end)
				throw new InvalidMetadataException();
			return (uint)(((b & 0x1F) << 24) | (*data++ << 16) |
					(*data++ << 8) | *data++);
		}

		public static bool ExistsInMetadata(TypeDef? type) => type is not null && !(type is TypeDefUser);

		public static bool ReferencesModule(ModuleDef sourceModule, ModuleDef? targetModule) {
			if (targetModule is null)
				return false;

			if (sourceModule == targetModule)
				return true;

			var targetAssembly = targetModule.Assembly;
			if (targetAssembly is not null) {
				// Don't compare version, there could be binding redirects
				var asmComparer = new AssemblyNameComparer(AssemblyNameComparerFlags.Name | AssemblyNameComparerFlags.PublicKeyToken | AssemblyNameComparerFlags.Culture | AssemblyNameComparerFlags.ContentType);
				foreach (var asmRef in sourceModule.GetAssemblyRefs()) {
					if (asmComparer.Equals(asmRef, targetAssembly))
						return true;
				}

				if (targetAssembly == sourceModule.Assembly) {
					foreach (var modRef in sourceModule.GetModuleRefs()) {
						if (StringComparer.OrdinalIgnoreCase.Equals(modRef.Name.String, targetModule.Name.String))
							return true;
					}
				}
			}

			return false;
		}

		public static Metadata? TryCreateMetadata(RawModuleBytes moduleData, bool isFileLayout) {
			try {
				return MetadataFactory.CreateMetadata(new PEImage((IntPtr)moduleData.Pointer, (uint)moduleData.Size, isFileLayout ? ImageLayout.File : ImageLayout.Memory, verify: true));
			}
			catch (IOException) {
			}
			catch (BadImageFormatException) {
			}
			return null;
		}

		public static bool CheckTypeDefOrTypeRefName(ITypeDefOrRef? tdr, UTF8String @namespace, UTF8String name) {
			if (tdr is TypeDef td)
				return td.Name == name && td.Namespace == @namespace;
			if (tdr is TypeRef tr)
				return tr.Name == name && tr.Namespace == @namespace;
			return false;
		}

		public static bool HasModuleInternalAccess(ModuleDef targetModule, ModuleDef sourceModule) =>
			HasIgnoresAccessChecksToAttribute(targetModule, sourceModule) ||
			HasInternalsVisibleToAttribute(targetModule, sourceModule);

		static bool HasIgnoresAccessChecksToAttribute(ModuleDef targetModule, ModuleDef sourceModule) {
			var targetAssembly = targetModule.Assembly;
			if (targetAssembly is null)
				return false;
			var sourceAssembly = sourceModule.Assembly;
			if (sourceAssembly is null)
				return false;
			foreach (var ca in sourceAssembly.CustomAttributes) {
				if (ca.ConstructorArguments.Count != 1)
					continue;
				if (!MDPatcherUtils.CheckTypeDefOrTypeRefName(ca.Constructor?.DeclaringType, nameSystem_Runtime_CompilerServices, nameIgnoresAccessChecksToAttribute))
					continue;
				var asmName = (ca.ConstructorArguments[0].Value as UTF8String)?.String;
				if (asmName is null)
					continue;
				int index = asmName.IndexOf(',');
				if (index >= 0)
					asmName = asmName.Substring(0, index);
				asmName = asmName.Trim();
				if (!StringComparer.OrdinalIgnoreCase.Equals(targetAssembly.Name.String, asmName))
					continue;

				return true;
			}
			return false;
		}
		static readonly UTF8String nameSystem_Runtime_CompilerServices = new UTF8String("System.Runtime.CompilerServices");
		static readonly UTF8String nameIgnoresAccessChecksToAttribute = new UTF8String("IgnoresAccessChecksToAttribute");
		static readonly UTF8String nameInternalsVisibleToAttribute = new UTF8String("InternalsVisibleToAttribute");

		static bool HasInternalsVisibleToAttribute(ModuleDef targetModule, ModuleDef sourceModule) {
			var targetAssembly = targetModule.Assembly;
			if (targetAssembly is null)
				return false;
			var sourceAssembly = sourceModule.Assembly;
			if (sourceAssembly is null)
				return false;
			foreach (var ca in targetAssembly.CustomAttributes) {
				if (ca.ConstructorArguments.Count != 1)
					continue;
				if (!MDPatcherUtils.CheckTypeDefOrTypeRefName(ca.Constructor?.DeclaringType, nameSystem_Runtime_CompilerServices, nameInternalsVisibleToAttribute))
					continue;
				var asmName = (ca.ConstructorArguments[0].Value as UTF8String)?.String;
				if (asmName is null)
					continue;
				int index = asmName.IndexOf(',');
				if (index >= 0)
					asmName = asmName.Substring(0, index);
				asmName = asmName.Trim();
				if (!StringComparer.OrdinalIgnoreCase.Equals(sourceAssembly.Name.String, asmName))
					continue;

				return true;

			}
			return false;
		}

		public static byte[] CreateIVTBlob(IAssembly sourceAssembly) => CreateIVTBlob(GetIVTString(sourceAssembly));

		static string GetIVTString(IAssembly sourceAssembly) {
			if (sourceAssembly.PublicKeyOrToken is PublicKeyToken)
				throw new InvalidOperationException("PublicKey must be used or it must be null");
			var publicKeyBytes = (sourceAssembly.PublicKeyOrToken as PublicKey)?.Data;
			if (publicKeyBytes is null || publicKeyBytes.Length == 0)
				return sourceAssembly.Name;
			else {
				var sb = new StringBuilder();
				sb.Append(sourceAssembly.Name.String);
				sb.Append(", PublicKey=");
				foreach (var b in publicKeyBytes) {
					sb.Append(ToLowerHexDigit(b >> 4));
					sb.Append(ToLowerHexDigit(b & 0x0F));
				}
				return sb.ToString();
			}
		}

		public static byte[] CreateIVTBlob(string newIVTString) {
			var caStream = new MemoryStream();
			var caWriter = new BinaryWriter(caStream);
			caWriter.Write((ushort)1);
			WriteString(caWriter, newIVTString);
			caWriter.Write((ushort)0);
			return caStream.ToArray();
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

		static char ToLowerHexDigit(int b) {
			Debug.Assert(0 <= b && b <= 0xF);
			if (b < 10)
				return (char)('0' + b);
			return (char)('a' + b - 10);
		}
	}
}
