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
using System.ComponentModel.Composition;
using dnSpy.Contracts.AsmEditor.Compile;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Languages;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Roslyn;
using dnSpy.Roslyn.Shared.CSharp;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace dnSpy.AsmEditor.Compile.Roslyn {
	[Export(typeof(ILanguageCompilerCreator))]
	sealed class CSharpLanguageCompilerCreator : ILanguageCompilerCreator {
		public double Order => 0;
		public ImageReference? Icon => new ImageReference(GetType().Assembly, "CSharpFile");
		public Guid Language => LanguageConstants.LANGUAGE_CSHARP;
		public ILanguageCompiler Create() => new CSharpLanguageCompiler(roslynCodeEditorCreator);

		readonly IRoslynCodeEditorCreator roslynCodeEditorCreator;

		[ImportingConstructor]
		CSharpLanguageCompilerCreator(IRoslynCodeEditorCreator roslynCodeEditorCreator) {
			this.roslynCodeEditorCreator = roslynCodeEditorCreator;
		}
	}

	sealed class CSharpLanguageCompiler : RoslynLanguageCompiler {
		protected override Guid ContentType => new Guid(ContentTypes.CSHARP_ROSLYN);
		protected override string LanguageName => LanguageNames.CSharp;
		protected override CompilationOptions CompilationOptions => new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true);
		protected override ParseOptions ParseOptions => new CSharpParseOptions(languageVersion: CSharpConstants.LatestVersion);
		protected override string FileExtension => ".cs";

		public CSharpLanguageCompiler(IRoslynCodeEditorCreator roslynCodeEditorCreator)
			: base(roslynCodeEditorCreator) {
		}
	}
}
