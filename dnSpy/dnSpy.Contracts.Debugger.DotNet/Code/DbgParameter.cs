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

namespace dnSpy.Contracts.Debugger.DotNet.Code {
	/// <summary>
	/// Method parameter info
	/// </summary>
	public readonly struct DbgParameter {
		/// <summary>
		/// Gets the parameter index
		/// </summary>
		public int Index { get; }

		/// <summary>
		/// Gets the parameter name
		/// </summary>
		public string Name { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="index">Parameter index</param>
		/// <param name="name">Parameter name</param>
		public DbgParameter(int index, string name) {
			if (index < 0)
				throw new ArgumentOutOfRangeException(nameof(index));
			Index = index;
			Name = name ?? throw new ArgumentNullException(nameof(name));
		}
	}
}
