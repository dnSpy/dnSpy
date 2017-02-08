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
using dnSpy.Contracts.Output;
using dnSpy.Contracts.Text;

namespace dnSpy.Output {
	sealed class CachedWriter : ICachedWriter {
		readonly List<ColorAndText> cachedList;
		readonly IOutputWriter owner;

		public CachedWriter(IOutputWriter owner) {
			this.owner = owner;
			cachedList = new List<ColorAndText>();
		}

		public void Dispose() => Flush();
		public void Write(IEnumerable<ColorAndText> text) => cachedList.AddRange(text);
		public void Write(object color, string text) => cachedList.Add(new ColorAndText(color, text));
		public void Write(TextColor color, string text) => cachedList.Add(new ColorAndText(color, text));
		public void WriteLine(TextColor color, string text) => WriteLine(color.Box(), text);

		public void Flush() {
			owner.Write(cachedList);
			cachedList.Clear();
		}

		public void WriteLine(object color, string text) {
			Write(color, text);
			Write(color, Environment.NewLine);
		}
	}
}
