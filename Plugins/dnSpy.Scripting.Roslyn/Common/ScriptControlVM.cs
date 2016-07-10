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
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;
using dnSpy.Contracts.App;
using dnSpy.Contracts.Scripting;
using dnSpy.Contracts.Text;
using dnSpy.Scripting.Roslyn.Properties;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.Scripting.Hosting;
using Microsoft.CodeAnalysis.Text;
using dnSpy.Contracts.Scripting.Roslyn;
using dnSpy.Contracts.Text.Editor;
using dnSpy.Roslyn.Shared.Text.Classification;
using Microsoft.CodeAnalysis.Host.Mef;
using dnSpy.Contracts.MVVM;

namespace dnSpy.Scripting.Roslyn.Common {
	sealed class UserScriptOptions {
		public readonly List<string> References = new List<string>();
		public readonly List<string> Imports = new List<string>();
		public readonly List<string> LibPaths = new List<string>();
		public readonly List<string> LoadPaths = new List<string>();
	}

	abstract class ScriptControlVM : ViewModelBase, IReplCommandHandler, IScriptGlobalsHelper {
		internal const string CMD_PREFIX = "#";

		public ICommand ResetCommand => new RelayCommand(a => Reset(), a => CanReset);
		public ICommand ClearCommand => new RelayCommand(a => ReplEditor.ClearScreen(), a => ReplEditor.CanClearScreen);
		public ICommand HistoryPreviousCommand => new RelayCommand(a => ReplEditor.SelectPreviousCommand(), a => ReplEditor.CanSelectPreviousCommand);
		public ICommand HistoryNextCommand => new RelayCommand(a => ReplEditor.SelectNextCommand(), a => ReplEditor.CanSelectNextCommand);
		public object ResetImageObject => this;
		public object ClearWindowContentImageObject => this;
		public object HistoryPreviousImageObject => this;
		public object HistoryNextImageObject => this;
		public bool CanReset => hasInitialized && (execState == null || !execState.IsInitializing);

		public void Reset(bool loadConfig = true) {
			if (!CanReset)
				return;
			if (execState != null) {
				execState.CancellationTokenSource.Cancel();
				try {
					execState.Globals.RaiseScriptReset();
				}
				catch {
					// Ignore buggy script exceptions
				}
			}
			isResetting = true;
			execState = null;
			ReplEditor.Reset();
			isResetting = false;
			ReplEditor.OutputPrintLine(dnSpy_Scripting_Roslyn_Resources.ResettingExecutionEngine, BoxedOutputColor.ReplOutputText);
			InitializeExecutionEngine(loadConfig, false);
		}
		bool isResetting;

		public IReplEditor ReplEditor { get; }

		public IEnumerable<IScriptCommand> ScriptCommands => toScriptCommand.Values;
		readonly Dictionary<string, IScriptCommand> toScriptCommand;

		IEnumerable<IScriptCommand> CreateScriptCommands() {
			yield return new ClearCommand();
			yield return new HelpCommand();
			yield return new ResetCommand();
		}

		readonly Dispatcher dispatcher;

		protected ScriptControlVM(IReplEditor replEditor, IServiceLocator serviceLocator) {
			this.dispatcher = Dispatcher.CurrentDispatcher;
			this.ReplEditor = replEditor;
			this.ReplEditor.CommandHandler = this;
			this.serviceLocator = serviceLocator;

			this.toScriptCommand = new Dictionary<string, IScriptCommand>(StringComparer.Ordinal);
			foreach (var sc in CreateScriptCommands()) {
				foreach (var name in sc.Names)
					this.toScriptCommand.Add(name, sc);
			}
		}

		protected abstract string Logo { get; }
		protected abstract string Help { get; }
		protected abstract Script<T> Create<T>(string code, ScriptOptions options, Type globalsType, InteractiveAssemblyLoader assemblyLoader);

		public void OnVisible() {
			if (hasInitialized)
				return;
			hasInitialized = true;

			this.ReplEditor.OutputPrintLine(Logo, BoxedOutputColor.ReplOutputText);
			InitializeExecutionEngine(true, true);
		}
		bool hasInitialized;

