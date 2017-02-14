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
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using dnlib.DotNet;
using dnSpy.AsmEditor.Properties;
using dnSpy.AsmEditor.ViewHelpers;
using dnSpy.Contracts.App;
using dnSpy.Contracts.AsmEditor.Compiler;
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.MVVM;
using dnSpy.Contracts.Text.Editor;
using dnSpy.Contracts.Utilities;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.AsmEditor.Compiler {
	abstract class EditCodeVM : ViewModelBase, IDisposable {
		readonly IOpenFromGAC openFromGAC;
		readonly IOpenAssembly openAssembly;
		readonly ILanguageCompiler languageCompiler;
		protected readonly IDecompiler decompiler;
		readonly AssemblyReferenceResolver assemblyReferenceResolver;

		internal MetroWindow OwnerWindow { get; set; }

		// Make all types, methods, fields public so we don't get a compilation error when trying
		// to reference an internal type or a private method in the original assembly.
		internal const bool makeEverythingPublic = true;

		protected const string MAIN_CODE_NAME = "main";
		protected const string MAIN_G_CODE_NAME = "main.g";

		public ModuleImporter Result { get; set; }
		public event EventHandler CodeCompiled;
		public bool HasDecompiled { get; private set; }
		public ICommand CompileCommand => new RelayCommand(a => CompileCode(), a => CanCompile);
		public ICommand AddAssemblyReferenceCommand => new RelayCommand(a => AddAssemblyReference(), a => CanAddAssemblyReference);
		public ICommand AddGacReferenceCommand => new RelayCommand(a => AddGacReference(), a => CanAddGacReference);
		public ICommand GoToNextDiagnosticCommand => new RelayCommand(a => GoToNextDiagnostic(), a => CanGoToNextDiagnostic);
		public ICommand GoToPreviousDiagnosticCommand => new RelayCommand(a => GoToPreviousDiagnostic(), a => CanGoToPreviousDiagnostic);

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

		public sealed class CodeDocument {
			public string Name => codeDocument.Name;
			public string NameNoExtension => codeDocument.NameNoExtension;
			public IDsWpfTextView TextView => codeDocument.TextView;
			public IDsWpfTextViewHost TextViewHost => codeDocument.TextViewHost;

			readonly ICodeDocument codeDocument;
			SnapshotPoint initialPosition;

			public CodeDocument(ICodeDocument codeDocument) {
				this.codeDocument = codeDocument;
				codeDocument.TextView.VisualElement.SizeChanged += VisualElement_SizeChanged;
			}

			void VisualElement_SizeChanged(object sender, SizeChangedEventArgs e) {
				if (e.NewSize.Height == 0)
					return;
				codeDocument.TextView.VisualElement.SizeChanged -= VisualElement_SizeChanged;

				Debug.Assert(initialPosition.Snapshot != null);
				if (initialPosition.Snapshot == null)
					return;
				codeDocument.TextView.Caret.MoveTo(initialPosition.TranslateTo(codeDocument.TextView.TextSnapshot, PointTrackingMode.Negative));
				codeDocument.TextView.EnsureCaretVisible(true);
			}

			public void Initialize(SnapshotPoint initialPosition) {
				this.initialPosition = initialPosition;
				codeDocument.TextView.Selection.Clear();
			}

			public void Dispose() => codeDocument.TextView.VisualElement.SizeChanged -= VisualElement_SizeChanged;
		}

		public ObservableCollection<CodeDocument> Documents { get; } = new ObservableCollection<CodeDocument>();
		public CodeDocument SelectedDocument {
			get { return selectedDocument; }
			set {
				if (selectedDocument != value) {
					selectedDocument = value;
					OnPropertyChanged(nameof(SelectedDocument));
				}
			}
		}
		CodeDocument selectedDocument;

		public ObservableCollection<CompilerDiagnosticVM> Diagnostics { get; } = new ObservableCollection<CompilerDiagnosticVM>();

		public CompilerDiagnosticVM SelectedCompilerDiagnosticVM {
			get { return selectedCompilerDiagnosticVM; }
			set {
				if (selectedCompilerDiagnosticVM != value) {
					selectedCompilerDiagnosticVM = value;
					OnPropertyChanged(nameof(SelectedCompilerDiagnosticVM));
				}
			}
		}
		CompilerDiagnosticVM selectedCompilerDiagnosticVM;

		protected readonly ModuleDef sourceModule;

		protected EditCodeVM(IRawModuleBytesProvider rawModuleBytesProvider, IOpenFromGAC openFromGAC, IOpenAssembly openAssembly, ILanguageCompiler languageCompiler, IDecompiler decompiler, ModuleDef sourceModule) {
			Debug.Assert(decompiler.CanDecompile(DecompilationType.TypeMethods));
			this.openFromGAC = openFromGAC;
			this.openAssembly = openAssembly;
			this.languageCompiler = languageCompiler;
			this.decompiler = decompiler;
			this.sourceModule = sourceModule;
			assemblyReferenceResolver = new AssemblyReferenceResolver(rawModuleBytesProvider, sourceModule.Context.AssemblyResolver, sourceModule, makeEverythingPublic);
		}

		protected abstract class AsyncStateBase : IDisposable {
			readonly CancellationTokenSource cancellationTokenSource;
			public CancellationToken CancellationToken { get; }
			bool disposed;

			protected AsyncStateBase() {
				cancellationTokenSource = new CancellationTokenSource();
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

		protected sealed class ReferenceDecompilerOutput : StringBuilderDecompilerOutput, IDecompilerOutput {
			readonly object reference;
			public Span? Span => statementSpan ?? referenceSpan;
			Span? referenceSpan;
			Span? statementSpan;
			MethodSourceStatement? methodSourceStatement;

			bool IDecompilerOutput.UsesCustomData => true;

			public ReferenceDecompilerOutput(object reference, MethodSourceStatement? methodSourceStatement) {
				this.reference = reference;
				this.methodSourceStatement = methodSourceStatement;
			}

			public override void Write(string text, object reference, DecompilerReferenceFlags flags, object color) =>
				Write(text, 0, text.Length, reference, flags, color);

			public override void Write(string text, int index, int length, object reference, DecompilerReferenceFlags flags, object color) {
				if (reference == this.reference && (flags & DecompilerReferenceFlags.Definition) != 0 && referenceSpan == null) {
					int start = NextPosition;
					base.Write(text, index, length, reference, flags, color);
					referenceSpan = new Span(start, Length - start);
				}
				else
					base.Write(text, index, length, reference, flags, color);
			}

			void IDecompilerOutput.AddCustomData<TData>(string id, TData data) {
				if (id == PredefinedCustomDataIds.DebugInfo)
					AddDebugInfo(data as MethodDebugInfo);
			}

			void AddDebugInfo(MethodDebugInfo info) {
				if (info == null)
					return;
				if (methodSourceStatement == null)
					return;
				if (methodSourceStatement.Value.Method != info.Method)
					return;
				var stmt = info.GetSourceStatementByCodeOffset(methodSourceStatement.Value.Statement.BinSpan.Start);
				if (stmt == null)
					return;
				statementSpan = new Span(stmt.Value.TextSpan.Start, stmt.Value.TextSpan.Length);
			}
		}

		protected abstract class DecompileCodeState : AsyncStateBase {
			public DecompilationContext DecompilationContext { get; }
			protected DecompileCodeState() => DecompilationContext = new DecompilationContext {
				CancellationToken = CancellationToken,
			};
		}
		protected DecompileCodeState decompileCodeState;

		sealed class CompileCodeState : AsyncStateBase {
		}
		CompileCodeState compileCodeState;

		protected void StartDecompile() => StartDecompileAsync().ContinueWith(t => {
			var ex = t.Exception;
			Debug.Assert(ex == null);
		}, CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.FromCurrentSynchronizationContext());

		protected struct SimpleDocument {
			public string NameNoExtension { get; }
			public string Text { get; }
			public Span? CaretSpan { get; }
			public SimpleDocument(string nameNoExtension, string text, Span? caretSpan) {
				NameNoExtension = nameNoExtension;
				Text = text;
				CaretSpan = caretSpan;
			}
		}

		protected sealed class DecompileAsyncResult {
			public List<SimpleDocument> Documents { get; } = new List<SimpleDocument>();

			public void AddDocument(string nameNoExtension, string text, Span? caretSpan) =>
				Documents.Add(new SimpleDocument(nameNoExtension, text, caretSpan));
		}

		async Task StartDecompileAsync() {
			bool canCompile = false, canceled = false;
			var assemblyReferences = Array.Empty<CompilerMetadataReference>();
			SimpleDocument[] simpleDocuments = Array.Empty<SimpleDocument>();
			try {
				var result = await DecompileAndGetRefsAsync();
				assemblyReferences = result.Value;
				simpleDocuments = result.Key.Documents.ToArray();
				canCompile = true;
			}
			catch (OperationCanceledException) {
				canceled = true;
			}
			catch (Exception ex) {
				simpleDocuments = new SimpleDocument[] {
					new SimpleDocument(MAIN_CODE_NAME, ex.ToString(), null)
				};
			}

			var codeDocs = Array.Empty<ICodeDocument>();
			if (!canceled) {
				// This helps a little to speed up the code
				ProfileOptimizationHelper.StartProfile("add-decompiled-code-" + decompiler.UniqueGuid.ToString());
				var docs = new List<IDecompiledDocument>();
				foreach (var simpleDoc in simpleDocuments)
					docs.Add(new DecompiledDocument(simpleDoc.Text, simpleDoc.NameNoExtension));
				codeDocs = languageCompiler.AddDecompiledCode(new DecompiledCodeResult(docs.ToArray(), assemblyReferences, assemblyReferenceResolver, PlatformHelper.GetPlatform(sourceModule)));
			}

			decompileCodeState?.Dispose();
			decompileCodeState = null;

			foreach (var doc in codeDocs)
				doc.TextView.Properties.AddProperty(editCodeTextViewKey, this);
			Documents.AddRange(codeDocs.Select(a => new CodeDocument(a)));
			SelectedDocument = Documents.FirstOrDefault(a => a.NameNoExtension == MAIN_CODE_NAME) ?? Documents.FirstOrDefault();
			Debug.Assert(Documents.Count == simpleDocuments.Length);
			for (int i = 0; i < Documents.Count; i++) {
				var doc = Documents[i];
				var caretSpan = simpleDocuments[i].CaretSpan;
				if (caretSpan != null && caretSpan.Value.End <= doc.TextView.TextSnapshot.Length)
					doc.Initialize(new SnapshotPoint(doc.TextView.TextSnapshot, caretSpan.Value.Start));
				else
					doc.Initialize(new SnapshotPoint(doc.TextView.TextSnapshot, 0));
			}

			CanCompile = canCompile;
			HasDecompiled = true;
			OnPropertyChanged(nameof(HasDecompiled));
		}
		static readonly object editCodeTextViewKey = new object();

		internal static EditCodeVM TryGet(ITextView textView) {
			textView.Properties.TryGetProperty(editCodeTextViewKey, out EditCodeVM vm);
			return vm;
		}

		async Task<KeyValuePair<DecompileAsyncResult, CompilerMetadataReference[]>> DecompileAndGetRefsAsync() {
			var result = await DecompileAsync().ConfigureAwait(false);
			decompileCodeState.CancellationToken.ThrowIfCancellationRequested();
			var refs = await CreateCompilerMetadataReferencesAsync(languageCompiler.RequiredAssemblyReferences, decompileCodeState.CancellationToken).ConfigureAwait(false);
			return new KeyValuePair<DecompileAsyncResult, CompilerMetadataReference[]>(result, refs);
		}

		Task<DecompileAsyncResult> DecompileAsync() {
			decompileCodeState = CreateDecompileCodeState();
			return Task.Run(() => DecompileAsync(decompileCodeState), decompileCodeState.CancellationToken);
		}

		protected abstract DecompileCodeState CreateDecompileCodeState();
		protected abstract Task<DecompileAsyncResult> DecompileAsync(DecompileCodeState decompileCodeState);

		public void CompileCode() {
			if (!CanCompile)
				return;
			CanCompile = false;

			ProfileOptimizationHelper.StartProfile("compile-" + decompiler.UniqueGuid.ToString());
			StartCompileAsync().ContinueWith(t => {
				var ex = t.Exception;
				Debug.Assert(ex == null);
			}, CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.FromCurrentSynchronizationContext());
		}

		Task<CompilerMetadataReference[]> CreateCompilerMetadataReferencesAsync(IEnumerable<string> extraAssemblyReferences, CancellationToken cancellationToken) {
			cancellationToken.ThrowIfCancellationRequested();

			var modules = new HashSet<ModuleDef>(new MetadataReferenceFinder(sourceModule, cancellationToken).Find(extraAssemblyReferences));

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

			ModuleImporter importer = null;
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
					importer = new ModuleImporter(sourceModule, makeEverythingPublic);
					Import(importer, result.Value);
					compilerDiagnostics = importer.Diagnostics;
					if (compilerDiagnostics.Any(a => a.Severity == CompilerDiagnosticSeverity.Error))
						importer = null;
				}
				catch (ModuleImporterAbortedException) {
					compilerDiagnostics = importer.Diagnostics;
					Debug.Assert(compilerDiagnostics.Length != 0);
					importer = null;
				}
				catch (Exception ex) {
					compilerDiagnostics = new CompilerDiagnostic[] { ToCompilerDiagnostic(ex) };
					importer = null;
				}
			}

			SetDiagnostics(compilerDiagnostics);

			compileCodeState?.Dispose();
			compileCodeState = null;
			CanCompile = true;

			if (importer != null) {
				Result = importer;
				CodeCompiled?.Invoke(this, EventArgs.Empty);
			}

			// The compile button sometimes doesn't get enabled again
			CommandManager.InvalidateRequerySuggested();
		}

		protected abstract void Import(ModuleImporter importer, CompilationResult result);

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
				Select(a => new CompilerDiagnosticVM(a, GetImageReference(a.Severity) ?? default(ImageReference))));
			SelectedCompilerDiagnosticVM = Diagnostics.FirstOrDefault();
		}

		static ImageReference? GetImageReference(CompilerDiagnosticSeverity severity) {
			switch (severity) {
			case CompilerDiagnosticSeverity.Hidden:	return DsImages.StatusHidden;
			case CompilerDiagnosticSeverity.Info:	return DsImages.StatusInformation;
			case CompilerDiagnosticSeverity.Warning:return DsImages.StatusWarning;
			case CompilerDiagnosticSeverity.Error:	return DsImages.StatusError;
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

		internal void MoveTo(CompilerDiagnosticVM diag) {
			if (string.IsNullOrEmpty(diag.FullPath))
				return;

			var doc = Documents.FirstOrDefault(a => a.Name == diag.FullPath);
			Debug.Assert(doc != null);
			if (doc == null)
				return;
			SelectedDocument = doc;

			if (diag.LineLocationSpan != null) {
				UIUtilities.Focus(doc.TextView.VisualElement, () => {
					// The caret isn't always moved unless we wait a little
					doc.TextView.VisualElement.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => {
						if (doc == SelectedDocument) {
							MoveCaretTo(doc.TextView, diag.LineLocationSpan.Value.StartLinePosition.Line, diag.LineLocationSpan.Value.StartLinePosition.Character);
							doc.TextView.EnsureCaretVisible();
							doc.TextView.Selection.Clear();
						}
					}));
				});
			}
		}

		static CaretPosition MoveCaretTo(ITextView textView, int line, int column) {
			if (line >= textView.TextSnapshot.LineCount)
				line = textView.TextSnapshot.LineCount - 1;
			var snapshotLine = textView.TextSnapshot.GetLineFromLineNumber(line);
			if (column >= snapshotLine.Length)
				column = snapshotLine.Length;
			return textView.Caret.MoveTo(snapshotLine.Start + column);
		}

		bool CanGoToNextDiagnostic => Diagnostics.Count > 0;
		bool CanGoToPreviousDiagnostic => Diagnostics.Count > 0;

		void GoToNextDiagnostic() {
			if (!CanGoToNextDiagnostic)
				return;
			GoToDiagnostic(1);
		}

		void GoToPreviousDiagnostic() {
			if (!CanGoToPreviousDiagnostic)
				return;
			GoToDiagnostic(-1);
		}

		void GoToDiagnostic(int offset) {
			var item = SelectedCompilerDiagnosticVM ?? Diagnostics.FirstOrDefault();
			if (item == null)
				return;
			int index = Diagnostics.IndexOf(item);
			Debug.Assert(index >= 0);
			if (index < 0)
				return;
			index = (index + offset) % Diagnostics.Count;
			if (index < 0)
				index += Diagnostics.Count;
			var diag = Diagnostics[index];
			SelectedCompilerDiagnosticVM = diag;
			MoveTo(diag);
		}

		public void Dispose() {
			foreach (var doc in Documents)
				doc.Dispose();
			decompileCodeState?.CancelAndDispose();
			compileCodeState?.CancelAndDispose();
			languageCompiler.Dispose();

			// Needed unless we want the memory usage to be 1GB+ after some number of edits.
			// The GC doesn't kick in until it's too late.
			GC.Collect();
			GC.WaitForPendingFinalizers();
		}
	}
}
