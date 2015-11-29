/*
    Copyright (C) 2014-2015 de4dot@gmail.com

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

namespace dnSpy.Contracts.Languages {
	/// <summary>
	/// Language manager
	/// </summary>
	public interface ILanguageManager {
		/// <summary>
		/// Gets all languages
		/// </summary>
		IEnumerable<ILanguage> Languages { get; }

		/// <summary>
		/// Current default language
		/// </summary>
		ILanguage SelectedLanguage { get; set; }

		/// <summary>
		/// Raised when <see cref="SelectedLanguage"/> has been updated
		/// </summary>
		event EventHandler<EventArgs> LanguageChanged;

		/// <summary>
		/// Finds a <see cref="ILanguage"/> instance. null is returned if it wasn't found
		/// </summary>
		/// <param name="nameUI">Name, see <see cref="ILanguage.NameUI"/></param>
		/// <returns></returns>
		ILanguage Find(string nameUI);

		/// <summary>
		/// Finds a <see cref="ILanguage"/> instance. Returns the first one if the language wasn't found
		/// </summary>
		/// <param name="nameUI">Name, see <see cref="ILanguage.NameUI"/></param>
		/// <returns></returns>
		ILanguage FindOrDefault(string nameUI);
	}
}