		public void RefreshThemeFields() {
			OnPropertyChanged(nameof(ResetImageObject));
			OnPropertyChanged(nameof(ClearWindowContentImageObject));
			OnPropertyChanged(nameof(HistoryPreviousImageObject));
			OnPropertyChanged(nameof(HistoryNextImageObject));
		}

		public bool IsCommand(string text) {
			if (ParseScriptCommand(text) != null)
				return true;
			return IsCompleteSubmission(text);
		}

		protected abstract bool IsCompleteSubmission(string text);

		sealed class ExecState {
			public ScriptOptions ScriptOptions;
			public readonly CancellationTokenSource CancellationTokenSource;
			public readonly ScriptGlobals Globals;
			public ScriptState<object> ScriptState;
			public Task<ScriptState<object>> ExecTask;
			public bool Executing;
			public bool IsInitializing;
			public ExecState(ScriptControlVM vm, Dispatcher dispatcher, CancellationTokenSource cts) {
				this.CancellationTokenSource = cts;
				this.Globals = new ScriptGlobals(vm, dispatcher, cts.Token);
				this.IsInitializing = true;
			}
		}
		ExecState execState;
		readonly object lockObj = new object();

		IEnumerable<string> GetDefaultScriptFilePaths() {
			const string SCRIPTS_DIR = "scripts";
			var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
			Debug.Assert(Directory.Exists(userProfile));
			if (Directory.Exists(userProfile)) {
				yield return Path.Combine(userProfile, SCRIPTS_DIR);
				yield return userProfile;
			}
			yield return Path.Combine(AppDirectories.DataDirectory, SCRIPTS_DIR);
			yield return Path.Combine(AppDirectories.BinDirectory, SCRIPTS_DIR);
		}

		IEnumerable<string> GetDefaultLibPaths() => GetDefaultScriptFilePaths();
		IEnumerable<string> GetDefaultLoadPaths() => GetDefaultScriptFilePaths();

		void InitializeExecutionEngine(bool loadConfig, bool showHelp) {
			Debug.Assert(execState == null);
			if (execState != null)
				throw new InvalidOperationException();

			execState = new ExecState(this, dispatcher, new CancellationTokenSource());
			var execStateCache = execState;
			Task.Run(() => {
				execStateCache.CancellationTokenSource.Token.ThrowIfCancellationRequested();

				var userOpts = new UserScriptOptions();
				if (loadConfig) {
					userOpts.LibPaths.AddRange(GetDefaultLibPaths());
					userOpts.LoadPaths.AddRange(GetDefaultLoadPaths());
					InitializeUserScriptOptions(userOpts);
				}
				var opts = ScriptOptions.Default;
				opts = opts.WithMetadataResolver(ScriptMetadataResolver.Default
								.WithBaseDirectory(AppDirectories.BinDirectory)
								.WithSearchPaths(userOpts.LibPaths.Distinct(StringComparer.OrdinalIgnoreCase)));
				opts = opts.WithSourceResolver(ScriptSourceResolver.Default
								.WithBaseDirectory(AppDirectories.BinDirectory)
								.WithSearchPaths(userOpts.LoadPaths.Distinct(StringComparer.OrdinalIgnoreCase)));
				opts = opts.WithImports(userOpts.Imports);
				opts = opts.WithReferences(userOpts.References);
				execStateCache.ScriptOptions = opts;

				var script = Create<object>(string.Empty, execStateCache.ScriptOptions, typeof(IScriptGlobals), null);
				execStateCache.CancellationTokenSource.Token.ThrowIfCancellationRequested();
				execStateCache.ScriptState = script.RunAsync(execStateCache.Globals, execStateCache.CancellationTokenSource.Token).Result;
				if (showHelp)
					this.ReplEditor.OutputPrintLine(Help, BoxedOutputColor.ReplOutputText);
			}, execStateCache.CancellationTokenSource.Token)
			.ContinueWith(t => {
				execStateCache.IsInitializing = false;
				var ex = t.Exception;
				if (!t.IsCanceled && !t.IsFaulted)
					CommandExecuted();
				else
					this.ReplEditor.OutputPrintLine($"Could not create the script:\n\n{ex}", BoxedOutputColor.Error, true);
			}, CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.FromCurrentSynchronizationContext());
		}

		protected abstract void InitializeUserScriptOptions(UserScriptOptions options);

