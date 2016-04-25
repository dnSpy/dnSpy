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
using dnSpy.Contracts.Scripting.Roslyn;
using dnSpy.Contracts.TextEditor;

namespace dnSpy.Scripting.Roslyn.Common {
	sealed class CachedWriter : ICachedWriter {
		readonly List<ColorAndText> cachedList;
		readonly ScriptGlobals owner;

		IPrintOptions ITextPrinter.PrintOptions => printOptionsImpl;
		public PrintOptionsImpl PrintOptions => printOptionsImpl;
		readonly PrintOptionsImpl printOptionsImpl;

		public CachedWriter(ScriptGlobals owner) {
			this.owner = owner;
			this.printOptionsImpl = owner.PrintOptionsImpl.Clone();
			this.cachedList = new List<ColorAndText>();
		}

		public void Dispose() => Flush();
		public void Print(string text) => Print(OutputColor.ReplOutputText, text);
		public void Print(string fmt, params object[] args) => Print(string.Format(fmt, args));
		public void Print(OutputColor color, string text) => cachedList.Add(new ColorAndText(color, text));
		public void Print(OutputColor color, string fmt, params object[] args) => Print(color, string.Format(fmt, args));
		public void Print(Exception ex, OutputColor color) => owner.Print(this, ex, color);
		public void Print(object value, OutputColor color) => owner.Print(this, value, color);
		public void PrintLine(Exception ex, OutputColor color) => owner.PrintLine(this, ex, color);
		public void PrintLine(object value, OutputColor color) => owner.PrintLine(this, value, color);
		public void PrintError(string text) => Print(OutputColor.Error, text);
		public void PrintError(string fmt, params object[] args) => PrintError(string.Format(fmt, args));
		public void PrintLine(string text) => PrintLine(OutputColor.ReplOutputText, text);
		public void PrintLine(string fmt, params object[] args) => PrintLine(string.Format(fmt, args));
		public void PrintLine(OutputColor color, string fmt, params object[] args) => PrintLine(color, string.Format(fmt, args));
		public void PrintLineError(string text) => PrintLine(OutputColor.Error, text);
		public void PrintLineError(string fmt, params object[] args) => PrintLineError(string.Format(fmt, args));
		public void Write(string text, OutputColor color) => Print(color, text);

		public void PrintLine(OutputColor color, string text) {
			Print(color, text);
			Print(Environment.NewLine);
		}

		public void Flush() {
			owner.Write(cachedList);
			cachedList.Clear();
		}
	}
}
