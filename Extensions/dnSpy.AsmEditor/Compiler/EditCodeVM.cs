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
using System.Diagnostics;
using System.IO;
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
using dnSpy.Contracts.ETW;
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
		readonly IPickFilename pickFilename;
		readonly ILanguageCompiler languageCompiler;
		protected readonly IDecompiler decompiler;
		readonly AssemblyReferenceResolver assemblyReferenceResolver;
		readonly HashSet<string> currentReferences;

		internal MetroWindow OwnerWindow { get; set; }

		protected string MainCodeName => "main" + languageCompiler.FileExtension;
		protected string MainGeneratedCodeName => "main.g" + languageCompiler.FileExtension;

		public ModuleImporter Result { get; set; }
		public event EventHandler CodeCompiled;
		public bool HasDecompiled { get; private set; }
		public ICommand CompileCommand => new RelayCommand(a => CompileCode(), a => CanCompile);
		public ICommand AddAssemblyReferenceCommand => new RelayCommand(a => AddAssemblyReference(), a => CanAddAssemblyReference);
		public ICommand AddGacReferenceCommand => new RelayCommand(a => AddGacReference(), a => CanAddGacReference);
		public ICommand AddDocumentsCommand => new RelayCommand(a => AddDocuments(), a => CanAddDocuments);
		public ImageReference AddDocumentsImage { get; }
		public ICommand GoToNextDiagnosticCommand => new RelayCommand(a => GoToNextDiagnostic(), a => CanGoToNextDiagnostic);
		public ICommand GoToPreviousDiagnosticCommand => new RelayCommand(a => GoToPreviousDiagnostic(), a => CanGoToPreviousDiagnostic);

		public string AddAssemblyReferenceToolTip => ToolTipHelper.AddKeyboardShortcut(dnSpy_AsmEditor_Resources.AddAssemblyReferenceToolTip, dnSpy_AsmEditor_Resources.ShortCutKeyCtrlO);
		public string AddGacReferenceToolTip => ToolTipHelper.AddKeyboardShortcut(dnSpy_AsmEditor_Resources.AddGacReferenceToolTip, dnSpy_AsmEditor_Resources.ShortCutKeyCtrlShiftO);
		public string AddDocumentsToolTip => ToolTipHelper.AddKeyboardShortcut(dnSpy_AsmEditor_Resources.AddDocumentsToolTip, dnSpy_AsmEditor_Resources.ShortCutKeyCtrlShiftA);

		public bool CanCompile {
			get => canCompile;
			set {
				if (canCompile != value) {
					canCompile = value;
					OnPropertyChanged(nameof(CanCompile));
				}
			}
		}
		bool canCompile;

		public sealed class CodeDocument : ViewModelBase {
			public string Name => codeDocument.Name;
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
			get => selectedDocument;
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
			get => selectedCompilerDiagnosticVM;
			set {
				if (selectedCompilerDiagnosticVM != value) {
					selectedCompilerDiagnosticVM = value;
					OnPropertyChanged(nameof(SelectedCompilerDiagnosticVM));
				}
			}
		}
		CompilerDiagnosticVM selectedCompilerDiagnosticVM;

		protected readonly ModuleDef sourceModule;
		readonly AssemblyNameInfo tempAssembly;

		protected EditCodeVM(EditCodeVMOptions options, TypeDef typeToEditOrNull) {
			Debug.Assert(options.Decompiler.CanDecompile(DecompilationType.TypeMethods));
			openFromGAC = options.OpenFromGAC;
			openAssembly = options.OpenAssembly;
			pickFilename = options.PickFilename;
			languageCompiler = options.LanguageCompiler;
			decompiler = options.Decompiler;
			sourceModule = options.SourceModule;
			AddDocumentsImage = options.AddDocumentsImage;
			currentReferences = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			if (typeToEditOrNull != null) {
				Debug.Assert(typeToEditOrNull.Module == sourceModule);
				while (typeToEditOrNull.DeclaringType != null)
					typeToEditOrNull = typeToEditOrNull.DeclaringType;
			}
			tempAssembly = new AssemblyNameInfo {
				HashAlgId = AssemblyHashAlgorithm.SHA1,
				Version = new Version(0, 0, 0, 0),
				Attributes = AssemblyAttributes.None,
				Name = Guid.NewGuid().ToString(),
				Culture = string.Empty,
			};
			if (!PublicKeyBase.IsNullOrEmpty2(sourceModule.Assembly?.PublicKeyOrToken)) {
				tempAssembly.PublicKeyOrToken = new PublicKey(publicKeyData);
				tempAssembly.Attributes |= AssemblyAttributes.PublicKey;
			}
			assemblyReferenceResolver = new AssemblyReferenceResolver(options.RawModuleBytesProvider, sourceModule.Context.AssemblyResolver, tempAssembly, sourceModule, typeToEditOrNull);
		}
		static readonly byte[] publicKeyData = new byte[] {
			0x00, 0x24, 0x00, 0x00, 0x04, 0x80, 0x00, 0x00, 0x94, 0x00, 0x00, 0x00, 0x06, 0x02, 0x00, 0x00,
			0x00, 0x24, 0x00, 0x00, 0x52, 0x53, 0x41, 0x31, 0x00, 0x04, 0x00, 0x00, 0x01, 0x00, 0x01, 0x00,
			0x9D, 0xE2, 0xB3, 0x41, 0x2F, 0xA8, 0x0C, 0x88, 0x40, 0x78, 0x7D, 0xBA, 0x09, 0xDB, 0x02, 0x5D,
			0xFA, 0x33, 0x08, 0xF2, 0xE6, 0x68, 0xC4, 0x87, 0x1C, 0x62, 0xF3, 0xCA, 0xDF, 0x74, 0xC5, 0x4B,
			0x05, 0x97, 0x73, 0xE1, 0x3B, 0x1B, 0x79, 0x52, 0x86, 0x89, 0x85, 0xD6, 0x58, 0xD3, 0xC6, 0x9E,
			0x35, 0x7E, 0xF5, 0x41, 0xF8, 0x1C, 0xE2, 0x18, 0x23, 0xB9, 0xDA, 0xDB, 0x32, 0x1F, 0xB2, 0xF3,
			0x27, 0x3C, 0xE5, 0x76, 0x4E, 0x49, 0x4C, 0x05, 0xD7, 0x91, 0xBA, 0x1E, 0x3F, 0x12, 0xCF, 0x99,
			0xFC, 0xA1, 0x55, 0x4A, 0x67, 0x4B, 0xB9, 0xD8, 0x4A, 0x77, 0x1D, 0x1E, 0x2A, 0x16, 0x89, 0x3B,
			0x55, 0x7C, 0x66, 0xCD, 0x00, 0x44, 0x5A, 0x7B, 0xB3, 0xB7, 0xAE, 0x4C, 0xC2, 0xBE, 0x4E, 0x1D,
			0x5F, 0x28, 0x48, 0x34, 0x5A, 0x63, 0xD3, 0xB3, 0xF7, 0xEA, 0xDD, 0x01, 0xF4, 0x60, 0xF4, 0xBF,
		};

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
				var stmt = info.GetSourceStatementByCodeOffset(methodSourceStatement.Value.Statement.ILSpan.Start);
				if (stmt == null)
					return;
				statementSpan = new Span(stmt.Value.TextSpan.Start, stmt.Value.TextSpan.Length);
			}
		}

		protected abstract class DecompileCodeState : AsyncStateBase {
			public DecompilationContext DecompilationContext { get; }
			protected DecompileCodeState() => DecompilationContext = new DecompilationContext {
				CancellationToken = CancellationToken,
				AsyncMethodBodyDecompilation = false,
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

		protected readonly struct SimpleDocument {
			public string Name { get; }
			public string Text { get; }
			public Span? CaretSpan { get; }
			public SimpleDocument(string name, string text, Span? caretSpan) {
				Name = name;
				Text = text;
				CaretSpan = caretSpan;
			}
		}

		protected sealed class DecompileAsyncResult {
			public List<SimpleDocument> Documents { get; } = new List<SimpleDocument>();

			public void AddDocument(string name, string text, Span? caretSpan) =>
				Documents.Add(new SimpleDocument(name, text, caretSpan));
		}

		async Task StartDecompileAsync() {
			bool canCompile = false, canceled = false;
			var assemblyReferences = Array.Empty<CompilerMetadataReference>();
			var simpleDocuments = Array.Empty<SimpleDocument>();
			try {
				var result = await DecompileAndGetRefsAsync();
				assemblyReferences = result.assemblyReferences;
				simpleDocuments = result.result.Documents.ToArray();
				canCompile = true;
			}
			catch (OperationCanceledException) {
				canceled = true;
			}
			catch (Exception ex) {
				simpleDocuments = new SimpleDocument[] {
					new SimpleDocument(MainCodeName, ex.ToString(), null)
				};
			}

			var codeDocs = Array.Empty<ICodeDocument>();
			if (!canceled) {
				// This helps a little to speed up the code
				ProfileOptimizationHelper.StartProfile("add-decompiled-code-" + decompiler.UniqueGuid.ToString());
				var docs = new List<CompilerDocumentInfo>();
				foreach (var simpleDoc in simpleDocuments)
					docs.Add(new CompilerDocumentInfo(simpleDoc.Text, simpleDoc.Name));
				var publicKeyData = (tempAssembly.PublicKeyOrToken as PublicKey)?.Data;
				languageCompiler.InitializeProject(new CompilerProjectInfo(tempAssembly.Name, publicKeyData, assemblyReferences, assemblyReferenceResolver, PlatformHelper.GetPlatform(sourceModule)));
				foreach (var ar in assemblyReferences) {
					if (!string.IsNullOrEmpty(ar.Filename))
						currentReferences.Add(ar.Filename);
				}
				codeDocs = languageCompiler.AddDocuments(docs.ToArray());
			}

			decompileCodeState?.Dispose();
			decompileCodeState = null;

			AddDocuments(codeDocs, initializeDocs: false);
			SelectedDocument = Documents.FirstOrDefault(a => a.Name == MainCodeName) ?? Documents.FirstOrDefault();
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

		void AddDocuments(ICodeDocument[] codeDocs, bool initializeDocs) {
			foreach (var doc in codeDocs)
				doc.TextView.Properties.AddProperty(editCodeTextViewKey, this);
			foreach (var codeDoc in codeDocs) {
				var doc = new CodeDocument(codeDoc);
				Documents.Add(doc);
				if (initializeDocs)
					doc.Initialize(new SnapshotPoint(doc.TextView.TextSnapshot, 0));
			}
		}

		static readonly object editCodeTextViewKey = new object();

		internal static EditCodeVM TryGet(ITextView textView) {
			textView.Properties.TryGetProperty(editCodeTextViewKey, out EditCodeVM vm);
			return vm;
		}

		async Task<(DecompileAsyncResult result, CompilerMetadataReference[] assemblyReferences)> DecompileAndGetRefsAsync() {
			var result = await DecompileAsync().ConfigureAwait(false);
			decompileCodeState.CancellationToken.ThrowIfCancellationRequested();
			var refs = await CreateCompilerMetadataReferencesAsync(languageCompiler.GetRequiredAssemblyReferences(sourceModule), decompileCodeState.CancellationToken).ConfigureAwait(false);
			return (result, refs);
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

			DnSpyEventSource.Log.CompileStart();
			ProfileOptimizationHelper.StartProfile("compile-" + decompiler.UniqueGuid.ToString());
			StartCompileAsync().ContinueWith(t => {
				DnSpyEventSource.Log.CompileStop();
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
					new CompilerDiagnostic(CompilerDiagnosticSeverity.Error, "The task was canceled", "DSWTF!", null, null, null),
				};
			}
			else if (caughtException != null) {
				compilerDiagnostics = new CompilerDiagnostic[] { ToCompilerDiagnostic(caughtException) };
			}
			else if (result?.Success == true) {
				ModuleImporterAssemblyResolver asmResolver = null;
				try {
					asmResolver = new ModuleImporterAssemblyResolver(assemblyReferenceResolver.GetReferences());
					importer = new ModuleImporter(sourceModule, asmResolver);
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
				finally {
					asmResolver?.Dispose();
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
			new CompilerDiagnostic(CompilerDiagnosticSeverity.Error, $"Exception: {ex.GetType()}: {ex.Message}", "DSBUG1", null, null, null);

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
				Select(a => new CompilerDiagnosticVM(a, GetImageReference(a.Severity) ?? default)));
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
				if (!string.IsNullOrEmpty(module.Location) && currentReferences.Contains(module.Location))
					continue;
				CompilerMetadataReference? cmr;
				if (module.IsManifestModule)
					cmr = assemblyReferenceResolver.Create(module.Assembly);
				else
					cmr = assemblyReferenceResolver.Create(module);
				if (cmr == null)
					continue;

				mdRefs.Add(cmr.Value);
				if (!string.IsNullOrEmpty(module.Location))
					currentReferences.Add(module.Location);
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

		bool CanAddDocuments => true;
		void AddDocuments() {
			var files = pickFilename.GetFilenames(null, null);
			if (files.Length == 0)
				return;
			try {
				var codeDocs = languageCompiler.AddDocuments(files.Select(a => new CompilerDocumentInfo(File.ReadAllText(a), Path.GetFileName(a))).ToArray());
				AddDocuments(codeDocs, initializeDocs: true);
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
			assemblyReferenceResolver.Dispose();
		}
	}
}
