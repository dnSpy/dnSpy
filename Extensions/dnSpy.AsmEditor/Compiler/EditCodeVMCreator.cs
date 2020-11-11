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
using System.Linq;
using dnlib.DotNet;
using dnSpy.AsmEditor.Properties;
using dnSpy.AsmEditor.ViewHelpers;
using dnSpy.Contracts.App;
using dnSpy.Contracts.AsmEditor.Compiler;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Documents.TreeView;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.MVVM;

namespace dnSpy.AsmEditor.Compiler {
	[Export(typeof(EditCodeVMCreator))]
	sealed class EditCodeVMCreator {
		readonly RawModuleBytesProvider rawModuleBytesProvider;
		readonly IOpenFromGAC openFromGAC;
		readonly IOpenAssembly openAssembly;
		readonly IPickFilename pickFilename;
		readonly IDecompilerService decompilerService;
		readonly ILanguageCompilerProvider[] languageCompilerProviders;

		[ImportingConstructor]
		EditCodeVMCreator(RawModuleBytesProvider rawModuleBytesProvider, IOpenFromGAC openFromGAC, IPickFilename pickFilename, IDocumentTreeView documentTreeView, IDecompilerService decompilerService, [ImportMany] IEnumerable<ILanguageCompilerProvider> languageCompilerProviders) {
			this.rawModuleBytesProvider = rawModuleBytesProvider;
			this.openFromGAC = openFromGAC;
			openAssembly = new OpenAssembly(documentTreeView.DocumentService);
			this.pickFilename = pickFilename;
			this.decompilerService = decompilerService;
			this.languageCompilerProviders = languageCompilerProviders.OrderBy(a => a.Order).ToArray();
		}

		public bool CanCreate(CompilationKind kind) => GetLanguageCompilerProvider(kind) is not null;

		(IDecompiler decompiler, ILanguageCompilerProvider languageCompilerProvider)? GetLanguageCompilerProvider(CompilationKind kind) {
			var language = TryGetUsedLanguage(kind);
			if (language is null)
				return null;

			var serviceCreator = languageCompilerProviders.FirstOrDefault(a => a.Language == language.GenericGuid);
			if (serviceCreator is null)
				return null;
			if (!serviceCreator.CanCompile(kind))
				return null;

			return (language, serviceCreator);
		}

		public ImageReference? GetIcon(CompilationKind kind) {
			var info = GetLanguageCompilerProvider(kind);
			return info?.languageCompilerProvider.Icon;
		}

		public string? GetHeader(CompilationKind kind) {
			var info = GetLanguageCompilerProvider(kind);
			if (info is null)
				return null;
			switch (kind) {
			case CompilationKind.EditAssembly:	return string.Format(dnSpy_AsmEditor_Resources.EditAssemblyCode2, info.Value.decompiler.GenericNameUI);
			case CompilationKind.EditMethod:	return string.Format(dnSpy_AsmEditor_Resources.EditMethodBodyCode, info.Value.decompiler.GenericNameUI);
			case CompilationKind.AddClass:		return string.Format(dnSpy_AsmEditor_Resources.EditCodeAddClass2, info.Value.decompiler.GenericNameUI);
			case CompilationKind.EditClass:		return string.Format(dnSpy_AsmEditor_Resources.EditCodeEditClass2, info.Value.decompiler.GenericNameUI);
			case CompilationKind.AddMembers:	return string.Format(dnSpy_AsmEditor_Resources.EditCodeAddClassMembers2, info.Value.decompiler.GenericNameUI);
			default: throw new ArgumentOutOfRangeException(nameof(kind));
			}
		}

