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
using Microsoft.CodeAnalysis.CSharp;

namespace dnSpy.Roslyn.Compiler.CSharp {
	[Export(typeof(ILanguageCompilerProvider))]
	sealed class CSharpLanguageCompilerProvider : RoslynLanguageCompilerProvider {
		public override ImageReference? Icon => DsImages.CSFileNode;
		public override Guid Language => DecompilerConstants.LANGUAGE_CSHARP;
		public override ILanguageCompiler Create(CompilationKind kind) => new CSharpLanguageCompiler(kind, csharpCompilerSettings, codeEditorProvider, docFactory, roslynDocumentChangedService, textViewUndoManagerProvider);

		readonly CSharpCompilerSettings csharpCompilerSettings;
		readonly ICodeEditorProvider codeEditorProvider;
		readonly IRoslynDocumentationProviderFactory docFactory;
		readonly IRoslynDocumentChangedService roslynDocumentChangedService;
		readonly ITextViewUndoManagerProvider textViewUndoManagerProvider;

		[ImportingConstructor]
		CSharpLanguageCompilerProvider(CSharpCompilerSettings csharpCompilerSettings, ICodeEditorProvider codeEditorProvider, IRoslynDocumentationProviderFactory docFactory, IRoslynDocumentChangedService roslynDocumentChangedService, ITextViewUndoManagerProvider textViewUndoManagerProvider) {
			this.csharpCompilerSettings = csharpCompilerSettings;
			this.codeEditorProvider = codeEditorProvider;
			this.docFactory = docFactory;
			this.roslynDocumentChangedService = roslynDocumentChangedService;
			this.textViewUndoManagerProvider = textViewUndoManagerProvider;
		}
	}

	sealed class CSharpLanguageCompiler : RoslynLanguageCompiler {
		protected override string TextViewRole => PredefinedDsTextViewRoles.RoslynCSharpCodeEditor;
		protected override string ContentType => ContentTypes.CSharpRoslyn;
		protected override string LanguageName => LanguageNames.CSharp;
		protected override ParseOptions ParseOptions => new CSharpParseOptions(languageVersion: LanguageVersion.Latest, preprocessorSymbols: GetPreprocessorSymbols());
		public override string FileExtension => ".cs";
		protected override string AppearanceCategory => AppearanceCategoryConstants.TextEditor;
		protected override bool SupportsNetModule => true;

		readonly CSharpCompilerSettings csharpCompilerSettings;

		public CSharpLanguageCompiler(CompilationKind kind, CSharpCompilerSettings csharpCompilerSettings, ICodeEditorProvider codeEditorProvider, IRoslynDocumentationProviderFactory docFactory, IRoslynDocumentChangedService roslynDocumentChangedService, ITextViewUndoManagerProvider textViewUndoManagerProvider)
			: base(kind, codeEditorProvider, docFactory, roslynDocumentChangedService, textViewUndoManagerProvider) =>
			this.csharpCompilerSettings = csharpCompilerSettings ?? throw new ArgumentNullException(nameof(csharpCompilerSettings));

		IEnumerable<string> GetPreprocessorSymbols() {
			foreach (var tmp in csharpCompilerSettings.PreprocessorSymbols.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries)) {
				var s = tmp.Trim();
				if (string.IsNullOrEmpty(s))
					continue;
				yield return s;
			}
		}

		protected override CompilationOptions CreateCompilationOptions(OutputKind outputKind) =>
			new CSharpCompilationOptions(outputKind,
				optimizationLevel: csharpCompilerSettings.Optimize ? OptimizationLevel.Release : OptimizationLevel.Debug,
				checkOverflow: csharpCompilerSettings.CheckOverflow,
				allowUnsafe: csharpCompilerSettings.AllowUnsafe);

		protected override CompilationOptions CreateCompilationOptionsNoAttributes(CompilationOptions compilationOptions) =>
			((CSharpCompilationOptions)compilationOptions).WithAllowUnsafe(false);

		public override IEnumerable<string> GetRequiredAssemblyReferences(ModuleDef editedModule) => Array.Empty<string>();

		static readonly HashSet<string> requiresCompilerMessagesUrl = new HashSet<string>(StringComparer.Ordinal) {
			"cs0001", "cs0006", "cs0007", "cs0015", "cs0016", "cs0019", "cs0029", "cs0034",
			"cs0038", "cs0039", "cs0050", "cs0051", "cs0052", "cs0071", "cs0103", "cs0106",
			"cs0108", "cs0115", "cs0116", "cs0120", "cs0122", "cs0134", "cs0151", "cs0163",
			"cs0165", "cs0173", "cs0178", "cs0188", "cs0201", "cs0229", "cs0233", "cs0234",
			"cs0246", "cs0260", "cs0266", "cs0269", "cs0270", "cs0304", "cs0310", "cs0311",
			"cs0413", "cs0417", "cs0420", "cs0429", "cs0433", "cs0445", "cs0446", "cs0465",
			"cs0467", "cs0504", "cs0507", "cs0518", "cs0523", "cs0545", "cs0552", "cs0563",
			"cs0570", "cs0571", "cs0579", "cs0592", "cs0616", "cs0618", "cs0650", "cs0675",
			"cs0686", "cs0702", "cs0703", "cs0731", "cs0826", "cs0834", "cs0840", "cs0843",
			"cs0845", "cs1001", "cs1009", "cs1018", "cs1019", "cs1026", "cs1029", "cs1058",
			"cs1060", "cs1061", "cs1112", "cs1501", "cs1502", "cs1519", "cs1540", "cs1546",
			"cs1548", "cs1564", "cs1567", "cs1579", "cs1591", "cs1598", "cs1607", "cs1610",
			"cs1612", "cs1614", "cs1616", "cs1640", "cs1644", "cs1656", "cs1658", "cs1674",
			"cs1683", "cs1685", "cs1690", "cs1691", "cs1699", "cs1700", "cs1701", "cs1703",
			"cs1704", "cs1705", "cs1708", "cs1716", "cs1721", "cs1726", "cs1729", "cs1762",
			"cs1919", "cs1921", "cs1926", "cs1933", "cs1936", "cs1941", "cs1942", "cs1943",
			"cs1946", "cs1956", "cs2032", "cs3003", "cs3007", "cs3009", "cs4014", "cs7003",
		};

		protected override string GetHelpUri(Diagnostic diagnostic) {
			string id = diagnostic.Id.ToLowerInvariant();

			// See https://github.com/dotnet/docs/tree/master/docs/csharp/misc
			const string miscUrl = "https://docs.microsoft.com/dotnet/csharp/misc/";

			// See https://github.com/dotnet/docs/tree/master/docs/csharp/language-reference/compiler-messages
			const string compilerMessagesUrl = "https://docs.microsoft.com/dotnet/csharp/language-reference/compiler-messages/";

			//TODO: https://github.com/dotnet/docs/issues/1491
			if (requiresCompilerMessagesUrl.Contains(id))
				return compilerMessagesUrl + id;
			if (id == "cs3024")
				return miscUrl + "compiler-warning-cs3024";
			return miscUrl + id;
		}
	}
}
