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
using dnSpy.Contracts.Scripting;
using dnSpy.Contracts.Text;

namespace dnSpy.Scripting.Roslyn.Common {
	interface IScriptGlobalsHelper {
		void Print(ScriptGlobals globals, object color, string text);
		void PrintLine(ScriptGlobals globals, object color, string text);
		void Print(ScriptGlobals globals, object color, PrintOptionsImpl printOptions, object value);
		void PrintLine(ScriptGlobals globals, object color, PrintOptionsImpl printOptions, object value);
		void Print(ScriptGlobals globals, object color, Exception ex);
		void PrintLine(ScriptGlobals globals, object color, Exception ex);
		void Print(ScriptGlobals globals, CachedWriter dest, object color, PrintOptionsImpl printOptions, object value);
		void Print(ScriptGlobals globals, CachedWriter dest, object color, Exception ex);
		void Write(ScriptGlobals globals, List<ColorAndText> list);
		IServiceLocator ServiceLocator { get; }
	}
}
