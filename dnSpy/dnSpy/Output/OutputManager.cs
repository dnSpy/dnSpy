/*
    Copyright (C) 2014-2016 de4dot@gmail.com

    This file is part of dnSpy

    dnSpy is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    dnSpy is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with dnSpy.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.Output;
using dnSpy.Contracts.TextEditor;
using dnSpy.Properties;
using dnSpy.Shared.MVVM;

namespace dnSpy.Output {
	interface IOutputManagerInternal : IOutputManager {
		IInputElement FocusedElement { get; }
		bool CanClearAll { get; }
		void ClearAll();
		bool CanSaveText { get; }
		void SaveText();
		OutputBufferVM SelectLog(int index);
		bool CanSelectLog(int index);
		bool WordWrap { get; set; }
		bool ShowLineNumbers { get; set; }
		bool ShowTimestamps { get; set; }
		void RefreshThemeFields();
		OutputBufferVM SelectedOutputBufferVM { get; }
	}

	[Export, Export(typeof(IOutputManagerInternal)), Export(typeof(IOutputManager)), PartCreationPolicy(CreationPolicy.Shared)]
	sealed class OutputManager : ViewModelBase, IOutputManagerInternal {
		public ICommand ClearAllCommand => new RelayCommand(a => ClearAll(), a => CanClearAll);
		public ICommand SaveCommand => new RelayCommand(a => SaveText(), a => CanSaveText);

		public bool WordWrap {
			get { return outputManagerSettingsImpl.WordWrap; }
			set {
				if (outputManagerSettingsImpl.WordWrap != value) {
					outputManagerSettingsImpl.WordWrap = value;
					OnPropertyChanged("WordWrap");
					foreach (var vm in OutputBuffers)
						vm.WordWrap = outputManagerSettingsImpl.WordWrap;
				}
			}
		}

		public bool ShowLineNumbers {
			get { return outputManagerSettingsImpl.ShowLineNumbers; }
			set {
				if (outputManagerSettingsImpl.ShowLineNumbers != value) {
					outputManagerSettingsImpl.ShowLineNumbers = value;
					OnPropertyChanged("ShowLineNumbers");
					foreach (var vm in OutputBuffers)
						vm.ShowLineNumbers = outputManagerSettingsImpl.ShowLineNumbers;
				}
			}
		}

		public bool ShowTimestamps {
			get { return outputManagerSettingsImpl.ShowTimestamps; }
			set {
				if (outputManagerSettingsImpl.ShowTimestamps != value) {
					outputManagerSettingsImpl.ShowTimestamps = value;
					OnPropertyChanged("ShowTimestamps");
					foreach (var vm in OutputBuffers)
						vm.ShowTimestamps = outputManagerSettingsImpl.ShowTimestamps;
				}
			}
		}

		public object TextEditorUIObject => SelectedOutputBufferVM?.TextEditorUIObject;
		public IInputElement FocusedElement => SelectedOutputBufferVM?.FocusedElement;
		public bool HasOutputWindows => SelectedOutputBufferVM != null;

		public OutputBufferVM SelectedOutputBufferVM {
			get { return selectedOutputBufferVM; }
			set {
				if (selectedOutputBufferVM != value) {
					selectedOutputBufferVM = value;
					outputManagerSettingsImpl.SelectedGuid = value?.Guid ?? Guid.Empty;
					OnPropertyChanged("SelectedOutputBufferVM");
					OnPropertyChanged("TextEditorUIObject");
					OnPropertyChanged("FocusedElement");
					OnPropertyChanged("HasOutputWindows");
				}
			}
		}
		OutputBufferVM selectedOutputBufferVM;

		public object ClearAllImageObject => this;
		public object SaveImageObject => this;
		public object ToggleWordWrapImageObject => this;

		public ObservableCollection<OutputBufferVM> OutputBuffers => outputBuffers;
		readonly ObservableCollection<OutputBufferVM> outputBuffers;
		readonly ILogEditorCreator logEditorCreator;
		readonly OutputManagerSettingsImpl outputManagerSettingsImpl;
		readonly IPickSaveFilename pickSaveFilename;
		Guid prevSelectedGuid;

		[ImportingConstructor]
		OutputManager(ILogEditorCreator logEditorCreator, OutputManagerSettingsImpl outputManagerSettingsImpl, IPickSaveFilename pickSaveFilename) {
			this.logEditorCreator = logEditorCreator;
			this.outputManagerSettingsImpl = outputManagerSettingsImpl;
			this.prevSelectedGuid = outputManagerSettingsImpl.SelectedGuid;
			this.pickSaveFilename = pickSaveFilename;
			this.outputBuffers = new ObservableCollection<OutputBufferVM>();
			this.outputBuffers.CollectionChanged += OutputBuffers_CollectionChanged;
		}

		void OutputBuffers_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
			if (SelectedOutputBufferVM == null)
				SelectedOutputBufferVM = OutputBuffers.FirstOrDefault();

			if (e.NewItems != null) {
				foreach (OutputBufferVM vm in e.NewItems) {
					vm.WordWrap = outputManagerSettingsImpl.WordWrap;
					vm.ShowLineNumbers = outputManagerSettingsImpl.ShowLineNumbers;
					vm.ShowTimestamps = outputManagerSettingsImpl.ShowTimestamps;
					if (vm.Guid == prevSelectedGuid && prevSelectedGuid != Guid.Empty) {
						SelectedOutputBufferVM = vm;
						prevSelectedGuid = Guid.Empty;
					}
				}
			}
		}

		public IOutputTextPane Create(Guid guid, string name, Guid? textEditorCommandGuid, Guid? textAreaCommandGuid) {
			if (name == null)
				throw new ArgumentNullException(nameof(name));

			var vm = OutputBuffers.FirstOrDefault(a => a.Guid == guid);
			Debug.Assert(vm == null || vm.Name == name);
			if (vm != null)
				return vm;

			var logEditorOptions = new LogEditorOptions {
				TextEditorCommandGuid = textEditorCommandGuid,
				TextAreaCommandGuid = textAreaCommandGuid,
				MenuGuid = new Guid(MenuConstants.GUIDOBJ_LOG_TEXTEDITORCONTROL_GUID),
				CreateGuidObjects = (creatorObject, openedFromKeyboard) => CreateGuidObjects(creatorObject, openedFromKeyboard),
			};
			var logEditor = logEditorCreator.Create(logEditorOptions);

			vm = new OutputBufferVM(guid, name, logEditor);
			int index = GetSortedInsertIndex(vm);
			OutputBuffers.Insert(index, vm);
			while (index < OutputBuffers.Count)
				OutputBuffers[index].Index = index++;
			return vm;
		}

		IEnumerable<GuidObject> CreateGuidObjects(GuidObject creatorObject, bool openedFromKeyboard) {
			yield return new GuidObject(MenuConstants.GUIDOBJ_OUTPUT_MANAGER_GUID, this);
			var vm = SelectedOutputBufferVM as IOutputTextPane;
			if (vm != null)
				yield return new GuidObject(MenuConstants.GUIDOBJ_ACTIVE_OUTPUT_TEXTPANE_GUID, vm);
		}

		int GetSortedInsertIndex(OutputBufferVM vm) {
			for (int i = 0; i < OutputBuffers.Count; i++) {
				if (StringComparer.InvariantCultureIgnoreCase.Compare(vm.Name, OutputBuffers[i].Name) < 0)
					return i;
			}
			return OutputBuffers.Count;
		}

		public IOutputTextPane Find(Guid guid) => OutputBuffers.FirstOrDefault(a => a.Guid == guid);
		public IOutputTextPane GetTextPane(Guid guid) => Find(guid) ?? new NotPresentOutputWriter(this, guid);

		public void Select(Guid guid) {
			var vm = OutputBuffers.FirstOrDefault(a => a.Guid == guid);
			Debug.Assert(vm != null);
			if (vm != null)
				this.SelectedOutputBufferVM = vm;
		}

		public void RefreshThemeFields() {
			OnPropertyChanged("ClearAllImageObject");
			OnPropertyChanged("SaveImageObject");
			OnPropertyChanged("ToggleWordWrapImageObject");
		}

		public bool CanClearAll => SelectedOutputBufferVM != null;

		public void ClearAll() {
			if (!CanClearAll)
				return;
			SelectedOutputBufferVM?.Clear();
		}

		public bool CanSaveText => SelectedOutputBufferVM != null;

		public void SaveText() {
			if (!CanSaveText)
				return;
			var vm = SelectedOutputBufferVM;
			var filename = pickSaveFilename.GetFilename(GetFilename(vm), "txt", TEXTFILES_FILTER);
			if (filename == null)
				return;
			try {
				File.WriteAllText(filename, vm.GetText());
			}
			catch (Exception ex) {
				Shared.App.MsgBox.Instance.Show(ex);
			}
		}
		static readonly string TEXTFILES_FILTER = string.Format("{1} (*.txt)|*.txt|{0} (*.*)|*.*", dnSpy_Resources.AllFiles, dnSpy_Resources.TextFiles);

		string GetFilename(OutputBufferVM vm) {
			// Same as VS2015
			var s = vm.Name.Replace(" ", string.Empty);
			return dnSpy_Resources.Window_Output + "-" + s + ".txt";
		}

		public bool CanSelectLog(int index) => (uint)index < (uint)OutputBuffers.Count;

		public OutputBufferVM SelectLog(int index) {
			if (!CanSelectLog(index))
				return null;
			SelectedOutputBufferVM = OutputBuffers[index];
			return SelectedOutputBufferVM;
		}
	}
}
