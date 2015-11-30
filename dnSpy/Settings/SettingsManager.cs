/*
    Copyright (C) 2014-2015 de4dot@gmail.com

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
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using dnSpy.Contracts.Settings;

namespace dnSpy.Settings {
	[Export, Export(typeof(ISettingsManager)), PartCreationPolicy(CreationPolicy.Shared)]
	sealed class SettingsManager : ISettingsManager {
		readonly Dictionary<string, ISettingsSection> sections;

		public ISettingsSection[] Sections {
			get { return sections.Values.ToArray(); }
		}

		SettingsManager() {
			this.sections = new Dictionary<string, ISettingsSection>(StringComparer.Ordinal);
		}

		public ISettingsSection CreateSection(string name) {
			Debug.Assert(name != null);
			if (name == null)
				throw new ArgumentNullException();

			var section = new SettingsSection(name);
			sections[name] = section;
			return section;
		}

		public ISettingsSection GetOrCreateSection(string name) {
			Debug.Assert(name != null);
			if (name == null)
				throw new ArgumentNullException();

			ISettingsSection section;
			if (sections.TryGetValue(name, out section))
				return section;

			section = new SettingsSection(name);
			sections[name] = section;
			return section;
		}

		public void RemoveSection(string name) {
			Debug.Assert(name != null);
			if (name == null)
				throw new ArgumentNullException();

			sections.Remove(name);
		}

		public void RemoveSection(ISettingsSection section) {
			Debug.Assert(section != null);
			if (section == null)
				throw new ArgumentNullException();

			ISettingsSection other;
			bool b = sections.TryGetValue(section.Name, out other);
			Debug.Assert(b && other == section);
			if (!b || other != section)
				return;

			sections.Remove(section.Name);
		}

		public ISettingsSection[] SectionsWithName(string name) {
			return Sections.Where(a => StringComparer.Ordinal.Equals(name, a.Name)).ToArray();
		}

		public ISettingsSection TryGetSection(string name) {
			return Sections.FirstOrDefault(a => StringComparer.Ordinal.Equals(name, a.Name));
		}

		public ISettingsSection RecreateSection(string name) {
			RemoveSection(name);
			return GetOrCreateSection(name);
		}
	}
}
