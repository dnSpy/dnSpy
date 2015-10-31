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
using System.Diagnostics;
using System.Xml.Linq;
using ICSharpCode.ILSpy;

namespace dnSpy.Debugger.Exceptions {
	enum ExceptionDiffType {
		Remove,
		AddOrUpdate,
	}

	sealed class ExceptionListSettings {
		public static readonly ExceptionListSettings Instance = new ExceptionListSettings();
		const string SETTINGS_NAME = "Exceptions";
		int disableSaveCounter;

		ExceptionListSettings() {
			ExceptionManager.Instance.Changed += ExceptionManager_Changed;
		}

		private void ExceptionManager_Changed(object sender, ExceptionManagerEventArgs e) {
			Save();
		}

		void Save() {
			DNSpySettings.Update(root => Save(root));
		}

		internal void OnLoaded() {
			disableSaveCounter++;
			try {
				LoadInternal();
			}
			finally {
				disableSaveCounter--;
			}
		}

		void LoadInternal() {
			DNSpySettings settings = DNSpySettings.Load();
			var exs = settings[SETTINGS_NAME];
			ExceptionManager.Instance.RestoreDefaults();
			foreach (var exx in exs.Elements("Exception")) {
				var exceptionType = (ExceptionType?)(int?)exx.Attribute("ExceptionType");
				var fullName = SessionSettings.Unescape((string)exx.Attribute("FullName"));
				bool? breakOnFirstChance = (bool?)exx.Attribute("BreakOnFirstChance");
				bool isOtherExceptions = (bool?)exx.Attribute("IsOtherExceptions") ?? false;
				var diffType = (ExceptionDiffType?)(int?)exx.Attribute("DiffType");

				if (diffType == null)
					continue;
				if (exceptionType == null || (int)exceptionType.Value < 0 || exceptionType.Value >= ExceptionType.Last)
					continue;
				if (fullName == null)
					continue;

				var key = new ExceptionInfoKey(exceptionType.Value, fullName);
				switch (diffType.Value) {
				case ExceptionDiffType.Remove:
					ExceptionManager.Instance.Remove(key);
					break;

				case ExceptionDiffType.AddOrUpdate:
					if (breakOnFirstChance == null)
						continue;
					ExceptionManager.Instance.AddOrUpdate(key, breakOnFirstChance.Value, isOtherExceptions);
					break;

				default:
					Debug.Fail("Unknown ExceptionDiffType");
					break;
				}
			}
		}

		void Save(XElement root) {
			// Prevent Load() from saving the settings every time a new exception is added
			if (disableSaveCounter != 0)
				return;

			var exs = new XElement(SETTINGS_NAME);
			var existingElement = root.Element(SETTINGS_NAME);
			if (existingElement != null)
				existingElement.ReplaceWith(exs);
			else
				root.Add(exs);

			foreach (var tuple in GetDiff()) {
				var exx = new XElement("Exception");
				exx.SetAttributeValue("ExceptionType", (int)tuple.Item2.ExceptionType);
				exx.SetAttributeValue("FullName", SessionSettings.Escape(tuple.Item2.Name));
				exx.SetAttributeValue("BreakOnFirstChance", tuple.Item2.BreakOnFirstChance);
				if (tuple.Item2.IsOtherExceptions)
					exx.SetAttributeValue("IsOtherExceptions", tuple.Item2.IsOtherExceptions);
				exx.SetAttributeValue("DiffType", (int)tuple.Item1);
				exs.Add(exx);
			}
		}

		static IEnumerable<Tuple<ExceptionDiffType, ExceptionInfo>> GetDiff() {
			var defaultInfos = new Dictionary<ExceptionInfoKey, ExceptionInfo>();
			foreach (var info in DefaultExceptionSettings.Instance.ExceptionInfos)
				defaultInfos[info.Key] = info;

			foreach (var info in ExceptionManager.Instance.ExceptionInfos) {
				if (info.IsOtherExceptions) {
					if (!info.BreakOnFirstChance)
						continue;
					yield return Tuple.Create(ExceptionDiffType.AddOrUpdate, info);
					continue;
				}

				ExceptionInfo info2;
				if (defaultInfos.TryGetValue(info.Key, out info2)) {
					defaultInfos.Remove(info.Key);
					if (info.Equals(info2))
						continue;
				}
				yield return Tuple.Create(ExceptionDiffType.AddOrUpdate, info);
			}
			foreach (var info in defaultInfos.Values)
				yield return Tuple.Create(ExceptionDiffType.Remove, info);
		}

		sealed class TemporarilyDisableSaveHelper : IDisposable {
			readonly ExceptionListSettings settings;

			public TemporarilyDisableSaveHelper(ExceptionListSettings settings) {
				this.settings = settings;
				settings.disableSaveCounter++;
			}

			public void Dispose() {
				settings.disableSaveCounter--;
				if (settings.disableSaveCounter == 0)
					settings.Save();
			}
		}

		public IDisposable TemporarilyDisableSave() {
			return new TemporarilyDisableSaveHelper(this);
		}
	}
}
