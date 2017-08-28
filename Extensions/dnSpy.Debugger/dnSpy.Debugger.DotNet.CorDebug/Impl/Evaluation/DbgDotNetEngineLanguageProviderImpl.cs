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
using dnSpy.Contracts.Debugger.DotNet.Evaluation.Engine;
using dnSpy.Contracts.Debugger.Engine.Evaluation;

namespace dnSpy.Debugger.DotNet.CorDebug.Impl.Evaluation {
	[ExportDbgEngineLanguageProvider(PredefinedDbgRuntimeGuids.DotNetFramework)]
	sealed class DbgDotNetFrameworkEngineLanguageProviderImpl : DbgDotNetEngineLanguageProvider {
		public override string RuntimeDisplayName => ".NET Framework";
		protected override Guid RuntimeGuid => PredefinedDbgRuntimeGuids.DotNetFramework_Guid;

		[ImportingConstructor]
		DbgDotNetFrameworkEngineLanguageProviderImpl(DbgDotNetLanguageService dbgDotNetLanguageService)
			: base(dbgDotNetLanguageService) {
		}
	}

	[ExportDbgEngineLanguageProvider(PredefinedDbgRuntimeGuids.DotNetCore)]
	sealed class DbgDotNetCoreEngineLanguageProviderImpl : DbgDotNetEngineLanguageProvider {
		public override string RuntimeDisplayName => ".NET Core";
		protected override Guid RuntimeGuid => PredefinedDbgRuntimeGuids.DotNetCore_Guid;

		[ImportingConstructor]
		DbgDotNetCoreEngineLanguageProviderImpl(DbgDotNetLanguageService dbgDotNetLanguageService)
			: base(dbgDotNetLanguageService) {
		}
	}
}