		bool IsSupportedLanguage(IDecompiler decompiler, CompilationKind kind) {
			if (decompiler is null)
				return false;

			switch (kind) {
			case CompilationKind.EditAssembly:
				if (!decompiler.CanDecompile(DecompilationType.AssemblyInfo))
					return false;
				break;

			case CompilationKind.EditMethod:
			case CompilationKind.EditClass:
			case CompilationKind.AddMembers:
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

		IDecompiler? TryGetUsedLanguage(CompilationKind kind) {
			var defaultDecompiler = decompilerService.Decompiler;
			if (IsSupportedLanguage(defaultDecompiler, kind))
				return defaultDecompiler;
			return decompilerService.AllDecompilers.FirstOrDefault(a => a.GenericGuid == defaultDecompiler.GenericGuid && IsSupportedLanguage(a, kind)) ??
					decompilerService.AllDecompilers.FirstOrDefault(a => IsSupportedLanguage(a, kind));
		}

		ImageReference GetAddDocumentsImage(ILanguageCompilerProvider languageCompilerProvider) =>
			languageCompilerProvider.Icon ?? DsImages.CSFileNode;

		public EditCodeVM CreateEditMethodCode(MethodDef method, IList<MethodSourceStatement> statements) {
			var info = GetLanguageCompilerProvider(CompilationKind.EditMethod);
			if (info is null)
				throw new InvalidOperationException();
			var options = new EditCodeVMOptions(
				rawModuleBytesProvider,
				openFromGAC,
				openAssembly,
				pickFilename,
				info.Value.languageCompilerProvider.Create(CompilationKind.EditMethod),
				info.Value.decompiler,
				method.Module,
				GetAddDocumentsImage(info.Value.languageCompilerProvider)
			);
			return new EditMethodCodeVM(options, method, statements);
		}

		public EditCodeVM CreateEditAssembly(ModuleDef module) {
			var info = GetLanguageCompilerProvider(CompilationKind.EditAssembly);
			if (info is null)
				throw new InvalidOperationException();
			var options = new EditCodeVMOptions(
				rawModuleBytesProvider,
				openFromGAC,
				openAssembly,
				pickFilename,
				info.Value.languageCompilerProvider.Create(CompilationKind.EditAssembly),
				info.Value.decompiler,
				module,
				GetAddDocumentsImage(info.Value.languageCompilerProvider)
			);
			return new EditAssemblyVM(options);
		}

		public EditCodeVM CreateAddClass(ModuleDef module) {
			var info = GetLanguageCompilerProvider(CompilationKind.AddClass);
			if (info is null)
				throw new InvalidOperationException();
			var options = new EditCodeVMOptions(
				rawModuleBytesProvider,
				openFromGAC,
				openAssembly,
				pickFilename,
				info.Value.languageCompilerProvider.Create(CompilationKind.AddClass),
				info.Value.decompiler,
				module,
				GetAddDocumentsImage(info.Value.languageCompilerProvider)
			);
			return new AddClassVM(options);
		}

		public EditCodeVM CreateEditClass(IMemberDef def, IList<MethodSourceStatement> statements) {
			var info = GetLanguageCompilerProvider(CompilationKind.EditClass);
			if (info is null)
				throw new InvalidOperationException();
			var options = new EditCodeVMOptions(
				rawModuleBytesProvider,
				openFromGAC,
				openAssembly,
				pickFilename,
				info.Value.languageCompilerProvider.Create(CompilationKind.EditClass),
				info.Value.decompiler,
				def.Module,
				GetAddDocumentsImage(info.Value.languageCompilerProvider)
			);
			return new EditClassVM(options, def, statements);
		}

		public EditCodeVM CreateAddMembers(IMemberDef def) {
			var info = GetLanguageCompilerProvider(CompilationKind.AddMembers);
			if (info is null)
				throw new InvalidOperationException();
			var options = new EditCodeVMOptions(
				rawModuleBytesProvider,
				openFromGAC,
				openAssembly,
				pickFilename,
				info.Value.languageCompilerProvider.Create(CompilationKind.AddMembers),
				info.Value.decompiler,
				def.Module,
				GetAddDocumentsImage(info.Value.languageCompilerProvider)
			);
			return new AddMembersCodeVM(options, def);
		}
	}
}
