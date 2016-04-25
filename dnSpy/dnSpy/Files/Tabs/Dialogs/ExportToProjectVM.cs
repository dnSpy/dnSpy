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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Input;
using dnSpy.Contracts.Languages;
using dnSpy.Languages.MSBuild;
using dnSpy.Properties;
using dnSpy.Shared.MVVM;

namespace dnSpy.Files.Tabs.Dialogs {
	interface IExportTask {
		void Execute(ExportToProjectVM vm);
		void Cancel(ExportToProjectVM vm);
	}

	sealed class ExportToProjectVM : ViewModelBase {
		enum State {
			Editing,
			Exporting,
			Complete,
		}

		public ICommand PickDestDirCommand {
			get { return new RelayCommand(a => PickDestDir(), a => CanPickDestDir); }
		}

		public ICommand ExportProjectsCommand {
			get { return new RelayCommand(a => ExportProjects(), a => CanExportProjects); }
		}

		public ICommand GenerateNewProjectGuidCommand {
			get { return new RelayCommand(a => ProjectGuid.Value = Guid.NewGuid()); }
		}

		public string Directory {
			get { return directory; }
			set {
				if (directory != value) {
					directory = value;
					OnPropertyChanged("Directory");
					HasErrorUpdated();
				}
			}
		}
		string directory;

		public string SolutionFilename {
			get { return solutionFilename; }
			set {
				if (solutionFilename != value) {
					solutionFilename = value;
					OnPropertyChanged("SolutionFilename");
					HasErrorUpdated();
				}
			}
		}
		string solutionFilename;

		public bool CreateSolution {
			get { return createSolution; }
			set {
				if (createSolution != value) {
					createSolution = value;
					OnPropertyChanged("CreateSolution");
					HasErrorUpdated();
				}
			}
		}
		bool createSolution;

		public ProjectVersion ProjectVersion {
			get { return (ProjectVersion)projectVersionVM.SelectedItem; }
			set { projectVersionVM.SelectedItem = value; }
		}

		public EnumListVM ProjectVersionVM {
			get { return projectVersionVM; }
		}
		readonly EnumListVM projectVersionVM = new EnumListVM(EnumVM.Create(typeof(ProjectVersion)));

		public IEnumerable<ILanguage> AllLanguages {
			get { return languageManager.AllLanguages.Where(a => a.ProjectFileExtension != null); }
		}
		readonly ILanguageManager languageManager;

		public ILanguage Language {
			get { return language; }
			set {
				if (language != value) {
					language = value;
					OnPropertyChanged("Language");
				}
			}
		}
		ILanguage language;

		public NullableGuidVM ProjectGuid {
			get { return projectGuidVM; }
		}
		readonly NullableGuidVM projectGuidVM;

		public bool DontReferenceStdLib {
			get { return dontReferenceStdLib; }
			set {
				if (dontReferenceStdLib != value) {
					dontReferenceStdLib = value;
					OnPropertyChanged("DontReferenceStdLib");
				}
			}
		}
		bool dontReferenceStdLib;

		public bool UnpackResources {
			get { return unpackResources; }
			set {
				if (unpackResources != value) {
					unpackResources = value;
					OnPropertyChanged("UnpackResources");
					OnPropertyChanged("CanCreateResX");
					OnPropertyChanged("CanDecompileBaml");
				}
			}
		}
		bool unpackResources;

		public bool CreateResX {
			get { return createResX; }
			set {
				if (createResX != value) {
					createResX = value;
					OnPropertyChanged("CreateResX");
				}
			}
		}
		bool createResX;

		public bool DecompileXaml {
			get { return decompileXaml; }
			set {
				if (decompileXaml != value) {
					decompileXaml = value;
					OnPropertyChanged("DecompileXaml");
				}
			}
		}
		bool decompileXaml;

		public bool OpenProject {
			get { return openProject; }
			set {
				if (openProject != value) {
					openProject = value;
					OnPropertyChanged("OpenProject");
				}
			}
		}
		bool openProject;

		public bool CanDecompileBaml {
			get { return UnpackResources && canDecompileBaml; }
		}
		readonly bool canDecompileBaml;

		public bool CanCreateResX {
			get { return UnpackResources && TheState == State.Editing; }
		}

		public string FilesToExportMessage {
			get { return filesToExportMessage; }
			set {
				if (filesToExportMessage != value) {
					filesToExportMessage = value;
					OnPropertyChanged("FilesToExportMessage");
				}
			}
		}
		string filesToExportMessage;

		public bool IsIndeterminate {
			get { return isIndeterminate; }
			set {
				if (isIndeterminate != value) {
					isIndeterminate = value;
					OnPropertyChanged("IsIndeterminate");
				}
			}
		}
		bool isIndeterminate;

