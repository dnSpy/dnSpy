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
using System.Windows.Threading;
using System.Xml.Linq;
using dnSpy.Files;

namespace ICSharpCode.ILSpy {
	/// <summary>
	/// Manages the available assembly lists.
	/// 
	/// Contains the list of list names; and provides methods for loading/saving and creating/deleting lists.
	/// </summary>
	public sealed class DnSpyFileListManager {
		const string LIST_SECTION_NAME = "List";
		const string FILELISTS_SECTION_NAME = "FileLists";
		const string FILE_SECTION_NAME = "File";

		//TODO: Remove these a couple of months after this commit
		const string FILELISTS_SECTION_NAME_OLD = "AssemblyLists";
		const string FILE_SECTION_NAME_OLD = "Assembly";

		public IDnSpyFileListOptions DnSpyFileListOptions {
			get { return options; }
		}
		readonly IDnSpyFileListOptions options;

		public DnSpyFileListManager(IDnSpyFileListOptions options, DNSpySettings spySettings) {
			this.options = options;
			var doc = GetFileListsElement(spySettings);
			foreach (var list in doc.Elements(LIST_SECTION_NAME))
				FileLists.Add(SessionSettings.Unescape((string)list.Attribute("name")));
		}

		public readonly ObservableCollection<string> FileLists = new ObservableCollection<string>();

		/// <summary>
		/// Loads an assembly list from the ILSpySettings.
		/// If no list with the specified name is found, the default list is loaded instead.
		/// </summary>
		public DnSpyFileList LoadList(DNSpySettings spySettings, string listName) {
			DnSpyFileList list = DoLoadList(spySettings, listName);
			if (!FileLists.Contains(list.Name))
				FileLists.Add(list.Name);
			return list;
		}

		static XElement GetFileListsElement(DNSpySettings spySettings) {
			var doc = spySettings.GetElement(FILELISTS_SECTION_NAME);
			if (doc == null)
				doc = spySettings[FILELISTS_SECTION_NAME_OLD];
			return doc ?? new XElement(FILELISTS_SECTION_NAME);
		}

		DnSpyFileList DoLoadList(DNSpySettings spySettings, string listName) {
			var doc = GetFileListsElement(spySettings);
			if (listName != null) {
				foreach (var listElem in doc.Elements(LIST_SECTION_NAME)) {
					if (SessionSettings.Unescape((string)listElem.Attribute("name")) == listName)
						return Initialize(Create(listElem));
				}
			}
			XElement firstList = doc.Elements(LIST_SECTION_NAME).FirstOrDefault();
			DnSpyFileList list;
			if (firstList != null)
				list = Create(firstList);
			else
				list = new DnSpyFileList(options, listName ?? DefaultListName);
			return Initialize(list);
		}

		DnSpyFileList Initialize(DnSpyFileList list) {
			list.CollectionChanged += (s, e) => Save(list);
			return list;
		}

		void Save(DnSpyFileList list) {
			if (!list.IsDirty) {
				list.IsDirty = true;
				App.Current.Dispatcher.BeginInvoke(
				DispatcherPriority.Background,
				new Action(
					delegate {
						bool callGc = true;//TODO:
						if (callGc) {
							GC.Collect();
							GC.WaitForPendingFinalizers();
						}
						list.IsDirty = false;
						SaveList(list);
					})
				);
			}
		}

		public void RefreshSave(DnSpyFileList list) {
			if (!list.IsDirty) {
				list.IsDirty = true;
				App.Current.Dispatcher.BeginInvoke(
					DispatcherPriority.Background,
					new Action(
						delegate {
							list.IsDirty = false;
							SaveList(list);
						})
				);
			}
		}

		public const string DefaultListName = "(Default)";

		DnSpyFileList Create(XElement listElement) {
			var name = SessionSettings.Unescape((string)listElement.Attribute("name"));
			var list = new DnSpyFileList(options, name);
			var elems = listElement.Elements(FILE_SECTION_NAME).ToList();
			elems.AddRange(listElement.Elements(FILE_SECTION_NAME_OLD));
			foreach (var asm in elems) {
				try {
					list.OpenFile(SessionSettings.Unescape((string)asm));
				}
				catch {
				}
			}
			list.IsDirty = false;
			return list;
		}

		/// <summary>
		/// Saves the specifies assembly list into the config file.
		/// </summary>
		public static void SaveList(DnSpyFileList list) {
			DNSpySettings.Update((root) => {
				var doc = root.Element(FILELISTS_SECTION_NAME) ?? root.Element(FILELISTS_SECTION_NAME_OLD);
				if (doc != null)
					doc.Name = FILELISTS_SECTION_NAME;
				if (doc == null) {
					doc = new XElement(FILELISTS_SECTION_NAME);
					root.Add(doc);
				}
				XElement listElement = doc.Elements(LIST_SECTION_NAME).FirstOrDefault(e => SessionSettings.Unescape((string)e.Attribute("name")) == list.Name);
				if (listElement != null)
					listElement.ReplaceWith(SaveAsXml(list));
				else
					doc.Add(SaveAsXml(list));
			});
		}

		static XElement SaveAsXml(DnSpyFileList list) {
			return new XElement(
				LIST_SECTION_NAME,
				new XAttribute("name", SessionSettings.Escape(list.Name)),
				list.GetDnSpyFiles().Where(file => !file.IsAutoLoaded && file.CanBeSavedToSettingsFile && !string.IsNullOrWhiteSpace(file.Filename)).Select(asm => new XElement(FILE_SECTION_NAME, SessionSettings.Escape(asm.Filename)))
			);
		}


		public bool CreateList(DnSpyFileList list) {
			if (!FileLists.Contains(list.Name)) {
				FileLists.Add(list.Name);
				Initialize(list);
				SaveList(list);
				return true;
			}
			return false;
		}

		public bool DeleteList(string Name) {
			if (FileLists.Contains(Name)) {
				FileLists.Remove(Name);

				DNSpySettings.Update(
					delegate (XElement root) {
						XElement doc = root.Element(FILELISTS_SECTION_NAME);
						if (doc == null)
							root.Element(FILELISTS_SECTION_NAME_OLD);
						if (doc == null)
							return;
						XElement listElement = doc.Elements(LIST_SECTION_NAME).FirstOrDefault(e => SessionSettings.Unescape((string)e.Attribute("name")) == Name);
						if (listElement != null)
							listElement.Remove();
					});
				return true;
			}
			return false;
		}
	}
}
