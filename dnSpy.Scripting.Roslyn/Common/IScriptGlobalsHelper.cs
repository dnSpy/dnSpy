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
using dnSpy.Contracts.Scripting;

namespace dnSpy.Scripting.Roslyn.Common {
	interface IScriptGlobalsHelper {
		/// <summary>
		/// Prints text. Method can be called from any thread
		/// </summary>
		/// <param name="globals">Globals</param>
		/// <param name="text">Text</param>
		void Print(ScriptGlobals globals, string text);

		/// <summary>
		/// Prints text plus a new line. Method can be called from any thread
		/// </summary>
		/// <param name="globals">Globals</param>
		/// <param name="text">Text</param>
		void PrintLine(ScriptGlobals globals, string text);

		/// <summary>
		/// Prints text. Method can be called from any thread
		/// </summary>
		/// <param name="globals">Globals</param>
		/// <param name="printOptions">Print options</param>
		/// <param name="value">Value</param>
		void Print(ScriptGlobals globals, PrintOptionsImpl printOptions, object value);

		/// <summary>
		/// Prints text plus a new line. Method can be called from any thread
		/// </summary>
		/// <param name="globals">Globals</param>
		/// <param name="printOptions">Print options</param>
		/// <param name="value">Value</param>
		void PrintLine(ScriptGlobals globals, PrintOptionsImpl printOptions, object value);

		/// <summary>
		/// Prints text. Method can be called from any thread
		/// </summary>
		/// <param name="globals">Globals</param>
		/// <param name="ex">Exception</param>
		void Print(ScriptGlobals globals, Exception ex);

		/// <summary>
		/// Prints text plus a new line. Method can be called from any thread
		/// </summary>
		/// <param name="globals">Globals</param>
		/// <param name="ex">Exception</param>
		void PrintLine(ScriptGlobals globals, Exception ex);

		/// <summary>
		/// Gets the service locator
		/// </summary>
		IServiceLocator ServiceLocator { get; }
	}
}
