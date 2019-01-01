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
using dnSpy.Contracts.Debugger.Exceptions;
using dnSpy.Contracts.MVVM;
using dnSpy.Contracts.Text.Classification;
using dnSpy.Debugger.UI;

namespace dnSpy.Debugger.ToolWindows.Exceptions {
	sealed class ExceptionVM : ViewModelBase {
		public bool BreakWhenThrown {
			get => (settings.Flags & DbgExceptionDefinitionFlags.StopFirstChance) != 0;
			set {
				Context.UIDispatcher.VerifyAccess();
				if (BreakWhenThrown == value)
					return;
				var flags = settings.Flags;
				if (value)
					flags |= DbgExceptionDefinitionFlags.StopFirstChance;
				else
					flags &= ~DbgExceptionDefinitionFlags.StopFirstChance;
				var newSettings = new DbgExceptionSettings(flags, settings.Conditions);
				Context.ExceptionSettingsService.Modify(Definition.Id, newSettings);
			}
		}

		public DbgExceptionSettings Settings {
			get => settings;
			set {
				Context.UIDispatcher.VerifyAccess();
				if (settings == value)
					return;
				var oldBreakWhenThrown = BreakWhenThrown;
				var oldConditions = settings.Conditions;
				settings = value;
				if (oldBreakWhenThrown != BreakWhenThrown)
					OnPropertyChanged(nameof(BreakWhenThrown));
				if (!Equals(oldConditions, settings.Conditions))
					OnPropertyChanged(nameof(ConditionsObject));
			}
		}
		DbgExceptionSettings settings;

		public IExceptionContext Context { get; }
		public DbgExceptionDefinition Definition { get; }
		public object NameObject => new FormatterObject<ExceptionVM>(this, PredefinedTextClassifierTags.ExceptionSettingsWindowName);
		public object CategoryObject => new FormatterObject<ExceptionVM>(this, PredefinedTextClassifierTags.ExceptionSettingsWindowCategory);
		public object ConditionsObject => new FormatterObject<ExceptionVM>(this, PredefinedTextClassifierTags.ExceptionSettingsWindowConditions);
		internal int Order { get; }

		public ExceptionVM(in DbgExceptionSettingsInfo info, IExceptionContext context, int order) {
			Definition = info.Definition;
			settings = info.Settings;
			Context = context ?? throw new ArgumentNullException(nameof(context));
			Order = order;
		}

		static bool Equals(ReadOnlyCollection<DbgExceptionConditionSettings> a, ReadOnlyCollection<DbgExceptionConditionSettings> b) {
			if (a == b)
				return true;
			if (a == null || b == null)
				return false;
			if (a.Count != b.Count)
				return false;
			for (int i = 0; i < a.Count; i++) {
				if (a[i].ConditionType != b[i].ConditionType)
					return false;
				if (!StringComparer.Ordinal.Equals(a[i].Condition, b[i].Condition))
					return false;
			}
			return true;
		}

		// UI thread
		internal void RefreshThemeFields_UI() {
			Context.UIDispatcher.VerifyAccess();
			OnPropertyChanged(nameof(NameObject));
			OnPropertyChanged(nameof(CategoryObject));
			OnPropertyChanged(nameof(ConditionsObject));
		}

		// UI thread
		internal void Dispose() => Context.UIDispatcher.VerifyAccess();
	}
}
