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
using System.Threading;
using dnSpy.Contracts.Scripting;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Editor;
using dnSpy.Scripting.Roslyn.Common;
using dnSpy.Scripting.Roslyn.Properties;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.Scripting.Hosting;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.VisualBasic.Scripting;
using Microsoft.CodeAnalysis.VisualBasic.Scripting.Hosting;

namespace dnSpy.Scripting.Roslyn.VisualBasic {
	sealed class VisualBasicControlVM : ScriptControlVM {
		protected override string Logo {
			get {
				// This is how MS gets the version, see roslyn/src/Interactive/EditorFeatures/VisualBasic/Interactive/VisualBasicReplServiceProvider.vb
				return string.Format("Microsoft (R) Roslyn Visual Basic Compiler version {0}",
					FileVersionInfo.GetVersionInfo(typeof(VisualBasicCommandLineArguments).Assembly.Location).FileVersion);
			}
		}

		static readonly string CODEFILTERTEXT = string.Format("{1} (*.vbx)|*.vbx|{0} (*.*)|*.*", dnSpy_Scripting_Roslyn_Resources.AllFiles, dnSpy_Scripting_Roslyn_Resources.VisualBasicScriptFiles);

		protected override string Help => dnSpy_Scripting_Roslyn_Resources.HelpString;
		protected override ObjectFormatter ObjectFormatter => VisualBasicObjectFormatter.Instance;
		protected override DiagnosticFormatter DiagnosticFormatter => VisualBasicDiagnosticFormatter.Instance;
		protected override string TextFilenameNoExtension => "VisualBasicScript";
		protected override string CodeFilenameNoExtension => "VisualBasicScript";
		protected override string CodeFileExtension => "vbx";
		protected override string CodeFilterText => CODEFILTERTEXT;

		public VisualBasicControlVM(IReplEditor replEditor, ReplSettings settings, IServiceLocator serviceLocator)
			: base(replEditor, settings, serviceLocator) {
		}

		protected override Script<T> Create<T>(string code, ScriptOptions options, Type globalsType, InteractiveAssemblyLoader assemblyLoader) =>
			VisualBasicScript.Create<T>(code, options, globalsType, assemblyLoader);

		protected override bool IsCompleteSubmission(string text) =>
			SyntaxFactory.IsCompleteSubmission(SyntaxFactory.ParseSyntaxTree(text, parseOptions));
		static readonly VisualBasicParseOptions parseOptions = new VisualBasicParseOptions(kind: SourceCodeKind.Script);

		protected override SyntaxTree CreateSyntaxTree(string code, CancellationToken cancellationToken) =>
			SyntaxFactory.ParseSyntaxTree(code, parseOptions, cancellationToken: cancellationToken);

		protected override Compilation CreateScriptCompilation(string assemblyName, SyntaxTree syntaxTree,
			IEnumerable<MetadataReference> references, CompilationOptions options,
			Compilation previousScriptCompilation, Type returnType, Type globalsType) =>
			VisualBasicCompilation.CreateScriptCompilation(assemblyName, syntaxTree, references, (VisualBasicCompilationOptions)options, (VisualBasicCompilation)previousScriptCompilation, returnType, globalsType);

		protected override void InitializeUserScriptOptions(UserScriptOptions options) {
			var rspFile = GetResponseFile("VisualBasicInteractive.rsp");
			if (rspFile == null)
				return;
			this.replEditor.OutputPrintLine(string.Format(dnSpy_Scripting_Roslyn_Resources.LoadingContextFromFile, Path.GetFileName(rspFile)), BoxedTextColor.ReplOutputText);

			foreach (var t in ResponseFileReader.Read(rspFile)) {
				switch (t.Item1.ToLowerInvariant()) {
				case "r":
				case "reference":
					options.References.AddRange(RespFileUtils.GetReferences(t.Item2));
					break;

				case "import":
				case "imports":
					options.Imports.AddRange(t.Item2.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries));
					break;

				case "lib":
				case "libpath":
				case "libpaths":
					options.LibPaths.AddRange(RespFileUtils.GetReferencePaths(t.Item2));
					break;

				case "loadpath":
				case "loadpaths":
					options.LoadPaths.AddRange(RespFileUtils.GetReferencePaths(t.Item2));
					break;

				default:
					Debug.Fail(string.Format("Unknown option: '{0}'", t.Item1));
					break;
				}
			}
		}
	}
}
