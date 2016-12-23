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
using dnlib.DotNet;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Text;

namespace dnSpy.Decompiler {
	sealed class DummyDecompiler : DecompilerBase {
		public override string FileExtension => ".---";
		public override Guid GenericGuid => new Guid("CAE0EC7B-4311-4C48-AF7C-36E5EA71249A");
		public override string ContentTypeString => ContentTypes.PlainText;
		public override string GenericNameUI => "---";
		public override double OrderUI => double.MaxValue;
		public override Guid UniqueGuid => new Guid("E4E6F1AA-FF88-48BC-B44C-49585E66DCF0");
		public override string UniqueNameUI => "---";
		public override DecompilerSettingsBase Settings { get; }

		sealed class DummySettings : DecompilerSettingsBase {
			public override DecompilerSettingsBase Clone() => new DummySettings();

			public override IEnumerable<IDecompilerOption> Options {
				get { yield break; }
			}

			protected override bool EqualsCore(object obj) => obj is DummySettings;
			protected override int GetHashCodeCore() => 0;
		}

		public DummyDecompiler() {
			Settings = new DummySettings();
		}

		public override void Decompile(MethodDef method, IDecompilerOutput output, DecompilationContext ctx) => WriteError(output);
		public override void Decompile(PropertyDef property, IDecompilerOutput output, DecompilationContext ctx) => WriteError(output);
		public override void Decompile(FieldDef field, IDecompilerOutput output, DecompilationContext ctx) => WriteError(output);
		public override void Decompile(EventDef ev, IDecompilerOutput output, DecompilationContext ctx) => WriteError(output);
		public override void Decompile(TypeDef type, IDecompilerOutput output, DecompilationContext ctx) => WriteError(output);
		public override void DecompileNamespace(string @namespace, IEnumerable<TypeDef> types, IDecompilerOutput output, DecompilationContext ctx) => WriteError(output);
		public override void Decompile(AssemblyDef asm, IDecompilerOutput output, DecompilationContext ctx) => WriteError(output);
		public override void Decompile(ModuleDef mod, IDecompilerOutput output, DecompilationContext ctx) => WriteError(output);

		// Should not be localized
		static readonly string errorText =
			"The decompiler extension wasn't built. Make sure you build every project before you press F5." + Environment.NewLine +
			"Uncheck: Settings -> Projects and Solutions -> Build and Run -> Only build startup projects and dependencies on Run" + Environment.NewLine;
		void WriteError(IDecompilerOutput output) =>
			output.Write(errorText, BoxedTextColor.Error);
	}
}
