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
using dnSpy.Contracts;
using dnSpy.Contracts.Settings;
using dnSpy.Files;

namespace ICSharpCode.ILSpy {
	/// <summary>
	/// Manages the available assembly lists.
	/// 
	/// Contains the list of list names; and provides methods for loading/saving and creating/deleting lists.
	/// </summary>
	public sealed class DnSpyFileListManager {
		const string LIST_SECTION_NAME = "List";
		const string SETTINGS_NAME = "92B7D8B9-DC49-4E59-B201-71DB2BD2B272";
		const string FILE_SECTION_NAME = "File";

		public IDnSpyFileListOptions DnSpyFileListOptions {
			get { return options; }
		}
		readonly IDnSpyFileListOptions options;

		public DnSpyFileListManager(IDnSpyFileListOptions options) {
			this.options = options;
			var section = DnSpy.App.SettingsManager.GetOrCreateSection(SETTINGS_NAME);
			foreach (var list in section.SectionsWithName(LIST_SECTION_NAME))
				FileLists.Add(list.Attribute<string>("name") ?? string.Empty);
		}

		public readonly ObservableCollection<string> FileLists = new ObservableCollection<string>();

		/// <summary>
		/// Loads an assembly list from the ILSpySettings.
		/// If no list with the specified name is found, the default list is loaded instead.
		/// </summary>
		public DnSpyFileList LoadList(string listName) {
			DnSpyFileList list = DoLoadList(listName);
			if (!FileLists.Contains(list.Name))
				FileLists.Add(list.Name);
			return list;
		}

		DnSpyFileList DoLoadList(string listName) {
			var section = DnSpy.App.SettingsManager.GetOrCreateSection(SETTINGS_NAME);
			if (listName != null) {
				foreach (var listSection in section.SectionsWithName(LIST_SECTION_NAME)) {
					if (listSection.Attribute<string>("name") == listName)
						return Initialize(Create(listSection));
				}
			}
			var firstList = section.SectionsWithName(LIST_SECTION_NAME).FirstOrDefault();
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

		DnSpyFileList Create(ISettingsSection section) {
			var name = section.Attribute<string>("name") ?? string.Empty;
			var list = new DnSpyFileList(options, name);
			foreach (var fileSection in section.SectionsWithName(FILE_SECTION_NAME)) {
				try {
					list.OpenFile(fileSection.Attribute<string>("Name"));
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
			var section = DnSpy.App.SettingsManager.GetOrCreateSection(SETTINGS_NAME);
			var listSection = section.SectionsWithName(LIST_SECTION_NAME).FirstOrDefault(e => e.Attribute<string>("name") == list.Name);
			if (listSection != null)
				section.RemoveSection(listSection);
			listSection = section.CreateSection(LIST_SECTION_NAME);
			SaveTo(listSection, list);
		}

		static void SaveTo(ISettingsSection section, DnSpyFileList list) {
			section.Attribute("name", list.Name);
			foreach (var file in list.GetDnSpyFiles().Where(file => !file.IsAutoLoaded && file.CanBeSavedToSettingsFile && !string.IsNullOrWhiteSpace(file.Filename))) {
				var fileSection = section.CreateSection(FILE_SECTION_NAME);
				fileSection.Attribute("Name", file.Filename);
			}
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

		public void DeleteList(string Name) {
			if (FileLists.Contains(Name)) {
				FileLists.Remove(Name);

				var section = DnSpy.App.SettingsManager.TryGetSection(SETTINGS_NAME);
				if (section == null)
					return;

				var listSection = section.SectionsWithName(LIST_SECTION_NAME).FirstOrDefault(e => e.Attribute<string>("name") == Name);
				if (listSection != null)
					section.RemoveSection(listSection);
			}
		}
	}
}
