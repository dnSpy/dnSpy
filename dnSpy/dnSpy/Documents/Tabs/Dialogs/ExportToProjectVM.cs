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
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.MVVM;
using dnSpy.Decompiler.MSBuild;
using dnSpy.Properties;

namespace dnSpy.Documents.Tabs.Dialogs {
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

		public ICommand PickDestDirCommand => new RelayCommand(a => PickDestDir(), a => CanPickDestDir);
		public ICommand ExportProjectsCommand => new RelayCommand(a => ExportProjects(), a => CanExportProjects);
		public ICommand GenerateNewProjectGuidCommand => new RelayCommand(a => ProjectGuid.Value = Guid.NewGuid());

		public string Directory {
			get { return directory; }
			set {
				if (directory != value) {
					directory = value;
					OnPropertyChanged(nameof(Directory));
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
					OnPropertyChanged(nameof(SolutionFilename));
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
					OnPropertyChanged(nameof(CreateSolution));
					HasErrorUpdated();
				}
			}
		}
		bool createSolution;

		public ProjectVersion ProjectVersion {
			get { return (ProjectVersion)ProjectVersionVM.SelectedItem; }
			set { ProjectVersionVM.SelectedItem = value; }
		}

		public EnumListVM ProjectVersionVM { get; } = new EnumListVM(EnumVM.Create(typeof(ProjectVersion)));
		public IEnumerable<IDecompiler> AllDecompilers => decompilerService.AllDecompilers.Where(a => a.ProjectFileExtension != null);
		readonly IDecompilerService decompilerService;

		public IDecompiler Decompiler {
			get { return decompiler; }
			set {
				if (decompiler != value) {
					decompiler = value;
					OnPropertyChanged(nameof(Decompiler));
				}
			}
		}
		IDecompiler decompiler;

		public NullableGuidVM ProjectGuid { get; }

		public bool DontReferenceStdLib {
			get { return dontReferenceStdLib; }
			set {
				if (dontReferenceStdLib != value) {
					dontReferenceStdLib = value;
					OnPropertyChanged(nameof(DontReferenceStdLib));
				}
			}
		}
		bool dontReferenceStdLib;

		public bool UnpackResources {
			get { return unpackResources; }
			set {
				if (unpackResources != value) {
					unpackResources = value;
					OnPropertyChanged(nameof(UnpackResources));
					OnPropertyChanged(nameof(CanCreateResX));
					OnPropertyChanged(nameof(CanDecompileBaml));
				}
			}
		}
		bool unpackResources;

		public bool CreateResX {
			get { return createResX; }
			set {
				if (createResX != value) {
					createResX = value;
					OnPropertyChanged(nameof(CreateResX));
				}
			}
		}
		bool createResX;

		public bool DecompileXaml {
			get { return decompileXaml; }
			set {
				if (decompileXaml != value) {
					decompileXaml = value;
					OnPropertyChanged(nameof(DecompileXaml));
				}
			}
		}
		bool decompileXaml;

		public bool OpenProject {
			get { return openProject; }
			set {
				if (openProject != value) {
					openProject = value;
					OnPropertyChanged(nameof(OpenProject));
				}
			}
		}
		bool openProject;

		public bool CanDecompileBaml => UnpackResources && canDecompileBaml;
		readonly bool canDecompileBaml;

		public bool CanCreateResX => UnpackResources && TheState == State.Editing;

		public string FilesToExportMessage {
			get { return filesToExportMessage; }
			set {
				if (filesToExportMessage != value) {
					filesToExportMessage = value;
					OnPropertyChanged(nameof(FilesToExportMessage));
				}
			}
		}
		string filesToExportMessage;

		public bool IsIndeterminate {
			get { return isIndeterminate; }
			set {
				if (isIndeterminate != value) {
					isIndeterminate = value;
					OnPropertyChanged(nameof(IsIndeterminate));
				}
			}
		}
		bool isIndeterminate;

