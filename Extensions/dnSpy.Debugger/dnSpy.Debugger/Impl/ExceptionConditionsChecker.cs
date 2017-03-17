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
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Text.RegularExpressions;
using dnSpy.Contracts.Debugger.Exceptions;
using dnSpy.Debugger.Utilities;

namespace dnSpy.Debugger.Impl {
	abstract class ExceptionConditionsChecker {
		public abstract bool ShouldBreak(DbgException exception);
	}

	[Export(typeof(ExceptionConditionsChecker))]
	sealed class ExceptionConditionsCheckerImpl : ExceptionConditionsChecker {
		readonly Lazy<DbgExceptionSettingsService> dbgExceptionSettingsService;

		[ImportingConstructor]
		ExceptionConditionsCheckerImpl(Lazy<DbgExceptionSettingsService> dbgExceptionSettingsService) =>
			this.dbgExceptionSettingsService = dbgExceptionSettingsService;

		public override bool ShouldBreak(DbgException exception) {
			if (exception == null)
				throw new ArgumentNullException(nameof(exception));

			// Always break if it's an unhandled exception
			if (exception.IsUnhandled)
				return true;

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
			Debug.Assert(settings.Condition != null);
			if (settings.Condition == null)
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

		bool CompareStrings(string s, string wildcardString) {
			if (s == null || wildcardString == null)
				return false;
			return WildcardsUtils.CreateRegex(wildcardString).IsMatch(s);
		}
	}
}
