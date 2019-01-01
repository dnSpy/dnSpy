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
using System.Diagnostics;
using dnSpy.Contracts.Debugger;

namespace dnSpy.Debugger.Impl {
	struct DbgBreakInfoCollectionBuilder {
		List<DbgBreakInfo> list;
		int count;
		DbgBreakInfo info0;
		DbgBreakInfo info1;

		public bool IsEmpty => count == 0;

		public void Add(DbgBreakInfo info) {
			switch (count) {
			case 0:
				info0 = info;
				break;

			case 1:
				info1 = info;
				break;

			default:
				if (list == null) {
					Debug.Assert(count == 2);
					list = new List<DbgBreakInfo>(count + 1) { info0, info1 };
				}
				list.Add(info);
				break;
			}
			count++;
		}

		public void Add(DbgMessageEventArgs e) => Add(new DbgBreakInfo(e));

		public DbgBreakInfo[] Create() {
			switch (count) {
			case 0:		return Array.Empty<DbgBreakInfo>();
			case 1:		return new DbgBreakInfo[] { info0 };
			case 2:		return new DbgBreakInfo[] { info0, info1 };
			default:	return list.ToArray();
			}
		}
	}
}
