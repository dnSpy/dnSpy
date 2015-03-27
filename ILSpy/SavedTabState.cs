using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using ICSharpCode.ILSpy.AvalonEdit;
using ICSharpCode.ILSpy.TreeNodes;

namespace ICSharpCode.ILSpy {
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
