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

using System.Collections;
using System.Collections.Generic;

namespace dnSpy.Contracts.Hex.Formatting {
	/// <summary>
	/// <see cref="HexSequenceElement"/> collection
	/// </summary>
	public abstract class HexAndAdornmentCollection : IReadOnlyList<HexSequenceElement> {
		/// <summary>
		/// Constructor
		/// </summary>
		protected HexAndAdornmentCollection() { }

		/// <summary>
		/// Gets the sequencer
		/// </summary>
		public abstract HexAndAdornmentSequencer Sequencer { get; }

		/// <summary>
		/// Gets an element
		/// </summary>
		/// <param name="index">Index</param>
		/// <returns></returns>
		public abstract HexSequenceElement this[int index] { get; }

		/// <summary>
		/// Gets the number of elements in this collection
		/// </summary>
		public abstract int Count { get; }

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		/// <summary>
		/// Gets the enumerator
		/// </summary>
		/// <returns></returns>
		public IEnumerator<HexSequenceElement> GetEnumerator() {
			for (int i = 0; i < Count; i++)
				yield return this[i];
		}
	}
}
