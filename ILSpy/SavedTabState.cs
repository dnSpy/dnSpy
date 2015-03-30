using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using ICSharpCode.ILSpy.AvalonEdit;
using ICSharpCode.ILSpy.TreeNodes;

namespace ICSharpCode.ILSpy
{
	public class SavedTabGroupsState
	{
		public List<SavedTabGroupState> Groups = new List<SavedTabGroupState>();
		public int Index;
		public bool IsHorizontal;

		public XElement ToXml(XElement xml)
		{
			xml.SetAttributeValue("index", Index);
			xml.SetAttributeValue("is-horizontal", IsHorizontal);

			foreach (var group in Groups)
				xml.Add(group.ToXml(new XElement("TabGroup")));

			return xml;
		}

		public static SavedTabGroupsState FromXml(XElement child)
		{
			var savedState = new SavedTabGroupsState();

			savedState.Index = (int)child.Attribute("index");
			savedState.IsHorizontal = (bool)child.Attribute("is-horizontal");

			foreach (var group in child.Elements("TabGroup"))
				savedState.Groups.Add(SavedTabGroupState.FromXml(group));

			return savedState;
		}
	}

	public class SavedTabGroupState
	{
		public List<SavedTabState> Tabs = new List<SavedTabState>();
		public int Index;

		public XElement ToXml(XElement xml)
		{
			xml.SetAttributeValue("index", Index);

			foreach (var tab in Tabs)
				xml.Add(tab.ToXml(new XElement("Tab")));

			return xml;
		}

		public static SavedTabGroupState FromXml(XElement child)
		{
			var savedState = new SavedTabGroupState();

			savedState.Index = (int)child.Attribute("index");

			foreach (var tab in child.Elements("Tab"))
				savedState.Tabs.Add(SavedTabState.FromXml(tab));

			return savedState;
		}
	}

	public class SavedTabState
	{
		public List<FullNodePathName> Paths = new List<FullNodePathName>();
		public List<string> ActiveAutoLoadedAssemblies;
		public EditorPositionState EditorPositionState;
		public string Language;

		public XElement ToXml(XElement xml)
		{
			xml.SetAttributeValue("language", Language);

			foreach (var path in Paths)
				xml.Add(path.ToXml(new XElement("Path")));

			var asms = new XElement("ActiveAutoLoadedAssemblies", ActiveAutoLoadedAssemblies.Select(p => new XElement("Node", p)));
			xml.Add(asms);

			xml.Add(EditorPositionState.ToXml(new XElement("EditorPositionState")));

			return xml;
		}

		public static SavedTabState FromXml(XElement child)
		{
			var savedState = new SavedTabState();

			savedState.Language = (string)child.Attribute("language") ?? "C#";

			foreach (var path in child.Elements("Path"))
				savedState.Paths.Add(FullNodePathName.FromXml(path));

			savedState.ActiveAutoLoadedAssemblies = new List<string>();
			var autoAsms = child.Element("ActiveAutoLoadedAssemblies");
			if (autoAsms != null)
				savedState.ActiveAutoLoadedAssemblies.AddRange(autoAsms.Elements().Select(e => (string)e));

			savedState.EditorPositionState = EditorPositionState.FromXml(child.Element("EditorPositionState"));

			return savedState;
		}
	}
}
