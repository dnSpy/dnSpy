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
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;
using dnSpy.AsmEditor.Hex;
using dnSpy.AsmEditor.Properties;
using dnSpy.Contracts.Files;
using dnSpy.Shared.MVVM;

namespace dnSpy.AsmEditor.SaveModule {
	sealed class SaveMultiModuleVM : INotifyPropertyChanged {
		ObservableCollection<SaveOptionsVM> modules = new ObservableCollection<SaveOptionsVM>();

		enum SaveState {
			/// <summary>
			/// We haven't started saving yet
			/// </summary>
			Loaded,

			/// <summary>
			/// We're saving
			/// </summary>
			Saving,

			/// <summary>
			/// We're canceling
			/// </summary>
			Canceling,

			/// <summary>
			/// Final state even if some files weren't saved
			/// </summary>
			Saved,
		}

		SaveState State {
			get { return saveState; }
			set {
				if (value != saveState) {
					saveState = value;
					OnPropertyChanged("IsLoaded");
					OnPropertyChanged("IsSaving");
					OnPropertyChanged("IsCanceling");
					OnPropertyChanged("IsSaved");
					OnPropertyChanged("CanSave");
					OnPropertyChanged("CanCancel");
					OnPropertyChanged("CanClose");
					OnPropertyChanged("IsSavingOrCanceling");
					OnModuleSettingsSaved();

					if (saveState == SaveState.Saved && OnSavedEvent != null)
						OnSavedEvent(this, EventArgs.Empty);
				}
			}
		}
		SaveState saveState = SaveState.Loaded;

		public ICommand SaveCommand {
			get { return new RelayCommand(a => Save(), a => CanExecuteSave); }
		}

		public ICommand CancelSaveCommand {
			get { return new RelayCommand(a => CancelSave(), a => IsSaving && moduleSaver != null); }
		}

		public event EventHandler OnSavedEvent;

		public bool IsLoaded {
			get { return State == SaveState.Loaded; }
		}

		public bool IsSaving {
			get { return State == SaveState.Saving; }
		}

		public bool IsCanceling {
			get { return State == SaveState.Canceling; }
		}

		public bool IsSaved {
			get { return State == SaveState.Saved; }
		}

		public bool CanSave {
			get { return IsLoaded; }
		}

		public bool CanCancel {
			get { return IsLoaded || IsSaving; }
		}

		public bool CanClose {
			get { return !CanCancel; }
		}

		public bool IsSavingOrCanceling {
			get { return IsSaving || IsCanceling; }
		}

		public bool CanExecuteSave {
			get { return string.IsNullOrEmpty(CanExecuteSaveError); }
		}

		public bool CanShowModuleErrors {
			get { return IsLoaded && !CanExecuteSave; }
		}

		public string CanExecuteSaveError {
			get {
				if (!IsLoaded)
					return "It's only possible to save when loaded";

				for (int i = 0; i < modules.Count; i++) {
					var module = modules[i];
					if (module.HasError)
						return string.Format(dnSpy_AsmEditor_Resources.SaveModules_FileHasErrors, i + 1, module.FileName.Trim() == string.Empty ? dnSpy_AsmEditor_Resources.EmptyFilename : module.FileName);
				}

				return null;
			}
		}

		public void OnModuleSettingsSaved() {
			OnPropertyChanged("CanExecuteSaveError");
			OnPropertyChanged("CanExecuteSave");
			OnPropertyChanged("CanShowModuleErrors");
		}

		public bool HasError {
			get { return hasError; }
			private set {
				if (hasError != value) {
					hasError = value;
					OnPropertyChanged("HasError");
					OnPropertyChanged("HasNoError");
				}
			}
		}
		bool hasError;

		public bool HasNoError {
			get { return !HasError; }
		}

		public int ErrorCount {
			get { return errorCount; }
			set {
				if (errorCount != value) {
					errorCount = value;
					OnPropertyChanged("ErrorCount");
					HasError = errorCount != 0;
				}
			}
		}
		int errorCount;

		public string LogMessage {
			get { return logMessage.ToString(); }
		}
		StringBuilder logMessage = new StringBuilder();

		public double ProgressMinimum {
			get { return 0; }
		}

		public double ProgressMaximum {
			get { return 100; }
		}

		public double TotalProgress {
			get { return totalProgress; }
			private set {
				if (totalProgress != value) {
					totalProgress = value;
					OnPropertyChanged("TotalProgress");
				}
			}
		}
		double totalProgress = 0;

		public double CurrentFileProgress {
			get { return currentFileProgress; }
			private set {
				if (currentFileProgress != value) {
					currentFileProgress = value;
					OnPropertyChanged("CurrentFileProgress");
				}
			}
		}
		double currentFileProgress = 0;

		public string CurrentFileName {
			get { return currentFileName; }
			set {
				if (currentFileName != value) {
					currentFileName = value;
					OnPropertyChanged("CurrentFileName");
				}
			}
		}
		string currentFileName = string.Empty;

		public ObservableCollection<SaveOptionsVM> Modules {
			get { return modules; }
		}

		readonly IMmapDisabler mmapDisabler;
		readonly Dispatcher dispatcher;

		public SaveMultiModuleVM(IMmapDisabler mmapDisabler, Dispatcher dispatcher, SaveOptionsVM options) {
			this.mmapDisabler = mmapDisabler;
			this.dispatcher = dispatcher;
			this.modules.Add(options);
		}

		public SaveMultiModuleVM(IMmapDisabler mmapDisabler, Dispatcher dispatcher, IEnumerable<object> objs) {
			this.mmapDisabler = mmapDisabler;
			this.dispatcher = dispatcher;
			this.modules.AddRange(objs.Select(m => Create(m)));
		}

