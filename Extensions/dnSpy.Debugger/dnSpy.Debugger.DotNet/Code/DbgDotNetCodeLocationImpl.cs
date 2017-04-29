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
using dnSpy.Contracts.Debugger.Code;
using dnSpy.Contracts.Debugger.DotNet.Code;
using dnSpy.Contracts.Metadata;

namespace dnSpy.Debugger.DotNet.Code {
	sealed class DbgDotNetCodeLocationImpl : DbgDotNetCodeLocation {
		public override string Type => PredefinedDbgCodeLocationTypes.DotNet;
		public override ModuleId Module { get; }
		public override uint Token { get; }
		public override uint Offset { get; }

		internal DbgBreakpointLocationFormatterImpl Formatter { get; set; }
		readonly DbgDotNetCodeLocationFactory factory;

		public DbgDotNetCodeLocationImpl(DbgDotNetCodeLocationFactory factory, ModuleId module, uint token, uint offset) {
			this.factory = factory ?? throw new ArgumentNullException(nameof(factory));
			Module = module;
			Token = token;
			Offset = offset;
		}

		public override DbgCodeLocation Clone() => factory.Create(Module, Token, Offset);

		protected override void CloseCore() { }

		public override bool Equals(object obj) =>
			obj is DbgDotNetCodeLocationImpl other &&
			Module == other.Module &&
			Token == other.Token &&
			Offset == other.Offset;

		public override int GetHashCode() => Module.GetHashCode() ^ (int)Token ^ (int)Offset;
	}
}
