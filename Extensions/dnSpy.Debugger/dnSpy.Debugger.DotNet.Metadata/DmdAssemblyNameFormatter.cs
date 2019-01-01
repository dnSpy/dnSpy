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

namespace dnSpy.Debugger.DotNet.Metadata {
	static class DmdAssemblyNameFormatter {
		public static string Format(string name, Version version, string culture, byte[] publicKeyOrToken, DmdAssemblyNameFlags attributes, bool isPublicKeyToken) {
			var sb = ObjectPools.AllocStringBuilder();
			Format(sb, name, version, culture, publicKeyOrToken, attributes, isPublicKeyToken);
			return ObjectPools.FreeAndToString(ref sb);
		}

		public static void Format(StringBuilder sb, string name, Version version, string culture, byte[] publicKeyOrToken, DmdAssemblyNameFlags attributes, bool isPublicKeyToken) {
			if (name == null)
				return;

			foreach (var c in name) {
				if (c == ',' || c == '=')
					sb.Append('\\');
				sb.Append(c);
			}

			if (version != null) {
				sb.Append(", Version=");
				sb.Append(version.ToString());
			}

			if (culture != null) {
				sb.Append(", Culture=");
				sb.Append(string.IsNullOrEmpty(culture) ? "neutral" : culture);
			}

			if (publicKeyOrToken != null) {
				sb.Append(isPublicKeyToken ? ", PublicKeyToken=" : ", PublicKey=");
				if (publicKeyOrToken.Length == 0)
					sb.Append("null");
				else
					WritHex(sb, publicKeyOrToken, upper: false);
			}

			if ((attributes & DmdAssemblyNameFlags.Retargetable) != 0)
				sb.Append(", Retargetable=Yes");

			if ((attributes & DmdAssemblyNameFlags.ContentType_Mask) == DmdAssemblyNameFlags.ContentType_WindowsRuntime)
				sb.Append(", ContentType=WindowsRuntime");
		}

		static void WritHex(StringBuilder sb, byte[] bytes, bool upper) {
			foreach (var b in bytes) {
				sb.Append(ToHexChar(b >> 4, upper));
				sb.Append(ToHexChar(b & 0x0F, upper));
			}
		}

		static char ToHexChar(int val, bool upper) {
			if (0 <= val && val <= 9)
				return (char)(val + '0');
			return (char)(val - 10 + (upper ? 'A' : 'a'));
		}
	}
}
