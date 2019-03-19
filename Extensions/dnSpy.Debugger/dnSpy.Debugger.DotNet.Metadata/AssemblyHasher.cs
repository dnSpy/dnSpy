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
using System.Security.Cryptography;

namespace dnSpy.Debugger.DotNet.Metadata {
	readonly struct AssemblyHasher : IDisposable {
		readonly HashAlgorithm hasher;

		public AssemblyHasher(DmdAssemblyHashAlgorithm hashAlgo) {
			switch (hashAlgo) {
			case DmdAssemblyHashAlgorithm.MD5:
				hasher = MD5.Create();
				break;

			case DmdAssemblyHashAlgorithm.None:
			case DmdAssemblyHashAlgorithm.SHA1:
			default:
				hasher = SHA1.Create();
				break;

			case DmdAssemblyHashAlgorithm.SHA256:
				hasher = SHA256.Create();
				break;

			case DmdAssemblyHashAlgorithm.SHA384:
				hasher = SHA384.Create();
				break;

			case DmdAssemblyHashAlgorithm.SHA512:
				hasher = SHA512.Create();
				break;
			}
		}

		public void Dispose() => (hasher as IDisposable)?.Dispose();

		public static byte[] Hash(byte[] data, DmdAssemblyHashAlgorithm hashAlgo) {
			if (data == null)
				return null;

			using (var asmHash = new AssemblyHasher(hashAlgo)) {
				asmHash.Hash(data);
				return asmHash.ComputeHash();
			}
		}

		public void Hash(byte[] data) => Hash(data, 0, data.Length);

		public void Hash(byte[] data, int offset, int length) {
			if (hasher.TransformBlock(data, offset, length, data, offset) != length)
				throw new IOException("Could not calculate hash");
		}

		public void Hash(Stream stream, uint length, byte[] buffer) {
			while (length > 0) {
				int len = length > (uint)buffer.Length ? buffer.Length : (int)length;
				if (stream.Read(buffer, 0, len) != len)
					throw new IOException("Could not read data");
				Hash(buffer, 0, len);
				length -= (uint)len;
			}
		}

		public byte[] ComputeHash() {
			hasher.TransformFinalBlock(new byte[0], 0, 0);
			return hasher.Hash;
		}

		public static byte[] CreatePublicKeyToken(byte[] publicKeyData) {
			if (publicKeyData == null || publicKeyData.Length == 0)
				return publicKeyData;
			var hash = Hash(publicKeyData, DmdAssemblyHashAlgorithm.SHA1);
			var pkt = new byte[8];
			for (int i = 0; i < pkt.Length && i < hash.Length; i++)
				pkt[i] = hash[hash.Length - i - 1];
			return pkt;
		}
	}
}
