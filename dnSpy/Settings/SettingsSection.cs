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
using System.Linq;
using dnSpy.Contracts.Settings;

namespace dnSpy.Settings {
	sealed class SettingsSection : ISettingsSection {
		readonly SectionAttributes sectionAttributes;
		readonly SettingsSectionCreator settingsSectionCreator;

		public string Name {
			get { return name; }
		}

		public Tuple<string, string>[] Attributes {
			get { return sectionAttributes.Attributes; }
		}

		readonly string name;

		public SettingsSection(string name) {
			this.name = name;
			this.sectionAttributes = new SectionAttributes();
			this.settingsSectionCreator = new SettingsSectionCreator();
		}

		public ISettingsSection[] Sections {
			get { return settingsSectionCreator.Sections; }
		}

		public ISettingsSection CreateSection(string name) {
			return settingsSectionCreator.CreateSection(name);
		}

		public ISettingsSection GetOrCreateSection(string name) {
			return settingsSectionCreator.GetOrCreateSection(name);
		}

		public void RemoveSection(string name) {
			settingsSectionCreator.RemoveSection(name);
		}

		public void RemoveSection(ISettingsSection section) {
			settingsSectionCreator.RemoveSection(section);
		}

		public ISettingsSection[] SectionsWithName(string name) {
			return Sections.Where(a => StringComparer.Ordinal.Equals(name, a.Name)).ToArray();
		}

		public ISettingsSection TryGetSection(string name) {
			return Sections.FirstOrDefault(a => StringComparer.Ordinal.Equals(name, a.Name));
		}

		public T Attribute<T>(string name) {
			return sectionAttributes.Attribute<T>(name);
		}

		public void Attribute<T>(string name, T value) {
			sectionAttributes.Attribute(name, value);
		}

		public void RemoveAttribute(string name) {
			sectionAttributes.RemoveAttribute(name);
		}

		public void CopyFrom(ISettingsSection section) {
			if (section == null)
				throw new ArgumentNullException();
			foreach (var attr in section.Attributes)
				this.Attribute(attr.Item1, attr.Item2);
			foreach (var child in section.Sections)
				CreateSection(child.Name).CopyFrom(child);
		}

		public override string ToString() {
			return Name;
		}
	}
}
