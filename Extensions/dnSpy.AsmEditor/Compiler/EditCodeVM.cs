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
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using dnlib.DotNet;
using dnSpy.AsmEditor.Properties;
using dnSpy.AsmEditor.ViewHelpers;
using dnSpy.Contracts.App;
using dnSpy.Contracts.AsmEditor.Compiler;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Languages;
using dnSpy.Contracts.MVVM;
using dnSpy.Contracts.Text.Editor;
using Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.AsmEditor.Compiler {
	sealed class EditCodeVM : ViewModelBase, IDisposable {
		readonly IImageManager imageManager;
		readonly IOpenFromGAC openFromGAC;
		readonly IOpenAssembly openAssembly;
		readonly ILanguageCompiler languageCompiler;
		readonly ILanguage language;
		readonly AssemblyReferenceResolver assemblyReferenceResolver;

		internal Window OwnerWindow { get; set; }

		// Make all types, methods, fields public so we don't get a compilation error when trying
		// to reference an internal type or a private method in the original assembly.
		const bool makeEverythingPublic = true;

		public ModuleImporter Result { get; set; }
		public event EventHandler CodeCompiled;
		public bool HasDecompiled { get; private set; }
		public ICommand CompileCommand => new RelayCommand(a => CompileCode(), a => CanCompile);
		public ICommand AddAssemblyReferenceCommand => new RelayCommand(a => AddAssemblyReference(), a => CanAddAssemblyReference);
		public ICommand AddGacReferenceCommand => new RelayCommand(a => AddGacReference(), a => CanAddGacReference);
		public object AddAssemblyReferenceImageObject => imageManager.GetImage(new ImageReference(GetType().Assembly, "Open"), BackgroundType.DialogWindow);
		public object AddGacReferenceImageObject => imageManager.GetImage(new ImageReference(GetType().Assembly, "Library"), BackgroundType.DialogWindow);

		public bool CanCompile {
			get { return canCompile; }
			set {
				if (canCompile != value) {
					canCompile = value;
					OnPropertyChanged(nameof(CanCompile));
				}
			}
		}
		bool canCompile;

		public ObservableCollection<ICodeDocument> Documents { get; } = new ObservableCollection<ICodeDocument>();
		public ICodeDocument SelectedDocument {
			get { return selectedDocument; }
			set {
				if (selectedDocument != value) {
					selectedDocument = value;
					OnPropertyChanged(nameof(SelectedDocument));
				}
			}
		}
		ICodeDocument selectedDocument;

		readonly MethodDef methodToEdit;

		public ObservableCollection<CompilerDiagnosticVM> Diagnostics { get; } = new ObservableCollection<CompilerDiagnosticVM>();

		public EditCodeVM(IImageManager imageManager, IOpenFromGAC openFromGAC, IOpenAssembly openAssembly, ILanguageCompiler languageCompiler, ILanguage language, MethodDef methodToEdit) {
			Debug.Assert(language.CanDecompile(DecompilationType.TypeMethods));
			this.imageManager = imageManager;
			this.openFromGAC = openFromGAC;
			this.openAssembly = openAssembly;
			this.languageCompiler = languageCompiler;
			this.language = language;
			this.methodToEdit = methodToEdit;
			this.assemblyReferenceResolver = new AssemblyReferenceResolver(methodToEdit.Module.Context.AssemblyResolver, methodToEdit.Module, makeEverythingPublic);
			StartDecompileAsync(methodToEdit).ContinueWith(t => {
				var ex = t.Exception;
				Debug.Assert(ex == null);
			}, CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.FromCurrentSynchronizationContext());
		}

		abstract class AsyncStateBase : IDisposable {
			readonly CancellationTokenSource cancellationTokenSource;
			public CancellationToken CancellationToken { get; }
			bool disposed;

			protected AsyncStateBase() {
				this.cancellationTokenSource = new CancellationTokenSource();
				CancellationToken = cancellationTokenSource.Token;
			}

			public void CancelAndDispose() {
				Cancel();
				Dispose();
			}

			void Cancel() {
				if (!disposed)
					cancellationTokenSource.Cancel();
			}

			public void Dispose() {
				disposed = true;
				cancellationTokenSource.Dispose();
			}
		}

		sealed class DecompileCodeState : AsyncStateBase {
			public StringBuilderDecompilerOutput MainOutput { get; }
			public StringBuilderDecompilerOutput HiddenOutput { get; }
			public DecompilationContext DecompilationContext { get; }

			public DecompileCodeState() {
				DecompilationContext = new DecompilationContext {
					CancellationToken = CancellationToken,
				};
				MainOutput = new StringBuilderDecompilerOutput();
				HiddenOutput = new StringBuilderDecompilerOutput();
			}
		}
		DecompileCodeState decompileCodeState;

		sealed class CompileCodeState : AsyncStateBase {
		}
		CompileCodeState compileCodeState;

		async Task StartDecompileAsync(MethodDef method) {
			bool canCompile = false, canceled = false;
			var assemblyReferences = Array.Empty<CompilerMetadataReference>();
			string mainCode, hiddenCode;
			try {
				assemblyReferences = await DecompileAndGetRefsAsync(method);
				mainCode = decompileCodeState.MainOutput.ToString();
				hiddenCode = decompileCodeState.HiddenOutput.ToString();
				canCompile = true;
			}
			catch (OperationCanceledException) {
				canceled = true;
				mainCode = string.Empty;
				hiddenCode = string.Empty;
			}
			catch (Exception ex) {
				mainCode = ex.ToString();
				hiddenCode = string.Empty;
			}

			const string MAIN_CODE_NAME = "main";
			var codeDocs = Array.Empty<ICodeDocument>();
			if (!canceled) {
				var docs = new List<IDecompiledDocument>();
				docs.Add(new DecompiledDocument(mainCode, MAIN_CODE_NAME));
				if (hiddenCode != string.Empty)
					docs.Add(new DecompiledDocument(hiddenCode, MAIN_CODE_NAME + ".g"));
				codeDocs = languageCompiler.AddDecompiledCode(new DecompiledCodeResult(docs.ToArray(), assemblyReferences, assemblyReferenceResolver, PlatformHelper.GetPlatform(method.Module)));
			}

			decompileCodeState?.Dispose();
			decompileCodeState = null;

			foreach (var doc in codeDocs)
				doc.TextView.Properties.AddProperty(editCodeTextViewKey, this);
			Documents.AddRange(codeDocs);
			SelectedDocument = Documents.FirstOrDefault(a => a.NameNoExtension == MAIN_CODE_NAME) ?? Documents.FirstOrDefault();
			foreach (var doc in Documents) {
				doc.TextView.MoveCaretTo(0, 0);
				doc.TextView.Selection.Clear();
			}

			CanCompile = canCompile;
			HasDecompiled = true;
			OnPropertyChanged(nameof(HasDecompiled));
		}
		static readonly object editCodeTextViewKey = new object();

		static internal EditCodeVM TryGet(ITextView textView) {
			EditCodeVM vm;
			textView.Properties.TryGetProperty(editCodeTextViewKey, out vm);
			return vm;
		}

		async Task<CompilerMetadataReference[]> DecompileAndGetRefsAsync(MethodDef method) {
			await DecompileAsync(method).ConfigureAwait(false);
			return await CreateCompilerMetadataReferencesAsync(method, assemblyReferenceResolver, languageCompiler.RequiredAssemblyReferences, decompileCodeState.CancellationToken).ConfigureAwait(false);
		}

		Task DecompileAsync(MethodDef method) {
			Debug.Assert(decompileCodeState == null);
			if (decompileCodeState != null)
				throw new InvalidOperationException();
			var state = new DecompileCodeState();
			decompileCodeState = state;

			return Task.Run(() => {
				state.CancellationToken.ThrowIfCancellationRequested();

				var type = method.DeclaringType;
				while (type.DeclaringType != null)
					type = type.DeclaringType;

				DecompileTypeMethods options;

				options = new DecompileTypeMethods(state.MainOutput, state.DecompilationContext, type);
				options.Methods.Add(method);
				options.DecompileHidden = false;
				options.MakeEverythingPublic = makeEverythingPublic;
				language.Decompile(DecompilationType.TypeMethods, options);

				state.CancellationToken.ThrowIfCancellationRequested();

				options = new DecompileTypeMethods(state.HiddenOutput, state.DecompilationContext, type);
				options.Methods.Add(method);
				options.DecompileHidden = true;
				options.MakeEverythingPublic = makeEverythingPublic;
				language.Decompile(DecompilationType.TypeMethods, options);

			}, state.CancellationToken);
		}

		public void CompileCode() {
			if (!CanCompile)
				return;
			CanCompile = false;

			StartCompileAsync().ContinueWith(t => {
				var ex = t.Exception;
				Debug.Assert(ex == null);
			}, CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.FromCurrentSynchronizationContext());
		}

		static Task<CompilerMetadataReference[]> CreateCompilerMetadataReferencesAsync(MethodDef method, AssemblyReferenceResolver assemblyReferenceResolver, IEnumerable<string> extraAssemblyReferences, CancellationToken cancellationToken) {
			cancellationToken.ThrowIfCancellationRequested();

			var modules = new HashSet<ModuleDef>(new MetadataReferenceFinder(method.Module, cancellationToken).Find(extraAssemblyReferences));

			var mdRefs = new List<CompilerMetadataReference>();
			foreach (var module in modules) {
				cancellationToken.ThrowIfCancellationRequested();

				CompilerMetadataReference? cmr;
				if (module.IsManifestModule)
					cmr = assemblyReferenceResolver.Create(module.Assembly);
				else
					cmr = assemblyReferenceResolver.Create(module);
				if (cmr == null)
					continue;

				mdRefs.Add(cmr.Value);
			}

			return Task.FromResult(mdRefs.ToArray());
		}

		async Task StartCompileAsync() {
			Result = null;
			SetDiagnostics(Array.Empty<CompilerDiagnostic>());

			bool canceled = false;
			Exception caughtException = null;
			CompilationResult? result = null;
			try {
				result = await CompileAsync();
			}
			catch (OperationCanceledException) {
				canceled = true;
			}
			catch (Exception ex) {
				caughtException = ex;
			}

			ModuleImporter moduleImporterResult = null;
			var compilerDiagnostics = result?.Diagnostics ?? Array.Empty<CompilerDiagnostic>();
			if (canceled) {
				// It gets canceled when the dialog box gets closed, or when Roslyn cancels the task
				// for some unknown reason.
				compilerDiagnostics = new CompilerDiagnostic[] {
					new CompilerDiagnostic(CompilerDiagnosticSeverity.Error, "The task was canceled", "DSWTF!", null, null),
				};
			}
			else if (caughtException != null) {
				compilerDiagnostics = new CompilerDiagnostic[] { ToCompilerDiagnostic(caughtException) };
			}
			else if (result?.Success == true) {
				try {
					moduleImporterResult = new ModuleImporter(methodToEdit.Module);
					moduleImporterResult.Import(result.Value.RawFile, result.Value.DebugFile, methodToEdit);
					compilerDiagnostics = moduleImporterResult.Diagnostics;
					if (compilerDiagnostics.Any(a => a.Severity == CompilerDiagnosticSeverity.Error))
						moduleImporterResult = null;
				}
				catch (ModuleImporterAbortedException) {
					compilerDiagnostics = moduleImporterResult.Diagnostics;
					Debug.Assert(compilerDiagnostics.Length != 0);
					moduleImporterResult = null;
				}
				catch (Exception ex) {
					compilerDiagnostics = new CompilerDiagnostic[] { ToCompilerDiagnostic(ex) };
					moduleImporterResult = null;
				}
			}

			SetDiagnostics(compilerDiagnostics);

			compileCodeState?.Dispose();
			compileCodeState = null;
			CanCompile = true;

			if (moduleImporterResult != null) {
				Result = moduleImporterResult;
				CodeCompiled?.Invoke(this, EventArgs.Empty);
			}

			// The compile button sometimes doesn't get enabled again
			CommandManager.InvalidateRequerySuggested();
		}

		static CompilerDiagnostic ToCompilerDiagnostic(Exception ex) =>
			new CompilerDiagnostic(CompilerDiagnosticSeverity.Error, $"Exception: {ex.GetType()}: {ex.Message}", "DSBUG1", null, null);

		Task<CompilationResult> CompileAsync() {
			Debug.Assert(compileCodeState == null);
			if (compileCodeState != null)
				throw new InvalidOperationException();
			var state = new CompileCodeState();
			compileCodeState = state;

			return Task.Run(() => {
				state.CancellationToken.ThrowIfCancellationRequested();

				return languageCompiler.CompileAsync(state.CancellationToken);
			}, state.CancellationToken);
		}

		void SetDiagnostics(IEnumerable<CompilerDiagnostic> diags) {
			Diagnostics.Clear();
			Diagnostics.AddRange(diags.OrderBy(a => a, CompilerDiagnosticComparer.Instance).
				Where(a => a.Severity != CompilerDiagnosticSeverity.Hidden).
				Select(a => new CompilerDiagnosticVM(a, CreateImage(a))));
		}

		object CreateImage(CompilerDiagnostic diag) {
			var imageName = GetImageName(diag.Severity);
			if (imageName == null)
				return null;
			return imageManager.GetImage(new ImageReference(GetType().Assembly, imageName), null);
		}

		static string GetImageName(CompilerDiagnosticSeverity severity) {
			switch (severity) {
			case CompilerDiagnosticSeverity.Hidden:	return "StatusHidden";
			case CompilerDiagnosticSeverity.Info:	return "StatusInformation";
			case CompilerDiagnosticSeverity.Warning:return "StatusWarning";
			case CompilerDiagnosticSeverity.Error:	return "StatusError";
			default: Debug.Fail($"Unknown severity: {severity}"); return null;
			}
		}

		bool CanAddAssemblyReference => CanCompile;
		void AddAssemblyReference() {
			if (!CanAddAssemblyReference)
				return;
			var modules = openAssembly.OpenMany().Select(a => a.ModuleDef).Where(a => a != null).ToArray();
			if (modules.Length != 0)
				AddReferences(modules);
		}

		bool CanAddGacReference => CanCompile;
		void AddGacReference() {
			if (!CanAddGacReference)
				return;
			AddReferences(openFromGAC.OpenAssemblies(false, OwnerWindow));
		}

		void AddReferences(ModuleDef[] modules) {
			var mdRefs = new List<CompilerMetadataReference>();
			foreach (var module in modules) {
				CompilerMetadataReference? cmr;
				if (module.IsManifestModule)
					cmr = assemblyReferenceResolver.Create(module.Assembly);
				else
					cmr = assemblyReferenceResolver.Create(module);
				if (cmr == null)
					continue;

				mdRefs.Add(cmr.Value);
			}
			if (mdRefs.Count == 0)
				return;

			try {
				if (!languageCompiler.AddMetadataReferences(mdRefs.ToArray()))
					MsgBox.Instance.Show(dnSpy_AsmEditor_Resources.Error_CouldNotAddAssemblyReferences);
			}
			catch (Exception ex) {
				MsgBox.Instance.Show(ex);
			}
		}

		public void Dispose() {
			decompileCodeState?.CancelAndDispose();
			compileCodeState?.CancelAndDispose();
			languageCompiler.Dispose();
		}
	}
}
