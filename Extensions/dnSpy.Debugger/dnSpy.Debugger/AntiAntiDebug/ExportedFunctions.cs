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
using System.Collections.Generic;

namespace dnSpy.Debugger.AntiAntiDebug {
	sealed class ExportedFunctions {
		readonly Dictionary<ushort, ulong> ordinalToFunc = new Dictionary<ushort, ulong>();
		readonly Dictionary<string, ulong> nameToFunc = new Dictionary<string, ulong>(StringComparer.Ordinal);

		public void Add(ushort ordinal, ulong address) => ordinalToFunc[ordinal] = address;
		public void Add(string name, ulong address) => nameToFunc[name] = address;
		public bool TryGet(ushort ordinal, out ulong address) => ordinalToFunc.TryGetValue(ordinal, out address);
		public bool TryGet(string name, out ulong address) => nameToFunc.TryGetValue(name, out address);
	}
}
