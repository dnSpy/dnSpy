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
using dnSpy.Contracts.Files.TreeView;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Languages;

namespace dnSpy.AsmEditor.Compiler {
	[Export(typeof(EditCodeVMCreator))]
	sealed class EditCodeVMCreator {
		readonly IImageManager imageManager;
		readonly IOpenFromGAC openFromGAC;
		readonly IOpenAssembly openAssembly;
		readonly ILanguageManager languageManager;
		readonly ILanguageCompilerCreator[] languageCompilerCreators;

		public bool CanCreate => TryGetUsedLanguage() != null;

		[ImportingConstructor]
		EditCodeVMCreator(IImageManager imageManager, IOpenFromGAC openFromGAC, IFileTreeView fileTreeView, ILanguageManager languageManager, [ImportMany] IEnumerable<ILanguageCompilerCreator> languageCompilerCreators) {
			this.imageManager = imageManager;
			this.openFromGAC = openFromGAC;
			this.openAssembly = new OpenAssembly(fileTreeView.FileManager);
			this.languageManager = languageManager;
			this.languageCompilerCreators = languageCompilerCreators.OrderBy(a => a.Order).ToArray();
		}

		public ImageReference? GetIcon() {
			var lang = TryGetUsedLanguage();
			Debug.Assert(lang != null);
			if (lang == null)
				return null;

			return languageCompilerCreators.FirstOrDefault(a => a.Language == lang.GenericGuid)?.Icon;
		}

		public string GetHeader() {
			var lang = TryGetUsedLanguage();
			Debug.Assert(lang != null);
			if (lang == null)
				return null;

			return string.Format(dnSpy_AsmEditor_Resources.EditMethodBodyCode, lang.GenericNameUI);
		}

		bool IsSupportedLanguage(ILanguage language) {
			if (language == null)
				return false;
			if (!language.CanDecompile(DecompilationType.TypeMethods))
				return false;
			return languageCompilerCreators.Any(a => a.Language == language.GenericGuid);
		}

		ILanguage TryGetUsedLanguage() {
			var defaultLanguage = languageManager.Language;
			if (IsSupportedLanguage(defaultLanguage))
				return defaultLanguage;
			return languageManager.AllLanguages.FirstOrDefault(a => a.GenericGuid == defaultLanguage.GenericGuid && IsSupportedLanguage(a)) ??
					languageManager.AllLanguages.FirstOrDefault(a => IsSupportedLanguage(a));
		}

		public EditCodeVM Create(MethodDef method) {
			Debug.Assert(CanCreate);
			if (!CanCreate)
				throw new InvalidOperationException();

			var language = TryGetUsedLanguage();
			Debug.Assert(language != null);
			if (language == null)
				throw new InvalidOperationException();

			var serviceCreator = languageCompilerCreators.FirstOrDefault(a => a.Language == language.GenericGuid);
			Debug.Assert(serviceCreator != null);
			if (serviceCreator == null)
				throw new InvalidOperationException();

			return new EditCodeVM(imageManager, openFromGAC, openAssembly, serviceCreator.Create(), language, method);
		}
	}
}
