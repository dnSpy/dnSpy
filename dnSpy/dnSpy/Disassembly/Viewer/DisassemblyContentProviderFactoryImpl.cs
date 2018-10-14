/*
    Copyright (C) 2014-2018 de4dot@gmail.com

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
using System.ComponentModel.Composition;
using dnSpy.Contracts.Disassembly;
using dnSpy.Contracts.Disassembly.Viewer;

namespace dnSpy.Disassembly.Viewer {
	[Export(typeof(DisassemblyContentProviderFactory))]
	sealed class DisassemblyContentProviderFactoryImpl : DisassemblyContentProviderFactory {
		readonly X86DisassemblyContentProviderFactoryDependencies x86Deps;

		[ImportingConstructor]
		DisassemblyContentProviderFactoryImpl(X86DisassemblyContentProviderFactoryDependencies x86Deps) => this.x86Deps = x86Deps;

		public override DisassemblyContentProvider Create(NativeCode code, DisassemblyContentFormatterOptions formatterOptions, ISymbolResolver symbolResolver, string header) {
			if (code.Blocks == null)
				throw new ArgumentException();

			switch (code.Kind) {
			case NativeCodeKind.X86_16:
				return new X86DisassemblyContentProviderFactory(x86Deps, 16, formatterOptions, symbolResolver, header, code.Optimization, code.Blocks, code.CodeInfo, code.VariableInfo).Create();

			case NativeCodeKind.X86_32:
				return new X86DisassemblyContentProviderFactory(x86Deps, 32, formatterOptions, symbolResolver, header, code.Optimization, code.Blocks, code.CodeInfo, code.VariableInfo).Create();

			case NativeCodeKind.X86_64:
				return new X86DisassemblyContentProviderFactory(x86Deps, 64, formatterOptions, symbolResolver, header, code.Optimization, code.Blocks, code.CodeInfo, code.VariableInfo).Create();

			default:
				throw new ArgumentException();
			}
		}
	}
}