		public void ExecuteCommand(string input) {
			try {
				if (!ExecuteCommandInternal(input))
					CommandExecuted();
			}
			catch (Exception ex) {
				ReplEditor.OutputPrint(ex.ToString(), BoxedOutputColor.Error, true);
				CommandExecuted();
			}
		}

		public void OnNewCommand() {
		}

		public Task OnCommandUpdatedAsync(IReplCommandInput command, CancellationToken cancellationToken) {
			if (isResetting)
				return Task.CompletedTask;
			Debug.Assert(execState != null);
			if (execState == null)
				throw new InvalidOperationException();

			string code = command.Input;
			const string assemblyName = "myasm";
			var previousScriptCompilation = execState.ScriptState.Script.GetCompilation();
			if (cancellationToken.IsCancellationRequested)
				return Task.CompletedTask;
			var options = previousScriptCompilation.Options;
			if (cancellationToken.IsCancellationRequested)
				return Task.CompletedTask;
			var syntaxTree = CreateSyntaxTree(code, cancellationToken);
			if (cancellationToken.IsCancellationRequested)
				return Task.CompletedTask;
			var sc = CreateScriptCompilation(assemblyName, syntaxTree, null, options, previousScriptCompilation, execState.ScriptState.Script.ReturnType, execState.ScriptState.Script.GlobalsType);
			if (cancellationToken.IsCancellationRequested)
				return Task.CompletedTask;
			var sem = sc.GetSemanticModel(syntaxTree);
			if (cancellationToken.IsCancellationRequested)
				return Task.CompletedTask;

			ClassifiedSpan[] classifiers;
			using (var workspace = new AdhocWorkspace(DesktopMefHostServices.DefaultServices))
				classifiers = Classifier.GetClassifiedSpans(sem, new TextSpan(0, command.Input.Length), workspace, cancellationToken).ToArray();
			if (cancellationToken.IsCancellationRequested)
				return Task.CompletedTask;
			var converter = new ClassificationTypeConverter(sem, cancellationToken);
			foreach (var c in classifiers)
				command.AddColor(c.TextSpan.Start, c.TextSpan.Length, converter.Convert(c));

			return Task.CompletedTask;
		}

		protected abstract SyntaxTree CreateSyntaxTree(string code, CancellationToken cancellationToken);
		protected abstract Compilation CreateScriptCompilation(string assemblyName, SyntaxTree syntaxTree, IEnumerable<MetadataReference> references, CompilationOptions options, Compilation previousScriptCompilation, Type returnType, Type globalsType);

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
				var scState = ParseScriptCommand(input);
				if (scState != null) {
					if (execState != null) {
						lock (lockObj)
							execState.Executing = false;
					}
					scState.Command.Execute(this, scState.Arguments);
					bool isReset = scState.Command is ResetCommand;
					if (!isReset)
						CommandExecuted();
					return true;
				}

				var oldState = execState;

				var taskSched = TaskScheduler.FromCurrentSynchronizationContext();
				Task.Run(() => {
					oldState.CancellationTokenSource.Token.ThrowIfCancellationRequested();

					var opts = oldState.ScriptOptions.WithReferences(Array.Empty<MetadataReference>()).WithImports(Array.Empty<string>());
					var execTask = oldState.ScriptState.ContinueWithAsync(input, opts, oldState.CancellationTokenSource.Token);
					oldState.CancellationTokenSource.Token.ThrowIfCancellationRequested();
					lock (lockObj) {
						if (oldState == execState)
							oldState.ExecTask = execTask;
					}
					execTask.ContinueWith(t => {
						var ex = t.Exception;
						bool isActive;
						lock (lockObj) {
							isActive = oldState == execState;
							if (isActive)
								oldState.ExecTask = null;
						}
						if (isActive) {
							try {
								if (ex != null)
									this.ReplEditor.OutputPrint(Format(ex.InnerException), BoxedOutputColor.Error, true);

								if (!t.IsCanceled && !t.IsFaulted) {
									oldState.ScriptState = t.Result;
									var val = t.Result.ReturnValue;
									if (val != null)
										ObjectOutputLine(BoxedOutputColor.ReplOutputText, oldState.Globals.PrintOptionsImpl, val, true);
								}
							}
							finally {
								CommandExecuted();
							}
						}
					}, CancellationToken.None, TaskContinuationOptions.None, taskSched);
				})
				.ContinueWith(t => {
					if (execState != null) {
						lock (lockObj)
							execState.Executing = false;
					}
					var innerEx = t.Exception?.InnerException;
					if (innerEx is CompilationErrorException) {
						var cee = (CompilationErrorException)innerEx;
						PrintDiagnostics(cee.Diagnostics);
						CommandExecuted();
					}
					else if (innerEx is OperationCanceledException)
						CommandExecuted();
					else {
						var ex = t.Exception;
						if (ex != null) {
							this.ReplEditor.OutputPrint(ex.ToString(), BoxedOutputColor.Error, true);
							CommandExecuted();
						}
					}
				}, CancellationToken.None, TaskContinuationOptions.None, taskSched);

