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
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.MVVM;
using dnSpy.Contracts.Text.Editor;
using dnSpy.Contracts.Utilities;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.AsmEditor.Compiler {
	sealed class EditCodeVM : ViewModelBase, IDisposable {
		readonly IOpenFromGAC openFromGAC;
		readonly IOpenAssembly openAssembly;
		readonly ILanguageCompiler languageCompiler;
		readonly IDecompiler decompiler;
		readonly AssemblyReferenceResolver assemblyReferenceResolver;

		internal MetroWindow OwnerWindow { get; set; }

		// Make all types, methods, fields public so we don't get a compilation error when trying
		// to reference an internal type or a private method in the original assembly.
		const bool makeEverythingPublic = true;

		public ModuleImporter Result { get; set; }
		public event EventHandler CodeCompiled;
		public bool HasDecompiled { get; private set; }
		public ICommand CompileCommand => new RelayCommand(a => CompileCode(), a => CanCompile);
		public ICommand AddAssemblyReferenceCommand => new RelayCommand(a => AddAssemblyReference(), a => CanAddAssemblyReference);
		public ICommand AddGacReferenceCommand => new RelayCommand(a => AddGacReference(), a => CanAddGacReference);

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

		readonly MethodDef methodToEdit;

		public ObservableCollection<CompilerDiagnosticVM> Diagnostics { get; } = new ObservableCollection<CompilerDiagnosticVM>();

		public EditCodeVM(IOpenFromGAC openFromGAC, IOpenAssembly openAssembly, ILanguageCompiler languageCompiler, IDecompiler decompiler, MethodDef methodToEdit, IList<MethodSourceStatement> statementsInMethodToEdit) {
			Debug.Assert(decompiler.CanDecompile(DecompilationType.TypeMethods));
			this.openFromGAC = openFromGAC;
			this.openAssembly = openAssembly;
			this.languageCompiler = languageCompiler;
			this.decompiler = decompiler;
			this.methodToEdit = methodToEdit;
			var methodSourceStatement = statementsInMethodToEdit.Count == 0 ? (MethodSourceStatement?)null : statementsInMethodToEdit[0];
			this.assemblyReferenceResolver = new AssemblyReferenceResolver(methodToEdit.Module.Context.AssemblyResolver, methodToEdit.Module, makeEverythingPublic);
			StartDecompileAsync(methodToEdit, methodSourceStatement).ContinueWith(t => {
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

		sealed class MyDecompilerOutput : StringBuilderDecompilerOutput, IDecompilerOutput {
			readonly object reference;
			public Span? Span => statementSpan ?? referenceSpan;
			Span? referenceSpan;
			Span? statementSpan;
			MethodSourceStatement? methodSourceStatement;

			bool IDecompilerOutput.UsesCustomData => true;

			public MyDecompilerOutput(object reference, MethodSourceStatement? methodSourceStatement) {
				this.reference = reference;
				this.methodSourceStatement = methodSourceStatement;
			}

			public override void Write(string text, object reference, DecompilerReferenceFlags flags, object color) {
				if (reference == this.reference && (flags & DecompilerReferenceFlags.Definition) != 0 && referenceSpan == null) {
					int start = NextPosition;
					base.Write(text, reference, flags, color);
					referenceSpan = new Span(start, Length - start);
				}
				else
					base.Write(text, reference, flags, color);
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
				if (methodSourceStatement.Value.Method != reference)
					return;
				var stmt = info.GetSourceStatementByCodeOffset(methodSourceStatement.Value.Statement.BinSpan.Start);
				if (stmt == null)
					return;
				statementSpan = new Span(stmt.Value.TextSpan.Start, stmt.Value.TextSpan.Length);
			}
		}

		sealed class DecompileCodeState : AsyncStateBase {
			public MyDecompilerOutput MainOutput { get; }
			public StringBuilderDecompilerOutput HiddenOutput { get; }
			public DecompilationContext DecompilationContext { get; }

			public DecompileCodeState(object referenceToEdit, MethodSourceStatement? methodSourceStatement) {
				DecompilationContext = new DecompilationContext {
					CancellationToken = CancellationToken,
				};
				MainOutput = new MyDecompilerOutput(referenceToEdit, methodSourceStatement);
				HiddenOutput = new StringBuilderDecompilerOutput();
			}
		}
		DecompileCodeState decompileCodeState;

		sealed class CompileCodeState : AsyncStateBase {
		}
		CompileCodeState compileCodeState;

		async Task StartDecompileAsync(MethodDef method, MethodSourceStatement? methodSourceStatement) {
			bool canCompile = false, canceled = false;
			var assemblyReferences = Array.Empty<CompilerMetadataReference>();
			string mainCode, hiddenCode;
			var refSpan = new Span(0, 0);
			try {
				assemblyReferences = await DecompileAndGetRefsAsync(method, methodSourceStatement);
				mainCode = decompileCodeState.MainOutput.ToString();
				hiddenCode = decompileCodeState.HiddenOutput.ToString();
				canCompile = true;
				var span = decompileCodeState.MainOutput.Span;
				if (span != null)
					refSpan = span.Value;
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
				// This helps a little to speed up the code
				ProfileOptimizationHelper.StartProfile("add-decompiled-code-" + decompiler.UniqueGuid.ToString());
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
			Documents.AddRange(codeDocs.Select(a => new CodeDocument(a)));
			SelectedDocument = Documents.FirstOrDefault(a => a.NameNoExtension == MAIN_CODE_NAME) ?? Documents.FirstOrDefault();
			foreach (var doc in Documents) {
				if (doc.NameNoExtension == MAIN_CODE_NAME && refSpan.End <= doc.TextView.TextSnapshot.Length)
					doc.Initialize(new SnapshotPoint(doc.TextView.TextSnapshot, refSpan.Start));
				else
					doc.Initialize(new SnapshotPoint(doc.TextView.TextSnapshot, 0));
			}

			CanCompile = canCompile;
			HasDecompiled = true;
			OnPropertyChanged(nameof(HasDecompiled));
		}
		static readonly object editCodeTextViewKey = new object();

		internal static EditCodeVM TryGet(ITextView textView) {
			EditCodeVM vm;
			textView.Properties.TryGetProperty(editCodeTextViewKey, out vm);
			return vm;
		}

		async Task<CompilerMetadataReference[]> DecompileAndGetRefsAsync(MethodDef method, MethodSourceStatement? methodSourceStatement) {
			await DecompileAsync(method, methodSourceStatement).ConfigureAwait(false);
			return await CreateCompilerMetadataReferencesAsync(method, assemblyReferenceResolver, languageCompiler.RequiredAssemblyReferences, decompileCodeState.CancellationToken).ConfigureAwait(false);
		}

		Task DecompileAsync(MethodDef method, MethodSourceStatement? methodSourceStatement) {
			Debug.Assert(decompileCodeState == null);
			if (decompileCodeState != null)
				throw new InvalidOperationException();
			var state = new DecompileCodeState(method, methodSourceStatement);
			decompileCodeState = state;

			return Task.Run(() => {
				state.CancellationToken.ThrowIfCancellationRequested();

				var type = method.DeclaringType;
				while (type.DeclaringType != null)
					type = type.DeclaringType;

				DecompileTypeMethods options;

				state.DecompilationContext.CalculateBinSpans = true;
				options = new DecompileTypeMethods(state.MainOutput, state.DecompilationContext, type);
				options.Methods.Add(method);
				options.DecompileHidden = false;
				options.MakeEverythingPublic = makeEverythingPublic;
				decompiler.Decompile(DecompilationType.TypeMethods, options);

				state.CancellationToken.ThrowIfCancellationRequested();

				state.DecompilationContext.CalculateBinSpans = false;
				options = new DecompileTypeMethods(state.HiddenOutput, state.DecompilationContext, type);
				options.Methods.Add(method);
				options.DecompileHidden = true;
				options.MakeEverythingPublic = makeEverythingPublic;
				decompiler.Decompile(DecompilationType.TypeMethods, options);

			}, state.CancellationToken);
		}

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
				Select(a => new CompilerDiagnosticVM(a, GetImageReference(a.Severity) ?? default(ImageReference))));
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
