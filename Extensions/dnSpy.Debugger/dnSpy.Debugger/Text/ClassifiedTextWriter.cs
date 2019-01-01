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

using System.Collections.Generic;
using dnSpy.Contracts.Debugger.Text;

namespace dnSpy.Debugger.Text {
	sealed class ClassifiedTextWriter : IDbgTextWriter {
		readonly List<ClassifiedText> result;

		public ClassifiedTextWriter() => result = new List<ClassifiedText>();

		/// <summary>
		/// Gets the classified text and resets itself so it can be re-used
		/// </summary>
		/// <returns></returns>
		public ClassifiedTextCollection GetClassifiedText() {
			var res = new ClassifiedTextCollection(result.ToArray());
			result.Clear();
			return res;
		}

		public void Clear() => result.Clear();

		public void Write(DbgTextColor color, string text) => result.Add(new ClassifiedText(color, text));

		public bool Equals(ClassifiedTextCollection collection) {
			if (collection.IsDefault)
				return false;
			var other = collection.Result;
			var result = this.result;
			if (other.Length != result.Count)
				return false;
			for (int i = 0; i < other.Length; i++) {
				if (!other[i].Equals(result[i]))
					return false;
			}
			return true;
		}
	}
}