				return true;
			}
			catch (Exception ex) {
				if (execState != null) {
					lock (lockObj)
						execState.Executing = false;
				}
				ReplEditor.OutputPrintLine($"Error executing script:\n\n{ex}", BoxedOutputColor.Error, true);
				return false;
			}
		}

		bool UnpackScriptCommand(string input, out string name, out string[] args) {
			name = null;
			args = null;

			var s = input.TrimStart();
			if (!s.StartsWith(CMD_PREFIX))
				return false;
			s = s.Substring(CMD_PREFIX.Length).TrimStart();

			var parts = s.Split(argSeps, StringSplitOptions.RemoveEmptyEntries);
			args = parts.Skip(1).ToArray();
			name = parts[0];
			return true;
		}
		static readonly char[] argSeps = new char[] { ' ', '\t', '\r', '\n', '\u0085', '\u2028', '\u2029' };

		sealed class ExecScriptCommandState {
			public readonly IScriptCommand Command;
			public readonly string[] Arguments;
			public ExecScriptCommandState(IScriptCommand sc, string[] args) {
				this.Command = sc;
				this.Arguments = args;
			}
		}

		ExecScriptCommandState ParseScriptCommand(string input) {
			string name;
			string[] args;
			if (!UnpackScriptCommand(input, out name, out args))
				return null;

			IScriptCommand sc;
			if (!toScriptCommand.TryGetValue(name, out sc))
				return null;

			return new ExecScriptCommandState(sc, args);
		}

		void PrintDiagnostics(ImmutableArray<Diagnostic> diagnostics) {
			const int MAX_DIAGS = 5;
			for (int i = 0; i < diagnostics.Length && i < MAX_DIAGS; i++)
				ReplEditor.OutputPrintLine(DiagnosticFormatter.Format(diagnostics[i], Thread.CurrentThread.CurrentUICulture), BoxedOutputColor.Error, true);
			int extraErrors = diagnostics.Length - MAX_DIAGS;
			if (extraErrors > 0) {
				if (extraErrors == 1)
					ReplEditor.OutputPrintLine(string.Format(dnSpy_Scripting_Roslyn_Resources.CompilationAdditionalError, extraErrors), BoxedOutputColor.Error, true);
				else
					ReplEditor.OutputPrintLine(string.Format(dnSpy_Scripting_Roslyn_Resources.CompilationAdditionalErrors, extraErrors), BoxedOutputColor.Error, true);
			}
		}

		void CommandExecuted() {
			this.ReplEditor.OnCommandExecuted();
			OnCommandExecuted?.Invoke(this, EventArgs.Empty);
		}
		public event EventHandler OnCommandExecuted;

		protected abstract ObjectFormatter ObjectFormatter { get; }
		protected abstract DiagnosticFormatter DiagnosticFormatter { get; }
		string Format(object value, PrintOptions printOptions) => ObjectFormatter.FormatObject(value, printOptions);
		string Format(Exception ex) => ObjectFormatter.FormatException(ex);

		/// <summary>
		/// Returns true if it's the current script
		/// </summary>
		/// <param name="globals">Globals</param>
		/// <returns></returns>
		bool IsCurrentScript(ScriptGlobals globals) => execState?.Globals == globals;

		void IScriptGlobalsHelper.Print(ScriptGlobals globals, object color, string text) {
			if (!IsCurrentScript(globals))
				return;
			ReplEditor.OutputPrint(text, color);
		}

		void IScriptGlobalsHelper.PrintLine(ScriptGlobals globals, object color, string text) {
			if (!IsCurrentScript(globals))
				return;
			ReplEditor.OutputPrintLine(text, color);
		}

		void IScriptGlobalsHelper.Print(ScriptGlobals globals, object color, PrintOptionsImpl printOptions, object value) {
			if (!IsCurrentScript(globals))
				return;
			ObjectOutput(color, printOptions, value);
		}

		void IScriptGlobalsHelper.PrintLine(ScriptGlobals globals, object color, PrintOptionsImpl printOptions, object value) {
			if (!IsCurrentScript(globals))
				return;
			ObjectOutputLine(color, printOptions, value);
		}

		void IScriptGlobalsHelper.Print(ScriptGlobals globals, object color, Exception ex) {
			if (!IsCurrentScript(globals))
				return;
			ReplEditor.OutputPrint(Format(ex), color);
		}

		void IScriptGlobalsHelper.PrintLine(ScriptGlobals globals, object color, Exception ex) {
			if (!IsCurrentScript(globals))
				return;
			ReplEditor.OutputPrintLine(Format(ex), color);
		}

		void IScriptGlobalsHelper.Print(ScriptGlobals globals, CachedWriter writer, object color, PrintOptionsImpl printOptions, object value) {
			if (!IsCurrentScript(globals))
				return;
			ObjectOutput(writer, color, printOptions, value);
		}

		void IScriptGlobalsHelper.Print(ScriptGlobals globals, CachedWriter writer, object color, Exception ex) {
			if (!IsCurrentScript(globals))
				return;
			writer.Write(Format(ex), color);
		}

		void IScriptGlobalsHelper.Write(ScriptGlobals globals, List<ColorAndText> list) {
			if (!IsCurrentScript(globals))
				return;
			ReplEditor.OutputPrint(list.Select(a => new ColorAndText(a.Color, a.Text)));
		}

		IOutputWritable GetOutputWritable(PrintOptionsImpl printOptions, object value) {
			if (!printOptions.AutoColorizeObjects)
				return null;
			return value as IOutputWritable;
		}

		sealed class OutputWriter : IOutputWriter {
			readonly ScriptControlVM owner;
			bool startOnNewLine;

			public static IOutputWriter Create(ScriptControlVM owner, bool startOnNewLine) {
				if (startOnNewLine)
					return new OutputWriter(owner, startOnNewLine);
				return normalOutputWriter = new OutputWriter(owner, false);
			}
			static IOutputWriter normalOutputWriter;

			OutputWriter(ScriptControlVM owner, bool startOnNewLine) {
				this.owner = owner;
			}

			public void Write(string text, object color) {
				owner.ReplEditor.OutputPrint(text, color ?? BoxedOutputColor.ReplScriptOutputText, startOnNewLine);
				startOnNewLine = false;
			}

			public void Write(string text, OutputColor color) => Write(text, color.Box());
		}

		void ObjectOutput(CachedWriter writer, object color, PrintOptionsImpl printOptions, object value) {
			var writable = GetOutputWritable(printOptions, value);
			if (writable != null)
				writable.WriteTo(writer);
			else
				writer.Write(Format(value, printOptions.RoslynPrintOptions), color);
		}

		void ObjectOutput(object color, PrintOptionsImpl printOptions, object value, bool startOnNewLine = false) {
			var writable = GetOutputWritable(printOptions, value);
			if (writable != null)
				writable.WriteTo(OutputWriter.Create(this, startOnNewLine));
			else
				ReplEditor.OutputPrint(Format(value, printOptions.RoslynPrintOptions), color, startOnNewLine);
		}
 
		void ObjectOutputLine(object color, PrintOptionsImpl printOptions, object value, bool startOnNewLine = false) {
			ObjectOutput(color, printOptions, value, startOnNewLine);
			ReplEditor.OutputPrintLine(string.Empty, color);
		}

		IServiceLocator IScriptGlobalsHelper.ServiceLocator => serviceLocator;
		readonly IServiceLocator serviceLocator;

		protected static string GetResponseFile(string filename) {
			foreach (var dir in AppDirectories.GetDirectories(string.Empty)) {
				var path = Path.Combine(dir, filename);
				if (File.Exists(path))
					return path;
			}
			Debug.Fail($"Couldn't find the response file: {filename}");
			return null;
		}
	}
}
