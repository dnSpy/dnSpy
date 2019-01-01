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
	/// Code range
	/// </summary>
	public readonly struct DbgCodeRange {
		/// <summary>
		/// Gets the start offset relative to the start of the method
		/// </summary>
		public uint Start { get; }

		/// <summary>
		/// Gets the end method offset (exclusive) relative to the start of the method
		/// </summary>
		public uint End { get; }

		/// <summary>
		/// Gets the length of the range
		/// </summary>
		public uint Length => End - Start;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="start">Start offset relative to the start of the method</param>
		/// <param name="end">End method offset (exclusive) relative to the start of the method</param>
		public DbgCodeRange(uint start, uint end) {
			if (end < start)
				throw new ArgumentOutOfRangeException(nameof(end));
			Start = start;
			End = end;
		}

		/// <summary>
		/// Checks whether <paramref name="offset"/> is within this range
		/// </summary>
		/// <param name="offset">Offset</param>
		/// <returns></returns>
		public bool Contains(uint offset) => Start <= offset && offset < End;
	}
}