		public double ProgressMinimum {
			get { return progressMinimum; }
			set {
				if (progressMinimum != value) {
					progressMinimum = value;
					OnPropertyChanged(nameof(ProgressMinimum));
				}
			}
		}
		double progressMinimum;

		public double ProgressMaximum {
			get { return progressMaximum; }
			set {
				if (progressMaximum != value) {
					progressMaximum = value;
					OnPropertyChanged(nameof(ProgressMaximum));
				}
			}
		}
		double progressMaximum;

		public double TotalProgress {
			get { return totalProgress; }
			set {
				if (totalProgress != value) {
					totalProgress = value;
					OnPropertyChanged(nameof(TotalProgress));
				}
			}
		}
		double totalProgress;

		State TheState {
			get { return state; }
			set {
				if (state != value) {
					state = value;
					OnPropertyChanged(nameof(CanEditSettings));
					OnPropertyChanged(nameof(IsComplete));
					OnPropertyChanged(nameof(IsNotComplete));
					OnPropertyChanged(nameof(IsExporting));
					OnPropertyChanged(nameof(CanCreateResX));
				}
			}
		}
		State state = State.Editing;

		public bool IsComplete => TheState == State.Complete;
		public bool IsNotComplete => !IsComplete;
		public bool IsExporting => TheState == State.Exporting;

		readonly IPickDirectory pickDirectory;
		readonly IExportTask exportTask;

		public ExportToProjectVM(IPickDirectory pickDirectory, IDecompilerService decompilerService, IExportTask exportTask, bool canDecompileBaml) {
			this.pickDirectory = pickDirectory;
			this.decompilerService = decompilerService;
			this.exportTask = exportTask;
			this.canDecompileBaml = canDecompileBaml;
			unpackResources = true;
			createResX = true;
			decompileXaml = canDecompileBaml;
			createSolution = true;
			ProjectVersionVM.SelectedItem = ProjectVersion.VS2010;
			decompiler = decompilerService.AllDecompilers.FirstOrDefault(a => a.ProjectFileExtension != null);
			isIndeterminate = false;
			ProjectGuid = new NullableGuidVM(Guid.NewGuid(), a => HasErrorUpdated());
		}

		bool CanPickDestDir => true;

		void PickDestDir() {
			var newDir = pickDirectory.GetDirectory(Directory);
			if (newDir != null)
				Directory = newDir;
		}

		bool CanExportProjects => TheState == State.Editing && !HasError;
		public bool CanEditSettings => TheState == State.Editing;

		void ExportProjects() {
			Debug.Assert(TheState == State.Editing);
			TheState = State.Exporting;
			exportTask.Execute(this);
		}

		public void Cancel() => exportTask.Cancel(this);

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
					OnPropertyChanged(nameof(ErrorLog));
				}
			}
		}
		string errorLog = string.Empty;

		public bool ExportErrors {
			get { return exportErrors; }
			set {
				if (exportErrors != value) {
					exportErrors = value;
					OnPropertyChanged(nameof(ExportErrors));
					OnPropertyChanged(nameof(NoExportErrors));
				}
			}
		}
		bool exportErrors;

		public bool NoExportErrors => !exportErrors;

		protected override string Verify(string columnName) {
			if (columnName == nameof(Directory)) {
				if (string.IsNullOrWhiteSpace(Directory))
					return dnSpy_Resources.Error_MissingDestinationFolder;
				if (File.Exists(Directory))
					return dnSpy_Resources.Error_FileAlreadyExists;
				return string.Empty;
			}
			if (CreateSolution && columnName == nameof(SolutionFilename)) {
				if (string.IsNullOrWhiteSpace(SolutionFilename))
					return dnSpy_Resources.Error_MissingFilename;
				return string.Empty;
			}
			return string.Empty;
		}

		public override bool HasError =>
			!string.IsNullOrEmpty(Verify(nameof(Directory))) ||
			!string.IsNullOrEmpty(Verify(nameof(SolutionFilename))) ||
			ProjectGuid.HasError;
	}
}
