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

namespace dnSpy.Debugger.DotNet.Metadata.Impl {
	sealed class AssemblyNameEqualityComparer : IEqualityComparer<DmdAssemblyName> {
		public static readonly AssemblyNameEqualityComparer Instance = new AssemblyNameEqualityComparer();
		AssemblyNameEqualityComparer() { }

		public bool Equals(DmdAssemblyName x, DmdAssemblyName y) {
			if (!StringComparer.OrdinalIgnoreCase.Equals(x.Name, y.Name))
				return false;

			if (x.Version != null && y.Version != null && x.Version != y.Version)
				return false;

			if (x.CultureName != null && y.CultureName != null && !StringComparer.OrdinalIgnoreCase.Equals(x.CultureName, y.CultureName))
				return false;

			if (x.GetPublicKeyToken() != null && y.GetPublicKeyToken() != null && !Equals(x.GetPublicKeyToken(), y.GetPublicKeyToken()))
				return false;

			return true;
		}

		bool Equals(byte[] a, byte[] b) {
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

		public int GetHashCode(DmdAssemblyName obj) {
			int hc = StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Name ?? string.Empty);
			if (obj.Version != null)
				hc ^= obj.Version.GetHashCode();
			if (obj.CultureName != null)
				hc ^= obj.CultureName.GetHashCode();
			return hc;
		}
	}
}
