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
using System.Text;

namespace dnSpy.Contracts.Text {
	/// <summary>
	/// Creates indentation strings
	/// </summary>
	public sealed class Indenter {
		readonly int indentSize;
		readonly int tabSize;
		readonly bool useTabs;
		readonly List<string?> cachedStrings;
		StringBuilder? sb;
		int indentLevel;

		/// <summary>
		/// Gets the indentation string
		/// </summary>
		public string String => GetIndentString(indentLevel);

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="indentSize">Size in characters of one indent</param>
		/// <param name="tabSize">Size of a tab in characters</param>
		/// <param name="useTabs">true to use tabs, false to use spaces</param>
		public Indenter(int indentSize, int tabSize, bool useTabs) {
			if (indentSize <= 0)
				throw new ArgumentOutOfRangeException(nameof(indentSize));
			if (tabSize <= 0)
				throw new ArgumentOutOfRangeException(nameof(tabSize));
			this.indentSize = indentSize;
			this.tabSize = tabSize;
			this.useTabs = useTabs;
			cachedStrings = new List<string?>();
		}

		/// <summary>
		/// Increments the indentation level
		/// </summary>
		public void IncreaseIndent() => indentLevel++;

		/// <summary>
		/// Decrements the indentation level
		/// </summary>
		public void DecreaseIndent() {
			if (indentLevel == 0)
				throw new InvalidOperationException();
			indentLevel--;
		}

		string GetIndentString(int level) {
			while (cachedStrings.Count <= level)
				cachedStrings.Add(null);

			var s = cachedStrings[level];
			if (s is not null)
				return s;
			s = CreateIndentString(level);
			cachedStrings[level] = s;
			return s;
		}

		string CreateIndentString(int level) {
			int totalChars = level * indentSize;
			if (!useTabs)
				return new string(' ', totalChars);

			// Common case is tabSize == indentSize, in which case we don't need a StringBuilder
			int tabs = totalChars / tabSize;
			int spaces = totalChars % tabSize;
			if (spaces == 0)
				return new string('\t', tabs);

			if (sb is null)
				sb = new StringBuilder();
			sb.Append('\t', tabs);
			sb.Append(' ', spaces);
			var s = sb.ToString();
			sb.Clear();
			return s;
		}

		/// <summary>
		/// Resets the instance so it can be re-used
		/// </summary>
		public void Reset() {
			indentLevel = 0;
			cachedStrings.Clear();
		}
	}
}
