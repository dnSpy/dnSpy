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
using dnSpy.Contracts.Output;
using dnSpy.Contracts.TextEditor;

namespace dnSpy.Output {
	sealed class NotPresentOutputWriter : IOutputTextPane {
		public Guid Guid => guid;

		readonly OutputManager outputManager;
		readonly Guid guid;

		IOutputTextPane TextPane {
			get {
				if (textPane != null)
					return textPane;
				return (textPane = outputManager.Find(guid)) ?? NullOutputTextPane.Instance;
			}
		}
		IOutputTextPane textPane;

		public NotPresentOutputWriter(OutputManager outputManager, Guid guid) {
			this.outputManager = outputManager;
			this.guid = guid;
		}

		public ICachedWriter CreateWriter() => new CachedWriter(this);
		public void WriteLine(OutputColor color, string s) => TextPane.WriteLine(color, s);
		public void Write(IEnumerable<ColorAndText> text) => TextPane.Write(text);
		public void Clear() => TextPane.Clear();
		public string GetText() => TextPane.GetText();
		public void Write(OutputColor color, string s) => TextPane.Write(color, s);
	}
}
