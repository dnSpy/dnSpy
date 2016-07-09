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

using System.IO;
using System.Text;
using dnlib.DotNet;
using dnSpy.Contracts.Languages;
using dnSpy.Decompiler.Shared;
using dnSpy.Languages.Properties;

namespace dnSpy.Languages.MSBuild {
	class TypeProjectFile : ProjectFile {
		public override string Description => string.Format(dnSpy_Languages_Resources.MSBuild_DecompileType, Type.FullName);
		public override BuildAction BuildAction => BuildAction.Compile;
		public override string Filename => filename;
		readonly string filename;

		protected readonly DecompilationContext decompilationContext;
		protected readonly ILanguage language;

		public TypeDef Type { get; }

		public TypeProjectFile(TypeDef type, string filename, DecompilationContext decompilationContext, ILanguage language) {
			this.Type = type;
			this.filename = filename;
			this.decompilationContext = decompilationContext;
			this.language = language;
		}

		public override void Create(DecompileContext ctx) {
			using (var writer = new StreamWriter(Filename, false, Encoding.UTF8)) {
				var output = new PlainTextOutput(writer);
				Decompile(ctx, output);
			}
		}

		protected virtual void Decompile(DecompileContext ctx, ITextOutput output) =>
			language.Decompile(Type, output, decompilationContext);
	}
}
