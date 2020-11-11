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
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.Exceptions;
using dnSpy.Debugger.Utilities;

namespace dnSpy.Debugger.Exceptions {
	[Export(typeof(IDbgManagerStartListener))]
	sealed class ExceptionConditionsChecker : IDbgManagerStartListener {
		readonly Lazy<DbgExceptionSettingsService> dbgExceptionSettingsService;
		readonly DebuggerSettings debuggerSettings;

		[ImportingConstructor]
		ExceptionConditionsChecker(Lazy<DbgExceptionSettingsService> dbgExceptionSettingsService, DebuggerSettings debuggerSettings) {
			this.dbgExceptionSettingsService = dbgExceptionSettingsService;
			this.debuggerSettings = debuggerSettings;
		}

		void IDbgManagerStartListener.OnStart(DbgManager dbgManager) => dbgManager.MessageExceptionThrown += DbgManager_MessageExceptionThrown;

		void DbgManager_MessageExceptionThrown(object? sender, DbgMessageExceptionThrownEventArgs e) =>
			e.Pause = ShouldBreak(e.Exception);

		bool ShouldBreak(DbgException exception) {
			if (exception.IsUnhandled)
				return !debuggerSettings.IgnoreUnhandledExceptions;

			var settings = dbgExceptionSettingsService.Value.GetSettings(exception.Id);
			if (!CheckBreakFlags(settings.Flags, exception.Flags))
				return false;
			if (!CheckConditions(settings.Conditions, exception))
				return false;

			return true;
		}

		bool CheckBreakFlags(DbgExceptionDefinitionFlags defFlags, DbgExceptionEventFlags exFlags) {
			if ((exFlags & DbgExceptionEventFlags.FirstChance) != 0 && (defFlags & DbgExceptionDefinitionFlags.StopFirstChance) != 0)
				return true;
			if ((exFlags & DbgExceptionEventFlags.SecondChance) != 0 && (defFlags & DbgExceptionDefinitionFlags.StopSecondChance) != 0)
				return true;
			return false;
		}

		bool CheckConditions(ReadOnlyCollection<DbgExceptionConditionSettings> conditions, DbgException exception) {
			if (conditions.Count != 0) {
				foreach (var condition in conditions) {
					if (!CheckConditions(condition, exception))
						return false;
				}
			}
			return true;
		}

		bool CheckConditions(DbgExceptionConditionSettings settings, DbgException exception) {
			Debug2.Assert(settings.Condition is not null);
			if (settings.Condition is null)
				return false;

			switch (settings.ConditionType) {
			case DbgExceptionConditionType.ModuleNameEquals:
				return ModuleNameEquals(settings, exception);

			case DbgExceptionConditionType.ModuleNameNotEquals:
				return !ModuleNameEquals(settings, exception);

			default:
				Debug.Fail($"Unknown condition type: {settings.ConditionType}");
				return false;
			}
		}

		bool ModuleNameEquals(DbgExceptionConditionSettings settings, DbgException exception) =>
			CompareStrings(exception.Module?.Name, settings.Condition);

		bool CompareStrings(string? s, string? wildcardString) {
			if (s is null || wildcardString is null)
				return false;
			return WildcardsUtils.CreateRegex(wildcardString).IsMatch(s);
		}
	}
}
