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

using System.Collections.Generic;

namespace dnSpy.Debugger.DotNet.Metadata.Impl {
	static class DmdTypeUtilities {
		public static bool IsFullyResolved(IList<DmdType> list) {
			for (int i = 0; i < list.Count; i++) {
				if (!((DmdTypeBase)list[i]).IsFullyResolved)
					return false;
			}
			return true;
		}

		public static IList<DmdType> FullResolve(IList<DmdType> list) {
			if (IsFullyResolved(list))
				return list;
			var res = new DmdType[list.Count];
			for (int i = 0; i < res.Length; i++) {
				var type = ((DmdTypeBase)list[i]).FullResolve();
				if (type == null)
					return null;
				res[i] = type;
			}
			return res;
		}
	}
}
