// Copyright (c) Cristian Civera (cristian@aspitalia.com)
// This code is distributed under the MS-PL (for details please see \doc\MS-PL.txt)

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Windows.Media;

namespace Ricciolo.StylesExplorer.MarkupReflection
{
	class NodesCollection : List<XmlBamlNode>
	{
		public XmlBamlNode Last
		{
			get
			{
				if (this.Count > 0)
				{
					int i = this.Count - 1;
					return this[i];
				}
				return null;
			}
		}
	
		public void RemoveLast()
		{
			if (this.Count > 0)
				this.Remove(this.Last);
		}
	
		public XmlBamlNode Dequeue()
		{
			return DequeueInternal(true);
		}
	
		public XmlBamlNode Peek()
		{
			return DequeueInternal(false);
		}
	
		XmlBamlNode DequeueInternal(bool remove)
		{
			if (this.Count > 0)
			{
				XmlBamlNode node = this[0];
				if (remove)
					this.RemoveAt(0);
				return node;
			}
			else
				return null;
		}
	
		public void Enqueue(XmlBamlNode node)
		{
			this.Add(node);
		}
	}
}
