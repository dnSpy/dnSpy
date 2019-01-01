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
using dndbg.Engine;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.Code;
using dnSpy.Contracts.Debugger.DotNet.Code;
using dnSpy.Contracts.Debugger.DotNet.CorDebug.Code;
using dnSpy.Contracts.Metadata;
using dnSpy.Debugger.DotNet.CorDebug.Impl;

namespace dnSpy.Debugger.DotNet.CorDebug.Code {
	sealed class DbgDotNetNativeCodeLocationImpl : DbgDotNetNativeCodeLocation {
		public override string Type => PredefinedDbgCodeLocationTypes.DotNetCorDebugNative;
		public override ModuleId Module { get; }
		public override uint Token { get; }
		public override uint Offset { get; }
		public override DbgILOffsetMapping ILOffsetMapping { get; }
		public DnDebuggerObjectHolder<CorCode> CorCode { get; }
		public override DbgModule DbgModule { get; }
		public override DbgDotNetNativeFunctionAddress NativeAddress { get; }

		internal DbgBreakpointLocationFormatterImpl Formatter { get; set; }

		readonly DbgDotNetNativeCodeLocationFactoryImpl owner;

		public DbgDotNetNativeCodeLocationImpl(DbgDotNetNativeCodeLocationFactoryImpl owner, DbgModule module, ModuleId moduleId, uint token, uint offset, DbgILOffsetMapping ilOffsetMapping, ulong nativeMethodAddress, ulong nativeMethodOffset, DnDebuggerObjectHolder<CorCode> corCode) {
			this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
			Module = moduleId;
			Token = token;
			Offset = offset;
			ILOffsetMapping = ilOffsetMapping;
			NativeAddress = new DbgDotNetNativeFunctionAddress(nativeMethodAddress, nativeMethodOffset);
			CorCode = corCode ?? throw new ArgumentNullException(nameof(corCode));
			DbgModule = module ?? throw new ArgumentNullException(nameof(module));
		}

		public override DbgCodeLocation Clone() =>
			owner.Create(DbgModule, Module, Token, Offset, ILOffsetMapping, NativeAddress.Address, NativeAddress.Offset, CorCode.AddRef());

		public override void Close() => owner.DbgManager.Value.Close(this);
		protected override void CloseCore(DbgDispatcher dispatcher) => CorCode.Close();

		public override bool Equals(object obj) =>
			obj is DbgDotNetNativeCodeLocationImpl other &&
			CorCode.Object == other.CorCode.Object &&
			Module == other.Module &&
			Token == other.Token &&
			Offset == other.Offset &&
			ILOffsetMapping == other.ILOffsetMapping &&
			NativeAddress.Address == other.NativeAddress.Address &&
			NativeAddress.Offset == other.NativeAddress.Offset;

		public override int GetHashCode() => Module.GetHashCode() ^ (int)Token ^ (int)Offset ^ (int)ILOffsetMapping ^ NativeAddress.Address.GetHashCode() ^ NativeAddress.Offset.GetHashCode() ^ CorCode.HashCode;
	}
}
