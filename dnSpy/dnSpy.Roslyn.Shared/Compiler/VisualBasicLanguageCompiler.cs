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
using System.ComponentModel.Composition;
using dnSpy.Contracts.AsmEditor.Compiler;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Editor;
using dnSpy.Contracts.Text.Editor.Operations;
using dnSpy.Roslyn.Shared.Documentation;
using dnSpy.Roslyn.Shared.Text;
using dnSpy.Roslyn.Shared.Text.Editor;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.VisualBasic;

namespace dnSpy.Roslyn.Shared.Compiler {
	[Export(typeof(ILanguageCompilerProvider))]
	sealed class VisualBasicLanguageCompilerCreator : RoslynLanguageCompilerProvider {
		public override ImageReference? Icon => DsImages.VBFileNode;
		public override Guid Language => DecompilerConstants.LANGUAGE_VISUALBASIC;
		public override ILanguageCompiler Create(CompilationKind kind) => new VisualBasicLanguageCompiler(kind, codeEditorProvider, docFactory, roslynDocumentChangedService, textViewUndoManagerProvider);

		readonly ICodeEditorProvider codeEditorProvider;
		readonly IRoslynDocumentationProviderFactory docFactory;
		readonly IRoslynDocumentChangedService roslynDocumentChangedService;
		readonly ITextViewUndoManagerProvider textViewUndoManagerProvider;

		[ImportingConstructor]
		VisualBasicLanguageCompilerCreator(ICodeEditorProvider codeEditorProvider, IRoslynDocumentationProviderFactory docFactory, IRoslynDocumentChangedService roslynDocumentChangedService, ITextViewUndoManagerProvider textViewUndoManagerProvider) {
			this.codeEditorProvider = codeEditorProvider;
			this.docFactory = docFactory;
			this.roslynDocumentChangedService = roslynDocumentChangedService;
			this.textViewUndoManagerProvider = textViewUndoManagerProvider;
		}
	}

	sealed class VisualBasicLanguageCompiler : RoslynLanguageCompiler {
		protected override string TextViewRole => PredefinedDsTextViewRoles.RoslynVisualBasicCodeEditor;
		protected override string ContentType => ContentTypes.VisualBasicRoslyn;
		protected override string LanguageName => LanguageNames.VisualBasic;
		protected override CompilationOptions CompilationOptions => new VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary);
		protected override ParseOptions ParseOptions => new VisualBasicParseOptions();
		protected override string FileExtension => ".vb";
		protected override string AppearanceCategory => RoslynAppearanceCategoryConstants.CodeEditor_VisualBasic;
		public override IEnumerable<string> RequiredAssemblyReferences => requiredAssemblyReferences;
		static readonly string[] requiredAssemblyReferences = new string[] {
			"Microsoft.VisualBasic, Version=10.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
		};

		public VisualBasicLanguageCompiler(CompilationKind kind, ICodeEditorProvider codeEditorProvider, IRoslynDocumentationProviderFactory docFactory, IRoslynDocumentChangedService roslynDocumentChangedService, ITextViewUndoManagerProvider textViewUndoManagerProvider)
			: base(kind, codeEditorProvider, docFactory, roslynDocumentChangedService, textViewUndoManagerProvider) {
		}
	}
}
