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
using System.Diagnostics;
using System.IO;
using dnSpy.Contracts.Scripting;
using dnSpy.Contracts.TextEditor;
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
		static VisualBasicControlVM() {
			const LanguageVersion latestVersion = LanguageVersion.VisualBasic14;
			Debug.Assert(!Enum.IsDefined(typeof(LanguageVersion), (LanguageVersion)((int)latestVersion + 1)));
			parseOptions = new VisualBasicParseOptions(languageVersion: latestVersion, kind: SourceCodeKind.Script);
		}

		protected override string Logo {
			get {
				// This is how MS gets the version, see roslyn/src/Interactive/EditorFeatures/VisualBasic/Interactive/VisualBasicReplServiceProvider.vb
				return string.Format("Microsoft (R) Roslyn Visual Basic Compiler version {0}",
					FileVersionInfo.GetVersionInfo(typeof(VisualBasicCommandLineArguments).Assembly.Location).FileVersion);
			}
		}

		protected override string Help => dnSpy_Scripting_Roslyn_Resources.HelpString;
		protected override ObjectFormatter ObjectFormatter => VisualBasicObjectFormatter.Instance;
		protected override DiagnosticFormatter DiagnosticFormatter => VisualBasicDiagnosticFormatter.Instance;

		public VisualBasicControlVM(IReplEditor replEditor, IServiceLocator serviceLocator)
			: base(replEditor, serviceLocator) {
		}

		protected override Script<T> Create<T>(string code, ScriptOptions options, Type globalsType, InteractiveAssemblyLoader assemblyLoader) =>
			VisualBasicScript.Create<T>(code, options, globalsType, assemblyLoader);

		protected override bool IsCompleteSubmission(string text) =>
			SyntaxFactory.IsCompleteSubmission(SyntaxFactory.ParseSyntaxTree(text, parseOptions));
		static readonly VisualBasicParseOptions parseOptions;

		protected override void InitializeUserScriptOptions(UserScriptOptions options) {
			var rspFile = GetResponseFile("VisualBasicInteractive.rsp");
			if (rspFile == null)
				return;
			this.replEditor.OutputPrintLine(string.Format(dnSpy_Scripting_Roslyn_Resources.LoadingContextFromFile, Path.GetFileName(rspFile)));

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