		public double ProgressMinimum {
			get { return progressMinimum; }
			set {
				if (progressMinimum != value) {
					progressMinimum = value;
					OnPropertyChanged("ProgressMinimum");
				}
			}
		}
		double progressMinimum;

		public double ProgressMaximum {
			get { return progressMaximum; }
			set {
				if (progressMaximum != value) {
					progressMaximum = value;
					OnPropertyChanged("ProgressMaximum");
				}
			}
		}
		double progressMaximum;

		public double TotalProgress {
			get { return totalProgress; }
			set {
				if (totalProgress != value) {
					totalProgress = value;
					OnPropertyChanged("TotalProgress");
				}
			}
		}
		double totalProgress;

		State TheState {
			get { return state; }
			set {
				if (state != value) {
					state = value;
					OnPropertyChanged("CanEditSettings");
					OnPropertyChanged("IsComplete");
					OnPropertyChanged("IsNotComplete");
					OnPropertyChanged("IsExporting");
					OnPropertyChanged("CanCreateResX");
				}
			}
		}
		State state = State.Editing;

		public bool IsComplete {
			get { return TheState == State.Complete; }
		}

		public bool IsNotComplete {
			get { return !IsComplete; }
		}

		public bool IsExporting {
			get { return TheState == State.Exporting; }
		}

		readonly IPickDirectory pickDirectory;
		readonly IExportTask exportTask;

		public ExportToProjectVM(IPickDirectory pickDirectory, ILanguageManager languageManager, IExportTask exportTask, bool canDecompileBaml) {
			this.pickDirectory = pickDirectory;
			this.languageManager = languageManager;
			this.exportTask = exportTask;
			this.canDecompileBaml = canDecompileBaml;
			this.unpackResources = true;
			this.createResX = true;
			this.decompileXaml = canDecompileBaml;
			this.createSolution = true;
			this.projectVersionVM.SelectedItem = ProjectVersion.VS2010;
			this.language = languageManager.AllLanguages.FirstOrDefault(a => a.ProjectFileExtension != null);
			this.isIndeterminate = false;
			this.projectGuidVM = new NullableGuidVM(Guid.NewGuid(), a => HasErrorUpdated());
		}

		bool CanPickDestDir {
			get { return true; }
		}

		void PickDestDir() {
			var newDir = pickDirectory.GetDirectory(Directory);
			if (newDir != null)
				Directory = newDir;
		}

		bool CanExportProjects {
			get { return TheState == State.Editing && !HasError; }
		}

		public bool CanEditSettings {
			get { return TheState == State.Editing; }
		}

		void ExportProjects() {
			Debug.Assert(TheState == State.Editing);
			TheState = State.Exporting;
			exportTask.Execute(this);
		}

		public void Cancel() {
			exportTask.Cancel(this);
		}

		public void OnExportComplete() {
			Debug.Assert(TheState == State.Exporting);
			TheState = State.Complete;
		}

		public void AddError(string msg) {
			ExportErrors = true;

			const int MAX_LEN = 8 * 1024;
			if (errorLog.Length < MAX_LEN) {
				var newValue = ErrorLog + msg + Environment.NewLine;
				if (newValue.Length > MAX_LEN)
					newValue = newValue.Substring(0, MAX_LEN) + "[...]";
				ErrorLog = newValue;
			}
		}

		public string ErrorLog {
			get { return errorLog; }
			set {
				if (errorLog != value) {
					errorLog = value;
					OnPropertyChanged("ErrorLog");
				}
			}
		}
		string errorLog = string.Empty;

		public bool ExportErrors {
			get { return exportErrors; }
			set {
				if (exportErrors != value) {
					exportErrors = value;
					OnPropertyChanged("ExportErrors");
					OnPropertyChanged("NoExportErrors");
				}
			}
		}
		bool exportErrors;

		public bool NoExportErrors {
			get { return !exportErrors; }
		}

		protected override string Verify(string columnName) {
			if (columnName == "Directory") {
				if (string.IsNullOrWhiteSpace(Directory))
					return dnSpy_Resources.Error_MissingDestinationFolder;
				if (File.Exists(Directory))
					return dnSpy_Resources.Error_FileAlreadyExists;
				return string.Empty;
			}
			if (CreateSolution && columnName == "SolutionFilename") {
				if (string.IsNullOrWhiteSpace(SolutionFilename))
					return dnSpy_Resources.Error_MissingFilename;
				return string.Empty;
			}
			return string.Empty;
		}

		public override bool HasError {
			get {
				return !string.IsNullOrEmpty(Verify("Directory")) ||
					!string.IsNullOrEmpty(Verify("SolutionFilename")) ||
					projectGuidVM.HasError;
			}
		}
	}
}
