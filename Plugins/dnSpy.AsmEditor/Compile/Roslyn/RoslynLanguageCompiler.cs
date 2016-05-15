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
using dnSpy.Contracts.AsmEditor.Compile;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Roslyn;
using dnSpy.Roslyn.Shared.Text;
using Microsoft.CodeAnalysis;

namespace dnSpy.AsmEditor.Compile.Roslyn {
	abstract class RoslynLanguageCompiler : ILanguageCompiler {
		protected abstract Guid ContentType { get; }
		protected abstract string LanguageName { get; }
		protected abstract CompilationOptions CompilationOptions { get; }
		protected abstract ParseOptions ParseOptions { get; }
		protected abstract string FileExtension { get; }

		readonly IRoslynCodeEditorCreator roslynCodeEditorCreator;
		readonly List<RoslynCodeDocument> documents = new List<RoslynCodeDocument>();
		RoslynCodeDocument mainDocument;
		AdhocWorkspace workspace;

		protected RoslynLanguageCompiler(IRoslynCodeEditorCreator roslynCodeEditorCreator) {
			this.roslynCodeEditorCreator = roslynCodeEditorCreator;
		}

		public void AddDecompiledCode(IDecompiledCodeResult decompiledCodeResult) {
			Debug.Assert(workspace == null);

			workspace = new AdhocWorkspace();
			var refs = decompiledCodeResult.AssemblyReferences.Select(a => MetadataReference.CreateFromImage(a.Data, a.IsAssemblyReference ? MetadataReferenceProperties.Assembly : MetadataReferenceProperties.Module)).ToArray();
			var projectId = ProjectId.CreateNewId();

			const string mainFilename = "main";
			mainDocument = AddDocument(projectId, mainFilename + FileExtension, decompiledCodeResult.MainCode);
			AddDocument(projectId, mainFilename + ".g" + FileExtension, decompiledCodeResult.HiddenCode);

			var projectInfo = ProjectInfo.Create(projectId, VersionStamp.Default, "compilecodeproj", Guid.NewGuid().ToString(), LanguageName,
				compilationOptions: CompilationOptions.WithOptimizationLevel(OptimizationLevel.Release).WithPlatform(decompiledCodeResult.Platform.ToPlatform()),
				parseOptions: ParseOptions,
				documents: documents.Select(a => a.Info),
				metadataReferences: refs,
				isSubmission: false, hostObjectType: null);
			workspace.AddProject(projectInfo);
			foreach (var doc in documents)
				workspace.OpenDocument(doc.Info.Id);
		}

		RoslynCodeDocument AddDocument(ProjectId projectId, string name, string code) {
			var options = new RoslynCodeEditorOptions();
			options.Options.ContentTypeGuid = ContentType;
			var codeEditor = roslynCodeEditorCreator.Create(options);
			codeEditor.TextBuffer.Replace(new Span(0, codeEditor.TextBuffer.CurrentSnapshot.Length), code);

			var documentInfo = DocumentInfo.Create(DocumentId.CreateNewId(projectId), name, null, SourceCodeKind.Regular, TextLoader.From(codeEditor.TextBuffer.AsTextContainer(), VersionStamp.Default));
			var doc = new RoslynCodeDocument(codeEditor, documentInfo);
			documents.Add(doc);
			return doc;
		}

		public ICodeDocument[] GetCodeDocuments(out ICodeDocument mainDocument) {
			mainDocument = this.mainDocument;
			return documents.ToArray();
		}

		public async Task<CompilationResult> CompileAsync(CancellationToken cancellationToken) {
			var project = workspace.CurrentSolution.Projects.First();
			Debug.Assert(project.SupportsCompilation);
			var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
			if (compilation == null)
				throw new InvalidOperationException("Project returned a null Compilation");
			var outputStream = new MemoryStream();
			var emitResult = compilation.Emit(outputStream, cancellationToken: cancellationToken);
			if (!emitResult.Success)
				return new CompilationResult(emitResult.Diagnostics.ToCompilerDiagnostics().ToArray());
			return new CompilationResult(outputStream.ToArray(), emitResult.Diagnostics.ToCompilerDiagnostics().ToArray());
		}

		public void Dispose() {
			// This also closes all documents
			workspace?.Dispose();
		}
	}
}
