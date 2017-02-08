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
using System.ComponentModel.Composition;
using System.Linq;
using dnSpy.Contracts.Decompiler;
using dnSpy.Events;

namespace dnSpy.Decompiler {
	[Export(typeof(IDecompilerService))]
	sealed class DecompilerService : IDecompilerService {
		readonly DecompilerServiceSettingsImpl decompilerServiceSettings;
		readonly IDecompiler[] decompilers;

		[ImportingConstructor]
		DecompilerService(DecompilerServiceSettingsImpl decompilerServiceSettings, [ImportMany] IDecompiler[] languages, [ImportMany] IDecompilerCreator[] creators) {
			this.decompilerServiceSettings = decompilerServiceSettings;
			var langs = new List<IDecompiler>(languages);
			foreach (var creator in creators)
				langs.AddRange(creator.Create());
			if (langs.Count == 0)
				langs.Add(new DummyDecompiler());
			decompilers = langs.OrderBy(a => a.OrderUI).ToArray();
			decompiler = FindOrDefault(decompilerServiceSettings.LanguageGuid);
			decompilerChanged = new WeakEventList<EventArgs>();
		}

		public IDecompiler Decompiler {
			get { return decompiler; }
			set {
				if (value == null)
					throw new ArgumentNullException(nameof(value));
				if (Array.IndexOf(decompilers, value) < 0)
					throw new InvalidOperationException("Can't set a language that isn't part of this instance's language collection");
				if (decompiler != value) {
					decompiler = value;
					decompilerServiceSettings.LanguageGuid = value.UniqueGuid;
					decompilerChanged.Raise(this, EventArgs.Empty);
				}
			}
		}
		IDecompiler decompiler;

		public event EventHandler<EventArgs> DecompilerChanged {
			add { decompilerChanged.Add(value); }
			remove { decompilerChanged.Remove(value); }
		}
		readonly WeakEventList<EventArgs> decompilerChanged;

		public IEnumerable<IDecompiler> AllDecompilers => decompilers;
		public IDecompiler Find(Guid guid) =>
			AllDecompilers.FirstOrDefault(a => a.GenericGuid == guid || a.UniqueGuid == guid);
		public IDecompiler FindOrDefault(Guid guid) =>
			Find(guid) ?? AllDecompilers.FirstOrDefault();
	}
}
