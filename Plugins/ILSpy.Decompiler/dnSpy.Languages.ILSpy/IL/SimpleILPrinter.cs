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

using dnlib.DotNet;
using dnSpy.Contracts.Languages;
using ICSharpCode.Decompiler.Disassembler;
using dnSpy.Contracts.Text;
using System.ComponentModel.Composition;
using dnSpy.Languages.ILSpy.Core.IL;
using dnSpy.Contracts.Decompiler;

namespace dnSpy.Languages.ILSpy.IL {
	[Export(typeof(ISimpleILPrinter))]
	sealed class SimpleILPrinter : ISimpleILPrinter {
		double ISimpleILPrinter.Order => -100;

		bool ISimpleILPrinter.Write(IDecompilerOutput output, IMemberRef member) => ILLanguageUtils.Write(output, member);
		void ISimpleILPrinter.Write(IDecompilerOutput output, MethodSig sig) => output.Write(sig);
		void ISimpleILPrinter.Write(IDecompilerOutput output, TypeSig type) => type.WriteTo(output);
	}
}
