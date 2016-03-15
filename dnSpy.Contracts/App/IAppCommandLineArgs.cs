/*
    Copyright (C) 2014-2016 de4dot@gmail.com

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
using dnSpy.Contracts.Languages;
using dnSpy.Contracts.Themes;

namespace dnSpy.Contracts.App {
	/// <summary>
	/// Application command line arguments
	/// </summary>
	public interface IAppCommandLineArgs {
		/// <summary>Settings filename</summary>
		string SettingsFilename { get; }

		/// <summary>Filenames to load</summary>
		IEnumerable<string> Filenames { get; }

		/// <summary>true if single-instance</summary>
		bool SingleInstance { get; }

		/// <summary>true to activate the window</summary>
		bool Activate { get; }

		/// <summary>Language, either human readable or a language guid
		/// (<see cref="ILanguage.GenericGuid"/> or <see cref="ILanguage.UniqueGuid"/>)</summary>
		string Language { get; }

		/// <summary>Culture</summary>
		string Culture { get; }

		/// <summary>Member to select, either an MD token or an XML doc name</summary>
		string SelectMember { get; }

		/// <summary>Show the file in a new tab</summary>
		bool NewTab { get; }

		/// <summary>Search string or null if none</summary>
		string SearchText { get; }

		/// <summary>Search type</summary>
		string SearchFor { get; }

		/// <summary>Search location</summary>
		string SearchIn { get; }

		/// <summary>Theme name (<see cref="ITheme.Guid"/> or <see cref="ITheme.Name"/>)</summary>
		string Theme { get; }

		/// <summary>true to load all saved files at startup</summary>
		bool LoadFiles { get; }

		/// <summary>Full screen</summary>
		bool? FullScreen { get; }

		/// <summary>Tool windows to show</summary>
		string ShowToolWindow { get; }

		/// <summary>Tool windows to hide</summary>
		string HideToolWindow { get; }

		/// <summary>
		/// Returns true if the argument is present
		/// </summary>
		/// <param name="argName">Argument name, eg. <c>--my-arg</c></param>
		/// <returns></returns>
		bool HasArgument(string argName);

		/// <summary>
		/// Gets the argument value or null if the argument isn't present
		/// </summary>
		/// <param name="argName">Argument name, eg. <c>--my-arg</c></param>
		/// <returns></returns>
		string GetArgumentValue(string argName);

		/// <summary>
		/// Gets all user arguments and values
		/// </summary>
		/// <returns></returns>
		IEnumerable<Tuple<string, string>> GetArguments();
	}
}
