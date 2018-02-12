/*
    Copyright (C) 2014-2017 de4dot@gmail.com

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
using System.IO;
using dnlib.DotNet;
using dnlib.DotNet.MD;
using dnlib.PE;

namespace dnSpy.AsmEditor.Compiler {
	static unsafe class MDPatcherUtils {
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
			if (value <= 0x1FFFFFFF)
				return 4;
			throw new ArgumentOutOfRangeException("UInt32 value can't be compressed");
		}

		public unsafe static uint ReadCompressedUInt32(ref byte* data, byte* end) {
			if (data >= end)
				throw new IndexOutOfRangeException();
			byte b = *data++;
			if ((b & 0x80) == 0)
				return b;

			if ((b & 0xC0) == 0x80) {
				if (data >= end)
					throw new IndexOutOfRangeException();
				return (uint)(((b & 0x3F) << 8) | *data++);
			}

			if (data + 2 >= end)
				throw new IndexOutOfRangeException();
			return (uint)(((b & 0x1F) << 24) | (*data++ << 16) |
					(*data++ << 8) | *data++);
		}

		public static bool ExistsInMetadata(TypeDef type) => type != null && !(type is TypeDefUser);

		public static bool ReferencesModule(ModuleDef sourceModule, ModuleDef targetModule) {
			if (targetModule == null)
				return false;

			if (sourceModule == targetModule)
				return true;

			var targetAssembly = targetModule.Assembly;
			if (targetAssembly != null) {
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

		public static IMetaData TryCreateMetadata(RawModuleBytes moduleData, bool isFileLayout) {
			try {
				return MetaDataCreator.CreateMetaData(new PEImage((IntPtr)moduleData.Pointer, moduleData.Size, isFileLayout ? ImageLayout.File : ImageLayout.Memory, verify: true));
			}
			catch (IOException) {
			}
			catch (BadImageFormatException) {
			}
			return null;
		}
	}
}
