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
using System.Collections.Generic;

namespace dnSpy.Contracts.HexEditor {
	sealed class HexBoxWriteEventArgs : EventArgs {
		public HexWriteType Type { get; }
		public ulong StartOffset { get; }
		public ulong EndOffset { get; }
		public int Size { get; }
		public bool IsBeforeWrite { get; }
		public Dictionary<object, object> Context { get; }
		public bool IsAfterWrite => !IsBeforeWrite;

		public HexBoxWriteEventArgs(HexWriteType type, ulong offs, int size, bool isBeforeWrite, Dictionary<object, object> context = null) {
			Type = type;
			StartOffset = offs;
			EndOffset = size == 0 ? offs : NumberUtils.AddUInt64(offs, (ulong)(size - 1));
			Size = size;
			IsBeforeWrite = isBeforeWrite;
			Context = context ?? new Dictionary<object, object>();
		}
	}
}
