// Copyright (c) Cristian Civera (cristian@aspitalia.com)
// This code is distributed under the MS-PL (for details please see \doc\MS-PL.txt)

using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace Ricciolo.StylesExplorer.MarkupReflection
{
	internal class XmlBamlProperty : XmlBamlNode
	{
		PropertyType propertyType;

		public XmlBamlProperty(XmlBamlElement parent, PropertyType propertyType)
		{
			this.Parent = parent;
			this.propertyType = propertyType;
		}

		public XmlBamlProperty(XmlBamlElement parent, PropertyType propertyType, PropertyDeclaration propertyDeclaration)
		{
			this.Parent = parent;
			this.PropertyDeclaration = propertyDeclaration;
			this.propertyType = propertyType;
		}

		public override string ToString()
		{
			return this.PropertyDeclaration.Name;
		}
		
		public XmlBamlElement Parent { get; set; }
		
		public PropertyDeclaration PropertyDeclaration { get; set; }

		public PropertyType PropertyType {
			get { return this.propertyType; }
		}

		public object Value { get; set; }

		public override XmlNodeType NodeType {
			get { return XmlNodeType.Attribute; }
		}
	}

	internal enum PropertyType
	{
		Key,
		Value,
		Content,
		List,
		Dictionary,
		Complex
	}

	internal class XmlBamlPropertyCollection : List<XmlBamlNode>
	{ }
}
