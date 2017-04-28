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
using dndbg.Engine;
using dnSpy.Contracts.Debugger.DotNet.CorDebug;
using dnSpy.Contracts.Debugger.DotNet.CorDebug.CallStack;
using dnSpy.Contracts.Metadata;

namespace dnSpy.Debugger.CorDebug.Impl.CallStack {
	sealed class DbgDotNetStackFrameLocationImpl : DbgDotNetStackFrameLocation {
		public override ModuleId Module { get; }
		public override uint Token { get; }
		public override uint ILOffset { get; }
		public override DbgILOffsetMapping ILOffsetMapping { get; }
		public override ulong NativeMethodAddress { get; }
		public override uint NativeMethodOffset { get; }
		public DnDebuggerObjectHolder<CorCode> CorCode { get; }

		public DbgDotNetStackFrameLocationImpl(ModuleId module, uint token, uint ilOffset, DbgILOffsetMapping ilOffsetMapping, DnDebuggerObjectHolder<CorCode> corCode, uint nativeOffset) {
			if (corCode == null)
				throw new ArgumentNullException(nameof(corCode));
			if (corCode.Object == null)
				throw new ArgumentException();
			if (corCode.Object.IsIL)
				throw new ArgumentException();
			Module = module;
			Token = token;
			ILOffset = ilOffset;
			ILOffsetMapping = ilOffsetMapping;
			NativeMethodAddress = corCode.Object.Address;
			NativeMethodOffset = nativeOffset;
			CorCode = corCode;
		}

		protected override void CloseCore() => CorCode.Close();
	}
}
