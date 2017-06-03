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

namespace dnSpy.Debugger.DotNet.Metadata {
	/// <summary>
	/// Parameter modifiers
	/// </summary>
	public struct DmdParameterModifier {
		readonly bool[] isByRef;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="parameterCount">Parameter count</param>
		public DmdParameterModifier(int parameterCount) {
			if (parameterCount <= 0)
				throw new ArgumentOutOfRangeException(nameof(parameterCount));
			isByRef = new bool[parameterCount];
		}

		/// <summary>
		/// Gets/sets the is-by-ref flag
		/// </summary>
		/// <param name="index">Parameter index</param>
		/// <returns></returns>
		public bool this[int index] {
			get => isByRef[index];
			set => isByRef[index] = value;
		}
	}
}
