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

namespace dnSpy.Debugger.DotNet.Metadata.Impl {
	sealed class AssemblyNameEqualityComparer : IEqualityComparer<IDmdAssemblyName> {
		readonly bool ignorePublicKeyToken;

		public AssemblyNameEqualityComparer(bool ignorePublicKeyToken) =>
			this.ignorePublicKeyToken = ignorePublicKeyToken;

		static readonly byte[][] systemPublicKeyTokens;

		static AssemblyNameEqualityComparer() {
			systemPublicKeyTokens = new byte[][] {
				new byte[8] { 0x1C, 0x9E, 0x25, 0x96, 0x86, 0xF9, 0x21, 0xE0 },
				new byte[8] { 0x5F, 0xD5, 0x7C, 0x54, 0x3A, 0x9C, 0x02, 0x47 },
				new byte[8] { 0x96, 0x9D, 0xB8, 0x05, 0x3D, 0x33, 0x22, 0xAC },
				new byte[8] { 0xB7, 0x7A, 0x5C, 0x56, 0x19, 0x34, 0xE0, 0x89 },
				new byte[8] { 0x31, 0xBF, 0x38, 0x56, 0xAD, 0x36, 0x4E, 0x35 },
				new byte[8] { 0xB0, 0x3F, 0x5F, 0x7F, 0x11, 0xD5, 0x0A, 0x3A },
				new byte[8] { 0x7C, 0xEC, 0x85, 0xD7, 0xBE, 0xA7, 0x79, 0x8E },
				new byte[8] { 0x07, 0x38, 0xEB, 0x9F, 0x13, 0x2E, 0xD7, 0x56 },
			};
		}

		public bool Equals(IDmdAssemblyName x, IDmdAssemblyName y) {
			if (!StringComparer.OrdinalIgnoreCase.Equals(x.Name, y.Name))
				return false;

			// Version number is ignored since an assembly reference can be redirected to any other version at runtime

			if (!StringComparer.OrdinalIgnoreCase.Equals(x.CultureName ?? string.Empty, y.CultureName ?? string.Empty))
				return false;

			return ignorePublicKeyToken || PublicKeyTokenEquals(x.GetPublicKeyToken(), y.GetPublicKeyToken());
		}

		static bool PublicKeyTokenEquals(byte[] a, byte[] b) {
			if (a == null)
				a = Array.Empty<byte>();
			if (b == null)
				b = Array.Empty<byte>();
			if (Equals(a, b))
				return true;
			return IsSystemPublicKeyToken(a) && IsSystemPublicKeyToken(b);
		}

		static bool IsSystemPublicKeyToken(byte[] a) {
			if (a == null)
				return false;
			foreach (var sys in systemPublicKeyTokens) {
				if (Equals(sys, a))
					return true;
			}
			return false;
		}

		static bool Equals(byte[] a, byte[] b) {
			if (a == b)
				return true;
			if (a == null || b == null)
				return false;
			if (a.Length != b.Length)
				return false;
			for (int i = 0; i < a.Length; i++) {
				if (a[i] != b[i])
					return false;
			}
			return true;
		}

		public int GetHashCode(IDmdAssemblyName obj) {
			int hc = StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Name ?? string.Empty);
			// Version number is ignored, see Equals()
			hc ^= (obj.CultureName ?? string.Empty).GetHashCode();
			// PublicKeyToken is ignored
			return hc;
		}
	}
}
