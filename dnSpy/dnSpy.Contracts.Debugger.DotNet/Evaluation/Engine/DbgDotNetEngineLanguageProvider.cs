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
using System.Collections.Generic;
using dnSpy.Contracts.Debugger.Engine.Evaluation;

namespace dnSpy.Contracts.Debugger.DotNet.Evaluation.Engine {
	/// <summary>
	/// Creates all supported .NET languages. Use <see cref="ExportDbgEngineLanguageProviderAttribute"/>
	/// to export an instance.
	/// </summary>
	public abstract class DbgDotNetEngineLanguageProvider : DbgEngineLanguageProvider {
		/// <summary>
		/// Gets the runtime guid, see <see cref="PredefinedDbgRuntimeGuids"/>
		/// </summary>
		protected abstract Guid RuntimeGuid { get; }

		readonly DbgDotNetLanguageService dbgDotNetLanguageService;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="dbgDotNetLanguageService">.NET language service instance</param>
		protected DbgDotNetEngineLanguageProvider(DbgDotNetLanguageService dbgDotNetLanguageService) =>
			this.dbgDotNetLanguageService = dbgDotNetLanguageService ?? throw new ArgumentNullException(nameof(dbgDotNetLanguageService));

		/// <summary>
		/// Creates all languages
		/// </summary>
		/// <returns></returns>
		public sealed override IEnumerable<DbgEngineLanguage> Create() => dbgDotNetLanguageService.CreateLanguages(RuntimeGuid);
	}
}
