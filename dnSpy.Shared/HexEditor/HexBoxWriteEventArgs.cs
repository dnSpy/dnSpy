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

namespace dnSpy.Shared.HexEditor {
	public sealed class HexBoxWriteEventArgs : EventArgs {
		public readonly HexWriteType Type;
		public readonly ulong StartOffset;
		public readonly ulong EndOffset;
		public readonly int Size;
		public readonly bool IsBeforeWrite;
		public readonly Dictionary<object, object> Context;

		public bool IsAfterWrite {
			get { return !IsBeforeWrite; }
		}

		public HexBoxWriteEventArgs(HexWriteType type, ulong offs, int size, bool isBeforeWrite, Dictionary<object, object> context = null) {
			this.Type = type;
			this.StartOffset = offs;
			this.EndOffset = size == 0 ? offs : NumberUtils.AddUInt64(offs, (ulong)(size - 1));
			this.Size = size;
			this.IsBeforeWrite = isBeforeWrite;
			this.Context = context ?? new Dictionary<object, object>();
		}
	}
}
