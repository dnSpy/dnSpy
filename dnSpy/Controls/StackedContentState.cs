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
using System.Windows;
using dnSpy.Contracts.Settings;

namespace dnSpy.Controls {
	sealed class StackedContentState {
		public List<GridLength> RowsCols { get; private set; }
		public bool IsHorizontal { get; set; }

		public StackedContentState() {
			this.RowsCols = new List<GridLength>();
		}
	}

	static class StackedContentStateSerializer {
		const string ISHORIZONTAL_ATTR = "horizontal";
		const string LENGTH_SECTION = "Length";
		const string LENGTH_ATTR = "l";

		public static void Serialize(ISettingsSection section, StackedContentState state) {
			section.Attribute(ISHORIZONTAL_ATTR, state.IsHorizontal);
			foreach (var length in state.RowsCols) {
				var lengthSect = section.CreateSection(LENGTH_SECTION);
				lengthSect.Attribute(LENGTH_ATTR, length);
			}
		}

		public static StackedContentState TryDeserialize(ISettingsSection section) {
			var state = new StackedContentState();
			bool? b = section.Attribute<bool?>(ISHORIZONTAL_ATTR);
			if (b == null)
				return null;
			state.IsHorizontal = b.Value;
			foreach (var lengthSect in section.SectionsWithName(LENGTH_SECTION)) {
				var length = lengthSect.Attribute<GridLength?>(LENGTH_ATTR);
				if (length == null)
					return null;
				state.RowsCols.Add(length.Value);
			}
			return state;
		}
	}
}
