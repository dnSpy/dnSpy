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
using System.Text;

namespace dnSpy.Debugger.DotNet.CorDebug.Impl {
	static partial class AppHostInfoData {
		public static readonly Encoding StringEncoding = Encoding.ASCII;
		public static readonly AppHostInfo[] KnownAppHostInfos = CreateAppHostInfos(GetSerializedAppHostInfos(), SerializedAppHostInfosCount);

		static AppHostInfo[] CreateAppHostInfos(byte[] serializedData, int elemCount) {
			var result = new AppHostInfo[elemCount];
			int resultIndex = 0;
			if (serializedData.Length != 0) {
				int o = 0;
#if APPHOSTINFO_STRINGS
				uint numStrings = DeserializeCompressedUInt32(serializedData, ref o);
				var stringsTable = new string[numStrings];
				for (uint i = 0; i < stringsTable.Length; i++)
					stringsTable[i] = DeserializeString(serializedData, ref o);
#endif
				while (o != serializedData.Length) {
#if APPHOSTINFO_STRINGS
					var rid = stringsTable[DeserializeCompressedUInt32(serializedData, ref o)];
					var version = stringsTable[DeserializeCompressedUInt32(serializedData, ref o)];
#endif
					var relPathOffset = DeserializeCompressedUInt32(serializedData, ref o);
					var hashDataOffset = DeserializeCompressedUInt32(serializedData, ref o);
					const uint hashDataSize = AppHostInfo.DefaultHashSize;
					var hash = DeserializeByteArray(serializedData, ref o, 20);
					var lastByte = serializedData[o++];
					result[resultIndex++] = new AppHostInfo(
#if APPHOSTINFO_STRINGS
						rid, version,
#endif
						relPathOffset, hashDataOffset, hashDataSize, hash, lastByte);
				}
			}
			if (resultIndex != result.Length)
				throw new InvalidOperationException($"Invalid serialized AppHostInfo count, expected: {elemCount}, actual: {resultIndex}");
			return result;
		}

		internal static string DeserializeString(byte[] data, ref int o) {
			int length = data[o];
			var s = StringEncoding.GetString(data, o + 1, length);
			o += length + 1;
			return s;
		}

		internal static uint DeserializeCompressedUInt32(byte[] data, ref int o) {
			uint result = 0;
			for (int shift = 0; shift < 32; shift += 7) {
				uint b = data[o++];
				if ((b & 0x80) == 0)
					return result | (b << shift);
				result |= (b & 0x7F) << shift;
			}
			throw new InvalidOperationException();
		}

		static byte[] DeserializeByteArray(byte[] data, ref int o, int length) {
			var result = new byte[length];
			Array.Copy(data, o, result, 0, result.Length);
			o += length;
			return result;
		}
	}
}
