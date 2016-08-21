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

namespace dndbg.Engine {
	public sealed class CorOverride {
		/// <summary>
		/// Gets the body function or null if it's not a <c>Method</c> token
		/// </summary>
		public CorFunction FunctionBody => Module.GetFunctionFromToken(BodyToken);

		/// <summary>
		/// Gets the declaration function or null if it's not a <c>Method</c> token
		/// </summary>
		public CorFunction FunctionDeclaration => Module.GetFunctionFromToken(DeclToken);

		public uint BodyToken { get; }
		public uint DeclToken { get; }
		public CorModule Module { get; }

		public CorOverride(CorModule module, uint bodyToken, uint declToken) {
			this.Module = module;
			this.BodyToken = bodyToken;
			this.DeclToken = declToken;
		}

		internal CorOverride(CorModule module, MethodOverrideInfo info) {
			this.Module = module;
			this.BodyToken = info.BodyToken;
			this.DeclToken = info.DeclToken;
		}
	}
}
