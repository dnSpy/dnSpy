
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml.Linq;

namespace ICSharpCode.ILSpy.TreeNodes
{
	[DebuggerDisplay("{Id} - {Name}")]
	public struct NodePathName : IEquatable<NodePathName>
	{
		readonly string id;
		readonly string name;

		public string Id {
			get { return id; }
		}

		public string Name {
			get { return name; }
		}

		public NodePathName(string id)
			: this(id, string.Empty)
		{
		}

		public NodePathName(string id, string name)
		{
			this.id = id;
			this.name = name;
		}

		public XElement ToXml(XElement xml)
		{
			xml.SetAttributeValue("id", id);
			xml.SetAttributeValue("name", name);
			return xml;
		}

		public static NodePathName FromXml(XElement doc)
		{
			if (doc == null)
				return new NodePathName();
			var id = SessionSettings.FromString((string)doc.Attribute("id"), string.Empty);
			var name = SessionSettings.FromString((string)doc.Attribute("name"), string.Empty);
			return new NodePathName(id, name);
		}

		public static bool operator ==(NodePathName a, NodePathName b)
		{
			return a.Equals(b);
		}

		public static bool operator !=(NodePathName a, NodePathName b)
		{
			return !a.Equals(b);
		}

		public bool Equals(NodePathName other)
		{
			return id == other.id && name == other.name;
		}

		public override bool Equals(object obj)
		{
			if (!(obj is NodePathName))
				return false;
			return Equals((NodePathName)obj);
		}

		public override int GetHashCode()
		{
			return id.GetHashCode() ^ name.GetHashCode();
		}
	}

	public struct FullNodePathName
	{
		List<NodePathName> names;

		public List<NodePathName> Names {
			get { return names ?? (names = new List<NodePathName>()); }
		}

		public XElement ToXml(XElement xml)
		{
			foreach (var name in names)
				xml.Add(name.ToXml(new XElement("Name")));
			return xml;
		}

		public static FullNodePathName FromXml(XElement doc)
		{
			var fullPath = new FullNodePathName();
			if (doc != null) {
				foreach (var xname in doc.Elements("Name"))
					fullPath.Names.Add(NodePathName.FromXml(xname));
			}
			return fullPath;
		}
	}
}
