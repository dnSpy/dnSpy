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
using System.Diagnostics;
using System.Linq;
using dnSpy.Contracts.Settings;

namespace dnSpy.Settings {
	sealed class SettingsSectionProvider {
		readonly List<ISettingsSection> sections;

		public SettingsSectionProvider() => sections = new List<ISettingsSection>();

		public ISettingsSection[] Sections => sections.ToArray();

		public ISettingsSection CreateSection(string name) {
			Debug.Assert(name != null);
			if (name == null)
				throw new ArgumentNullException(nameof(name));

			var section = new SettingsSection(name);
			sections.Add(section);
			return section;
		}

		public ISettingsSection GetOrCreateSection(string name) {
			Debug.Assert(name != null);
			if (name == null)
				throw new ArgumentNullException(nameof(name));

			var section = sections.FirstOrDefault(a => StringComparer.Ordinal.Equals(name, a.Name));
			if (section != null)
				return section;
			sections.Add(section = new SettingsSection(name));
			return section;
		}

		public void RemoveSection(string name) {
			Debug.Assert(name != null);
			if (name == null)
				throw new ArgumentNullException(nameof(name));

			for (int i = sections.Count - 1; i >= 0; i--) {
				if (StringComparer.Ordinal.Equals(name, sections[i].Name))
					sections.RemoveAt(i);
			}
		}

		public void RemoveSection(ISettingsSection section) {
			Debug.Assert(section != null);
			if (section == null)
				throw new ArgumentNullException(nameof(section));

			bool b = sections.Remove(section);
			Debug.Assert(b);
		}
	}
}
