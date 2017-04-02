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
using System.ComponentModel;
using System.Diagnostics;
using dnSpy.Contracts.Debugger.Breakpoints.Code;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.MVVM;
using dnSpy.Contracts.Text.Classification;
using dnSpy.Debugger.Breakpoints.Code;
using dnSpy.Debugger.UI;

namespace dnSpy.Debugger.ToolWindows.CodeBreakpoints {
	sealed class CodeBreakpointVM : ViewModelBase {
		public bool IsEnabled {
			get => settings.IsEnabled;
			set {
				if (settings.IsEnabled == value)
					return;
				CodeBreakpoint.IsEnabled = value;
			}
		}

		public ImageReference ImageReference => BreakpointImageUtilities.GetImage(breakpointKind);

		public ICodeBreakpointContext Context { get; }
		public DbgCodeBreakpoint CodeBreakpoint { get; }
		public object NameObject => new FormatterObject<CodeBreakpointVM>(this, PredefinedTextClassifierTags.CodeBreakpointsWindowName);
		public object ConditionObject => new FormatterObject<CodeBreakpointVM>(this, PredefinedTextClassifierTags.CodeBreakpointsWindowCondition);
		public object HitCountObject => new FormatterObject<CodeBreakpointVM>(this, PredefinedTextClassifierTags.CodeBreakpointsWindowHitCount);
		public object FilterObject => new FormatterObject<CodeBreakpointVM>(this, PredefinedTextClassifierTags.CodeBreakpointsWindowFilter);
		public object WhenHitObject => new FormatterObject<CodeBreakpointVM>(this, PredefinedTextClassifierTags.CodeBreakpointsWindowWhenHit);
		public object ModuleObject => new FormatterObject<CodeBreakpointVM>(this, PredefinedTextClassifierTags.CodeBreakpointsWindowModule);
		internal int Order { get; }

		DbgCodeBreakpointSettings settings;
		BreakpointKind breakpointKind;

		internal DbgBreakpointLocationFormatter BreakpointLocationFormatter { get; }

		public CodeBreakpointVM(DbgCodeBreakpoint codeBreakpoint, DbgBreakpointLocationFormatter dbgBreakpointLocationFormatter, ICodeBreakpointContext context, int order) {
			CodeBreakpoint = codeBreakpoint ?? throw new ArgumentNullException(nameof(codeBreakpoint));
			Context = context ?? throw new ArgumentNullException(nameof(context));
			Order = order;
			BreakpointLocationFormatter = dbgBreakpointLocationFormatter ?? throw new ArgumentNullException(nameof(dbgBreakpointLocationFormatter));
			settings = CodeBreakpoint.Settings;
			breakpointKind = BreakpointImageUtilities.GetBreakpointKind(ref settings);
			dbgBreakpointLocationFormatter.PropertyChanged += DbgBreakpointLocationFormatter_PropertyChanged;
		}

		void DbgBreakpointLocationFormatter_PropertyChanged(object sender, PropertyChangedEventArgs e) {
			switch (e.PropertyName) {
			case DbgBreakpointLocationFormatter.NameProperty:
				OnPropertyChanged(nameof(NameObject));
				break;

			case DbgBreakpointLocationFormatter.ModuleProperty:
				OnPropertyChanged(nameof(ModuleObject));
				break;

			default:
				Debug.Fail($"Unknown property: {e.PropertyName}");
				break;
			}
		}

		// UI thread
		internal void RefreshThemeFields_UI() {
			Context.UIDispatcher.VerifyAccess();
			OnPropertyChanged(nameof(NameObject));
			OnPropertyChanged(nameof(ConditionObject));
			OnPropertyChanged(nameof(HitCountObject));
			OnPropertyChanged(nameof(FilterObject));
			OnPropertyChanged(nameof(WhenHitObject));
			OnPropertyChanged(nameof(ModuleObject));
		}

		// UI thread
		internal void RefreshNameColumn_UI() {
			Context.UIDispatcher.VerifyAccess();
			OnPropertyChanged(nameof(NameObject));
		}

		// UI thread
		internal void UpdateSettings_UI(DbgCodeBreakpointSettings newSettings) {
			Context.UIDispatcher.VerifyAccess();
			var oldSettings = settings;
			settings = newSettings;
			if (oldSettings.IsEnabled != newSettings.IsEnabled)
				OnPropertyChanged(nameof(IsEnabled));
			var newBreakpointKind = BreakpointImageUtilities.GetBreakpointKind(ref settings);
			if (newBreakpointKind != breakpointKind) {
				breakpointKind = newBreakpointKind;
				OnPropertyChanged(nameof(ImageReference));
			}
			if (oldSettings.Condition != newSettings.Condition)
				OnPropertyChanged(nameof(ConditionObject));
			if (oldSettings.HitCount != newSettings.HitCount)
				OnPropertyChanged(nameof(HitCountObject));
			if (oldSettings.Filter != newSettings.Filter)
				OnPropertyChanged(nameof(FilterObject));
			if (oldSettings.Trace != newSettings.Trace)
				OnPropertyChanged(nameof(WhenHitObject));
		}

		// UI thread
		internal void Dispose() {
			Context.UIDispatcher.VerifyAccess();
			BreakpointLocationFormatter.PropertyChanged -= DbgBreakpointLocationFormatter_PropertyChanged;
		}
	}
}
