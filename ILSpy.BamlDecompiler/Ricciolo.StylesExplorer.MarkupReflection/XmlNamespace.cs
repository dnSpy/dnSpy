// Copyright (c) Cristian Civera (cristian@aspitalia.com)
// This code is distributed under the MS-PL (for details please see \doc\MS-PL.txt)

using System.Collections.Generic;

namespace Ricciolo.StylesExplorer.MarkupReflection
{
	internal class XmlNamespace
	{
		private string _prefix;
		private string _namespace;

		public XmlNamespace(string prefix, string ns)
		{
			_prefix = prefix;
			_namespace = ns;
		}

		public string Prefix
		{
			get { return _prefix; }
		}

		public string Namespace
		{
			get { return _namespace; }
		}

		public override bool Equals(object obj)
		{
			if (obj is XmlNamespace)
			{
				XmlNamespace o = (XmlNamespace)obj;
				return (o.Prefix.Equals(this.Prefix) && o.Namespace.Equals(this.Namespace));
			}
			return base.Equals(obj);
		}

		public override int GetHashCode()
		{
			return _prefix.GetHashCode() + _namespace.GetHashCode() >> 20;
		}
	}

	internal class XmlNamespaceCollection : List<XmlNamespace>
	{}
}