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
using dnSpy.Contracts.Settings;

namespace dnSpy.Debugger.ToolWindows.Watch {
	abstract class WatchWindowExpressionsSettings {
		public abstract string[] GetExpressions(int windowIndex);
		public abstract void SetExpressions(int windowIndex, string[] expressions);
	}

	[Export(typeof(WatchWindowExpressionsSettings))]
	sealed class WatchWindowExpressionsSettingsImpl : WatchWindowExpressionsSettings {
		static readonly Guid SETTINGS_GUID = new Guid("4052EF89-2381-4CD0-B296-A5497C204B98");

		sealed class WatchInfo {
			public int WindowIndex { get; }
			public ISettingsSection? Section { get; set; }
			public string[] Expressions { get; set; }
			public WatchInfo(int windowIndex) {
				WindowIndex = windowIndex;
				Expressions = Array.Empty<string>();
			}
		}

		readonly ISettingsService settingsService;
		readonly ISettingsSection watchSection;
		readonly WatchInfo[] infos;

		[ImportingConstructor]
		WatchWindowExpressionsSettingsImpl(ISettingsService settingsService) {
			this.settingsService = settingsService;
			watchSection = settingsService.GetOrCreateSection(SETTINGS_GUID);
			infos = new WatchInfo[WatchWindowsHelper.NUMBER_OF_WATCH_WINDOWS];
			for (int i = 0; i < infos.Length; i++)
				infos[i] = new WatchInfo(i);

			var expressions = new List<string>();
			foreach (var sect in watchSection.SectionsWithName("Watch")) {
				var index = sect.Attribute<int?>("Index") ?? -1;
				if ((uint)index >= (uint)infos.Length)
					continue;
				var info = infos[index];
				info.Section = sect;
				expressions.Clear();
				foreach (var exprSect in sect.SectionsWithName("Expression")) {
					var expr = exprSect.Attribute<string>("Value");
					if (expr is null)
						continue;
					expressions.Add(expr);
				}
				info.Expressions = expressions.ToArray();
			}
		}

		public override string[] GetExpressions(int windowIndex) {
			if ((uint)windowIndex >= (uint)infos.Length)
				throw new ArgumentOutOfRangeException(nameof(windowIndex));
			return infos[windowIndex].Expressions;
		}

		public override void SetExpressions(int windowIndex, string[] expressions) {
			if ((uint)windowIndex >= (uint)infos.Length)
				throw new ArgumentOutOfRangeException(nameof(windowIndex));
			var info = infos[windowIndex];
			info.Expressions = expressions ?? throw new ArgumentNullException(nameof(expressions));
			Save(info);
		}

		void Save(WatchInfo info) {
			if (info.Section is not null)
				watchSection.RemoveSection(info.Section);
			var sect = info.Section = watchSection.CreateSection("Watch");
			sect.Attribute("Index", info.WindowIndex);
			foreach (var expr in info.Expressions) {
				var exprSect = sect.CreateSection("Expression");
				exprSect.Attribute("Value", expr);
			}
		}
	}
}
