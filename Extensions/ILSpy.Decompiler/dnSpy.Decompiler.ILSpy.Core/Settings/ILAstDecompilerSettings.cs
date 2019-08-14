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

#if DEBUG
using System;
using System.Collections.Generic;
using dnSpy.Contracts.Decompiler;

namespace dnSpy.Decompiler.ILSpy.Core.Settings {
	sealed class ILAstDecompilerSettings : DecompilerSettingsBase {
		public override int Version => 0;
		public override event EventHandler? VersionChanged { add { } remove { } }

		public ILAstDecompilerSettings() {
		}

		ILAstDecompilerSettings(ILAstDecompilerSettings other) {
		}

		public override DecompilerSettingsBase Clone() => new ILAstDecompilerSettings(this);

		public override IEnumerable<IDecompilerOption> Options {
			get { yield break; }
		}

		public override bool Equals(object? obj) => obj is ILAstDecompilerSettings;
		public override int GetHashCode() => 0;
	}
}
#endif
