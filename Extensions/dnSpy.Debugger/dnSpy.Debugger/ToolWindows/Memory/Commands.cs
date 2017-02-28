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
using System.Diagnostics;
using System.Linq;
using System.Text;
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.Hex.Editor;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Utilities;

namespace dnSpy.Debugger.ToolWindows.Memory {
	static class Constants {
		public const string PROCESSES_GUID = "992DE40A-1063-415F-97C5-A7D520154505";
		public const string GROUP_PROCESSES = "0,44434B63-B5B6-4454-9D94-1AFD8B10819D";
	}

	[ExportMenuItem(Header = "res:ProcessCommand", Icon = DsImagesAttribute.Process, Guid = Constants.PROCESSES_GUID, Group = MenuConstants.GROUP_CTX_HEXVIEW_MISC, Order = 2000)]
	sealed class ProcessesContexMenuEntry : MenuItemBase<ProcessesContexMenuEntry.Context> {
		protected sealed override object CachedContextKey => ContextKey;
		static readonly object ContextKey = new object();

		internal sealed class Context {
			public HexBuffer Buffer { get; }
			public Context(HexBuffer buffer) => Buffer = buffer;
		}

		readonly ProcessHexBufferProvider processHexBufferProvider;

		[ImportingConstructor]
		ProcessesContexMenuEntry(ProcessHexBufferProvider processHexBufferProvider) => this.processHexBufferProvider = processHexBufferProvider;

		public override void Execute(Context context) { }
		protected override Context CreateContext(IMenuItemContext context) => CreateContext(processHexBufferProvider, context);

		internal static Context CreateContext(ProcessHexBufferProvider processHexBufferProvider, IMenuItemContext context) {
			var hexView = context.Find<HexView>();
			if (hexView == null)
				return null;
			if (!processHexBufferProvider.IsValidBuffer(hexView.Buffer))
				return null;
			return new Context(hexView.Buffer);
		}
	}

	[ExportMenuItem(OwnerGuid = Constants.PROCESSES_GUID, Group = Constants.GROUP_PROCESSES, Order = 0)]
	sealed class ProcessesSubContexMenuEntry : MenuItemBase, IMenuItemProvider {
		readonly ProcessHexBufferProvider processHexBufferProvider;

		[ImportingConstructor]
		ProcessesSubContexMenuEntry(ProcessHexBufferProvider processHexBufferProvider) => this.processHexBufferProvider = processHexBufferProvider;

		public override void Execute(IMenuItemContext context) { }

		IEnumerable<CreatedMenuItem> IMenuItemProvider.Create(IMenuItemContext context) {
			var ctx = ProcessesContexMenuEntry.CreateContext(processHexBufferProvider, context);
			Debug.Assert(ctx != null);
			if (ctx == null)
				yield break;

			var currentPid = processHexBufferProvider.GetProcessId(ctx.Buffer);
			foreach (var pid in processHexBufferProvider.ProcessIds.OrderBy(a => a)) {
				var attr = new ExportMenuItemAttribute { Header = UIUtilities.EscapeMenuItemHeader(GetProcessHeader(pid)) };
				bool isChecked = pid == currentPid;
				var item = new MyMenuItem(ctx2 => processHexBufferProvider.SetProcessStream(ctx.Buffer, pid), isChecked);
				yield return new CreatedMenuItem(attr, item);
			}
		}

		string GetProcessHeader(int pid) {
			try {
				using (var p = Process.GetProcessById(pid)) {
					var title = Filter(p.MainWindowTitle, 200);
					var name = p.MainModule.ModuleName;
					if (string.IsNullOrWhiteSpace(title))
						return $"{pid} {name}";
					return $"{pid} {name} - {title}";
				}
			}
			catch {
			}
			return pid.ToString();
		}

		static string Filter(string s, int maxLength) {
			var sb = new StringBuilder();
			foreach (var c in s) {
				if (sb.Length >= maxLength) {
					sb.Append("[...]");
					break;
				}
				if (c < ' ' || Array.IndexOf(LineConstants.newLineChars, c) >= 0)
					sb.Append(' ');
				else
					sb.Append(c);
			}
			return sb.ToString();
		}
	}

	sealed class MyMenuItem : MenuItemBase {
		readonly Action<IMenuItemContext> action;
		readonly bool isChecked;

		public MyMenuItem(Action<IMenuItemContext> action, bool isChecked = false) {
			this.action = action;
			this.isChecked = isChecked;
		}

		public override void Execute(IMenuItemContext context) => action(context);
		public override bool IsChecked(IMenuItemContext context) => isChecked;
	}
}
