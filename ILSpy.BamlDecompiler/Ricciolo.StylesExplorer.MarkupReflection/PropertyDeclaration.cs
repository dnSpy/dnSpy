// Copyright (c) Cristian Civera (cristian@aspitalia.com)
// This code is distributed under the MS-PL (for details please see \doc\MS-PL.txt)

using System;
using System.Collections.Generic;
using System.Text;

namespace Ricciolo.StylesExplorer.MarkupReflection
{
	internal class PropertyDeclaration
	{
		private TypeDeclaration declaringType;
		private string name;

		// Methods
		public PropertyDeclaration(string name)
		{
			this.name = name;
			this.declaringType = null;
		}

		public PropertyDeclaration(string name, TypeDeclaration declaringType)
		{
			this.name = name;
			this.declaringType = declaringType;
		}

		public override string ToString()
		{
			if (((this.DeclaringType != null) && (this.DeclaringType.Name == "XmlNamespace")) && ((this.DeclaringType.Namespace == null) && (this.DeclaringType.Assembly == null)))
			{
				if ((this.Name == null) || (this.Name.Length == 0))
				{
					return "xmlns";
				}
				return ("xmlns:" + this.Name);
			}
			return this.Name;
		}

		// Properties
		public TypeDeclaration DeclaringType
		{
			get
			{
				return this.declaringType;
			}
		}

		public string Name
		{
			get
			{
				return this.name;
			}
		}
	}
}
