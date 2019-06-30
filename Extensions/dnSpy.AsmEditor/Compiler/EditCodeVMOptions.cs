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
using dnlib.DotNet;
using dnSpy.AsmEditor.ViewHelpers;
using dnSpy.Contracts.App;
using dnSpy.Contracts.AsmEditor.Compiler;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.MVVM;

namespace dnSpy.AsmEditor.Compiler {
	readonly struct EditCodeVMOptions {
		public readonly RawModuleBytesProvider RawModuleBytesProvider;
		public readonly IOpenFromGAC OpenFromGAC;
		public readonly IOpenAssembly OpenAssembly;
		public readonly IPickFilename PickFilename;
		public readonly ILanguageCompiler LanguageCompiler;
		public readonly IDecompiler Decompiler;
		public readonly ModuleDef SourceModule;
		public readonly ImageReference AddDocumentsImage;

		public EditCodeVMOptions(RawModuleBytesProvider rawModuleBytesProvider, IOpenFromGAC openFromGAC, IOpenAssembly openAssembly, IPickFilename pickFilename, ILanguageCompiler languageCompiler, IDecompiler decompiler, ModuleDef sourceModule, ImageReference addDocumentsImage) {
			RawModuleBytesProvider = rawModuleBytesProvider ?? throw new ArgumentNullException(nameof(rawModuleBytesProvider));
			OpenFromGAC = openFromGAC ?? throw new ArgumentNullException(nameof(openFromGAC));
			OpenAssembly = openAssembly ?? throw new ArgumentNullException(nameof(openAssembly));
			PickFilename = pickFilename ?? throw new ArgumentNullException(nameof(pickFilename));
			LanguageCompiler = languageCompiler ?? throw new ArgumentNullException(nameof(languageCompiler));
			Decompiler = decompiler ?? throw new ArgumentNullException(nameof(decompiler));
			SourceModule = sourceModule ?? throw new ArgumentNullException(nameof(sourceModule));
			AddDocumentsImage = addDocumentsImage;
		}
	}
}
