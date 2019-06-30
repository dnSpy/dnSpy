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
using System.Collections.Generic;
using dnSpy.Contracts.Scripting.Roslyn;
using dnSpy.Contracts.Text;

namespace dnSpy.Scripting.Roslyn.Common {
	sealed class CachedWriter : ICachedWriter {
		readonly List<ColorAndText> cachedList;
		readonly ScriptGlobals owner;

		IPrintOptions ITextPrinter.PrintOptions => PrintOptions;
		public PrintOptionsImpl PrintOptions { get; }

		public CachedWriter(ScriptGlobals owner) {
			this.owner = owner;
			PrintOptions = owner.PrintOptionsImpl.Clone();
			cachedList = new List<ColorAndText>();
		}

		public void Dispose() => Flush();
		public void Print(string? text) => Print(BoxedTextColor.ReplScriptOutputText, text);
		public void Print(string fmt, params object[] args) => Print(string.Format(fmt, args));
		public void Print(object? color, string? text) => cachedList.Add(new ColorAndText(color ?? BoxedTextColor.ReplScriptOutputText, text ?? string.Empty));
		public void Print(object? color, string fmt, params object[] args) => Print(color ?? BoxedTextColor.ReplScriptOutputText, string.Format(fmt, args));
		public void Print(Exception ex, object? color) => owner.Print(this, ex, color ?? BoxedTextColor.Error);
		public void Print(object value, object? color) => owner.Print(this, value, color ?? BoxedTextColor.ReplScriptOutputText);
		public void PrintLine(Exception ex, object? color) => owner.PrintLine(this, ex, color ?? BoxedTextColor.Error);
		public void PrintLine(object value, object? color) => owner.PrintLine(this, value, color ?? BoxedTextColor.ReplScriptOutputText);
		public void Print(TextColor color, string? text) => cachedList.Add(new ColorAndText(color, text ?? string.Empty));
		public void Print(TextColor color, string fmt, params object[] args) => Print(color, string.Format(fmt, args));
		public void Print(Exception ex, TextColor color) => owner.Print(this, ex, color);
		public void Print(object value, TextColor color) => owner.Print(this, value, color);
		public void PrintLine(Exception ex, TextColor color) => owner.PrintLine(this, ex, color);
		public void PrintLine(object value, TextColor color) => owner.PrintLine(this, value, color);
		public void PrintError(string? text) => Print(BoxedTextColor.Error, text);
		public void PrintError(string fmt, params object[] args) => PrintError(string.Format(fmt, args));
		public void PrintLine(string? text) => PrintLine(BoxedTextColor.ReplScriptOutputText, text);
		public void PrintLine(string fmt, params object[] args) => PrintLine(string.Format(fmt, args));
		public void PrintLine(object? color, string fmt, params object[] args) => PrintLine(color ?? BoxedTextColor.ReplScriptOutputText, string.Format(fmt, args));
		public void PrintLine(TextColor color, string fmt, params object[] args) => PrintLine(color, string.Format(fmt, args));
		public void PrintLineError(string? text) => PrintLine(BoxedTextColor.Error, text);
		public void PrintLineError(string fmt, params object[] args) => PrintLineError(string.Format(fmt, args));
		public void Write(string? text, object? color) => Print(color ?? BoxedTextColor.ReplScriptOutputText, text);
		public void Write(string? text, TextColor color) => Print(color, text);
		public void PrintLine(TextColor color, string? text) => PrintLine(color.Box(), text);

		public void PrintLine(object? color, string? text) {
			Print(color, text);
			Print(Environment.NewLine);
		}

		public void Flush() {
			owner.Write(cachedList);
			cachedList.Clear();
		}
	}
}
