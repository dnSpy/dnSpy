// Copyright (c) Cristian Civera (cristian@aspitalia.com)
// This code is distributed under the MS-PL (for details please see \doc\MS-PL.txt)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace Ricciolo.StylesExplorer.MarkupReflection
{
	internal class XmlBamlElement : XmlBamlNode
	{
		private ArrayList _arguments = new ArrayList();
		private XmlNamespaceCollection _namespaces = new XmlNamespaceCollection();
		private TypeDeclaration _typeDeclaration;
		private KeysResourcesCollection _keysResources = new KeysResourcesCollection();
		private long _position;

		public XmlBamlElement()
		{
		}


		public XmlBamlElement(XmlBamlElement parent)
		{
			this.Namespaces.AddRange(parent.Namespaces);
		}

		public XmlNamespaceCollection Namespaces
		{
			get { return _namespaces; }
		}

		public TypeDeclaration TypeDeclaration
		{
			get
			{
				return this._typeDeclaration;
			}
			set
			{
				this._typeDeclaration = value;
			}
		}

		public override XmlNodeType NodeType
		{
			get
			{
				return XmlNodeType.Element;
			}
		}

		public long Position
		{
			get { return _position; }
			set { _position = value; }
		}

		public override string ToString()
		{
			return String.Format("Element: {0}", TypeDeclaration.Name);
		}
	}

	internal class XmlBamlEndElement : XmlBamlElement
	{
		public XmlBamlEndElement(XmlBamlElement start)
		{
			this.TypeDeclaration = start.TypeDeclaration;
			this.Namespaces.AddRange(start.Namespaces);
		}

		public override XmlNodeType NodeType
		{
			get
			{
				return XmlNodeType.EndElement;
			}
		}

		public override string ToString()
		{
			return String.Format("EndElement: {0}", TypeDeclaration.Name);
		}
	}

	internal class KeyMapping
	{
		private string _key;
		private TypeDeclaration _declaration;
		private string _trueKey;

		public KeyMapping(string key, TypeDeclaration declaration, string trueKey)
		{
			_key = key;
			_declaration = declaration;
			_trueKey = trueKey;
		}

		public string Key
		{
			get { return _key; }
		}

		public TypeDeclaration Declaration
		{
			get { return _declaration; }
		}

		public string TrueKey
		{
			get { return _trueKey; }
		}

		public override string ToString()
		{
			return String.Format("{0} - {1} - {2}", Key, Declaration, TrueKey);
		}
	}

	internal class KeysResourcesCollection : List<KeysResource>
	{
		public KeysResource Last
		{
			get
			{
				if (this.Count == 0)
					return null;
				return this[this.Count - 1];
			}
		}

		public KeysResource First
		{
			get
			{
				if (this.Count == 0)
					return null;
				return this[0];
			}
		}
	}

	internal class KeysResource
	{
		private KeysTable _keys = new KeysTable();
		private ArrayList _staticResources = new ArrayList();

		public KeysTable Keys
		{
			get { return _keys; }
		}

		public ArrayList StaticResources
		{
			get { return _staticResources; }
		}
	}

	internal class KeysTable
	{
		private Hashtable table = new Hashtable();

		public String this[long position]
		{
			get
			{
				return (string)this.table[position];
			}
			set
			{
				this.table[position] = value;
			}
		}

		public int Count
		{
			get { return this.table.Count; }
		}

		public void Remove(long position)
		{
			this.table.Remove(position);
		}

		public bool HasKey(long position)
		{
			return this.table.ContainsKey(position);
		}
	}
}
