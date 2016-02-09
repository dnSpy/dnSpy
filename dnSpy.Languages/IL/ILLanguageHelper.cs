/*
    Copyright (C) 2014-2016 de4dot@gmail.com

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

using dnlib.DotNet.Emit;
using dnSpy.Shared.Languages.XmlDoc;

namespace dnSpy.Languages.IL {
	public static class ILLanguageHelper {
		static readonly string[] cachedOpCodeDocs = new string[0x200];
		public static string GetOpCodeDocumentation(OpCode code) {
			int index = (int)code.Code;
			int hi = index >> 8;
			if (hi == 0xFE)
				index -= 0xFD00;
			else if (hi != 0)
				return null;
			var s = cachedOpCodeDocs[index];
			if (s != null)
				return s;

			var docProvider = XmlDocLoader.MscorlibDocumentation;
			if (docProvider != null) {
				string docXml = docProvider.GetDocumentation("F:System.Reflection.Emit.OpCodes." + code.Code.ToString());
				if (docXml != null) {
					XmlDocRenderer renderer = new XmlDocRenderer();
					renderer.AddXmlDocumentation(docXml);
					return cachedOpCodeDocs[index] = renderer.ToString();
				}
			}

			return null;
		}
	}
}
