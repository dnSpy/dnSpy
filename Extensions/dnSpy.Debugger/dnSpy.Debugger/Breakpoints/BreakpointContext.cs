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
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Images;

namespace dnSpy.Debugger.Breakpoints {
	interface IBreakpointContext {
		IImageService ImageService { get; }
		IDecompiler Decompiler { get; }
		IModuleLoader ModuleLoader { get; }
		bool SyntaxHighlight { get; }
		bool UseHexadecimal { get; }
		bool ShowTokens { get; }
		bool ShowModuleNames { get; }
		bool ShowParameterTypes { get; }
		bool ShowParameterNames { get; }
		bool ShowOwnerTypes { get; }
		bool ShowReturnTypes { get; }
		bool ShowNamespaces { get; }
		bool ShowTypeKeywords { get; }
	}

	sealed class BreakpointContext : IBreakpointContext {
		public IImageService ImageService { get; }
		public IDecompiler Decompiler { get; set; }
		public bool SyntaxHighlight { get; set; }
		public bool UseHexadecimal { get; set; }
		public bool ShowTokens { get; set; }
		public bool ShowModuleNames { get; set; }
		public bool ShowParameterTypes { get; set; }
		public bool ShowParameterNames { get; set; }
		public bool ShowOwnerTypes { get; set; }
		public bool ShowReturnTypes { get; set; }
		public bool ShowNamespaces { get; set; }
		public bool ShowTypeKeywords { get; set; }

		public IModuleLoader ModuleLoader => moduleLoader.Value;
		readonly Lazy<IModuleLoader> moduleLoader;

		public BreakpointContext(IImageService imageService, Lazy<IModuleLoader> moduleLoader) {
			this.ImageService = imageService;
			this.moduleLoader = moduleLoader;
		}
	}
}
