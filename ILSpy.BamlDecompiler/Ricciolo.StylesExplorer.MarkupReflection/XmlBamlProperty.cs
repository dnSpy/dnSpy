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
		private PropertyDeclaration propertyDeclaration;
		private PropertyType propertyType;
		private object value;

		public XmlBamlProperty(PropertyType propertyType)
		{
			this.propertyType = propertyType;
		}

		public XmlBamlProperty(PropertyType propertyType, PropertyDeclaration propertyDeclaration)
		{
			this.propertyDeclaration = propertyDeclaration;
			this.propertyType = propertyType;
		}

		public override string ToString()
		{
			return this.PropertyDeclaration.Name;
		}

		public PropertyDeclaration PropertyDeclaration
		{
			get
			{
				return this.propertyDeclaration;
			}
			set
			{
				this.propertyDeclaration = value;
			}
		}

		public PropertyType PropertyType
		{
			get
			{
				return this.propertyType;
			}
		}

		public object Value
		{
			get
			{
				return this.value;
			}
			set
			{
				this.value = value;
			}
		}

		public override XmlNodeType NodeType
		{
			get
			{
				return XmlNodeType.Attribute;
			}
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
