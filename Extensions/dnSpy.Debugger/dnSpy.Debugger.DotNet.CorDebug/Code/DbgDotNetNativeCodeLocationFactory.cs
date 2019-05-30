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
using System.ComponentModel.Composition;
using dndbg.Engine;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.DotNet.Code;
using dnSpy.Contracts.Debugger.DotNet.CorDebug.Code;
using dnSpy.Contracts.Metadata;
using dnSpy.Debugger.DotNet.CorDebug.Impl;

namespace dnSpy.Debugger.DotNet.CorDebug.Code {
	abstract class DbgDotNetNativeCodeLocationFactory {
		public abstract DbgDotNetNativeCodeLocation Create(DbgModule module, ModuleId moduleId, uint token, uint ilOffset, DbgILOffsetMapping ilOffsetMapping, ulong nativeMethodAddress, ulong nativeMethodOffset, DnDebuggerObjectHolder<CorCode> corCode);
	}

	[Export(typeof(DbgDotNetNativeCodeLocationFactory))]
	sealed class DbgDotNetNativeCodeLocationFactoryImpl : DbgDotNetNativeCodeLocationFactory {
		internal Lazy<DbgManager> DbgManager { get; }

		[ImportingConstructor]
		DbgDotNetNativeCodeLocationFactoryImpl(Lazy<DbgManager> dbgManager) => DbgManager = dbgManager;

		public override DbgDotNetNativeCodeLocation Create(DbgModule module, ModuleId moduleId, uint token, uint ilOffset, DbgILOffsetMapping ilOffsetMapping, ulong nativeMethodAddress, ulong nativeMethodOffset, DnDebuggerObjectHolder<CorCode> corCode) {
			if (module is null)
				throw new ArgumentNullException(nameof(module));
			if (corCode is null)
				throw new ArgumentNullException(nameof(corCode));
			return new DbgDotNetNativeCodeLocationImpl(this, module, moduleId, token, ilOffset, ilOffsetMapping, nativeMethodAddress, nativeMethodOffset, corCode);
		}
	}
}
