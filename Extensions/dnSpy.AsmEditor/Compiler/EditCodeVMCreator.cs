/*
    Copyright (C) 2014-2017 de4dot@gmail.com

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
		readonly IRawModuleBytesProvider rawModuleBytesProvider;
		readonly IOpenFromGAC openFromGAC;
		readonly IOpenAssembly openAssembly;
		readonly IDecompilerService decompilerService;
		readonly ILanguageCompilerProvider[] languageCompilerProviders;

		[ImportingConstructor]
		EditCodeVMCreator(IRawModuleBytesProvider rawModuleBytesProvider, IOpenFromGAC openFromGAC, IDocumentTreeView documentTreeView, IDecompilerService decompilerService, [ImportMany] IEnumerable<ILanguageCompilerProvider> languageCompilerProviders) {
			this.rawModuleBytesProvider = rawModuleBytesProvider;
			this.openFromGAC = openFromGAC;
			openAssembly = new OpenAssembly(documentTreeView.DocumentService);
			this.decompilerService = decompilerService;
			this.languageCompilerProviders = languageCompilerProviders.OrderBy(a => a.Order).ToArray();
		}

		public bool CanCreate(CompilationKind kind) => GetLanguageCompilerProvider(kind) != null;

		KeyValuePair<IDecompiler, ILanguageCompilerProvider>? GetLanguageCompilerProvider(CompilationKind kind) {
			var language = TryGetUsedLanguage(kind);
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
			switch (kind) {
			case CompilationKind.Assembly:		return string.Format(dnSpy_AsmEditor_Resources.EditAssemblyCode2, info.Value.Key.GenericNameUI);
			case CompilationKind.Method:		return string.Format(dnSpy_AsmEditor_Resources.EditMethodBodyCode, info.Value.Key.GenericNameUI);
			case CompilationKind.AddClass:		return string.Format(dnSpy_AsmEditor_Resources.EditCodeAddClass2, info.Value.Key.GenericNameUI);
			case CompilationKind.EditClass:		return string.Format(dnSpy_AsmEditor_Resources.EditCodeEditClass2, info.Value.Key.GenericNameUI);
			default: throw new ArgumentOutOfRangeException(nameof(kind));
			}
		}

		bool IsSupportedLanguage(IDecompiler decompiler, CompilationKind kind) {
			if (decompiler == null)
				return false;

			switch (kind) {
			case CompilationKind.Assembly:
				if (!decompiler.CanDecompile(DecompilationType.AssemblyInfo))
					return false;
				break;

			case CompilationKind.Method:
			case CompilationKind.EditClass:
				if (!decompiler.CanDecompile(DecompilationType.TypeMethods))
					return false;
				break;

			case CompilationKind.AddClass:
				break;

			default:
				throw new ArgumentOutOfRangeException(nameof(kind));
			}

			return languageCompilerProviders.Any(a => a.Language == decompiler.GenericGuid);
		}

		IDecompiler TryGetUsedLanguage(CompilationKind kind) {
			var defaultDecompiler = decompilerService.Decompiler;
			if (IsSupportedLanguage(defaultDecompiler, kind))
				return defaultDecompiler;
			return decompilerService.AllDecompilers.FirstOrDefault(a => a.GenericGuid == defaultDecompiler.GenericGuid && IsSupportedLanguage(a, kind)) ??
					decompilerService.AllDecompilers.FirstOrDefault(a => IsSupportedLanguage(a, kind));
		}

		public EditCodeVM CreateEditMethodCode(MethodDef method, IList<MethodSourceStatement> statements) {
			var info = GetLanguageCompilerProvider(CompilationKind.Method);
			if (info == null)
				throw new InvalidOperationException();
			return new EditMethodCodeVM(rawModuleBytesProvider, openFromGAC, openAssembly, info.Value.Value.Create(CompilationKind.Method), info.Value.Key, method, statements);
		}

		public EditCodeVM CreateEditAssembly(ModuleDef module) {
			var info = GetLanguageCompilerProvider(CompilationKind.Assembly);
			if (info == null)
				throw new InvalidOperationException();
			return new EditAssemblyVM(rawModuleBytesProvider, openFromGAC, openAssembly, info.Value.Value.Create(CompilationKind.Assembly), info.Value.Key, module);
		}

		public EditCodeVM CreateAddClass(ModuleDef module) {
			var info = GetLanguageCompilerProvider(CompilationKind.AddClass);
			if (info == null)
				throw new InvalidOperationException();
			return new AddClassVM(rawModuleBytesProvider, openFromGAC, openAssembly, info.Value.Value.Create(CompilationKind.AddClass), info.Value.Key, module);
		}

		public EditCodeVM CreateEditClass(IMemberDef def, IList<MethodSourceStatement> statements) {
			var info = GetLanguageCompilerProvider(CompilationKind.EditClass);
			if (info == null)
				throw new InvalidOperationException();
			return new EditClassVM(rawModuleBytesProvider, openFromGAC, openAssembly, info.Value.Value.Create(CompilationKind.EditClass), info.Value.Key, def, statements);
		}
	}
}
