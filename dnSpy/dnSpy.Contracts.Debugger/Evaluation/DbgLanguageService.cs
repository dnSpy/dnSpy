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
using System.Collections.ObjectModel;

namespace dnSpy.Contracts.Debugger.Evaluation {
	/// <summary>
	/// Debugger language service
	/// </summary>
	public abstract class DbgLanguageService {
		/// <summary>
		/// Gets all languages available by a <see cref="DbgRuntime"/>
		/// </summary>
		/// <param name="runtimeGuid">Runtime GUID, see <see cref="PredefinedDbgRuntimeGuids"/></param>
		/// <returns></returns>
		public abstract ReadOnlyCollection<DbgLanguage> GetLanguages(Guid runtimeGuid);

		/// <summary>
		/// Sets the language that should be used by all runtimes with GUID <paramref name="runtimeGuid"/>
		/// </summary>
		/// <param name="runtimeGuid">Runtime GUID, see <see cref="PredefinedDbgRuntimeGuids"/></param>
		/// <param name="language">Language to use</param>
		public abstract void SetCurrentLanguage(Guid runtimeGuid, DbgLanguage language);

		/// <summary>
		/// Gets the current language the runtime uses
		/// </summary>
		/// <param name="runtimeGuid">Runtime GUID, see <see cref="PredefinedDbgRuntimeGuids"/></param>
		/// <returns></returns>
		public abstract DbgLanguage GetCurrentLanguage(Guid runtimeGuid);

		/// <summary>
		/// Raised when a runtime's current language is changed
		/// </summary>
		public abstract event EventHandler<DbgLanguageChangedEventArgs> LanguageChanged;
	}

	/// <summary>
	/// Language changed event args
	/// </summary>
	public struct DbgLanguageChangedEventArgs {
		/// <summary>
		/// Runtime GUID, see <see cref="PredefinedDbgRuntimeGuids"/>
		/// </summary>
		public Guid RuntimeGuid { get; }

		/// <summary>
		/// New language
		/// </summary>
		public DbgLanguage Language { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="runtimeGuid">Runtime GUID, see <see cref="PredefinedDbgRuntimeGuids"/></param>
		/// <param name="language">New language</param>
		public DbgLanguageChangedEventArgs(Guid runtimeGuid, DbgLanguage language) {
			RuntimeGuid = runtimeGuid;
			Language = language ?? throw new ArgumentNullException(nameof(language));
		}
	}
}
