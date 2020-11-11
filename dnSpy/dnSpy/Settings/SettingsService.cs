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
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using dnSpy.Contracts.Settings;

namespace dnSpy.Settings {
	[Export, Export(typeof(ISettingsService))]
	class SettingsService : ISettingsService {
		readonly object lockObj;
		readonly Dictionary<string, ISettingsSection> sections;

		public ISettingsSection[] Sections => sections.Values.ToArray();

		protected SettingsService() {
			lockObj = new object();
			sections = new Dictionary<string, ISettingsSection>(StringComparer.Ordinal);
		}

		protected void Reset() {
			lock (lockObj)
				sections.Clear();
		}

		public ISettingsSection GetOrCreateSection(Guid guid) {
			if (guid == Guid.Empty)
				throw new ArgumentOutOfRangeException(nameof(guid));

			var name = guid.ToString();
			lock (lockObj) {
				if (sections.TryGetValue(name, out var section))
					return section;

				section = new SettingsSection(name);
				sections[name] = section;
				return section;
			}
		}

		public void RemoveSection(Guid guid) {
			if (guid == Guid.Empty)
				throw new ArgumentOutOfRangeException(nameof(guid));

			var name = guid.ToString();
			lock (lockObj)
				sections.Remove(name);
		}

		public void RemoveSection(ISettingsSection section) {
			Debug2.Assert(section is not null);
			if (section is null)
				throw new ArgumentNullException(nameof(section));

			lock (lockObj) {
				bool b = sections.TryGetValue(section.Name, out var other);
				Debug.Assert(b && other == section);
				if (!b || other != section)
					return;

				sections.Remove(section.Name);
			}
		}

		public ISettingsSection RecreateSection(Guid guid) {
			if (guid == Guid.Empty)
				throw new ArgumentOutOfRangeException(nameof(guid));

			lock (lockObj) {
				RemoveSection(guid);
				return GetOrCreateSection(guid);
			}
		}
	}
}
