/*
    Copyright (C) 2014-2017 de4dot@gmail.com

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
using dnSpy.AsmEditor.Properties;
using dnSpy.Contracts.Documents;
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.MVVM;

namespace dnSpy.AsmEditor.SaveModule {
	sealed class SaveMultiModuleVM : INotifyPropertyChanged {
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
					OnPropertyChanged(nameof(IsLoaded));
					OnPropertyChanged(nameof(IsSaving));
					OnPropertyChanged(nameof(IsCanceling));
					OnPropertyChanged(nameof(IsSaved));
					OnPropertyChanged(nameof(CanSave));
					OnPropertyChanged(nameof(CanCancel));
					OnPropertyChanged(nameof(CanClose));
					OnPropertyChanged(nameof(IsSavingOrCanceling));
					OnModuleSettingsSaved();

					if (saveState == SaveState.Saved && OnSavedEvent != null)
						OnSavedEvent(this, EventArgs.Empty);
				}
			}
		}
		SaveState saveState = SaveState.Loaded;

		public ICommand SaveCommand => new RelayCommand(a => Save(), a => CanExecuteSave);
		public ICommand CancelSaveCommand => new RelayCommand(a => CancelSave(), a => IsSaving && moduleSaver != null);
		public event EventHandler OnSavedEvent;
		public bool IsLoaded => State == SaveState.Loaded;
		public bool IsSaving => State == SaveState.Saving;
		public bool IsCanceling => State == SaveState.Canceling;
		public bool IsSaved => State == SaveState.Saved;
		public bool CanSave => IsLoaded;
		public bool CanCancel => IsLoaded || IsSaving;
		public bool CanClose => !CanCancel;
		public bool IsSavingOrCanceling => IsSaving || IsCanceling;
		public bool CanExecuteSave => string.IsNullOrEmpty(CanExecuteSaveError);
		public bool CanShowModuleErrors => IsLoaded && !CanExecuteSave;

		public string CanExecuteSaveError {
			get {
				if (!IsLoaded)
					return "It's only possible to save when loaded";

				for (int i = 0; i < Modules.Count; i++) {
					var module = Modules[i];
					if (module.HasError)
						return string.Format(dnSpy_AsmEditor_Resources.SaveModules_FileHasErrors, i + 1, module.FileName.Trim() == string.Empty ? dnSpy_AsmEditor_Resources.EmptyFilename : module.FileName);
				}

				return null;
			}
		}

		public void OnModuleSettingsSaved() {
			OnPropertyChanged(nameof(CanExecuteSaveError));
			OnPropertyChanged(nameof(CanExecuteSave));
			OnPropertyChanged(nameof(CanShowModuleErrors));
		}

		public bool HasError {
			get { return hasError; }
			private set {
				if (hasError != value) {
					hasError = value;
					OnPropertyChanged(nameof(HasError));
					OnPropertyChanged(nameof(HasNoError));
				}
			}
		}
		bool hasError;

		public bool HasNoError => !HasError;

		public int ErrorCount {
			get { return errorCount; }
			set {
				if (errorCount != value) {
					errorCount = value;
					OnPropertyChanged(nameof(ErrorCount));
					HasError = errorCount != 0;
				}
			}
		}
		int errorCount;

		public string LogMessage => logMessage.ToString();
		StringBuilder logMessage = new StringBuilder();

		public double ProgressMinimum => 0;
		public double ProgressMaximum => 100;

		public double TotalProgress {
			get { return totalProgress; }
			private set {
				if (totalProgress != value) {
					totalProgress = value;
					OnPropertyChanged(nameof(TotalProgress));
				}
			}
		}
		double totalProgress = 0;

		public double CurrentFileProgress {
			get { return currentFileProgress; }
			private set {
				if (currentFileProgress != value) {
					currentFileProgress = value;
					OnPropertyChanged(nameof(CurrentFileProgress));
				}
			}
		}
		double currentFileProgress = 0;

		public string CurrentFileName {
			get { return currentFileName; }
			set {
				if (currentFileName != value) {
					currentFileName = value;
					OnPropertyChanged(nameof(CurrentFileName));
				}
			}
		}
		string currentFileName = string.Empty;

		public ObservableCollection<SaveOptionsVM> Modules { get; } = new ObservableCollection<SaveOptionsVM>();

		readonly IMmapDisabler mmapDisabler;
		readonly Dispatcher dispatcher;

		public SaveMultiModuleVM(IMmapDisabler mmapDisabler, Dispatcher dispatcher, SaveOptionsVM options) {
			this.mmapDisabler = mmapDisabler;
			this.dispatcher = dispatcher;
			Modules.Add(options);
		}

		public SaveMultiModuleVM(IMmapDisabler mmapDisabler, Dispatcher dispatcher, IEnumerable<object> objs) {
			this.mmapDisabler = mmapDisabler;
			this.dispatcher = dispatcher;
			Modules.AddRange(objs.Select(m => Create(m)));
		}

		static SaveOptionsVM Create(object obj) {
			var document = obj as IDsDocument;
			if (document != null)
				return new SaveModuleOptionsVM(document);

			var buffer = obj as HexBuffer;
			if (buffer != null)
				return new SaveHexOptionsVM(buffer);

			throw new InvalidOperationException();
		}

		SaveOptionsVM GetSaveOptionsVM(object obj) => Modules.FirstOrDefault(a => a.UndoDocument == obj);

		public bool WasSaved(object obj) {
			var data = GetSaveOptionsVM(obj);
			if (data == null)
				return false;
			savedFile.TryGetValue(data, out bool saved);
			return saved;
		}

		public string GetSavedFileName(object obj) => GetSaveOptionsVM(obj)?.FileName;

		public void Save() {
			if (!CanExecuteSave)
				return;
			State = SaveState.Saving;
			TotalProgress = 0;
			CurrentFileProgress = 0;
			CurrentFileName = string.Empty;
			savedFile.Clear();

			var mods = Modules.ToArray();
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
				TotalProgress = totalProgress;
				CurrentFileProgress = currentFileProgress;
			});
		}

		void moduleSaver_OnLogMessage(object sender, ModuleSaverLogEventArgs e) =>
			AsyncAddMessage(e.Message, e.Event == ModuleSaverLogEvent.Error || e.Event == ModuleSaverLogEvent.Warning, true);

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
						OnPropertyChanged(nameof(LogMessage));
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
		void OnPropertyChanged(string propName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
	}
}
