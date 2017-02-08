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
using System.Linq;
using dnSpy.Contracts.Settings;

namespace dnSpy.Settings {
	sealed class SettingsSection : ISettingsSection {
		readonly SectionAttributes sectionAttributes;
		readonly SettingsSectionProvider settingsSectionProvider;

		public string Name { get; }
		public Tuple<string, string>[] Attributes => sectionAttributes.Attributes;

		public SettingsSection(string name) {
			Name = name;
			sectionAttributes = new SectionAttributes();
			settingsSectionProvider = new SettingsSectionProvider();
		}

		public ISettingsSection[] Sections => settingsSectionProvider.Sections;
		public ISettingsSection CreateSection(string name) => settingsSectionProvider.CreateSection(name);
		public ISettingsSection GetOrCreateSection(string name) => settingsSectionProvider.GetOrCreateSection(name);
		public void RemoveSection(string name) => settingsSectionProvider.RemoveSection(name);
		public void RemoveSection(ISettingsSection section) => settingsSectionProvider.RemoveSection(section);
		public ISettingsSection[] SectionsWithName(string name) => Sections.Where(a => StringComparer.Ordinal.Equals(name, a.Name)).ToArray();
		public ISettingsSection TryGetSection(string name) => Sections.FirstOrDefault(a => StringComparer.Ordinal.Equals(name, a.Name));
		public T Attribute<T>(string name) => sectionAttributes.Attribute<T>(name);
		public void Attribute<T>(string name, T value) => sectionAttributes.Attribute(name, value);
		public void RemoveAttribute(string name) => sectionAttributes.RemoveAttribute(name);

		public void CopyFrom(ISettingsSection section) {
			if (section == null)
				throw new ArgumentNullException(nameof(section));
			foreach (var attr in section.Attributes)
				Attribute(attr.Item1, attr.Item2);
			foreach (var child in section.Sections)
				CreateSection(child.Name).CopyFrom(child);
		}

		public override string ToString() => Name;
	}
}
