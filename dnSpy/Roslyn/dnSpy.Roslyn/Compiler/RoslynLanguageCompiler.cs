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
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using dnlib.DotNet;
using dnSpy.Contracts.AsmEditor.Compiler;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Text.Editor;
using dnSpy.Contracts.Text.Editor.Operations;
using dnSpy.Roslyn.Documentation;
using dnSpy.Roslyn.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Roslyn.Compiler {
	abstract class RoslynLanguageCompilerProvider : ILanguageCompilerProvider {
		public double Order => 0;
		public abstract ImageReference? Icon { get; }
		public abstract Guid Language { get; }

		public abstract ILanguageCompiler Create(CompilationKind kind);

		public bool CanCompile(CompilationKind kind) {
			switch (kind) {
			case CompilationKind.EditAssembly:
			case CompilationKind.EditMethod:
			case CompilationKind.AddClass:
			case CompilationKind.EditClass:
			case CompilationKind.AddMembers:
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
		protected abstract ParseOptions ParseOptions { get; }
		public abstract string FileExtension { get; }
		protected abstract string AppearanceCategory { get; }

		protected abstract bool SupportsNetModule { get; }

		readonly CompilationKind kind;
		readonly ICodeEditorProvider codeEditorProvider;
		readonly List<RoslynCodeDocument> documents;
		readonly IRoslynDocumentationProviderFactory docFactory;
		readonly IRoslynDocumentChangedService roslynDocumentChangedService;
		readonly ITextViewUndoManagerProvider textViewUndoManagerProvider;
		readonly ProjectId projectId;
		readonly HashSet<DocumentId> loadedDocuments;
		AdhocWorkspace workspace;

		protected RoslynLanguageCompiler(CompilationKind kind, ICodeEditorProvider codeEditorProvider, IRoslynDocumentationProviderFactory docFactory, IRoslynDocumentChangedService roslynDocumentChangedService, ITextViewUndoManagerProvider textViewUndoManagerProvider) {
			this.kind = kind;
			this.codeEditorProvider = codeEditorProvider ?? throw new ArgumentNullException(nameof(codeEditorProvider));
			this.docFactory = docFactory ?? throw new ArgumentNullException(nameof(docFactory));
			this.roslynDocumentChangedService = roslynDocumentChangedService ?? throw new ArgumentNullException(nameof(roslynDocumentChangedService));
			this.textViewUndoManagerProvider = textViewUndoManagerProvider ?? throw new ArgumentNullException(nameof(textViewUndoManagerProvider));
			documents = new List<RoslynCodeDocument>();
			projectId = ProjectId.CreateNewId();
			loadedDocuments = new HashSet<DocumentId>();
		}

		OutputKind GetDefaultOutputKind(CompilationKind kind) {
			if (!SupportsNetModule)
				return OutputKind.DynamicallyLinkedLibrary;

			switch (kind) {
			case CompilationKind.EditAssembly:
				// We can't use netmodule when editing assembly attributes since the compiler won't add an assembly for obvious reasons
				return OutputKind.DynamicallyLinkedLibrary;

			case CompilationKind.EditMethod:
			case CompilationKind.AddClass:
			case CompilationKind.EditClass:
			case CompilationKind.AddMembers:
				// Use a netmodule to prevent the compiler from adding assembly attributes. Sometimes the compiler must
				// add assembly attributes but the attributes have missing members and the compiler can't compile the code.
				//	error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.CompilationRelaxationsAttribute..ctor'
				//	error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.RuntimeCompatibilityAttribute..ctor'
				// If unsafe code is enabled, it will try to add even more attributes.
				return OutputKind.NetModule;

			default:
				Debug.Fail($"Unknown {nameof(CompilationKind)}: {kind}");
				goto case CompilationKind.EditMethod;
			}
		}

		protected abstract CompilationOptions CreateCompilationOptions(OutputKind outputKind);
		protected abstract CompilationOptions CreateCompilationOptionsNoAttributes(CompilationOptions compilationOptions);

		public abstract IEnumerable<string> GetRequiredAssemblyReferences(ModuleDef editedModule);

		public void InitializeProject(CompilerProjectInfo projectInfo) {
			Debug.Assert(workspace == null);

			workspace = new AdhocWorkspace(RoslynMefHostServices.DefaultServices);
			workspace.WorkspaceChanged += Workspace_WorkspaceChanged;
			var refs = projectInfo.AssemblyReferences.Select(a => a.CreateMetadataReference(docFactory)).ToArray();

			var compilationOptions = CreateCompilationOptions(GetDefaultOutputKind(kind))
				.WithPlatform(GetPlatform(projectInfo.Platform))
				.WithAssemblyIdentityComparer(DesktopAssemblyIdentityComparer.Default);
			if (projectInfo.PublicKey != null) {
				compilationOptions = compilationOptions
					.WithCryptoPublicKey(ImmutableArray.Create<byte>(projectInfo.PublicKey))
					.WithDelaySign(true);
			}
			var roslynProjInfo = ProjectInfo.Create(projectId, VersionStamp.Create(), "compilecodeproj", projectInfo.AssemblyName, LanguageName,
				compilationOptions: compilationOptions,
				parseOptions: ParseOptions,
				metadataReferences: refs,
				isSubmission: false, hostObjectType: null);
			workspace.AddProject(roslynProjInfo);
		}

		void Workspace_WorkspaceChanged(object sender, WorkspaceChangeEventArgs e) {
			if (isDisposed)
				return;
			if (e.Kind == WorkspaceChangeKind.DocumentChanged) {
				if (!loadedDocuments.Add(e.DocumentId))
					return;
				RefreshTextViews();
			}
			else if (e.Kind == WorkspaceChangeKind.ProjectChanged) {
				var oldProj = e.OldSolution.Projects.Single();
				var newProj = e.NewSolution.Projects.Single();
				if (CollectionEquals(oldProj.MetadataReferences, newProj.MetadataReferences))
					return;
				RefreshTextViews();
			}
		}

		static bool CollectionEquals<TElement>(IReadOnlyList<TElement> a, IReadOnlyList<TElement> b) where TElement : class {
			if (a == b)
				return true;
			if (a == null || b == null)
				return false;
			if (a.Count != b.Count)
				return false;
			for (int i = 0; i < a.Count; i++) {
				if (a[i] != b[i])
					return false;
			}
			return true;
		}

		void RefreshTextViews() {
			foreach (var doc in documents)
				roslynDocumentChangedService.RaiseDocumentChanged(doc.TextView.TextSnapshot);
		}

		static Platform GetPlatform(TargetPlatform platform) {
			// AnyCpu32BitPreferred can only be used when creating executables (we create a dll)
			if (platform == TargetPlatform.AnyCpu32BitPreferred)
				return Platform.AnyCpu;
			return platform.ToPlatform();
		}

		RoslynCodeDocument CreateDocument(ProjectId projectId, CompilerDocumentInfo doc) {
			var options = new CodeEditorOptions();
			options.ContentTypeString = ContentType;
			options.Roles.Add(PredefinedDsTextViewRoles.RoslynCodeEditor);
			options.Roles.Add(TextViewRole);
			var codeEditor = codeEditorProvider.Create(options);
			codeEditor.TextView.Options.SetOptionValue(DefaultWpfViewOptions.AppearanceCategory, AppearanceCategory);
			codeEditor.TextView.Options.SetOptionValue(DefaultTextViewHostOptions.GlyphMarginId, true);

			var textBuffer = codeEditor.TextView.TextBuffer;
			textBuffer.Replace(new Span(0, textBuffer.CurrentSnapshot.Length), doc.Code);

			var documentInfo = DocumentInfo.Create(DocumentId.CreateNewId(projectId), doc.Name, null, SourceCodeKind.Regular, TextLoader.From(codeEditor.TextBuffer.AsTextContainer(), VersionStamp.Create()));
			return new RoslynCodeDocument(codeEditor, documentInfo);
		}

		public ICodeDocument[] AddDocuments(CompilerDocumentInfo[] documents) {
			var newDocuments = new List<RoslynCodeDocument>();

			foreach (var doc in documents)
				newDocuments.Add(CreateDocument(projectId, doc));
			this.documents.AddRange(newDocuments);

			foreach (var doc in newDocuments)
				workspace.AddDocument(doc.Info);

			foreach (var doc in newDocuments)
				workspace.OpenDocument(doc.Info.Id);

			foreach (var doc in newDocuments) {
				if (textViewUndoManagerProvider.TryGetTextViewUndoManager(doc.TextView, out var manager))
					manager.ClearUndoHistory();
			}

			return newDocuments.ToArray();
		}

		public async Task<CompilationResult> CompileAsync(CancellationToken cancellationToken) {
			var project = workspace.CurrentSolution.Projects.First();
			Debug.Assert(project.SupportsCompilation);
			var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
			if (compilation == null)
				throw new InvalidOperationException("Project returned a null Compilation");

			var result = Compile(compilation, cancellationToken);
			if (result.Success)
				return result;

			// We allow unsafe code but the compiler tries to add extra attributes to the assembly. Sometimes
			// the corlib doesn't have the required members and the compiler fails to compile the code.
			// Let's try again but without unsafe code.
			var noAttrOptions = CreateCompilationOptionsNoAttributes(compilation.Options);
			if (noAttrOptions != compilation.Options) {
				var compilation2 = compilation.WithOptions(noAttrOptions);
				var result2 = Compile(compilation2, cancellationToken);
				if (result2.Success)
					return result2;
			}

			return result;
		}

		CompilationResult Compile(Compilation compilation, CancellationToken cancellationToken) {
			var peStream = new MemoryStream();
			MemoryStream pdbStream = null;
			var emitOpts = new EmitOptions(debugInformationFormat: DebugInformationFormat.PortablePdb);
			if (emitOpts.DebugInformationFormat == DebugInformationFormat.Pdb || emitOpts.DebugInformationFormat == DebugInformationFormat.PortablePdb)
				pdbStream = new MemoryStream();
			var emitResult = compilation.Emit(peStream, pdbStream, options: emitOpts, cancellationToken: cancellationToken);
			var diagnostics = emitResult.Diagnostics.ToCompilerDiagnostics(GetHelpUri).ToArray();
			if (!emitResult.Success)
				return new CompilationResult(diagnostics);
			return new CompilationResult(peStream.ToArray(), new DebugFileResult(emitOpts.DebugInformationFormat.ToDebugFileFormat(), pdbStream?.ToArray()), diagnostics);
		}

		protected abstract string GetHelpUri(Diagnostic diagnostic);

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
