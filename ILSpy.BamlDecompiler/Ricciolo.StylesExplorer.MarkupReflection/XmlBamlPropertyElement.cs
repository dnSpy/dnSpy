// Copyright (c) Cristian Civera (cristian@aspitalia.com)
// This code is distributed under the MS-PL (for details please see \doc\MS-PL.txt)

using System;
using System.Collections.Generic;
using System.Text;

namespace Ricciolo.StylesExplorer.MarkupReflection
{
	internal class XmlBamlPropertyElement : XmlBamlElement
	{
		private readonly PropertyType _propertyType;
		private PropertyDeclaration propertyDeclaration;
		
		public XmlBamlPropertyElement(PropertyType propertyType, PropertyDeclaration propertyDeclaration)
		{
			_propertyType = propertyType;
			this.propertyDeclaration = propertyDeclaration;
		}

		public XmlBamlPropertyElement(XmlBamlElement parent, PropertyType propertyType, PropertyDeclaration propertyDeclaration)
			: base(parent)
		{
			_propertyType = propertyType;
			this.propertyDeclaration = propertyDeclaration;
			this.TypeDeclaration = propertyDeclaration.DeclaringType;
		}

		public PropertyDeclaration PropertyDeclaration
		{
			get
			{
				return this.propertyDeclaration;
			}
		}

		public PropertyType PropertyType
		{
			get { return _propertyType; }
		}

		public override string ToString()
		{
			return String.Format("PropertyElement: {0}.{1}", TypeDeclaration.Name.Replace('+', '.'), PropertyDeclaration.Name);
		}
	}
}
