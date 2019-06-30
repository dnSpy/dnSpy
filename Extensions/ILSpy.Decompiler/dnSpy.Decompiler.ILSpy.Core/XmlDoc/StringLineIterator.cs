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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace dnSpy.Decompiler.ILSpy.Core.XmlDoc {
	struct SubStringInfo {
		public readonly int Index;
		public readonly int Length;

		public SubStringInfo(int index, int length) {
			Index = index;
			Length = length;
		}
	}

	struct StringLineIterator : IEnumerable<SubStringInfo>, IEnumerator<SubStringInfo> {
		readonly string s;
		int index;
		readonly int end;
		SubStringInfo info;
		bool finished;

		public StringLineIterator(string s, int index, int length) {
			this.s = s;
			this.index = index;
			end = index + length;
			info = default;
			finished = false;
		}

		public StringLineIterator GetEnumerator() => this;

		IEnumerator<SubStringInfo> IEnumerable<SubStringInfo>.GetEnumerator() {
			Debug.Fail("'this' was boxed");
			return GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator() {
			Debug.Fail("'this' was boxed");
			return GetEnumerator();
		}

		public SubStringInfo Current => info;

		object IEnumerator.Current {
			get { Debug.Fail("'this' was boxed"); return info; }
		}

		public void Dispose() { }

		public bool MoveNext() {
			int newLineIndex = s.IndexOfAny(newLineChars, index, end - index);
			if (newLineIndex < 0) {
				if (finished)
					return false;
				info = new SubStringInfo(index, end - index);
				finished = true;
				return true;
			}
			int len = newLineIndex - index;
			info = new SubStringInfo(index, len);
			if (s[newLineIndex] == '\r' && newLineIndex + 1 < s.Length && s[newLineIndex + 1] == '\n')
				newLineIndex++;
			index = newLineIndex + 1;
			return true;
		}
		static readonly char[] newLineChars = new char[] { '\r', '\n', '\u0085', '\u2028', '\u2029' };

		public void Reset() => throw new NotImplementedException();
	}
}
