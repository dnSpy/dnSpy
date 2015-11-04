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
using dnlib.DotNet;

namespace dnSpy.Decompiler {
	public class TokenReference : IEquatable<TokenReference> {
		public readonly ModuleDef ModuleDef;
		public readonly uint Token;

		public TokenReference(IMemberRef mr)
			: this(mr.Module, mr.MDToken.Raw) {
		}

		public TokenReference(ModuleDef module, uint token) {
			this.ModuleDef = module;
			this.Token = token;
		}

		public bool Equals(TokenReference other) {
			return other != null &&
				Token == other.Token &&
				ModuleDef == other.ModuleDef;
		}

		public override bool Equals(object obj) {
			return Equals(obj as TokenReference);
		}

		public override int GetHashCode() {
			return (ModuleDef == null ? 0 : ModuleDef.GetHashCode()) ^ (int)Token;
		}
	}
}
