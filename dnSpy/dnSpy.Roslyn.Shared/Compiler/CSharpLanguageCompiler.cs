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
using dnSpy.Roslyn.Shared.Text.Editor;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace dnSpy.Roslyn.Shared.Compiler {
	[Export(typeof(ILanguageCompilerProvider))]
	sealed class CSharpLanguageCompilerProvider : ILanguageCompilerProvider {
		public double Order => 0;
		public ImageReference? Icon => new ImageReference(GetType().Assembly, "CSharpFile");
		public Guid Language => DecompilerConstants.LANGUAGE_CSHARP;
		public ILanguageCompiler Create() => new CSharpLanguageCompiler(codeEditorProvider);

		readonly ICodeEditorProvider codeEditorProvider;

		[ImportingConstructor]
		CSharpLanguageCompilerProvider(ICodeEditorProvider codeEditorProvider) {
			this.codeEditorProvider = codeEditorProvider;
		}
	}

	sealed class CSharpLanguageCompiler : RoslynLanguageCompiler {
		protected override string ContentType => ContentTypes.CSharpRoslyn;
		protected override string LanguageName => LanguageNames.CSharp;
		protected override CompilationOptions CompilationOptions => new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true);
		protected override ParseOptions ParseOptions => new CSharpParseOptions();
		protected override string FileExtension => ".cs";
		protected override string AppearanceCategory => RoslynAppearanceCategoryConstants.CodeEditor_CSharp;
		public override IEnumerable<string> RequiredAssemblyReferences => Array.Empty<string>();

		public CSharpLanguageCompiler(ICodeEditorProvider codeEditorProvider)
			: base(codeEditorProvider) {
		}
	}
}
