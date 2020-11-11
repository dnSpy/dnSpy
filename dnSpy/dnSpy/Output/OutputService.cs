/*
    Copyright (C) 2014-2019 de4dot@gmail.com

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
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using dnSpy.Contracts.App;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.MVVM;
using dnSpy.Contracts.Output;
using dnSpy.Contracts.Settings.AppearanceCategory;
using dnSpy.Contracts.Text.Editor;
using dnSpy.Contracts.Utilities;
using dnSpy.Output.Settings;
using dnSpy.Properties;
using dnSpy.Text.Editor;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Output {
	interface IOutputServiceInternal : IOutputService {
		IInputElement? FocusedElement { get; }
		bool CanCopy { get; }
		void Copy();
		bool CanClearAll { get; }
		void ClearAll();
		bool CanSaveText { get; }
		void SaveText();
		OutputBufferVM? SelectLog(int index);
		bool CanSelectLog(int index);
		bool WordWrap { get; set; }
		bool ShowLineNumbers { get; set; }
		bool ShowTimestamps { get; set; }
		OutputBufferVM? SelectedOutputBufferVM { get; }
		double ZoomLevel { get; }
	}

	[Export(typeof(IOutputServiceInternal)), Export(typeof(IOutputService))]
	sealed class OutputService : ViewModelBase, IOutputServiceInternal {
		public ICommand ClearAllCommand => new RelayCommand(a => ClearAll(), a => CanClearAll);
		public ICommand SaveCommand => new RelayCommand(a => SaveText(), a => CanSaveText);

		public string ClearAllToolTip => ToolTipHelper.AddKeyboardShortcut(dnSpy_Resources.Output_ClearAll_ToolTip, dnSpy_Resources.ShortCutKeyCtrlL);
		public string SaveToolTip => ToolTipHelper.AddKeyboardShortcut(dnSpy_Resources.Output_Save_ToolTip, dnSpy_Resources.ShortCutKeyCtrlS);
		public string WordWrapToolTip => ToolTipHelper.AddKeyboardShortcut(dnSpy_Resources.Output_WordWrap_ToolTip, dnSpy_Resources.ShortCutKeyCtrlECtrlW);

		public bool WordWrap {
			get => (outputWindowOptionsService.Default.WordWrapStyle & WordWrapStyles.WordWrap) != 0;
			set {
				if (WordWrap != value) {
					if (value)
						outputWindowOptionsService.Default.WordWrapStyle |= WordWrapStyles.WordWrap;
					else
						outputWindowOptionsService.Default.WordWrapStyle &= ~WordWrapStyles.WordWrap;
				}
			}
		}

		public bool ShowLineNumbers {
			get => outputWindowOptionsService.Default.LineNumberMargin;
			set => outputWindowOptionsService.Default.LineNumberMargin = value;
		}

		public bool ShowTimestamps {
			get => outputWindowOptionsService.Default.ShowTimestamps;
			set => outputWindowOptionsService.Default.ShowTimestamps = value;
		}

		public object? TextEditorUIObject => SelectedOutputBufferVM?.TextEditorUIObject;
		public IInputElement? FocusedElement => SelectedOutputBufferVM?.FocusedElement;
		public bool HasOutputWindows => SelectedOutputBufferVM is not null;
		public double ZoomLevel => SelectedOutputBufferVM?.ZoomLevel ?? 100;

		public OutputBufferVM? SelectedOutputBufferVM {
			get => selectedOutputBufferVM;
			set {
				if (selectedOutputBufferVM != value) {
					selectedOutputBufferVM = value;
					outputServiceSettingsImpl.SelectedGuid = value?.Guid ?? Guid.Empty;
					OnPropertyChanged(nameof(SelectedOutputBufferVM));
					OnPropertyChanged(nameof(TextEditorUIObject));
					OnPropertyChanged(nameof(FocusedElement));
					OnPropertyChanged(nameof(HasOutputWindows));
				}
			}
		}
		OutputBufferVM? selectedOutputBufferVM;

		public ObservableCollection<OutputBufferVM> OutputBuffers => outputBuffers;
		readonly ObservableCollection<OutputBufferVM> outputBuffers;
		readonly ILogEditorProvider logEditorProvider;
		readonly OutputServiceSettingsImpl outputServiceSettingsImpl;
		readonly IPickSaveFilename pickSaveFilename;
		Guid prevSelectedGuid;
		readonly IEditorOperationsFactoryService editorOperationsFactoryService;
		readonly IMenuService menuService;
		readonly IOutputWindowOptionsService outputWindowOptionsService;

		[ImportingConstructor]
		OutputService(IOutputWindowOptionsService outputWindowOptionsService, IEditorOperationsFactoryService editorOperationsFactoryService, ILogEditorProvider logEditorProvider, OutputServiceSettingsImpl outputServiceSettingsImpl, IPickSaveFilename pickSaveFilename, IMenuService menuService, [ImportMany] IEnumerable<Lazy<IOutputServiceListener, IOutputServiceListenerMetadata>> outputServiceListeners) {
			this.outputWindowOptionsService = outputWindowOptionsService;
			outputWindowOptionsService.OptionChanged += OutputWindowOptionsService_OptionChanged;
			this.editorOperationsFactoryService = editorOperationsFactoryService;
			this.logEditorProvider = logEditorProvider;
			this.outputServiceSettingsImpl = outputServiceSettingsImpl;
			prevSelectedGuid = outputServiceSettingsImpl.SelectedGuid;
			this.pickSaveFilename = pickSaveFilename;
			this.menuService = menuService;
			outputBuffers = new ObservableCollection<OutputBufferVM>();
			outputBuffers.CollectionChanged += OutputBuffers_CollectionChanged;

			var listeners = outputServiceListeners.OrderBy(a => a.Metadata.Order).ToArray();
			Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Send, new Action(() => {
				foreach (var lazy in outputServiceListeners) {
					var l = lazy.Value;
					(l as IOutputServiceListener2)?.Initialize(this);
				}
			}));
		}

		void OutputWindowOptionsService_OptionChanged(object? sender, OptionChangedEventArgs e) {
			if (e.OptionId == DefaultTextViewOptions.WordWrapStyleName)
				OnPropertyChanged(nameof(WordWrap));
		}

		void OutputBuffers_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) {
			if (SelectedOutputBufferVM is null)
				SelectedOutputBufferVM = OutputBuffers.FirstOrDefault();

			if (e.NewItems is not null) {
				foreach (OutputBufferVM? vm in e.NewItems) {
					Debug2.Assert(vm is not null);
					if (vm.Guid == prevSelectedGuid && prevSelectedGuid != Guid.Empty) {
						SelectedOutputBufferVM = vm;
						prevSelectedGuid = Guid.Empty;
						break;
					}
				}
			}
		}

		public IOutputTextPane Create(Guid guid, string name, string contentType) =>
			Create(guid, name, (object)contentType);

		public IOutputTextPane Create(Guid guid, string name, IContentType? contentType) =>
			Create(guid, name, (object?)contentType);

		IOutputTextPane Create(Guid guid, string name, object? contentTypeObj) {
			if (name is null)
				throw new ArgumentNullException(nameof(name));

			var vm = OutputBuffers.FirstOrDefault(a => a.Guid == guid);
			Debug2.Assert(vm is null || vm.Name == name);
			if (vm is not null)
				return vm;

			var logEditorOptions = new LogEditorOptions {
				MenuGuid = new Guid(MenuConstants.GUIDOBJ_LOG_TEXTEDITORCONTROL_GUID),
				ContentType = contentTypeObj as IContentType,
				ContentTypeString = contentTypeObj as string,
				CreateGuidObjects = args => CreateGuidObjects(args),
			};
			logEditorOptions.ExtraRoles.Add(PredefinedDsTextViewRoles.OutputTextPane);
			var logEditor = logEditorProvider.Create(logEditorOptions);
			logEditor.TextView.Options.SetOptionValue(DefaultWpfViewOptions.AppearanceCategory, AppearanceCategoryConstants.OutputWindow);

			// Prevent toolwindow's ctx menu from showing up when right-clicking in the left margin
			menuService.InitializeContextMenu(logEditor.TextViewHost.HostControl, Guid.NewGuid());

			vm = new OutputBufferVM(editorOperationsFactoryService, guid, name, logEditor);
			int index = GetSortedInsertIndex(vm);
			OutputBuffers.Insert(index, vm);
			while (index < OutputBuffers.Count)
				OutputBuffers[index].Index = index++;

			OutputTextPaneUtils.AddInstance(vm, logEditor.TextView);
			return vm;
		}

		IEnumerable<GuidObject> CreateGuidObjects(GuidObjectsProviderArgs args) {
			yield return new GuidObject(MenuConstants.GUIDOBJ_OUTPUT_SERVICE_GUID, this);
			if (SelectedOutputBufferVM is IOutputTextPane vm)
				yield return new GuidObject(MenuConstants.GUIDOBJ_ACTIVE_OUTPUT_TEXTPANE_GUID, vm);
		}

		int GetSortedInsertIndex(OutputBufferVM vm) {
			for (int i = 0; i < OutputBuffers.Count; i++) {
				if (StringComparer.InvariantCultureIgnoreCase.Compare(vm.Name, OutputBuffers[i].Name) < 0)
					return i;
			}
			return OutputBuffers.Count;
		}

		public IOutputTextPane? Find(Guid guid) => OutputBuffers.FirstOrDefault(a => a.Guid == guid);
		public IOutputTextPane GetTextPane(Guid guid) => Find(guid) ?? new NotPresentOutputWriter(this, guid);

		public void Select(Guid guid) {
			var vm = OutputBuffers.FirstOrDefault(a => a.Guid == guid);
			Debug2.Assert(vm is not null);
			if (vm is not null)
				SelectedOutputBufferVM = vm;
		}

		public bool CanCopy => SelectedOutputBufferVM?.CanCopy == true;
		public void Copy() => SelectedOutputBufferVM?.Copy();

		public bool CanClearAll => SelectedOutputBufferVM is not null;

		public void ClearAll() {
			if (!CanClearAll)
				return;
			SelectedOutputBufferVM?.Clear();
		}

		public bool CanSaveText => SelectedOutputBufferVM is not null;

		public void SaveText() {
			if (!CanSaveText)
				return;
			Debug2.Assert(SelectedOutputBufferVM is not null);
			var vm = SelectedOutputBufferVM;
			var filename = pickSaveFilename.GetFilename(GetFilename(vm), "txt", TEXTFILES_FILTER);
			if (filename is null)
				return;
			try {
				File.WriteAllText(filename, vm.GetText(), Encoding.UTF8);
			}
			catch (Exception ex) {
				MsgBox.Instance.Show(ex);
			}
		}
		static readonly string TEXTFILES_FILTER = $"{dnSpy_Resources.TextFiles} (*.txt)|*.txt|{dnSpy_Resources.AllFiles} (*.*)|*.*";

		string GetFilename(OutputBufferVM vm) {
			// Same as VS2015
			var s = vm.Name.Replace(" ", string.Empty);
			return dnSpy_Resources.Window_Output + "-" + s + ".txt";
		}

		public bool CanSelectLog(int index) => (uint)index < (uint)OutputBuffers.Count;

		public OutputBufferVM? SelectLog(int index) {
			if (!CanSelectLog(index))
				return null;
			SelectedOutputBufferVM = OutputBuffers[index];
			return SelectedOutputBufferVM;
		}
	}
}
