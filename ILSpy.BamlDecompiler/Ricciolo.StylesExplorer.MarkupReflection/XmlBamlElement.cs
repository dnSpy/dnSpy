// Copyright (c) Cristian Civera (cristian@aspitalia.com)
// This code is distributed under the MS-PL (for details please see \doc\MS-PL.txt)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace Ricciolo.StylesExplorer.MarkupReflection
{
	class XmlBamlElement : XmlBamlNode
	{
		XmlNamespaceCollection _namespaces = new XmlNamespaceCollection();

		public XmlBamlElement()
		{
		}

		public XmlBamlElement(XmlBamlElement parent)
		{
			this.Parent = parent;
			this.Namespaces.AddRange(parent.Namespaces);
		}

		public XmlNamespaceCollection Namespaces {
			get { return _namespaces; }
		}
		
		public XmlBamlElement Parent { get; private set; }
		
		public TypeDeclaration TypeDeclaration { get; set; }

		public override XmlNodeType NodeType {
			get { return XmlNodeType.Element; }
		}

		public long Position { get; set; }
		
		public bool IsImplicit { get; set; }

		public override string ToString()
		{
			return string.Format("Element: {0}", TypeDeclaration.Name);
		}
	}

	class XmlBamlEndElement : XmlBamlElement
	{
		public XmlBamlEndElement(XmlBamlElement start)
		{
			this.TypeDeclaration = start.TypeDeclaration;
			this.Namespaces.AddRange(start.Namespaces);
		}

		public override XmlNodeType NodeType {
			get { return XmlNodeType.EndElement; }
		}

		public override string ToString()
		{
			return string.Format("EndElement: {0}", TypeDeclaration.Name);
		}
	}
}
