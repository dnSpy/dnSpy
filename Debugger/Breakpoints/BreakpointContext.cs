/*
    Copyright (C) 2014-2015 de4dot@gmail.com

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
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Languages;

namespace dnSpy.Debugger.Breakpoints {
	interface IBreakpointContext {
		IImageManager ImageManager { get; }
		ILanguage Language { get; }
		ILanguageManager LanguageManager { get; }
		IModuleLoader ModuleLoader { get; }
		bool SyntaxHighlight { get; }
		bool UseHexadecimal { get; }
		bool ShowTokens { get; }
	}

	sealed class BreakpointContext : IBreakpointContext {
		public IImageManager ImageManager { get; private set; }
		public ILanguage Language { get; set; }
		public ILanguageManager LanguageManager { get; private set; }
		public bool SyntaxHighlight { get; set; }
		public bool UseHexadecimal { get; set; }
		public bool ShowTokens { get; set; }

		public IModuleLoader ModuleLoader {
			get { return moduleLoader.Value; }
		}
		readonly Lazy<IModuleLoader> moduleLoader;

		public BreakpointContext(IImageManager imageManager, ILanguageManager languageManager, Lazy<IModuleLoader> moduleLoader) {
			this.ImageManager = imageManager;
			this.LanguageManager = languageManager;
			this.moduleLoader = moduleLoader;
		}
	}
}
