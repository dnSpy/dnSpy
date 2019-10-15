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
using Iced.Intel;

namespace dnSpy.Disassembly.Viewer.X86 {
	sealed class MnemonicReference {
		public Code Code { get; }
		public string Mnemonic { get; }
		public CpuidFeature[] CpuidFeatures { get; }

		public MnemonicReference(Code code, string mnemonic, CpuidFeature[] cpuidFeatures) {
			Code = code;
			Mnemonic = mnemonic;
			CpuidFeatures = cpuidFeatures;
		}

		public override bool Equals(object? obj) =>
			obj is MnemonicReference other &&
			// Code is not compared, eg. there are many Code values for 'mov' but they
			// should all be considered equal when moving from ref to ref (eg. hit tab)
			other.Mnemonic == Mnemonic;

		public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Mnemonic);
	}
}
