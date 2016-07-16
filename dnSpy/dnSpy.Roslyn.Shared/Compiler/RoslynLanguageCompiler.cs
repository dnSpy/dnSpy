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
using dnSpy.Contracts.Text.Editor.Roslyn;
using dnSpy.Roslyn.Shared.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.OptionsExtensionMethods;

namespace dnSpy.Roslyn.Shared.Compiler {
	abstract class RoslynLanguageCompiler : ILanguageCompiler {
		protected abstract string ContentType { get; }
		protected abstract string LanguageName { get; }
		protected abstract CompilationOptions CompilationOptions { get; }
		protected abstract ParseOptions ParseOptions { get; }
		protected abstract string FileExtension { get; }
		protected abstract string AppearanceCategory { get; }
		public abstract IEnumerable<string> RequiredAssemblyReferences { get; }

		readonly IRoslynCodeEditorCreator roslynCodeEditorCreator;
		readonly List<RoslynCodeDocument> documents;
		AdhocWorkspace workspace;

		protected RoslynLanguageCompiler(IRoslynCodeEditorCreator roslynCodeEditorCreator) {
			this.roslynCodeEditorCreator = roslynCodeEditorCreator;
			this.documents = new List<RoslynCodeDocument>();
		}

		public ICodeDocument[] AddDecompiledCode(IDecompiledCodeResult decompiledCodeResult) {
			Debug.Assert(workspace == null);

			workspace = new AdhocWorkspace(DesktopMefHostServices.DefaultServices);
			var refs = decompiledCodeResult.AssemblyReferences.Select(a => a.CreateMetadataReference()).ToArray();
			var projectId = ProjectId.CreateNewId();

			foreach (var doc in decompiledCodeResult.Documents)
				documents.Add(CreateDocument(projectId, doc.NameNoExtension));

			var projectInfo = ProjectInfo.Create(projectId, VersionStamp.Default, "compilecodeproj", Guid.NewGuid().ToString(), LanguageName,
				compilationOptions: CompilationOptions.WithOptimizationLevel(OptimizationLevel.Release).WithPlatform(GetPlatform(decompiledCodeResult.Platform)),
				parseOptions: ParseOptions,
				documents: documents.Select(a => a.Info),
				metadataReferences: refs,
				isSubmission: false, hostObjectType: null);
			workspace.AddProject(projectInfo);
			foreach (var doc in documents)
				workspace.OpenDocument(doc.Info.Id);

			// Initialize the code after Roslyn has initialized so colorization works. The caret
			// will force creation of lines and they won't be colorized if Roslyn hasn't init'd yet.
			for (int i = 0; i < documents.Count; i++) {
				var doc = decompiledCodeResult.Documents[i];
				var textBuffer = documents[i].TextView.TextBuffer;
				textBuffer.Replace(new Span(0, textBuffer.CurrentSnapshot.Length), doc.Code);
			}

			return documents.ToArray();
		}

		static Platform GetPlatform(TargetPlatform platform) {
			// AnyCpu32BitPreferred can only be used when creating executables (we create a dll)
			if (platform == TargetPlatform.AnyCpu32BitPreferred)
				return Platform.AnyCpu;
			return platform.ToPlatform();
		}

		RoslynCodeDocument CreateDocument(ProjectId projectId, string nameNoExtension) {
			var options = new RoslynCodeEditorOptions();
			options.ContentTypeString = ContentType;
			var codeEditor = roslynCodeEditorCreator.Create(options);
			codeEditor.TextView.Options.SetOptionValue(DefaultWpfViewOptions.AppearanceCategory, AppearanceCategory);
			codeEditor.TextView.Options.SetOptionValue(DefaultTextViewHostOptions.GlyphMarginId, true);

			var documentInfo = DocumentInfo.Create(DocumentId.CreateNewId(projectId), nameNoExtension + FileExtension, null, SourceCodeKind.Regular, TextLoader.From(codeEditor.TextBuffer.AsTextContainer(), VersionStamp.Default));
			return new RoslynCodeDocument(codeEditor, documentInfo, nameNoExtension);
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
			var newProj = workspace.CurrentSolution.Projects.First().AddMetadataReferences(metadataReferences.Select(a => a.CreateMetadataReference()));
			return workspace.TryApplyChanges(newProj.Solution);
		}

		public void Dispose() {
			// This also closes all documents
			workspace?.Dispose();
			foreach (var doc in documents)
				doc.Dispose();
			documents.Clear();
		}
	}
}
