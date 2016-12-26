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
using System.Linq;

namespace dnSpy.Contracts.Hex.Text {
	/// <summary>
	/// <see cref="HexClassifiedText"/> collection
	/// </summary>
	public sealed class HexClassifiedTextCollection : IEnumerable<HexClassifiedText> {
		readonly HexClassifiedText[] text;

		/// <summary>
		/// Gets the number of elements in the collection
		/// </summary>
		public int Count => text.Length;

		/// <summary>
		/// Gets a <see cref="HexClassifiedText"/>
		/// </summary>
		/// <param name="index">Index</param>
		/// <returns></returns>
		public HexClassifiedText this[int index] => text[index];

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="text">Text</param>
		public HexClassifiedTextCollection(IEnumerable<HexClassifiedText> text) {
			if (text == null)
				throw new ArgumentNullException(nameof(text));
			this.text = text.ToArray();
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="text">Text</param>
		public HexClassifiedTextCollection(HexClassifiedText[] text) {
			if (text == null)
				throw new ArgumentNullException(nameof(text));
			this.text = text;
		}

		/// <summary>
		/// Gets the enumerator
		/// </summary>
		/// <returns></returns>
		public IEnumerator<HexClassifiedText> GetEnumerator() {
			foreach (var e in text)
				yield return e;
		}

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	}
}
