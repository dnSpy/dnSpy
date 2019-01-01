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
using Microsoft.CodeAnalysis.VisualBasic;

namespace dnSpy.Roslyn.Compiler.VisualBasic {
	[Export(typeof(ILanguageCompilerProvider))]
	sealed class VisualBasicLanguageCompilerProvider : RoslynLanguageCompilerProvider {
		public override ImageReference? Icon => DsImages.VBFileNode;
		public override Guid Language => DecompilerConstants.LANGUAGE_VISUALBASIC;
		public override ILanguageCompiler Create(CompilationKind kind) => new VisualBasicLanguageCompiler(kind, visualBasicCompilerSettings, codeEditorProvider, docFactory, roslynDocumentChangedService, textViewUndoManagerProvider);

		readonly VisualBasicCompilerSettings visualBasicCompilerSettings;
		readonly ICodeEditorProvider codeEditorProvider;
		readonly IRoslynDocumentationProviderFactory docFactory;
		readonly IRoslynDocumentChangedService roslynDocumentChangedService;
		readonly ITextViewUndoManagerProvider textViewUndoManagerProvider;

		[ImportingConstructor]
		VisualBasicLanguageCompilerProvider(VisualBasicCompilerSettings visualBasicCompilerSettings, ICodeEditorProvider codeEditorProvider, IRoslynDocumentationProviderFactory docFactory, IRoslynDocumentChangedService roslynDocumentChangedService, ITextViewUndoManagerProvider textViewUndoManagerProvider) {
			this.visualBasicCompilerSettings = visualBasicCompilerSettings;
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
		protected override ParseOptions ParseOptions => new VisualBasicParseOptions(languageVersion: LanguageVersion.Latest, preprocessorSymbols: GetPreprocessorSymbols());
		public override string FileExtension => ".vb";
		protected override string AppearanceCategory => AppearanceCategoryConstants.TextEditor;
		// The VB compiler doesn't support netmodule + embed runtime
		protected override bool SupportsNetModule => !embedVbCoreRuntime;

		readonly VisualBasicCompilerSettings visualBasicCompilerSettings;
		bool embedVbCoreRuntime;

		public VisualBasicLanguageCompiler(CompilationKind kind, VisualBasicCompilerSettings visualBasicCompilerSettings, ICodeEditorProvider codeEditorProvider, IRoslynDocumentationProviderFactory docFactory, IRoslynDocumentChangedService roslynDocumentChangedService, ITextViewUndoManagerProvider textViewUndoManagerProvider)
			: base(kind, codeEditorProvider, docFactory, roslynDocumentChangedService, textViewUndoManagerProvider) =>
			this.visualBasicCompilerSettings = visualBasicCompilerSettings ?? throw new ArgumentNullException(nameof(visualBasicCompilerSettings));

		IEnumerable<KeyValuePair<string, object>> GetPreprocessorSymbols() {
			foreach (var tmp in visualBasicCompilerSettings.PreprocessorSymbols.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries)) {
				var s = tmp.Trim();
				if (string.IsNullOrEmpty(s))
					continue;
				var index = s.IndexOf('=');
				if (index >= 0) {
					var value = s.Substring(index + 1).Trim();
					s = s.Substring(0, index).Trim();
					if (string.IsNullOrEmpty(s))
						continue;
					if (bool.TryParse(value, out bool valueBoolean))
						yield return new KeyValuePair<string, object>(s, valueBoolean);
					else if (int.TryParse(value, out int valueInt32))
						yield return new KeyValuePair<string, object>(s, valueInt32);
					else if (double.TryParse(value, out double valueDouble))
						yield return new KeyValuePair<string, object>(s, valueDouble);
					else {
						if (value.Length >= 2 && value[0] == '"' && value[value.Length - 1] == '"')
							value = value.Substring(1, value.Length - 2);
						yield return new KeyValuePair<string, object>(s, value);
					}
				}
				else
					yield return new KeyValuePair<string, object>(s, true);
			}
		}

		protected override CompilationOptions CreateCompilationOptions(OutputKind outputKind) =>
			new VisualBasicCompilationOptions(outputKind,
				optionStrict: visualBasicCompilerSettings.OptionStrict ? OptionStrict.On : OptionStrict.Off,
				optionInfer: visualBasicCompilerSettings.OptionInfer,
				optionExplicit: visualBasicCompilerSettings.OptionExplicit,
				optionCompareText: !visualBasicCompilerSettings.OptionCompareBinary,
				embedVbCoreRuntime: embedVbCoreRuntime,
				optimizationLevel: visualBasicCompilerSettings.Optimize ? OptimizationLevel.Release : OptimizationLevel.Debug);

		protected override CompilationOptions CreateCompilationOptionsNoAttributes(CompilationOptions compilationOptions) => compilationOptions;

		public override IEnumerable<string> GetRequiredAssemblyReferences(ModuleDef editedModule) {
			var frameworkKind = FrameworkDetector.GetFrameworkKind(editedModule);
			// If we're editing mscorlib, embed the types
			if (visualBasicCompilerSettings.EmbedVBRuntime || editedModule.Assembly.IsCorLib())
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
