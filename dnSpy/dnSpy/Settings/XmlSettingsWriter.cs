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

using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using dnSpy.Contracts.App;
using dnSpy.Contracts.Settings;

namespace dnSpy.Settings {
	struct XmlSettingsWriter {
		readonly ISettingsManager mgr;
		readonly string filename;

		public XmlSettingsWriter(ISettingsManager mgr, string filename = null) {
			this.mgr = mgr;
			this.filename = filename ?? AppDirectories.SettingsFilename;
		}

		public void Write() {
			Directory.CreateDirectory(Path.GetDirectoryName(filename));
			var doc = new XDocument(new XElement(XmlSettingsConstants.XML_ROOT_NAME));
			Write(doc.Root);
			doc.Save(filename);
		}

		static ISettingsSection[] Sort(ISettingsSection[] sections) {
			// This preserves the order of elements with the same name, which some code depend on.
			return sections.OrderBy(a => a.Name.ToUpperInvariant()).ToArray();
		}

		void Write(XElement root) {
			foreach (var section in Sort(mgr.Sections))
				root.Add(Write(section, 0));
		}

		XElement Write(ISettingsSection section, int recursionCounter) {
			var xmlSect = new XElement(XmlSettingsConstants.SECTION_NAME);

			if (recursionCounter >= XmlSettingsConstants.MAX_CHILD_DEPTH)
				return xmlSect;

			xmlSect.SetAttributeValue(XmlSettingsConstants.SECTION_ATTRIBUTE_NAME, XmlUtils.EscapeAttributeValue(section.Name));
			foreach (var attr in section.Attributes.OrderBy(a => a.Item1.ToUpperInvariant())) {
				var n = XmlUtils.FilterAttributeName(attr.Item1);
				Debug.Assert(n != null, "Invalid character(s) in section attribute name. Only valid XML attribute names can be used.");
				if (n == null)
					continue;
				bool b = n == XmlSettingsConstants.SECTION_ATTRIBUTE_NAME;
				Debug.Assert(!b, string.Format("Attribute name '{0}' is reserved for use by the XML writer", XmlSettingsConstants.SECTION_ATTRIBUTE_NAME));
				if (b)
					continue;
				xmlSect.SetAttributeValue(n, XmlUtils.EscapeAttributeValue(attr.Item2));
			}

			foreach (var childSection in Sort(section.Sections))
				xmlSect.Add(Write(childSection, recursionCounter + 1));

			return xmlSect;
		}
	}
}
