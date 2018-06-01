/*
    Copyright (C) 2014-2018 de4dot@gmail.com

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
using dnlib.DotNet;
using dnSpy.Contracts.AsmEditor.Compiler;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Settings.AppearanceCategory;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Editor;
using dnSpy.Contracts.Text.Editor.Operations;
using dnSpy.Roslyn.Documentation;
using dnSpy.Roslyn.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.VisualBasic;

namespace dnSpy.Roslyn.Compiler.VisualBasic {
	[Export(typeof(ILanguageCompilerProvider))]
	sealed class VisualBasicLanguageCompilerProvider : RoslynLanguageCompilerProvider {
		public override ImageReference? Icon => DsImages.VBFileNode;
		public override Guid Language => DecompilerConstants.LANGUAGE_VISUALBASIC;
		public override ILanguageCompiler Create(CompilationKind kind) => new VisualBasicLanguageCompiler(kind, codeEditorProvider, docFactory, roslynDocumentChangedService, textViewUndoManagerProvider);

		readonly ICodeEditorProvider codeEditorProvider;
		readonly IRoslynDocumentationProviderFactory docFactory;
		readonly IRoslynDocumentChangedService roslynDocumentChangedService;
		readonly ITextViewUndoManagerProvider textViewUndoManagerProvider;

		[ImportingConstructor]
		VisualBasicLanguageCompilerProvider(ICodeEditorProvider codeEditorProvider, IRoslynDocumentationProviderFactory docFactory, IRoslynDocumentChangedService roslynDocumentChangedService, ITextViewUndoManagerProvider textViewUndoManagerProvider) {
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
		protected override ParseOptions ParseOptions => new VisualBasicParseOptions(languageVersion: LanguageVersion.Latest);
		public override string FileExtension => ".vb";
		protected override string AppearanceCategory => AppearanceCategoryConstants.TextEditor;

		bool embedVbCoreRuntime;

		public VisualBasicLanguageCompiler(CompilationKind kind, ICodeEditorProvider codeEditorProvider, IRoslynDocumentationProviderFactory docFactory, IRoslynDocumentChangedService roslynDocumentChangedService, ITextViewUndoManagerProvider textViewUndoManagerProvider)
			: base(kind, codeEditorProvider, docFactory, roslynDocumentChangedService, textViewUndoManagerProvider) {
		}

		protected override CompilationOptions CreateCompilationOptions(bool allowUnsafe) =>
			new VisualBasicCompilationOptions(DefaultOutputKind, embedVbCoreRuntime: embedVbCoreRuntime);

		public override IEnumerable<string> GetRequiredAssemblyReferences(ModuleDef editedModule) {
			var frameworkKind = FrameworkDetector.GetFrameworkKind(editedModule);
			// If we're editing mscorlib, embed the types
			if (editedModule.Assembly.IsCorLib())
				frameworkKind = FrameworkKind.Unknown;
			switch (frameworkKind) {
			case FrameworkKind.DotNetFramework2:
				return new[] { "Microsoft.VisualBasic, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a" };

			case FrameworkKind.DotNetFramework4:
				return new[] { "Microsoft.VisualBasic, Version=10.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a" };

			default:
				embedVbCoreRuntime = true;
				return Array.Empty<string>();
			}
		}

		protected override string GetHelpUri(Diagnostic diagnostic) {
			string id = diagnostic.Id.ToLowerInvariant();
			// See https://github.com/dotnet/docs/tree/master/docs/visual-basic/misc
			const string URL = "https://docs.microsoft.com/dotnet/visual-basic/misc/";
			return URL + id;
		}
	}
}
