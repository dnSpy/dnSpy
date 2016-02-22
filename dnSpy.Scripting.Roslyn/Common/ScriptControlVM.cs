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
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using dnSpy.Contracts.App;
using dnSpy.Contracts.Scripting;
using dnSpy.Contracts.TextEditor;
using dnSpy.Scripting.Roslyn.Properties;
using dnSpy.Shared.MVVM;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.Scripting.Hosting;

namespace dnSpy.Scripting.Roslyn.Common {
	abstract class ScriptControlVM : ViewModelBase, IReplCommandHandler, IScriptGlobalsHelper {
		public ICommand ResetCommand {
			get { return new RelayCommand(a => Reset(), a => CanReset); }
		}

		public ICommand ClearCommand {
			get { return new RelayCommand(a => replEditor.Clear(), a => replEditor.CanClear); }
		}

		public ICommand HistoryPreviousCommand {
			get { return new RelayCommand(a => replEditor.SelectPreviousCommand(), a => replEditor.CanSelectPreviousCommand); }
		}

		public ICommand HistoryNextCommand {
			get { return new RelayCommand(a => replEditor.SelectNextCommand(), a => replEditor.CanSelectNextCommand); }
		}

		public object ResetImageObject { get { return this; } }
		public object ClearWindowContentImageObject { get { return this; } }
		public object HistoryPreviousImageObject { get { return this; } }
		public object HistoryNextImageObject { get { return this; } }

		public bool CanReset {
			get { return hasInitialized && (execState == null || !execState.IsInitializing); }
		}

		public void Reset() {
			if (!CanReset)
				return;
			if (execState != null)
				execState.CancellationTokenSource.Cancel();
			execState = null;
			replEditor.Reset();
			replEditor.OutputPrintLine(dnSpy_Scripting_Roslyn_Resources.ResettingExecutionEngine);
			bool loadConfig = true;//TODO: set to false if "noconfig" was used
			InitializeExecutionEngine(loadConfig, false);
		}

		protected readonly IReplEditor replEditor;

		protected ScriptControlVM(IReplEditor replEditor, IServiceLocator serviceLocator) {
			this.replEditor = replEditor;
			this.replEditor.CommandHandler = this;
			this.serviceLocator = serviceLocator;
		}

		protected abstract string Logo { get; }
		protected abstract string Help { get; }
		protected abstract Script<object> Create(string code, ScriptOptions options, Type globalsType, InteractiveAssemblyLoader assemblyLoader);

		public void OnVisible() {
			if (hasInitialized)
				return;
			hasInitialized = true;

			this.replEditor.OutputPrintLine(Logo);
			InitializeExecutionEngine(true, true);
		}
		bool hasInitialized;

		public void RefreshThemeFields() {
			OnPropertyChanged("ResetImageObject");
			OnPropertyChanged("ClearWindowContentImageObject");
			OnPropertyChanged("HistoryPreviousImageObject");
			OnPropertyChanged("HistoryNextImageObject");
		}

		public bool IsCommand(string text) {
			return true;//TODO: Ask Roslyn whether it's something that appears to be a valid command
		}

		sealed class ExecState {
			public ScriptOptions ScriptOptions;
			public readonly CancellationTokenSource CancellationTokenSource;
			public readonly ScriptGlobals Globals;
			public ScriptState<object> ScriptState;
			public Task<ScriptState<object>> ExecTask;
			public bool Executing;
			public bool IsInitializing;
			public ExecState(ScriptControlVM vm, CancellationTokenSource cts) {
				this.CancellationTokenSource = cts;
				this.Globals = new ScriptGlobals(vm, cts.Token);
				this.IsInitializing = true;
			}
		}
		ExecState execState;
		readonly object lockObj = new object();

		void InitializeExecutionEngine(bool loadConfig, bool showHelp) {
			Debug.Assert(execState == null);
			if (execState != null)
				throw new InvalidOperationException();

			execState = new ExecState(this, new CancellationTokenSource());
			var execStateCache = execState;
			Task.Factory.StartNew(() => {
				AppCulture.InitializeCulture();
				execStateCache.CancellationTokenSource.Token.ThrowIfCancellationRequested();

				var opts = ScriptOptions.Default;
				if (loadConfig)
					opts = CreateScriptOptions(opts);
				execStateCache.ScriptOptions = opts;

				var script = Create(string.Empty, execStateCache.ScriptOptions, execStateCache.Globals.GetType(), null);
				execStateCache.CancellationTokenSource.Token.ThrowIfCancellationRequested();
				execStateCache.ScriptState = script.RunAsync(execStateCache.Globals, execStateCache.CancellationTokenSource.Token).Result;
				if (showHelp)
					this.replEditor.OutputPrintLine(Help);
			}, execStateCache.CancellationTokenSource.Token)
			.ContinueWith(t => {
				execStateCache.IsInitializing = false;
				var ex = t.Exception;
				if (!t.IsCanceled && !t.IsFaulted)
					CommandExecuted();
				else
					replEditor.OutputPrintLine(string.Format("Could not create the script:\n\n{0}", ex));
			}, CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.FromCurrentSynchronizationContext());
		}

		protected abstract ScriptOptions CreateScriptOptions(ScriptOptions options);

