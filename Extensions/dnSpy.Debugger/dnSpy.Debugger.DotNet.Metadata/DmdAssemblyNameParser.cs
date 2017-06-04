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
using System.IO;
using System.Text;

namespace dnSpy.Debugger.DotNet.Metadata {
	struct DmdAssemblyNameParser : IDisposable {
		public static void Parse(DmdAssemblyName result, string asmFullName) {
			if (result == null)
				throw new ArgumentNullException(nameof(result));
			if (asmFullName == null)
				throw new ArgumentNullException(nameof(asmFullName));
			try {
				using (var parser = new DmdAssemblyNameParser(asmFullName))
					parser.Parse(result);
			}
			catch {
			}
		}

		readonly StringReader reader;

		DmdAssemblyNameParser(string asmFullName) => reader = new StringReader(asmFullName);

		public DmdAssemblyName Parse(DmdAssemblyName asmName) {
			asmName.Name = ReadAssemblyNameId();
			SkipWhite();
			if (PeekChar() != ',')
				return asmName;
			ReadChar();

			while (true) {
				SkipWhite();
				int c = PeekChar();
				if (c == -1 || c == ']')
					break;
				if (c == ',') {
					ReadChar();
					continue;
				}

				string key = ReadId();
				SkipWhite();
				if (PeekChar() != '=')
					continue;
				ReadChar();
				string value = ReadId();

				switch (key.ToUpperInvariant()) {
				case "VERSION":
					asmName.Version = Version.TryParse(value, out var version) ? version : null;
					break;

				case "CONTENTTYPE":
					if (StringComparer.OrdinalIgnoreCase.Equals(value, "WindowsRuntime"))
						asmName.Flags = (asmName.Flags & ~DmdAssemblyNameFlags.ContentType_Mask) | DmdAssemblyNameFlags.ContentType_WindowsRuntime;
					else
						asmName.Flags = (asmName.Flags & ~DmdAssemblyNameFlags.ContentType_Mask) | DmdAssemblyNameFlags.ContentType_Default;
					break;

				case "RETARGETABLE":
					if (StringComparer.OrdinalIgnoreCase.Equals(value, "Yes"))
						asmName.Flags |= DmdAssemblyNameFlags.Retargetable;
					else
						asmName.Flags &= ~DmdAssemblyNameFlags.Retargetable;
					break;

				case "PUBLICKEY":
					if (StringComparer.OrdinalIgnoreCase.Equals(value, "null") ||
						StringComparer.OrdinalIgnoreCase.Equals(value, "neutral"))
						asmName.SetPublicKey(Array.Empty<byte>());
					else
						asmName.SetPublicKey(HexUtils.ParseBytes(value));
					break;

				case "PUBLICKEYTOKEN":
					if (StringComparer.OrdinalIgnoreCase.Equals(value, "null") ||
						StringComparer.OrdinalIgnoreCase.Equals(value, "neutral"))
						asmName.SetPublicKeyToken(Array.Empty<byte>());
					else
						asmName.SetPublicKeyToken(HexUtils.ParseBytes(value));
					break;

				case "CULTURE":
				case "LANGUAGE":
					if (StringComparer.OrdinalIgnoreCase.Equals(value, "neutral"))
						asmName.CultureName = string.Empty;
					else
						asmName.CultureName = value;
					break;
				}
			}

			return asmName;
		}

		string ReadAssemblyNameId() {
			SkipWhite();
			var sb = new StringBuilder();
			int c;
			while ((c = GetAsmNameChar()) != -1)
				sb.Append((char)c);
			return sb.ToString().Trim();
		}

		int GetAsmNameChar() {
			int c = PeekChar();
			if (c == -1)
				return -1;
			switch (c) {
			case '\\':
				ReadChar();
				return ReadChar();

			case ']':
			case ',':
				return -1;

			default:
				return ReadChar();
			}
		}

		int GetIdChar(bool ignoreWhiteSpace) {
			int c = PeekChar();
			if (c == -1)
				return -1;
			if (ignoreWhiteSpace && char.IsWhiteSpace((char)c))
				return -1;
			switch (c) {
			case '\\':
				ReadChar();
				return ReadChar();

			case ',':
			case '+':
			case '&':
			case '*':
			case '[':
			case ']':
			case '=':
				return -1;

			default:
				return ReadChar();
			}
		}

		void SkipWhite() {
			while (true) {
				int next = PeekChar();
				if (next == -1)
					break;
				if (!char.IsWhiteSpace((char)next))
					break;
				ReadChar();
			}
		}

		string ReadId() => ReadId(true);

		string ReadId(bool ignoreWhiteSpace) {
			SkipWhite();
			var sb = new StringBuilder();
			int c;
			while ((c = GetIdChar(ignoreWhiteSpace)) != -1)
				sb.Append((char)c);
			return sb.ToString();
		}

		int PeekChar() => reader.Peek();
		int ReadChar() => reader.Read();

		public void Dispose() => reader.Dispose();
	}
}
