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

namespace dnSpy.Decompiler.ILSpy.XmlDoc {
	struct XmlDocLine : IEnumerable<SubString?>, IEnumerator<SubString?> {
		readonly string s;
		readonly int end;
		SubString? current;
		SubStringInfo? indent;
		StringLineIterator iter;
		int emptyLines;

		public XmlDocLine(string s)
			: this(s, 0, s.Length) {
		}

		public XmlDocLine(string s, int start, int length) {
			this.s = s;
			this.end = start + length;
			this.current = null;
			this.indent = null;
			this.iter = new StringLineIterator(s, start, end - start);
			this.emptyLines = 0;
		}

		public XmlDocLine GetEnumerator() => this;

		IEnumerator<SubString?> IEnumerable<SubString?>.GetEnumerator() {
			Debug.Fail("'this' was boxed");
			return GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator() {
			Debug.Fail("'this' was boxed");
			return GetEnumerator();
		}

		public SubString? Current => current;

		object IEnumerator.Current {
			get { Debug.Fail("'this' was boxed"); return current; }
		}

		public void Dispose() { }

		public bool MoveNext() {
			if (this.indent == null) {
				for (;;) {
					if (!iter.MoveNext())
						return false;
					if (!IsWhiteSpace(s, iter.Current))
						break;
				}

				indent = GetIndentation(s, iter.Current);
				goto start2;
			}

			if (emptyLines != 0)
				goto start2;
start:
			if (!iter.MoveNext())
				return false;
start2:
			if (IsWhiteSpace(s, iter.Current)) {
				emptyLines++;
				goto start;
			}

			if (emptyLines != 0) {
				if (emptyLines != -1) {
					emptyLines--;
					if (emptyLines == 0)
						emptyLines = -1;
					current = null;
					return true;
				}
				emptyLines = 0;
			}

			int index, end;
			Trim(out index, out end);
			current = new SubString(s, index, end - index);
			return true;
		}

		void Trim(out int trimmedIndex, out int trimmedEnd) {
			Debug.Assert(indent != null);

			int index = iter.Current.Index;
			int end = index + iter.Current.Length;
			if (indent.Value.Length > iter.Current.Length) {
				trimmedIndex = index;
				trimmedEnd = end;
				return;
			}

			int end2 = index + indent.Value.Length;
			for (int i = index, j = indent.Value.Index; i < end2; i++, j++) {
				if (s[i] != s[j]) {
					trimmedIndex = index;
					trimmedEnd = end;
					return;
				}
			}

			trimmedIndex = index + indent.Value.Length;
			trimmedEnd = end;
			Debug.Assert(trimmedIndex <= trimmedEnd);
		}

		SubStringInfo GetIndentation(string doc, SubStringInfo info) {
			int end = info.Index + info.Length;
			int i = info.Index;
			for (; i < end; i++) {
				if (!char.IsWhiteSpace(doc[i]))
					break;
			}
			return new SubStringInfo(info.Index, i - info.Index);
		}

		bool IsWhiteSpace(string doc, SubStringInfo info) {
			int end = info.Index + info.Length;
			for (int i = info.Index; i < end; i++) {
				if (!char.IsWhiteSpace(doc[i]))
					return false;
			}
			return true;
		}

		public void Reset() {
			throw new NotImplementedException();
		}
	}
}