		public void ExecuteCommand(string input) {
			try {
				if (!ExecuteCommandInternal(input))
					CommandExecuted();
			}
			catch (Exception ex) {
				replEditor.OutputPrintLine(ex.ToString());
				CommandExecuted();
			}
		}

		bool ExecuteCommandInternal(string input) {
			Debug.Assert(execState != null && !execState.IsInitializing);
			if (execState == null || execState.IsInitializing)
				return true;
			lock (lockObj) {
				Debug.Assert(execState.ExecTask == null && !execState.Executing);
				if (execState.ExecTask != null || execState.Executing)
					return true;
				execState.Executing = true;
			}

			try {
				var oldState = execState;

				var taskSched = TaskScheduler.FromCurrentSynchronizationContext();
				Task.Factory.StartNew(() => {
					AppCulture.InitializeCulture();
					oldState.CancellationTokenSource.Token.ThrowIfCancellationRequested();

					var execTask = oldState.ScriptState.ContinueWithAsync(input, oldState.ScriptOptions, oldState.CancellationTokenSource.Token);
					oldState.CancellationTokenSource.Token.ThrowIfCancellationRequested();
					lock (lockObj) {
						if (oldState == execState)
							oldState.ExecTask = execTask;
					}
					execTask.ContinueWith(t => {
						AppCulture.InitializeCulture();

						var ex = t.Exception;
						bool isActive;
						lock (lockObj) {
							isActive = oldState == execState;
							if (isActive)
								oldState.ExecTask = null;
						}
						if (isActive) {
							if (ex != null)
								replEditor.OutputPrintLine(ex.ToString());

							if (!t.IsCanceled && !t.IsFaulted) {
								oldState.ScriptState = t.Result;
								var val = t.Result.ReturnValue;
								if (val != null)
									replEditor.OutputPrintLine(Format(val));
							}

							CommandExecuted();
						}
					}, CancellationToken.None, TaskContinuationOptions.None, taskSched);
				})
				.ContinueWith(t => {
					if (execState != null) {
						lock (lockObj)
							execState.Executing = false;
					}
					var ex = t.Exception;
					if (ex != null && ex.InnerException is CompilationErrorException) {
						var cee = (CompilationErrorException)ex.InnerException;
						const int MAX_DIAGS = 5;
						for (int i = 0; i < cee.Diagnostics.Length && i < MAX_DIAGS; i++)
							replEditor.OutputPrintLine(cee.Diagnostics[i].ToString());
						int extraErrors = cee.Diagnostics.Length - MAX_DIAGS;
						if (extraErrors > 0) {
							if (extraErrors == 1)
								replEditor.OutputPrintLine(string.Format(dnSpy_Scripting_Roslyn_Resources.CompilationAdditionalError, extraErrors));
							else
								replEditor.OutputPrintLine(string.Format(dnSpy_Scripting_Roslyn_Resources.CompilationAdditionalErrors, extraErrors));
						}
						CommandExecuted();
					}
					else
						ReportException(t);
				}, CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.FromCurrentSynchronizationContext());

				return true;
			}
			catch (Exception ex) {
				if (execState != null) {
					lock (lockObj)
						execState.Executing = false;
				}
				replEditor.OutputPrintLine(string.Format("Error executing script:\n\n{0}", ex));
				return false;
			}
		}

		void ReportException(Task t) {
			var ex = t.Exception;
			if (ex != null)
				replEditor.OutputPrintLine(ex.ToString());
		}

		void CommandExecuted() {
			this.replEditor.OnCommandExecuted();
			if (OnCommandExecuted != null)
				OnCommandExecuted(this, EventArgs.Empty);
		}
		public event EventHandler OnCommandExecuted;

		protected abstract ObjectFormatter ObjectFormatter { get; }

		string Format(object value) {
			return ObjectFormatter.FormatObject(value);
		}

		/// <summary>
		/// Returns true if it's the current script
		/// </summary>
		/// <param name="globals">Globals</param>
		/// <returns></returns>
		bool IsCurrentScript(ScriptGlobals globals) {
			var es = execState;
			return es != null && es.Globals == globals;
		}

		void IScriptGlobalsHelper.Print(ScriptGlobals globals, string text) {
			if (!IsCurrentScript(globals))
				return;
			replEditor.OutputPrint(text);
		}

		void IScriptGlobalsHelper.PrintLine(ScriptGlobals globals, string text) {
			if (!IsCurrentScript(globals))
				return;
			replEditor.OutputPrintLine(text);
		}

		void IScriptGlobalsHelper.Print(ScriptGlobals globals, object value) {
			if (!IsCurrentScript(globals))
				return;
			replEditor.OutputPrint(Format(value));
		}

		void IScriptGlobalsHelper.PrintLine(ScriptGlobals globals, object value) {
			if (!IsCurrentScript(globals))
				return;
			replEditor.OutputPrintLine(Format(value));
		}

		IServiceLocator IScriptGlobalsHelper.ServiceLocator {
			get { return serviceLocator; }
		}
		readonly IServiceLocator serviceLocator;

		protected static string GetResponseFile(string filename) {
			foreach (var dir in AppDirectories.GetDirectories(string.Empty)) {
				var path = Path.Combine(dir, filename);
				if (File.Exists(path))
					return path;
			}
			Debug.Fail(string.Format("Couldn't find the response file: {0}", filename));
			return null;
		}
	}
}
