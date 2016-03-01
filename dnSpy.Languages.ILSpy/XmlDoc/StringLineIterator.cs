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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace dnSpy.Languages.ILSpy.XmlDoc {
	struct SubStringInfo {
		public readonly int Index;
		public readonly int Length;

		public SubStringInfo(int index, int length) {
			this.Index = index;
			this.Length = length;
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
			this.end = index + length;
			this.info = default(SubStringInfo);
			this.finished = false;
		}

		public StringLineIterator GetEnumerator() {
			return this;
		}

		IEnumerator<SubStringInfo> IEnumerable<SubStringInfo>.GetEnumerator() {
			Debug.Fail("'this' was boxed");
			return GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator() {
			Debug.Fail("'this' was boxed");
			return GetEnumerator();
		}

		public SubStringInfo Current {
			get { return info; }
		}

		object IEnumerator.Current {
			get { Debug.Fail("'this' was boxed"); return info; }
		}

		public void Dispose() {
		}

		public bool MoveNext() {
			int newLineIndex = this.s.IndexOfAny(newLineChars, this.index, end - this.index);
			if (newLineIndex < 0) {
				if (this.finished)
					return false;
				this.info = new SubStringInfo(this.index, end - this.index);
				this.finished = true;
				return true;
			}
			int len = newLineIndex - this.index;
			this.info = new SubStringInfo(this.index, len);
			if (s[newLineIndex] == '\r' && newLineIndex + 1 < s.Length && s[newLineIndex + 1] == '\n')
				newLineIndex++;
			this.index = newLineIndex + 1;
			return true;
		}
		static readonly char[] newLineChars = new char[] { '\r', '\n' };

		public void Reset() {
			throw new NotImplementedException();
		}
	}
}
