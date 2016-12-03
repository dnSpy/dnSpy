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
using dnSpy.Contracts.App;
using dnSpy.Contracts.ToolWindows.App;

namespace dnSpy.MainApp {
	[Export(typeof(IAppCommandLineArgsHandler))]
	sealed class AppCommandLineArgsHandler : IAppCommandLineArgsHandler {
		readonly IDsToolWindowService toolWindowService;

		[ImportingConstructor]
		AppCommandLineArgsHandler(IDsToolWindowService toolWindowService) {
			this.toolWindowService = toolWindowService;
		}

		public double Order => 0;

		sealed class ToolWindowInfo {
			public Guid Guid { get; }
			public AppToolWindowLocation? Location { get; set; }

			public ToolWindowInfo(Guid guid) {
				Guid = guid;
			}
		}

		public void OnNewArgs(IAppCommandLineArgs args) {
			foreach (var info in GetToolWindowInfos(args.HideToolWindow))
				toolWindowService.Close(info.Guid);
			foreach (var info in GetToolWindowInfos(args.ShowToolWindow)) {
				var content = toolWindowService.Show(info.Guid, info.Location);
				Debug.Assert(content != null);
				if (content == null)
					continue;
				if (info.Location == null)
					continue;
				if (toolWindowService.CanMove(content, info.Location.Value))
					toolWindowService.Move(content, info.Location.Value);
			}
		}

		IEnumerable<ToolWindowInfo> GetToolWindowInfos(string arg) {
			if (string.IsNullOrEmpty(arg))
				yield break;
			foreach (var tw in arg.Split(new char[] { ',' })) {
				var opts = tw.Split(new char[] { '!' }, 2);
				Guid guid;
				bool b = Guid.TryParse(opts[0], out guid);
				Debug.Assert(b);
				if (!b)
					continue;
				var info = new ToolWindowInfo(guid);
				if (opts.Length == 2)
					info.Location = GetLocation(opts[1]);
				yield return info;
			}
		}

		static AppToolWindowLocation? GetLocation(string arg) {
			switch (arg.Trim()) {
			case "l":
			case "left":
				return AppToolWindowLocation.Left;
			case "r":
			case "right":
				return AppToolWindowLocation.Right;
			case "t":
			case "top":
				return AppToolWindowLocation.Top;
			case "b":
			case "bottom":
				return AppToolWindowLocation.Bottom;
			case "d":
			case "default":
			case "dh":
			case "default-horiz":
			case "default-horizontal":
				return AppToolWindowLocation.DefaultHorizontal;
			case "dv":
			case "default-vert":
			case "default-vertical":
				return AppToolWindowLocation.DefaultVertical;
			}
			Debug.Fail("Invalid option");
			return null;
		}
	}
}
