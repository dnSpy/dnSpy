// Copyright (c) Cristian Civera (cristian@aspitalia.com)
// This code is distributed under the MS-PL (for details please see \doc\MS-PL.txt)

using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace Ricciolo.StylesExplorer.MarkupReflection
{
	internal class XmlBamlText : XmlBamlNode
	{
		private string _text;

		public XmlBamlText(string text)
		{
			_text = text;
		}

		public string Text
		{
			get { return _text; }
		}

		public override System.Xml.XmlNodeType NodeType
		{
			get
			{
				return XmlNodeType.Text;
			}
		}
	}
}
