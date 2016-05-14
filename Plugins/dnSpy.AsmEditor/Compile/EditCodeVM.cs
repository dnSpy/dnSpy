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
using System.Windows.Input;
using dnlib.DotNet;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Languages;
using dnSpy.Decompiler.Shared;
using dnSpy.Shared.MVVM;

namespace dnSpy.AsmEditor.Compile {
	sealed class EditCodeVM : ViewModelBase, IDisposable {
		readonly IImageManager imageManager;
		readonly ILanguageCompiler languageCompiler;
		readonly ILanguage language;
		readonly AssemblyReferenceResolver assemblyReferenceResolver;

		// Make all types, methods, fields public so we don't get a compilation error when trying
		// to reference an internal type or a private method in the original assembly.
		const bool makeEverythingPublic = true;

		public bool HasDecompiled { get; private set; }
		public ICommand CompileCommand => new RelayCommand(a => CompileCode(), a => CanCompile);

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

		public ObservableCollection<CompilerDiagnosticVM> Diagnostics { get; } = new ObservableCollection<CompilerDiagnosticVM>();

		public EditCodeVM(IImageManager imageManager, ILanguageCompiler languageCompiler, ILanguage language, MethodDef method) {
			Debug.Assert(language.CanDecompile(DecompilationType.TypeMethods));
			this.imageManager = imageManager;
			this.languageCompiler = languageCompiler;
			this.language = language;
			this.assemblyReferenceResolver = new AssemblyReferenceResolver(method.Module.Context.AssemblyResolver, method.Module, makeEverythingPublic);
			StartDecompileAsync(method).ContinueWith(t => {
				var ex = t.Exception;
				Debug.Assert(ex == null);
			}, CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.FromCurrentSynchronizationContext());
		}

		abstract class AsyncStateBase : IDisposable {
			readonly CancellationTokenSource cancellationTokenSource;
			public CancellationToken CancellationToken => cancellationTokenSource.Token;

			protected AsyncStateBase() {
				this.cancellationTokenSource = new CancellationTokenSource();
			}

			public void CancelAndDispose() {
				Cancel();
				Dispose();
			}

			void Cancel() => cancellationTokenSource.Cancel();

			public void Dispose() {
				if (!cancellationTokenSource.IsCancellationRequested)
					cancellationTokenSource.Dispose();
			}
		}

		sealed class DecompileCodeState : AsyncStateBase {
			public PlainTextOutput MainOutput { get; }
			public PlainTextOutput HiddenOutput { get; }
			public DecompilationContext DecompilationContext { get; }

			public DecompileCodeState() {
				DecompilationContext = new DecompilationContext {
					CancellationToken = CancellationToken,
				};
				MainOutput = new PlainTextOutput();
				HiddenOutput = new PlainTextOutput();
			}
		}
		DecompileCodeState decompileCodeState;

		sealed class CompileCodeState : AsyncStateBase {
		}
		CompileCodeState compileCodeState;

		async Task StartDecompileAsync(MethodDef method) {
			bool canceled = false;
			Exception caughtException = null;
			bool canCompile = false;
			var assemblyReferences = Array.Empty<CompilerMetadataReference>();
			try {
				await DecompileAsync(method);
				assemblyReferences = await CreateCompilerMetadataReferencesAsync(method, assemblyReferenceResolver, decompileCodeState.CancellationToken);
				canCompile = true;
			}
			catch (OperationCanceledException) {
				canceled = true;
			}
			catch (Exception ex) {
				caughtException = ex;
			}

			string mainCode, hiddenCode;
			if (canceled) {
				// This will never be shown to the user so there's no need to translate the string.
				mainCode = "Canceled!";
				hiddenCode = string.Empty;
			}
			else if (caughtException != null) {
				mainCode = caughtException.ToString();
				hiddenCode = string.Empty;
			}
			else {
				mainCode = decompileCodeState.MainOutput.ToString();
				hiddenCode = decompileCodeState.HiddenOutput.ToString();
			}
			languageCompiler.AddDecompiledCode(mainCode, hiddenCode, assemblyReferences, assemblyReferenceResolver);

			decompileCodeState?.Dispose();
			decompileCodeState = null;

			ICodeDocument mainDocument;
			Documents.AddRange(languageCompiler.GetCodeDocuments(out mainDocument));
			SelectedDocument = mainDocument;

			CanCompile = canCompile;
			HasDecompiled = true;
			OnPropertyChanged(nameof(HasDecompiled));
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

				options = new DecompileTypeMethods(state.HiddenOutput, state.DecompilationContext, type);
				options.Methods.Add(method);
				options.DecompileHidden = true;
				options.MakeEverythingPublic = makeEverythingPublic;
				language.Decompile(DecompilationType.TypeMethods, options);

			}, state.CancellationToken);
		}

		void CompileCode() {
			Debug.Assert(CanCompile);
			if (!CanCompile)
				return;
			CanCompile = false;

			StartCompileAsync().ContinueWith(t => {
				var ex = t.Exception;
				Debug.Assert(ex == null);
			}, CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.FromCurrentSynchronizationContext());
		}

		static Task<CompilerMetadataReference[]> CreateCompilerMetadataReferencesAsync(MethodDef method, AssemblyReferenceResolver assemblyReferenceResolver, CancellationToken cancellationToken) {
			cancellationToken.ThrowIfCancellationRequested();

			var modules = new HashSet<ModuleDef>(new MetadataReferenceFinder(method.Module, cancellationToken).Find());

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

			var compilerDiagnostics = result?.Diagnostics ?? Array.Empty<CompilerDiagnostic>();
			if (canceled) {
				// It gets canceled when the dialog box gets closed
			}
			else if (caughtException != null) {
				compilerDiagnostics = new CompilerDiagnostic[] {
					new CompilerDiagnostic(CompilerDiagnosticSeverity.Error, $"Exception: {caughtException.GetType()}: {caughtException.Message}", "DS1BUG", null, null),
				};
			}

			SetDiagnostics(compilerDiagnostics);

			compileCodeState?.Dispose();
			compileCodeState = null;
			CanCompile = true;
		}

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
			Diagnostics.AddRange(diags.OrderBy(a => a, CompilerDiagnosticComparer.Instance).Select(a => new CompilerDiagnosticVM(a, CreateImage(a))));
		}

		object CreateImage(CompilerDiagnostic diag) {
			var imageName = GetImageName(diag.Severity);
			if (imageName == null)
				return null;
			return imageManager.GetImage(GetType().Assembly, imageName, BackgroundType.GridViewItem);
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

		public void Dispose() {
			decompileCodeState?.CancelAndDispose();
			compileCodeState?.CancelAndDispose();
			languageCompiler.Dispose();
		}
	}
}
