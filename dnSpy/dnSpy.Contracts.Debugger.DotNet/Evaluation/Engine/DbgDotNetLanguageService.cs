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
	/// Used by a <see cref="DbgDotNetEngineLanguageProvider"/> to create all supported .NET languages
	/// </summary>
	public abstract class DbgDotNetLanguageService {
		/// <summary>
		/// Creates all languages
		/// </summary>
		/// <param name="runtimeGuid">Runtime guid, see <see cref="PredefinedDbgRuntimeGuids"/></param>
		/// <returns></returns>
		public abstract IEnumerable<DbgEngineLanguage> CreateLanguages(Guid runtimeGuid);

		/// <summary>
		/// Creates a <see cref="DbgEngineObjectIdFactory"/>
		/// </summary>
		/// <param name="runtimeGuid">Runtime guid, see <see cref="PredefinedDbgRuntimeGuids"/></param>
		/// <returns></returns>
		public abstract DbgEngineObjectIdFactory GetEngineObjectIdFactory(Guid runtimeGuid);
	}
}
