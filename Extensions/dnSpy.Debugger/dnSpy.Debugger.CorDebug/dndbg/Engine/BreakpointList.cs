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
using System.Collections.Generic;

namespace dndbg.Engine {
	sealed class BreakpointList<TBP> {
		readonly Dictionary<DnModuleId, List<TBP>> dict = new Dictionary<DnModuleId, List<TBP>>();

		public IEnumerable<TBP> GetBreakpoints() {
			foreach (var list in dict.Values) {
				foreach (var bp in list)
					yield return bp;
			}
		}

		public TBP[] GetBreakpoints(DnModuleId module) {
			if (!dict.TryGetValue(module, out var list))
				return Array.Empty<TBP>();
			return list.ToArray();
		}

		public void Add(DnModuleId module, TBP bp) {
			if (!dict.TryGetValue(module, out var list))
				dict.Add(module, list = new List<TBP>());
			list.Add(bp);
		}

		public bool Remove(DnModuleId module, TBP bp) {
			if (!dict.TryGetValue(module, out var list))
				return false;
			return list.Remove(bp);
		}
	}
}
