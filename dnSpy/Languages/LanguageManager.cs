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

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using dnSpy.Contracts.Languages;
using dnSpy.Events;

namespace dnSpy.Languages {
	[Export, Export(typeof(ILanguageManager)), PartCreationPolicy(CreationPolicy.Shared)]
	sealed class LanguageManager : ILanguageManager {
		readonly LanguageManagerSettingsImpl languageManagerSettings;
		readonly ILanguage[] languages;

		[ImportingConstructor]
		LanguageManager(LanguageManagerSettingsImpl languageManagerSettings, [ImportMany] ILanguage[] languages, [ImportMany] ILanguageCreator[] creators) {
			this.languageManagerSettings = languageManagerSettings;
			var langs = new List<ILanguage>(languages);
			foreach (var creator in creators)
				langs.AddRange(creator.Create());
			this.languages = langs.OrderBy(a => a.OrderUI).ToArray();
			Debug.Assert(this.languages.Length != 0);
			this.selectedLanguage = FindOrDefault(languageManagerSettings.LanguageName);
			this.languageChanged = new WeakEventList<EventArgs>();
		}

		public ILanguage SelectedLanguage {
			get { return selectedLanguage; }
			set {
				if (value == null)
					throw new ArgumentNullException();
				if (Array.IndexOf(languages, value) < 0)
					throw new InvalidOperationException("Can't set a language that isn't part of this instance's language collection");
				if (selectedLanguage != value) {
					selectedLanguage = value;
					languageManagerSettings.LanguageName = value.NameUI;
					languageChanged.Raise(this, EventArgs.Empty);
				}
			}
		}
		ILanguage selectedLanguage;

		public event EventHandler<EventArgs> LanguageChanged {
			add { languageChanged.Add(value); }
			remove { languageChanged.Remove(value); }
		}
		readonly WeakEventList<EventArgs> languageChanged;

		public IEnumerable<ILanguage> Languages {
			get { return languages.AsEnumerable(); }
		}

		public ILanguage Find(string nameUI) {
			return this.Languages.FirstOrDefault(a => StringComparer.OrdinalIgnoreCase.Equals(a.NameUI, nameUI));
		}

		public ILanguage FindOrDefault(string nameUI) {
			return Find(nameUI) ?? Languages.FirstOrDefault();
		}
	}
}
