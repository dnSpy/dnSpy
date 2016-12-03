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
using System.Threading;
using System.Threading.Tasks;
using dnSpy.Contracts.AsmEditor.Compiler;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Text.Editor;
using dnSpy.Contracts.Text.Editor.Operations;
using dnSpy.Roslyn.Shared.Documentation;
using dnSpy.Roslyn.Shared.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Roslyn.Shared.Compiler {
	abstract class RoslynLanguageCompilerProvider : ILanguageCompilerProvider {
		public double Order => 0;
		public abstract ImageReference? Icon { get; }
		public abstract Guid Language { get; }

		public abstract ILanguageCompiler Create(CompilationKind kind);

		public bool CanCompile(CompilationKind kind) {
			switch (kind) {
			case CompilationKind.Assembly:
			case CompilationKind.Method:
			case CompilationKind.AddClass:
			case CompilationKind.EditClass:
				return true;
			default:
				Debug.Fail($"Unknown kind: {kind}");
				return false;
			}
		}
	}

	abstract class RoslynLanguageCompiler : ILanguageCompiler {
		protected abstract string TextViewRole { get; }
		protected abstract string ContentType { get; }
		protected abstract string LanguageName { get; }
		protected abstract CompilationOptions CompilationOptions { get; }
		protected abstract ParseOptions ParseOptions { get; }
		protected abstract string FileExtension { get; }
		protected abstract string AppearanceCategory { get; }
		public abstract IEnumerable<string> RequiredAssemblyReferences { get; }

		readonly ICodeEditorProvider codeEditorProvider;
		readonly List<RoslynCodeDocument> documents;
		readonly IRoslynDocumentationProviderFactory docFactory;
		readonly IRoslynDocumentChangedService roslynDocumentChangedService;
		readonly ITextViewUndoManagerProvider textViewUndoManagerProvider;
		AdhocWorkspace workspace;

		protected RoslynLanguageCompiler(CompilationKind kind, ICodeEditorProvider codeEditorProvider, IRoslynDocumentationProviderFactory docFactory, IRoslynDocumentChangedService roslynDocumentChangedService, ITextViewUndoManagerProvider textViewUndoManagerProvider) {
			if (codeEditorProvider == null)
				throw new ArgumentNullException(nameof(codeEditorProvider));
			if (docFactory == null)
				throw new ArgumentNullException(nameof(docFactory));
			if (roslynDocumentChangedService == null)
				throw new ArgumentNullException(nameof(roslynDocumentChangedService));
			if (textViewUndoManagerProvider == null)
				throw new ArgumentNullException(nameof(textViewUndoManagerProvider));
			this.codeEditorProvider = codeEditorProvider;
			this.docFactory = docFactory;
			this.roslynDocumentChangedService = roslynDocumentChangedService;
			this.textViewUndoManagerProvider = textViewUndoManagerProvider;
			documents = new List<RoslynCodeDocument>();
		}

		public ICodeDocument[] AddDecompiledCode(IDecompiledCodeResult decompiledCodeResult) {
			Debug.Assert(workspace == null);

			workspace = new AdhocWorkspace(RoslynMefHostServices.DefaultServices);
			workspace.WorkspaceChanged += Workspace_WorkspaceChanged;
			var refs = decompiledCodeResult.AssemblyReferences.Select(a => a.CreateMetadataReference(docFactory)).ToArray();
			var projectId = ProjectId.CreateNewId();

			foreach (var doc in decompiledCodeResult.Documents)
				documents.Add(CreateDocument(projectId, doc));

			var projectInfo = ProjectInfo.Create(projectId, VersionStamp.Create(), "compilecodeproj", Guid.NewGuid().ToString(), LanguageName,
				compilationOptions: CompilationOptions
						.WithOptimizationLevel(OptimizationLevel.Release)
						.WithPlatform(GetPlatform(decompiledCodeResult.Platform))
						.WithAssemblyIdentityComparer(DesktopAssemblyIdentityComparer.Default),
				parseOptions: ParseOptions,
				documents: documents.Select(a => a.Info),
				metadataReferences: refs,
				isSubmission: false, hostObjectType: null);
			workspace.AddProject(projectInfo);
			foreach (var doc in documents)
				workspace.OpenDocument(doc.Info.Id);

			foreach (var doc in documents) {
				ITextViewUndoManager manager;
				if (textViewUndoManagerProvider.TryGetTextViewUndoManager(doc.TextView, out manager))
					manager.ClearUndoHistory();
			}

			return documents.ToArray();
		}

		void Workspace_WorkspaceChanged(object sender, WorkspaceChangeEventArgs e) {
			if (isDisposed)
				return;
			if (e.Kind != WorkspaceChangeKind.DocumentChanged)
				return;
			docChangedEventCount++;
			if (docChangedEventCount != documents.Count)
				return;

			workspace.WorkspaceChanged -= Workspace_WorkspaceChanged;
			foreach (var doc in documents)
				roslynDocumentChangedService.RaiseDocumentChanged(doc.TextView.TextSnapshot);
		}
		int docChangedEventCount;

		static Platform GetPlatform(TargetPlatform platform) {
			// AnyCpu32BitPreferred can only be used when creating executables (we create a dll)
			if (platform == TargetPlatform.AnyCpu32BitPreferred)
				return Platform.AnyCpu;
			return platform.ToPlatform();
		}

		RoslynCodeDocument CreateDocument(ProjectId projectId, IDecompiledDocument doc) {
			var options = new CodeEditorOptions();
			options.ContentTypeString = ContentType;
			options.Roles.Add(PredefinedDsTextViewRoles.RoslynCodeEditor);
			options.Roles.Add(TextViewRole);
			var codeEditor = codeEditorProvider.Create(options);
			codeEditor.TextView.Options.SetOptionValue(DefaultWpfViewOptions.AppearanceCategory, AppearanceCategory);
			codeEditor.TextView.Options.SetOptionValue(DefaultTextViewHostOptions.GlyphMarginId, true);

			var textBuffer = codeEditor.TextView.TextBuffer;
			textBuffer.Replace(new Span(0, textBuffer.CurrentSnapshot.Length), doc.Code);

			var documentInfo = DocumentInfo.Create(DocumentId.CreateNewId(projectId), doc.NameNoExtension + FileExtension, null, SourceCodeKind.Regular, TextLoader.From(codeEditor.TextBuffer.AsTextContainer(), VersionStamp.Create()));
			return new RoslynCodeDocument(codeEditor, documentInfo, doc.NameNoExtension);
		}

		public async Task<CompilationResult> CompileAsync(CancellationToken cancellationToken) {
			var project = workspace.CurrentSolution.Projects.First();
			Debug.Assert(project.SupportsCompilation);
			var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
			if (compilation == null)
				throw new InvalidOperationException("Project returned a null Compilation");
			var peStream = new MemoryStream();
			MemoryStream pdbStream = null;
			var emitOpts = new EmitOptions(debugInformationFormat: DebugInformationFormat.Pdb);
			if (emitOpts.DebugInformationFormat == DebugInformationFormat.Pdb || emitOpts.DebugInformationFormat == DebugInformationFormat.PortablePdb)
				pdbStream = new MemoryStream();
			var emitResult = compilation.Emit(peStream, pdbStream, options: emitOpts, cancellationToken: cancellationToken);
			var diagnostics = emitResult.Diagnostics.ToCompilerDiagnostics().ToArray();
			if (!emitResult.Success)
				return new CompilationResult(diagnostics);
			return new CompilationResult(peStream.ToArray(), new DebugFileResult(emitOpts.DebugInformationFormat.ToDebugFileFormat(), pdbStream?.ToArray()), diagnostics);
		}

		public bool AddMetadataReferences(CompilerMetadataReference[] metadataReferences) {
			Debug.Assert(workspace != null);
			if (workspace == null)
				throw new InvalidOperationException();
			var newProj = workspace.CurrentSolution.Projects.First().AddMetadataReferences(metadataReferences.Select(a => a.CreateMetadataReference(docFactory)));
			return workspace.TryApplyChanges(newProj.Solution);
		}

		public void Dispose() {
			if (isDisposed)
				return;
			isDisposed = true;
			if (workspace != null) {
				workspace.WorkspaceChanged -= Workspace_WorkspaceChanged;
				// This also closes all documents
				workspace.Dispose();
			}
			foreach (var doc in documents)
				doc.Dispose();
			documents.Clear();
		}
		bool isDisposed;
	}
}
