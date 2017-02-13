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

using System;
using dnlib.DotNet;

namespace dnSpy.Contracts.Decompiler {
	/// <summary>
	/// Token reference
	/// </summary>
	public class TokenReference : IEquatable<TokenReference> {
		/// <summary>
		/// Owner module
		/// </summary>
		public ModuleDef ModuleDef { get; }

		/// <summary>
		/// Token
		/// </summary>
		public uint Token { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="reference">Reference</param>
		public TokenReference(IMemberRef reference)
			: this(reference.Module, reference.MDToken.Raw) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="module">Owner module</param>
		/// <param name="token">Token</param>
		public TokenReference(ModuleDef module, uint token) {
			ModuleDef = module ?? throw new ArgumentNullException(nameof(module));
			Token = token;
		}

		/// <summary>
		/// Equals()
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public bool Equals(TokenReference other) => other != null && Token == other.Token && ModuleDef == other.ModuleDef;

		/// <summary>
		/// Equals()
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public override bool Equals(object obj) => Equals(obj as TokenReference);

		/// <summary>
		/// Equals()
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode() => (ModuleDef == null ? 0 : ModuleDef.GetHashCode()) ^ (int)Token;

		/// <summary>
		/// ToString()
		/// </summary>
		/// <returns></returns>
		public override string ToString() => new MDToken(Token).ToString() + " " + ModuleDef.ToString();
	}
}
