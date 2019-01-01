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
using System.ComponentModel.Composition;
using System.Diagnostics;
using dnSpy.Contracts.Command;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Editor;
using dnSpy.Roslyn.Internal.SignatureHelp;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Roslyn.Intellisense.SignatureHelp {
	[ExportCommandTargetFilterProvider(CommandTargetFilterOrder.IntellisenseRoslynSignatureHelp)]
	sealed class DefaultTextViewCommandTargetFilterProvider : ICommandTargetFilterProvider {
		readonly Lazy<ISignatureHelpBroker> signatureHelpBroker;

		[ImportingConstructor]
		DefaultTextViewCommandTargetFilterProvider(Lazy<ISignatureHelpBroker> signatureHelpBroker) => this.signatureHelpBroker = signatureHelpBroker;

		public ICommandTargetFilter Create(object target) {
			if (target is ITextView textView && textView.Roles.ContainsAll(roles))
				return new CommandTargetFilter(textView, signatureHelpBroker);
			return null;
		}
		static readonly string[] roles = new string[] {
			PredefinedDsTextViewRoles.RoslynCodeEditor,
			PredefinedTextViewRoles.Editable,
		};
	}

	sealed class CommandTargetFilter : ICommandTargetFilter {
		readonly ITextView textView;
		readonly Lazy<ISignatureHelpBroker> signatureHelpBroker;
		SignatureHelpSession session;

		public CommandTargetFilter(ITextView textView, Lazy<ISignatureHelpBroker> signatureHelpBroker) {
			this.textView = textView ?? throw new ArgumentNullException(nameof(textView));
			this.signatureHelpBroker = signatureHelpBroker ?? throw new ArgumentNullException(nameof(signatureHelpBroker));
			textView.Caret.PositionChanged += Caret_PositionChanged;
		}

		bool IsSupportedContentType => textView.TextDataModel.ContentType.IsOfType(ContentTypes.RoslynCode);

		public CommandTargetStatus CanExecute(Guid group, int cmdId) {
			if (!IsSupportedContentType)
				return CommandTargetStatus.NotHandled;
			if (group == CommandConstants.TextEditorGroup) {
				switch ((TextEditorIds)cmdId) {
				case TextEditorIds.TYPECHAR:
				case TextEditorIds.PARAMINFO:
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
			if (!IsSupportedContentType)
				return CommandTargetStatus.NotHandled;

			var oldSnapshot = textView.TextSnapshot;

			// Make sure that changes to the text buffer have been applied before we try
			// to get the sig helps from Roslyn.
			nextCommandTarget.Execute(group, cmdId, args, ref result);

			if (group == CommandConstants.TextEditorGroup) {
				switch ((TextEditorIds)cmdId) {
				case TextEditorIds.TYPECHAR:
					var s = args as string;
					if (s == null || s.Length != 1)
						break;
					if (session != null) {
						if (session.IsRetriggerCharacter(s[0]))
							TriggerSession(new SignatureHelpTriggerInfo(SignatureHelpTriggerReason.RetriggerCommand, s[0]));
						else if (session.IsTriggerCharacter(s[0]))
							TriggerSession(new SignatureHelpTriggerInfo(SignatureHelpTriggerReason.TypeCharCommand, s[0]));
					}
					else
						TriggerSession(new SignatureHelpTriggerInfo(SignatureHelpTriggerReason.TypeCharCommand, s[0]));
					return CommandTargetStatus.Handled;

				case TextEditorIds.PARAMINFO:
					TriggerSession(new SignatureHelpTriggerInfo(SignatureHelpTriggerReason.InvokeSignatureHelpCommand));
					return CommandTargetStatus.Handled;
				}
			}

			// Need to retrigger it if user backspaced and deleted a comma. We need to check for it
			// here because Caret-pos-changed handler doesn't retrigger it for perf reasons.
			if (session != null && oldSnapshot != textView.TextSnapshot)
				TriggerSession(new SignatureHelpTriggerInfo(SignatureHelpTriggerReason.RetriggerCommand));

			return CommandTargetStatus.Handled;
		}

		void TriggerSession(SignatureHelpTriggerInfo triggerInfo) {
			var position = textView.Caret.Position.BufferPosition;

			if (session == null) {
				session = SignatureHelpSession.TryCreate(position, triggerInfo, signatureHelpBroker, textView);
				if (session == null)
					return;
				session.Disposed += SignatureHelpSession_Disposed;
			}
			session.Restart(position, triggerInfo);
		}

		void SignatureHelpSession_Disposed(object sender, EventArgs e) {
			var session = (SignatureHelpSession)sender;
			session.Disposed -= SignatureHelpSession_Disposed;
			if (this.session == session)
				this.session = null;
		}

		void Caret_PositionChanged(object sender, CaretPositionChangedEventArgs e) {
			if (session == null)
				return;
			// PERF: retriggering is very slow
			if (ShouldRetrigger(e))
				TriggerSession(new SignatureHelpTriggerInfo(SignatureHelpTriggerReason.RetriggerCommand));
		}

		bool ShouldRetrigger(CaretPositionChangedEventArgs e) {
			// This case is handled by the command handler
			if (e.OldPosition.BufferPosition.Snapshot != e.NewPosition.BufferPosition.Snapshot)
				return false;

			if (IsRetriggerCharacter(e.OldPosition) || IsRetriggerCharacter(e.NewPosition))
				return true;

			if (e.OldPosition.VirtualBufferPosition.IsInVirtualSpace != e.NewPosition.VirtualBufferPosition.IsInVirtualSpace)
				return true;
			if (e.OldPosition.VirtualSpaces > 0)
				return false;
			var newPos = e.NewPosition.BufferPosition;
			var oldPos = e.OldPosition.BufferPosition;
			int diff = Math.Abs(oldPos.Position - newPos.Position);
			return diff > 1;
		}

		bool IsRetriggerCharacter(CaretPosition caretPos) {
			Debug.Assert(session != null);
			if (session == null)
				return false;
			if (caretPos.VirtualSpaces > 0)
				return false;
			char c = caretPos.BufferPosition.GetChar();
			return session.IsTriggerCharacter(c) || session.IsRetriggerCharacter(c);
		}

		void CancelSession() {
			session?.Dispose();
			session = null;
		}

		public void SetNextCommandTarget(ICommandTarget commandTarget) => nextCommandTarget = commandTarget;
		ICommandTarget nextCommandTarget;

		public void Dispose() {
			CancelSession();
			session = null;
			textView.Caret.PositionChanged -= Caret_PositionChanged;
		}
	}
}
