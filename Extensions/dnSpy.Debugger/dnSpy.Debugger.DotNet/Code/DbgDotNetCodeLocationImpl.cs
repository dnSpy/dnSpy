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
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.Code;
using dnSpy.Contracts.Debugger.DotNet.Code;
using dnSpy.Contracts.Metadata;

namespace dnSpy.Debugger.DotNet.Code {
	sealed class DbgDotNetCodeLocationImpl : DbgDotNetCodeLocation {
		public override string Type => PredefinedDbgCodeLocationTypes.DotNet;
		public override ModuleId Module { get; }
		public override uint Token { get; }
		public override uint Offset { get; }
		public override DbgILOffsetMapping ILOffsetMapping { get; }
		public override DbgModule DbgModule => null;
		public override DbgDotNetNativeFunctionAddress NativeAddress => DbgDotNetNativeFunctionAddress.None;

		internal DbgBreakpointLocationFormatterImpl Formatter { get; set; }
		readonly DbgDotNetCodeLocationFactoryImpl factory;

		public DbgDotNetCodeLocationImpl(DbgDotNetCodeLocationFactoryImpl factory, ModuleId module, uint token, uint offset, DbgILOffsetMapping ilOffsetMapping) {
			this.factory = factory ?? throw new ArgumentNullException(nameof(factory));
			Module = module;
			Token = token;
			Offset = offset;
			ILOffsetMapping = ilOffsetMapping;
		}

		public override DbgCodeLocation Clone() => factory.Create(Module, Token, Offset, ILOffsetMapping);
		public override void Close() => factory.DbgManager.Value.Close(this);
		protected override void CloseCore(DbgDispatcher dispatcher) { }

		public override bool Equals(object obj) =>
			obj is DbgDotNetCodeLocationImpl other &&
			Module == other.Module &&
			Token == other.Token &&
			Offset == other.Offset;

		public override int GetHashCode() => Module.GetHashCode() ^ (int)Token ^ (int)Offset;
	}
}
