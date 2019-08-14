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
using System.Collections.ObjectModel;

namespace dnSpy.Contracts.Debugger.Evaluation {
	/// <summary>
	/// Debugger language service
	/// </summary>
	public abstract class DbgLanguageService {
		/// <summary>
		/// Gets all languages
		/// </summary>
		/// <param name="runtimeKindGuid">Runtime kind GUID, see <see cref="PredefinedDbgRuntimeKindGuids"/></param>
		/// <returns></returns>
		public abstract ReadOnlyCollection<DbgLanguage> GetLanguages(Guid runtimeKindGuid);

		/// <summary>
		/// Sets the language that should be used by all runtimes with GUID <paramref name="runtimeKindGuid"/>
		/// </summary>
		/// <param name="runtimeKindGuid">Runtime kind GUID, see <see cref="PredefinedDbgRuntimeKindGuids"/></param>
		/// <param name="language">Language to use</param>
		public abstract void SetCurrentLanguage(Guid runtimeKindGuid, DbgLanguage language);

		/// <summary>
		/// Gets the current language the runtime uses
		/// </summary>
		/// <param name="runtimeKindGuid">Runtime kind GUID, see <see cref="PredefinedDbgRuntimeKindGuids"/></param>
		/// <returns></returns>
		public abstract DbgLanguage GetCurrentLanguage(Guid runtimeKindGuid);

		/// <summary>
		/// Raised when a runtime's current language is changed
		/// </summary>
		public abstract event EventHandler<DbgLanguageChangedEventArgs>? LanguageChanged;
	}

	/// <summary>
	/// Language changed event args
	/// </summary>
	public readonly struct DbgLanguageChangedEventArgs {
		/// <summary>
		/// Runtime kind GUID, see <see cref="PredefinedDbgRuntimeKindGuids"/>
		/// </summary>
		public Guid RuntimeKindGuid { get; }

		/// <summary>
		/// New language
		/// </summary>
		public DbgLanguage Language { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="runtimeKindGuid">Runtime kind GUID, see <see cref="PredefinedDbgRuntimeKindGuids"/></param>
		/// <param name="language">New language</param>
		public DbgLanguageChangedEventArgs(Guid runtimeKindGuid, DbgLanguage language) {
			RuntimeKindGuid = runtimeKindGuid;
			Language = language ?? throw new ArgumentNullException(nameof(language));
		}
	}
}
