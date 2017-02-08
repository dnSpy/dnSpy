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
using System.Globalization;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using dnSpy.Contracts.Decompiler;

namespace dnSpy.BamlDecompiler {
	struct XamlOutputCreator {
		readonly XamlOutputOptions options;

		public XamlOutputCreator(XamlOutputOptions options) {
			if (options == null)
				throw new ArgumentNullException(nameof(options));
			this.options = options;
		}

		public string CreateText(XDocument document) {
			if (options == null)
				throw new InvalidOperationException();
			if (document == null)
				throw new ArgumentNullException(nameof(document));

			var settings = new XmlWriterSettings {
				Indent = true,
				IndentChars = options.IndentChars ?? "\t",
				NewLineChars = options.NewLineChars ?? Environment.NewLine,
				NewLineOnAttributes = options.NewLineOnAttributes,
				OmitXmlDeclaration = true,
			};
			using (var writer = new StringWriter(CultureInfo.InvariantCulture)) {
				using (var xmlWriter = XmlWriter.Create(writer, settings))
					document.WriteTo(xmlWriter);
				// WriteTo() doesn't add a final newline
				writer.WriteLine();
				return writer.ToString();
			}
		}
	}
}
