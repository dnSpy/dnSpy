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

using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Text;
using System.Threading;
using dnlib.DotNet;
using dnSpy.BamlDecompiler.Baml;
using dnSpy.Contracts.Decompiler;

namespace dnSpy.BamlDecompiler {
	[Export(typeof(IBamlDecompiler))]
	sealed class BamlDecompiler : IBamlDecompiler {
		public IList<string> Decompile(ModuleDef module, byte[] data, CancellationToken token, BamlDecompilerOptions bamlDecompilerOptions, Stream output, XamlOutputOptions outputOptions) {
			var doc = BamlReader.ReadDocument(new MemoryStream(data), token);
			var asmRefs = new List<string>();
			var xaml = new XamlDecompiler().Decompile(module, doc, token, bamlDecompilerOptions, asmRefs);
			var resData = Encoding.UTF8.GetBytes(new XamlOutputCreator(outputOptions).CreateText(xaml));
			output.Write(resData, 0, resData.Length);
			return asmRefs;
		}
	}
}
