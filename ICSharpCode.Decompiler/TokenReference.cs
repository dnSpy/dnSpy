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
		public readonly string Filename;
		public readonly uint Token;

		public TokenReference(IMemberRef mr)
			: this(mr.Module == null ? null : mr.Module.Location, mr.MDToken.Raw) {
		}

		public TokenReference(string filename, uint token) {
			this.Filename = filename;
			this.Token = token;
		}

		public bool Equals(TokenReference other) {
			return other != null &&
				Token == other.Token &&
				StringComparer.OrdinalIgnoreCase.Equals(Filename, other.Filename);
		}

		public override bool Equals(object obj) {
			return Equals(obj as TokenReference);
		}

		public override int GetHashCode() {
			return (Filename ?? string.Empty).GetHashCode() ^ (int)Token;
		}
	}
}
