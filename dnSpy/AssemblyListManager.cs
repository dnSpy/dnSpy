// Copyright (c) 2011 AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Xml.Linq;

namespace ICSharpCode.ILSpy
{
	/// <summary>
	/// Manages the available assembly lists.
	/// 
	/// Contains the list of list names; and provides methods for loading/saving and creating/deleting lists.
	/// </summary>
	sealed class AssemblyListManager
	{
		public AssemblyListManager(ILSpySettings spySettings)
		{
			XElement doc = spySettings["AssemblyLists"];
			foreach (var list in doc.Elements("List")) {
				AssemblyLists.Add((string)list.Attribute("name"));
			}
		}
		
		public readonly ObservableCollection<string> AssemblyLists = new ObservableCollection<string>();
		
		/// <summary>
		/// Loads an assembly list from the ILSpySettings.
		/// If no list with the specified name is found, the default list is loaded instead.
		/// </summary>
		public AssemblyList LoadList(ILSpySettings spySettings, string listName)
		{
			AssemblyList list = DoLoadList(spySettings, listName);
			if (!AssemblyLists.Contains(list.ListName))
				AssemblyLists.Add(list.ListName);
			return list;
		}
		
		AssemblyList DoLoadList(ILSpySettings spySettings, string listName)
		{
			XElement doc = spySettings["AssemblyLists"];
			if (listName != null) {
				foreach (var list in doc.Elements("List")) {
					if ((string)list.Attribute("name") == listName) {
						return new AssemblyList(list);
					}
				}
			}
			XElement firstList = doc.Elements("List").FirstOrDefault();
			if (firstList != null)
				return new AssemblyList(firstList);
			else
				return new AssemblyList(listName ?? DefaultListName);
		}
		
		public const string DefaultListName = "(Default)";
		
		/// <summary>
		/// Saves the specifies assembly list into the config file.
		/// </summary>
		public static void SaveList(AssemblyList list)
		{
			ILSpySettings.Update(
				delegate (XElement root) {
					XElement doc = root.Element("AssemblyLists");
					if (doc == null) {
						doc = new XElement("AssemblyLists");
						root.Add(doc);
					}
					XElement listElement = doc.Elements("List").FirstOrDefault(e => (string)e.Attribute("name") == list.ListName);
					if (listElement != null)
						listElement.ReplaceWith(list.SaveAsXml());
					else
						doc.Add(list.SaveAsXml());
				});
		}

		public bool CreateList(AssemblyList list)
		{
			if (!AssemblyLists.Contains(list.ListName))
			{
				AssemblyLists.Add(list.ListName);
				SaveList(list);
				return true;
			}
			return false;
		}

		public bool DeleteList(string Name)
		{
			if (AssemblyLists.Contains(Name))
			{
				AssemblyLists.Remove(Name);

				ILSpySettings.Update(
					delegate(XElement root)
					{
						XElement doc = root.Element("AssemblyLists");
						if (doc == null)
						{
							return;
						}
						XElement listElement = doc.Elements("List").FirstOrDefault(e => (string)e.Attribute("name") == Name);
						if (listElement != null)
							listElement.Remove();
					});
				return true;
			}
			return false;
		}
	}
}