		static SaveOptionsVM Create(object obj) {
			var file = obj as IDnSpyFile;
			if (file != null)
				return new SaveModuleOptionsVM(file);

			var doc = obj as AsmEdHexDocument;
			if (doc != null)
				return new SaveHexOptionsVM(doc);

			throw new InvalidOperationException();
		}

		SaveOptionsVM GetSaveOptionsVM(object obj) {
			return modules.FirstOrDefault(a => a.UndoDocument == obj);
		}

		public bool WasSaved(object obj) {
			var data = GetSaveOptionsVM(obj);
			if (data == null)
				return false;
			bool saved;
			savedFile.TryGetValue(data, out saved);
			return saved;
		}

		public string GetSavedFileName(object obj) {
			var data = GetSaveOptionsVM(obj);
			return data == null ? null : data.FileName;
		}

		public void Save() {
			if (!CanExecuteSave)
				return;
			State = SaveState.Saving;
			TotalProgress = 0;
			CurrentFileProgress = 0;
			CurrentFileName = string.Empty;
			savedFile.Clear();

			var mods = modules.ToArray();
			mmapDisabler.Disable(mods.Select(a => a.FileName));
			new Thread(() => SaveAsync(mods)).Start();
		}

		void ExecInOldThread(Action action) {
			if (dispatcher.HasShutdownStarted || dispatcher.HasShutdownFinished)
				return;
			dispatcher.BeginInvoke(DispatcherPriority.Background, action);
		}

		ModuleSaver moduleSaver;
		void SaveAsync(SaveOptionsVM[] mods) {
			try {
				moduleSaver = new ModuleSaver(mods);
				moduleSaver.OnProgressUpdated += moduleSaver_OnProgressUpdated;
				moduleSaver.OnLogMessage += moduleSaver_OnLogMessage;
				moduleSaver.OnWritingFile += moduleSaver_OnWritingFile;
				moduleSaver.SaveAll();
				AsyncAddMessage(dnSpy_AsmEditor_Resources.SaveModules_Log_AllFilesWritten, false, false);
			}
			catch (TaskCanceledException) {
				AsyncAddMessage(dnSpy_AsmEditor_Resources.SaveModules_Log_SaveWasCanceled, true, false);
			}
			catch (UnauthorizedAccessException ex) {
				AsyncAddMessage(string.Format(dnSpy_AsmEditor_Resources.SaveModules_Log_AccessError, ex.Message), true, false);
			}
			catch (IOException ex) {
				AsyncAddMessage(string.Format(dnSpy_AsmEditor_Resources.SaveModules_Log_FileError, ex.Message), true, false);
			}
			catch (Exception ex) {
				AsyncAddMessage(string.Format(dnSpy_AsmEditor_Resources.SaveModules_Log_Exception, ex), true, false);
			}
			moduleSaver = null;

			ExecInOldThread(() => {
				CurrentFileName = string.Empty;
				State = SaveState.Saved;
			});
		}

		void moduleSaver_OnWritingFile(object sender, ModuleSaverWriteEventArgs e) {
			if (e.Starting) {
				ExecInOldThread(() => {
					CurrentFileName = e.File.FileName;
				});
				AsyncAddMessage(string.Format(dnSpy_AsmEditor_Resources.SaveModules_Log_WritingFile, e.File.FileName), false, false);
			}
			else {
				shownMessages.Clear();
				savedFile.Add(e.File, true);
			}
		}
		Dictionary<SaveOptionsVM, bool> savedFile = new Dictionary<SaveOptionsVM, bool>();

		void moduleSaver_OnProgressUpdated(object sender, EventArgs e) {
			var moduleSaver = (ModuleSaver)sender;
			double totalProgress = 100 * moduleSaver.TotalProgress;
			double currentFileProgress = 100 * moduleSaver.CurrentFileProgress;
			ExecInOldThread(() => {
				this.TotalProgress = totalProgress;
				this.CurrentFileProgress = currentFileProgress;
			});
		}

		void moduleSaver_OnLogMessage(object sender, ModuleSaverLogEventArgs e) {
			AsyncAddMessage(e.Message, e.Event == ModuleSaverLogEvent.Error || e.Event == ModuleSaverLogEvent.Warning, true);
		}

		void AsyncAddMessage(string msg, bool isError, bool canIgnore) {
			// If there are a lot of errors, we don't want to add a ton of extra delegates to be
			// called in the old thread. Just use one so we don't slow down everything to a crawl.
			lock (addMessageStringBuilder) {
				if (!canIgnore || !shownMessages.Contains(msg)) {
					addMessageStringBuilder.AppendLine(msg);
					if (canIgnore)
						shownMessages.Add(msg);
				}
				if (isError)
					errors++;
				if (!hasAddedMessage) {
					hasAddedMessage = true;
					ExecInOldThread(() => {
						string logMsgTmp;
						int errorsTmp;
						lock (addMessageStringBuilder) {
							logMsgTmp = addMessageStringBuilder.ToString();
							errorsTmp = errors;

							hasAddedMessage = false;
							addMessageStringBuilder.Clear();
							errors = 0;
						}

						ErrorCount += errorsTmp;
						logMessage.Append(logMsgTmp);
						OnPropertyChanged("LogMessage");
					});
				}
			}
		}
		HashSet<string> shownMessages = new HashSet<string>(StringComparer.Ordinal);
		StringBuilder addMessageStringBuilder = new StringBuilder();
		int errors;
		bool hasAddedMessage;

		public void CancelSave() {
			if (!IsSaving)
				return;
			var ms = moduleSaver;
			if (ms == null)
				return;

			State = SaveState.Canceling;
			ms.CancelAsync();
		}

		public event PropertyChangedEventHandler PropertyChanged;

		void OnPropertyChanged(string propName) {
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(propName));
		}
	}
}
