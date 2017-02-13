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
using System.ComponentModel.Composition;
using System.Diagnostics;
using dnSpy.Contracts.Command;
using dnSpy.Contracts.Hex.Editor;
using dnSpy.Contracts.Hex.Intellisense;

namespace dnSpy.Hex.Intellisense.DnSpy {
	[ExportCommandTargetFilterProvider(CommandTargetFilterOrder.HexDefaultIntellisenseQuickInfo)]
	sealed class QuickInfoCommandTargetFilterProvider : ICommandTargetFilterProvider {
		readonly HexQuickInfoBroker quickInfoBroker;

		[ImportingConstructor]
		QuickInfoCommandTargetFilterProvider(HexQuickInfoBroker quickInfoBroker) {
			this.quickInfoBroker = quickInfoBroker;
		}

		public ICommandTargetFilter Create(object target) {
			var hexView = target as HexView;
			if (hexView != null)
				return new QuickInfoCommandTargetFilter(quickInfoBroker, hexView);
			return null;
		}
	}

	sealed class QuickInfoCommandTargetFilter : ICommandTargetFilter {
		readonly HexQuickInfoBroker quickInfoBroker;
		readonly HexView hexView;
		HexQuickInfoSession quickInfoSession;

		public QuickInfoCommandTargetFilter(HexQuickInfoBroker quickInfoBroker, HexView hexView) {
			this.quickInfoBroker = quickInfoBroker ?? throw new ArgumentNullException(nameof(quickInfoBroker));
			this.hexView = hexView ?? throw new ArgumentNullException(nameof(hexView));
		}

		public CommandTargetStatus CanExecute(Guid group, int cmdId) {
			if (group == CommandConstants.HexEditorGroup) {
				switch ((HexEditorIds)cmdId) {
				case HexEditorIds.QUICKINFO:
					return CommandTargetStatus.Handled;
				}
			}
			return CommandTargetStatus.NotHandled;
		}

		public CommandTargetStatus Execute(Guid group, int cmdId, object args = null) {
			object result = null;
			return Execute(group, cmdId, args, ref result);
		}

		public CommandTargetStatus Execute(Guid group, int cmdId, object args, ref object result) {
			if (group == CommandConstants.HexEditorGroup) {
				switch ((HexEditorIds)cmdId) {
				case HexEditorIds.QUICKINFO:
					TriggerQuickInfo();
					return CommandTargetStatus.Handled;
				}
			}
			return CommandTargetStatus.NotHandled;
		}

		void TriggerQuickInfo() {
			if (quickInfoSession != null) {
				quickInfoSession.Dismissed -= QuickInfoSession_Dismissed;
				quickInfoSession.Dismiss();
				quickInfoSession = null;
			}
			quickInfoSession = quickInfoBroker.TriggerQuickInfo(hexView, hexView.Caret.Position.Position.ActivePosition, false);
			if (quickInfoSession?.IsDismissed == false)
				quickInfoSession.Dismissed += QuickInfoSession_Dismissed;
			else
				quickInfoSession = null;
		}

		void QuickInfoSession_Dismissed(object sender, EventArgs e) {
			var qiSession = (HexQuickInfoSession)sender;
			qiSession.Dismissed -= QuickInfoSession_Dismissed;
			Debug.Assert(qiSession == quickInfoSession);
			if (qiSession == quickInfoSession)
				quickInfoSession = null;
		}

		public void SetNextCommandTarget(ICommandTarget commandTarget) { }
		public void Dispose() { }
	}
}
