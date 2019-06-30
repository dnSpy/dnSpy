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
using System.Collections.ObjectModel;

namespace dnSpy.Debugger.DotNet.Metadata {
	static class DmdCustomModifierUtilities {
		public static DmdType[] GetModifiers(ReadOnlyCollection<DmdCustomModifier> modifiers, bool requiredModifiers) {
			int count = 0;
			foreach (var modifier in modifiers) {
				if (modifier.IsRequired == requiredModifiers)
					count++;
			}
			if (count == 0)
				return Array.Empty<DmdType>();

			var res = new DmdType[count];
			int w = 0;
			for (int r = 0; r < modifiers.Count; r++) {
				var modifier = modifiers[r];
				if (modifier.IsRequired == requiredModifiers)
					res[w++] = modifier.Type;
			}
			if (w != res.Length)
				throw new InvalidOperationException();
			return res;
		}
	}
}
