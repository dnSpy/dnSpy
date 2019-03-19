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
using dnlib.DotNet;

namespace dnSpy.Contracts.Metadata {
	/// <summary>
	/// <see cref="ModuleId"/> and token
	/// </summary>
	public readonly struct ModuleTokenId : IEquatable<ModuleTokenId> {
		/// <summary>
		/// Gets the module id
		/// </summary>
		public ModuleId Module => module;
		readonly ModuleId module;

		/// <summary>
		/// Gets the token in the module
		/// </summary>
		public uint Token => token;
		readonly uint token;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="module">Module id</param>
		/// <param name="mdToken">Token</param>
		public ModuleTokenId(ModuleId module, MDToken mdToken)
			: this(module, mdToken.Raw) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="module">Module id</param>
		/// <param name="token">Token</param>
		public ModuleTokenId(ModuleId module, uint token) {
			this.module = module;
			this.token = token;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="module">Module id</param>
		/// <param name="token">Token</param>
		public ModuleTokenId(ModuleId module, int token) {
			this.module = module;
			this.token = (uint)token;
		}

		/// <summary>
		/// Equals()
		/// </summary>
		/// <param name="other">Other instance</param>
		/// <returns></returns>
		public bool Equals(ModuleTokenId other) => token == other.token && module.Equals(other.module);

		/// <summary>
		/// Equals()
		/// </summary>
		/// <param name="obj">Object</param>
		/// <returns></returns>
		public override bool Equals(object obj) => obj is ModuleTokenId && Equals((ModuleTokenId)obj);

		/// <summary>
		/// GetHashCode()
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode() => module.GetHashCode() ^ (int)token;

		/// <summary>
		/// ToString()
		/// </summary>
		/// <returns></returns>
		public override string ToString() => token.ToString("X8") + " " + module.ToString();
	}
}
