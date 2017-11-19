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

namespace dnSpy.Debugger.DotNet.Mono.Impl {
	static class ObjectConstants {
		const int OffsetToStringData_32 = 12;
		const int OffsetToStringData_64 = 20;

		const int OffsetToArrayData_32 = 16;
		const int OffsetToArrayData_64 = 32;

		public static int GetOffsetToStringData(int pointerSize) {
			if (pointerSize == 4)
				return OffsetToStringData_32;
			if (pointerSize == 8)
				return OffsetToStringData_64;
			throw new InvalidOperationException();
		}

		public static int GetOffsetToArrayData(int pointerSize) {
			if (pointerSize == 4)
				return OffsetToArrayData_32;
			if (pointerSize == 8)
				return OffsetToArrayData_64;
			throw new InvalidOperationException();
		}
	}
}
