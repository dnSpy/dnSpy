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
using System.ComponentModel.Composition;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.DotNet.Code;
using dnSpy.Contracts.Debugger.DotNet.Metadata.Internal;

namespace dnSpy.Debugger.DotNet.Mono.Impl {
	[Export(typeof(DbgEngineImplDependencies))]
	sealed class DbgEngineImplDependencies {
		public DbgDotNetCodeRangeService DotNetCodeRangeService { get; }
		public DebuggerSettings DebuggerSettings { get; }
		public Lazy<DbgDotNetCodeLocationFactory> DbgDotNetCodeLocationFactory { get; }
		public DbgModuleMemoryRefreshedNotifier2 DbgModuleMemoryRefreshedNotifier { get; }
		public DbgRawMetadataService RawMetadataService { get; }

		[ImportingConstructor]
		DbgEngineImplDependencies(DbgDotNetCodeRangeService dbgDotNetCodeRangeService, DebuggerSettings debuggerSettings, Lazy<DbgDotNetCodeLocationFactory> dbgDotNetCodeLocationFactory, DbgModuleMemoryRefreshedNotifier2 dbgModuleMemoryRefreshedNotifier, DbgRawMetadataService rawMetadataService) {
			DotNetCodeRangeService = dbgDotNetCodeRangeService;
			DebuggerSettings = debuggerSettings;
			DbgDotNetCodeLocationFactory = dbgDotNetCodeLocationFactory;
			DbgModuleMemoryRefreshedNotifier = dbgModuleMemoryRefreshedNotifier;
			RawMetadataService = rawMetadataService;
		}
	}
}
