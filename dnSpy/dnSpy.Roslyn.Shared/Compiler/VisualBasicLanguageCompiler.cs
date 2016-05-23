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
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Languages;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Editor.Roslyn;
using dnSpy.Roslyn.Shared.VisualBasic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.VisualBasic;

namespace dnSpy.Roslyn.Shared.Compiler {
	[Export(typeof(ILanguageCompilerCreator))]
	sealed class VisualBasicLanguageCompilerCreator : ILanguageCompilerCreator {
		public double Order => 0;
		public ImageReference? Icon => new ImageReference(GetType().Assembly, "VisualBasicFile");
		public Guid Language => LanguageConstants.LANGUAGE_VISUALBASIC;
		public ILanguageCompiler Create() => new VisualBasicLanguageCompiler(roslynCodeEditorCreator);

		readonly IRoslynCodeEditorCreator roslynCodeEditorCreator;

		[ImportingConstructor]
		VisualBasicLanguageCompilerCreator(IRoslynCodeEditorCreator roslynCodeEditorCreator) {
			this.roslynCodeEditorCreator = roslynCodeEditorCreator;
		}
	}

	sealed class VisualBasicLanguageCompiler : RoslynLanguageCompiler {
		protected override Guid ContentType => new Guid(ContentTypes.VISUALBASIC_ROSLYN);
		protected override string LanguageName => LanguageNames.VisualBasic;
		protected override CompilationOptions CompilationOptions => new VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary);
		protected override ParseOptions ParseOptions => new VisualBasicParseOptions(languageVersion: VisualBasicConstants.LatestVersion);
		protected override string FileExtension => ".vb";
		public override IEnumerable<string> RequiredAssemblyReferences => requiredAssemblyReferences;
		static readonly string[] requiredAssemblyReferences = new string[] {
			"Microsoft.VisualBasic, Version=10.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
		};

		public VisualBasicLanguageCompiler(IRoslynCodeEditorCreator roslynCodeEditorCreator)
			: base(roslynCodeEditorCreator) {
		}
	}
}
