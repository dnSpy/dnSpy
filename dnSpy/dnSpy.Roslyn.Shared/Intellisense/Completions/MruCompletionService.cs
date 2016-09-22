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

using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;

namespace dnSpy.Roslyn.Shared.Intellisense.Completions {
	interface IMruCompletionService {
		void AddText(string text);
		int GetMruIndex(string text);
	}

	[Export(typeof(IMruCompletionService))]
	sealed class MruCompletionService : IMruCompletionService {
		const int MaxItems = 10;
		readonly List<string> items;

		MruCompletionService() {
			items = new List<string>(MaxItems);
		}

		public void AddText(string text) {
			int index = items.IndexOf(text);
			if (index == 0)
				return;
			else if (index > 0)
				items.RemoveAt(index);
			else if (items.Count >= MaxItems)
				items.RemoveAt(items.Count - 1);
			Debug.Assert(items.Count < MaxItems);
			items.Insert(0, text);
		}

		public int GetMruIndex(string text) {
			for (int i = 0; i < items.Count; i++) {
				if (items[i] == text)
					return i;
			}
			return int.MaxValue;
		}
	}
}
