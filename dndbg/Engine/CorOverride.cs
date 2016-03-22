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
		public CorFunction FunctionBody {
			get { return module.GetFunctionFromToken(bodyToken); }
		}

		/// <summary>
		/// Gets the declaration function or null if it's not a <c>Method</c> token
		/// </summary>
		public CorFunction FunctionDeclaration {
			get { return module.GetFunctionFromToken(declToken); }
		}

		public uint BodyToken {
			get { return bodyToken; }
		}
		readonly uint bodyToken;

		public uint DeclToken {
			get { return declToken; }
		}
		readonly uint declToken;

		public CorModule Module {
			get { return module; }
		}
		readonly CorModule module;

		public CorOverride(CorModule module, uint bodyToken, uint declToken) {
			this.module = module;
			this.bodyToken = bodyToken;
			this.declToken = declToken;
		}

		internal CorOverride(CorModule module, MethodOverrideInfo info) {
			this.module = module;
			this.bodyToken = info.BodyToken;
			this.declToken = info.DeclToken;
		}
	}
}
