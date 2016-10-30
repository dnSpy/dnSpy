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
using System.Diagnostics;
using System.Linq;
using dnlib.DotNet;
using dnSpy.AsmEditor.Properties;
using dnSpy.AsmEditor.ViewHelpers;
using dnSpy.Contracts.App;
using dnSpy.Contracts.AsmEditor.Compiler;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Documents.TreeView;
using dnSpy.Contracts.Images;

namespace dnSpy.AsmEditor.Compiler {
	[Export(typeof(EditCodeVMCreator))]
	sealed class EditCodeVMCreator {
		readonly IOpenFromGAC openFromGAC;
		readonly IOpenAssembly openAssembly;
		readonly IDecompilerService decompilerService;
		readonly ILanguageCompilerProvider[] languageCompilerProviders;

		[ImportingConstructor]
		EditCodeVMCreator(IOpenFromGAC openFromGAC, IDocumentTreeView documentTreeView, IDecompilerService decompilerService, [ImportMany] IEnumerable<ILanguageCompilerProvider> languageCompilerProviders) {
			this.openFromGAC = openFromGAC;
			this.openAssembly = new OpenAssembly(documentTreeView.DocumentService);
			this.decompilerService = decompilerService;
			this.languageCompilerProviders = languageCompilerProviders.OrderBy(a => a.Order).ToArray();
		}

		public bool CanCreate(CompilationKind kind) => GetLanguageCompilerProvider(kind) != null;

		KeyValuePair<IDecompiler, ILanguageCompilerProvider>? GetLanguageCompilerProvider(CompilationKind kind) {
			var language = TryGetUsedLanguage();
			if (language == null)
				return null;

			var serviceCreator = languageCompilerProviders.FirstOrDefault(a => a.Language == language.GenericGuid);
			if (serviceCreator == null)
				return null;
			if (!serviceCreator.CanCompile(kind))
				return null;

			return new KeyValuePair<IDecompiler, ILanguageCompilerProvider>(language, serviceCreator);
		}

		public ImageReference? GetIcon(CompilationKind kind) {
			var info = GetLanguageCompilerProvider(kind);
			return info?.Value.Icon;
		}

		public string GetHeader(CompilationKind kind) {
			var info = GetLanguageCompilerProvider(kind);
			if (info == null)
				return null;
			return string.Format(dnSpy_AsmEditor_Resources.EditMethodBodyCode, info.Value.Key.GenericNameUI);
		}

		bool IsSupportedLanguage(IDecompiler decompiler) {
			if (decompiler == null)
				return false;
			if (!decompiler.CanDecompile(DecompilationType.TypeMethods))
				return false;
			return languageCompilerProviders.Any(a => a.Language == decompiler.GenericGuid);
		}

		IDecompiler TryGetUsedLanguage() {
			var defaultDecompiler = decompilerService.Decompiler;
			if (IsSupportedLanguage(defaultDecompiler))
				return defaultDecompiler;
			return decompilerService.AllDecompilers.FirstOrDefault(a => a.GenericGuid == defaultDecompiler.GenericGuid && IsSupportedLanguage(a)) ??
					decompilerService.AllDecompilers.FirstOrDefault(a => IsSupportedLanguage(a));
		}

		public EditCodeVM CreateEditMethodCode(MethodDef method, IList<MethodSourceStatement> statements) {
			var info = GetLanguageCompilerProvider(CompilationKind.Method);
			if (info == null)
				throw new InvalidOperationException();
			return new EditMethodCodeVM(openFromGAC, openAssembly, info.Value.Value.Create(CompilationKind.Method), info.Value.Key, method, statements);
		}
	}
}
