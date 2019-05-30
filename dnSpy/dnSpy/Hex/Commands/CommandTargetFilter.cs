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
using dnSpy.Contracts.Hex.Editor;
using dnSpy.Contracts.Hex.Editor.OptionsExtensionMethods;

namespace dnSpy.Hex.Commands {
	[ExportCommandTargetFilterProvider(CommandTargetFilterOrder.HexEditor - 1)]
	sealed class CommandTargetFilterProvider : ICommandTargetFilterProvider {
		readonly HexCommandOperationsFactoryService hexCommandOperationsFactoryService;

		[ImportingConstructor]
		CommandTargetFilterProvider(HexCommandOperationsFactoryService hexCommandOperationsFactoryService) => this.hexCommandOperationsFactoryService = hexCommandOperationsFactoryService;

		public ICommandTargetFilter? Create(object target) {
			if (target is HexView hexView)
				return new CommandTargetFilter(hexView, hexCommandOperationsFactoryService.GetCommandOperations(hexView));
			return null;
		}
	}

	sealed class CommandTargetFilter : ICommandTargetFilter {
		readonly HexView hexView;
		readonly HexCommandOperations hexCommandOperations;

		public CommandTargetFilter(HexView hexView, HexCommandOperations hexCommandOperations) {
			this.hexView = hexView ?? throw new ArgumentNullException(nameof(hexView));
			this.hexCommandOperations = hexCommandOperations ?? throw new ArgumentNullException(nameof(hexCommandOperations));
		}

		static bool IsEditCommand(Guid group, int cmdId) {
			if (group == HexCommandConstants.HexCommandGroup) {
				switch ((HexCommandIds)cmdId) {
				case HexCommandIds.GoToPositionAbsolute:
				case HexCommandIds.GoToPositionFile:
				case HexCommandIds.GoToPositionRVA:
				case HexCommandIds.GoToPositionCurrent:
				case HexCommandIds.GoToMetadataBlob:
				case HexCommandIds.GoToMetadataStrings:
				case HexCommandIds.GoToMetadataUS:
				case HexCommandIds.GoToMetadataGUID:
				case HexCommandIds.GoToMetadataTable:
				case HexCommandIds.GoToMetadataMemberRva:
				case HexCommandIds.Select:
				case HexCommandIds.SaveSelection:
				case HexCommandIds.EditLocalSettings:
				case HexCommandIds.ResetLocalSettings:
				case HexCommandIds.ToggleUseRelativePositions:
					return false;

				case HexCommandIds.FillSelection:
					return true;

				default:
					Debug.Fail($"Unknown {nameof(HexCommandIds)} value: {group} {(HexCommandIds)cmdId}");
					return true;
				}
			}
			return false;
		}

		bool IsReadOnly => hexView.Buffer.IsReadOnly || hexView.Options.DoesViewProhibitUserInput();

		public CommandTargetStatus CanExecute(Guid group, int cmdId) {
			if (IsReadOnly && IsEditCommand(group, cmdId))
				return CommandTargetStatus.NotHandled;

			if (group == HexCommandConstants.HexCommandGroup) {
				switch ((HexCommandIds)cmdId) {
				case HexCommandIds.GoToPositionAbsolute:
				case HexCommandIds.GoToPositionFile:
				case HexCommandIds.GoToPositionRVA:
				case HexCommandIds.GoToPositionCurrent:
				case HexCommandIds.GoToMetadataBlob:
				case HexCommandIds.GoToMetadataStrings:
				case HexCommandIds.GoToMetadataUS:
				case HexCommandIds.GoToMetadataGUID:
				case HexCommandIds.GoToMetadataTable:
				case HexCommandIds.GoToMetadataMemberRva:
				case HexCommandIds.Select:
				case HexCommandIds.SaveSelection:
				case HexCommandIds.FillSelection:
				case HexCommandIds.EditLocalSettings:
				case HexCommandIds.ResetLocalSettings:
				case HexCommandIds.ToggleUseRelativePositions:
					return CommandTargetStatus.Handled;
				default:
					return CommandTargetStatus.NotHandled;
				}
			}
			return CommandTargetStatus.NotHandled;
		}

		public CommandTargetStatus Execute(Guid group, int cmdId, object? args = null) {
			object? result = null;
			return Execute(group, cmdId, args, ref result);
		}

		public CommandTargetStatus Execute(Guid group, int cmdId, object? args, ref object? result) {
			if (IsReadOnly && IsEditCommand(group, cmdId))
				return CommandTargetStatus.NotHandled;

			if (group == HexCommandConstants.HexCommandGroup) {
				switch ((HexCommandIds)cmdId) {
				case HexCommandIds.GoToPositionAbsolute:
					hexCommandOperations.GoToPosition(PositionKind.Absolute);
					return CommandTargetStatus.Handled;

				case HexCommandIds.GoToPositionFile:
					hexCommandOperations.GoToPosition(PositionKind.File);
					return CommandTargetStatus.Handled;

				case HexCommandIds.GoToPositionRVA:
					hexCommandOperations.GoToPosition(PositionKind.RVA);
					return CommandTargetStatus.Handled;

				case HexCommandIds.GoToPositionCurrent:
					hexCommandOperations.GoToPosition(PositionKind.CurrentPosition);
					return CommandTargetStatus.Handled;

				case HexCommandIds.GoToMetadataBlob:
					hexCommandOperations.GoToMetadata(GoToMetadataKind.Blob);
					return CommandTargetStatus.Handled;

				case HexCommandIds.GoToMetadataStrings:
					hexCommandOperations.GoToMetadata(GoToMetadataKind.Strings);
					return CommandTargetStatus.Handled;

				case HexCommandIds.GoToMetadataUS:
					hexCommandOperations.GoToMetadata(GoToMetadataKind.US);
					return CommandTargetStatus.Handled;

				case HexCommandIds.GoToMetadataGUID:
					hexCommandOperations.GoToMetadata(GoToMetadataKind.GUID);
					return CommandTargetStatus.Handled;

				case HexCommandIds.GoToMetadataTable:
					hexCommandOperations.GoToMetadata(GoToMetadataKind.Table);
					return CommandTargetStatus.Handled;

				case HexCommandIds.GoToMetadataMemberRva:
					hexCommandOperations.GoToMetadata(GoToMetadataKind.MemberRva);
					return CommandTargetStatus.Handled;

				case HexCommandIds.Select:
					hexCommandOperations.Select();
					return CommandTargetStatus.Handled;

				case HexCommandIds.SaveSelection:
					hexCommandOperations.SaveSelection();
					return CommandTargetStatus.Handled;

				case HexCommandIds.FillSelection:
					hexCommandOperations.FillSelection();
					return CommandTargetStatus.Handled;

				case HexCommandIds.EditLocalSettings:
					hexCommandOperations.EditLocalSettings();
					return CommandTargetStatus.Handled;

				case HexCommandIds.ResetLocalSettings:
					hexCommandOperations.ResetLocalSettings();
					return CommandTargetStatus.Handled;

				case HexCommandIds.ToggleUseRelativePositions:
					hexCommandOperations.ToggleUseRelativePositions();
					return CommandTargetStatus.Handled;

				default:
					return CommandTargetStatus.NotHandled;
				}
			}
			return CommandTargetStatus.NotHandled;
		}

		public void SetNextCommandTarget(ICommandTarget commandTarget) { }
		public void Dispose() { }
	}
}
