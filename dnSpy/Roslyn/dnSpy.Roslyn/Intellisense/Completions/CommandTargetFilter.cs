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
using dnSpy.Contracts.Language.Intellisense;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Editor;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Roslyn.Intellisense.Completions {
	[ExportCommandTargetFilterProvider(CommandTargetFilterOrder.IntellisenseRoslynStatmentCompletion)]
	sealed class DefaultTextViewCommandTargetFilterProvider : ICommandTargetFilterProvider {
		readonly Lazy<ICompletionBroker> completionBroker;

		[ImportingConstructor]
		DefaultTextViewCommandTargetFilterProvider(Lazy<ICompletionBroker> completionBroker) => this.completionBroker = completionBroker;

		public ICommandTargetFilter? Create(object target) {
			if (target is ITextView textView && textView.Roles.ContainsAll(roles))
				return new CommandTargetFilter(textView, completionBroker);
			return null;
		}
		static readonly string[] roles = new string[] {
			PredefinedDsTextViewRoles.RoslynCodeEditor,
			PredefinedTextViewRoles.Editable,
		};
	}

	sealed class CommandTargetFilter : ICommandTargetFilter {
		readonly ITextView textView;
		readonly Lazy<ICompletionBroker> completionBroker;
		ICompletionSession? completionSession;

		public CommandTargetFilter(ITextView textView, Lazy<ICompletionBroker> completionBroker) {
			this.textView = textView ?? throw new ArgumentNullException(nameof(textView));
			this.completionBroker = completionBroker ?? throw new ArgumentNullException(nameof(completionBroker));
		}

		CompletionService? TryGetRoslynCompletionService() => CompletionInfo.Create(textView.TextSnapshot)?.CompletionService;

		EnterKeyRule? TryGetEnterKeyRule() {
			if (!HasSession)
				return null;
			Debug2.Assert(!(completionSession is null));

			if (completionSession.SelectedCompletionSet?.SelectionStatus.Completion is RoslynCompletion completion)
				return completion.CompletionItem.Rules.EnterKeyRule;

			return TryGetRoslynCompletionService()?.GetRules().DefaultEnterKeyRule;
		}

		bool IsSupportedContentType => textView.TextDataModel.ContentType.IsOfType(ContentTypes.RoslynCode);

		public CommandTargetStatus CanExecute(Guid group, int cmdId) {
			if (!IsSupportedContentType)
				return CommandTargetStatus.NotHandled;
			if (group == CommandConstants.TextEditorGroup) {
				switch ((TextEditorIds)cmdId) {
				case TextEditorIds.RETURN:
				case TextEditorIds.TYPECHAR:
				case TextEditorIds.BACKSPACE:
				case TextEditorIds.COMPLETEWORD:
				case TextEditorIds.SHOWMEMBERLIST:
					return CommandTargetStatus.Handled;
				}
			}
			return CommandTargetStatus.NotHandled;
		}

		public CommandTargetStatus Execute(Guid group, int cmdId, object? args = null) {
			object? result = null;
			return Execute(group, cmdId, args, ref result);
		}

		bool ShouldPassThroughEnterKey(EnterKeyRule enterKeyRule) {
			if (enterKeyRule == EnterKeyRule.Default)
				enterKeyRule = TryGetRoslynCompletionService()?.GetRules().DefaultEnterKeyRule ?? enterKeyRule;

			switch (enterKeyRule) {
			case EnterKeyRule.Never:
				return false;

			case EnterKeyRule.Always:
				return true;

			case EnterKeyRule.AfterFullyTypedWord:
				if (!HasSession)
					return false;
				Debug2.Assert(!(completionSession is null));
				var completion = completionSession.SelectedCompletionSet?.SelectionStatus.Completion;
				if (completion is null)
					return false;
				var span = completionSession.SelectedCompletionSet!.ApplicableTo;
				var text = span.GetText(span.TextBuffer.CurrentSnapshot);
				return text.Equals(completion.TryGetFilterText(), StringComparison.CurrentCultureIgnoreCase);

			case EnterKeyRule.Default:
				return false;

			default:
				Debug.Fail($"New {nameof(EnterKeyRule)} value: {enterKeyRule}");
				goto case EnterKeyRule.Default;
			}
		}

		bool TryCommitCharacter(char c) {
			if (!HasSession)
				return false;
			Debug2.Assert(!(completionSession is null));
			var completionService = TryGetRoslynCompletionService();
			if (completionService is null)
				return false;
			var rules = completionService.GetRules();
			if (rules.DefaultCommitCharacters.Contains(c)) {
				completionSession.Commit();
				return true;
			}
			return false;
		}

		public CommandTargetStatus Execute(Guid group, int cmdId, object? args, ref object? result) {
			if (!IsSupportedContentType)
				return CommandTargetStatus.NotHandled;

			if (HasSession) {
				Debug2.Assert(!(completionSession is null));
				if (group == CommandConstants.TextEditorGroup) {
					switch ((TextEditorIds)cmdId) {
					case TextEditorIds.RETURN:
						// Cache it because it could read from text buffer which gets modified by Commit()
						bool passThrough = ShouldPassThroughEnterKey(TryGetEnterKeyRule() ?? EnterKeyRule.Default);
						if (completionSession.SelectedCompletionSet?.SelectionStatus.IsSelected != true) {
							passThrough = true;
							completionSession.Dismiss();
						}
						else
							completionSession.Commit();
						if (!passThrough)
							return CommandTargetStatus.Handled;
						break;

					case TextEditorIds.TYPECHAR:
						if (HasSession && completionSession.SelectedCompletionSet?.SelectionStatus.IsSelected == true) {
							var s = args as string;
							if (s is null || s.Length != 1)
								break;
							TryCommitCharacter(s[0]);
						}
						break;
					}
				}
			}

			// Make sure that changes to the text buffer have been applied before we try
			// to get the completions from Roslyn.
			nextCommandTarget!.Execute(group, cmdId, args, ref result);

			if (group == CommandConstants.TextEditorGroup) {
				switch ((TextEditorIds)cmdId) {
				case TextEditorIds.TYPECHAR:
					if (!HasSession) {
						var s = args as string;
						if (s is null || s.Length != 1)
							break;
						TryStartSession(s[0], false);
					}
					break;

				case TextEditorIds.BACKSPACE:
					if (!HasSession)
						TryStartSession('\b', true);
					break;

				case TextEditorIds.COMPLETEWORD:
					StartSession();
					if (HasSession) {
						Debug2.Assert(!(completionSession is null));
						if (completionSession.SelectedCompletionSet?.SelectionStatus.IsUnique == true)
							completionSession.Commit();
					}
					return CommandTargetStatus.Handled;

				case TextEditorIds.SHOWMEMBERLIST:
					StartSession();
					return CommandTargetStatus.Handled;
				}
			}
			return CommandTargetStatus.Handled;
		}

		bool TryStartSession(char c, bool isDelete) {
			if (HasSession)
				return false;

			var info = CompletionInfo.Create(textView.TextSnapshot);
			if (info is null)
				return false;
			int pos = textView.Caret.Position.BufferPosition.Position;
			var completionTrigger = isDelete ? CompletionTrigger.CreateDeletionTrigger(c) : CompletionTrigger.CreateInsertionTrigger(c);
			if (!info.Value.CompletionService.ShouldTriggerCompletion(info.Value.SourceText, pos, completionTrigger))
				return false;

			StartSession(info, completionTrigger);
			return HasSession;
		}

		bool HasSession => !(completionSession is null);

		void StartSession(CompletionInfo? info = null, CompletionTrigger? completionTrigger = null) {
			if (HasSession)
				return;
			var triggerPoint = textView.TextSnapshot.CreateTrackingPoint(textView.Caret.Position.BufferPosition.Position, PointTrackingMode.Negative, TrackingFidelityMode.Forward);
			completionSession = completionBroker.Value.CreateCompletionSession(textView, triggerPoint, trackCaret: true);
			if (!(completionTrigger is null))
				completionSession.Properties.AddProperty(typeof(CompletionTrigger), completionTrigger);
			completionSession.Dismissed += CompletionSession_Dismissed;
			completionSession.Start();
		}

		void CompletionSession_Dismissed(object? sender, EventArgs e) {
			var session = (ICompletionSession)sender!;
			session.Dismissed -= CompletionSession_Dismissed;
			if (completionSession == session)
				completionSession = null;
		}

		public void SetNextCommandTarget(ICommandTarget commandTarget) => nextCommandTarget = commandTarget;
		ICommandTarget? nextCommandTarget;

		public void Dispose() { }
	}
}
