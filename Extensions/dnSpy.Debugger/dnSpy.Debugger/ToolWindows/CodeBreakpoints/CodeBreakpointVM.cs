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
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using dnSpy.Contracts.Controls.ToolWindows;
using dnSpy.Contracts.Debugger.Breakpoints.Code;
using dnSpy.Contracts.Debugger.Text;
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

		public string? ErrorToolTip => errorToolTip;
		string? errorToolTip;

		public ImageReference ImageReference => BreakpointImageUtilities.GetImage(breakpointKind);

		public ICodeBreakpointContext Context { get; }
		public DbgCodeBreakpoint CodeBreakpoint { get; }
		public object NameObject => new FormatterObject<CodeBreakpointVM>(this, PredefinedTextClassifierTags.CodeBreakpointsWindowName);
		public object LabelsObject => new FormatterObject<CodeBreakpointVM>(this, PredefinedTextClassifierTags.CodeBreakpointsWindowLabels);
		public object ConditionObject => new FormatterObject<CodeBreakpointVM>(this, PredefinedTextClassifierTags.CodeBreakpointsWindowCondition);
		public object HitCountObject => new FormatterObject<CodeBreakpointVM>(this, PredefinedTextClassifierTags.CodeBreakpointsWindowHitCount);
		public object FilterObject => new FormatterObject<CodeBreakpointVM>(this, PredefinedTextClassifierTags.CodeBreakpointsWindowFilter);
		public object WhenHitObject => new FormatterObject<CodeBreakpointVM>(this, PredefinedTextClassifierTags.CodeBreakpointsWindowWhenHit);
		public object ModuleObject => new FormatterObject<CodeBreakpointVM>(this, PredefinedTextClassifierTags.CodeBreakpointsWindowModule);
		internal int Order { get; }

		public IEditableValue LabelsEditableValue { get; }
		public IEditValueProvider LabelsEditValueProvider { get; }

		DbgCodeBreakpointSettings settings;
		BreakpointKind breakpointKind;

		internal DbgBreakpointLocationFormatter BreakpointLocationFormatter { get; }

		public CodeBreakpointVM(DbgCodeBreakpoint codeBreakpoint, DbgBreakpointLocationFormatter dbgBreakpointLocationFormatter, ICodeBreakpointContext context, int order, IEditValueProvider labelsEditValueProvider) {
			CodeBreakpoint = codeBreakpoint ?? throw new ArgumentNullException(nameof(codeBreakpoint));
			Context = context ?? throw new ArgumentNullException(nameof(context));
			Order = order;
			LabelsEditValueProvider = labelsEditValueProvider ?? throw new ArgumentNullException(nameof(labelsEditValueProvider));
			LabelsEditableValue = new EditableValueImpl(() => GetLabelsString(), s => CodeBreakpoint.Labels = CreateLabelsCollection(s));
			BreakpointLocationFormatter = dbgBreakpointLocationFormatter ?? throw new ArgumentNullException(nameof(dbgBreakpointLocationFormatter));
			settings = CodeBreakpoint.Settings;
			breakpointKind = BreakpointImageUtilities.GetBreakpointKind(CodeBreakpoint);
			errorToolTip = CodeBreakpoint.BoundBreakpointsMessage.Severity == DbgBoundCodeBreakpointSeverity.None ? null : CodeBreakpoint.BoundBreakpointsMessage.Message;
			BreakpointLocationFormatter.PropertyChanged += DbgBreakpointLocationFormatter_PropertyChanged;
		}

		internal static ReadOnlyCollection<string> CreateLabelsCollection(string? s) =>
			new ReadOnlyCollection<string>((s ?? string.Empty).Split(new[] { CodeBreakpointFormatter.LabelsSeparatorChar }, StringSplitOptions.RemoveEmptyEntries).Select(a => a.Trim()).ToArray());

		// UI thread
		internal string GetLabelsString() {
			Context.UIDispatcher.VerifyAccess();
			var output = new DbgStringBuilderTextWriter();
			Context.Formatter.WriteLabels(output, this);
			return output.ToString();
		}

		// random thread
		void UI(Action callback) => Context.UIDispatcher.UI(callback);

		// random thread
		void DbgBreakpointLocationFormatter_PropertyChanged(object? sender, PropertyChangedEventArgs e) =>
			UI(() => DbgBreakpointLocationFormatter_PropertyChanged_UI(e.PropertyName));

		// UI thread
		void DbgBreakpointLocationFormatter_PropertyChanged_UI(string? propertyName) {
			Context.UIDispatcher.VerifyAccess();
			switch (propertyName) {
			case DbgBreakpointLocationFormatter.NameProperty:
				OnPropertyChanged(nameof(NameObject));
				break;

			case DbgBreakpointLocationFormatter.ModuleProperty:
				OnPropertyChanged(nameof(ModuleObject));
				break;

			default:
				Debug.Fail($"Unknown property: {propertyName}");
				break;
			}
		}

		// UI thread
		internal void RefreshThemeFields_UI() {
			Context.UIDispatcher.VerifyAccess();
			OnPropertyChanged(nameof(NameObject));
			OnPropertyChanged(nameof(LabelsObject));
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
		internal void OnHitCountChanged_UI() {
			Context.UIDispatcher.VerifyAccess();
			OnPropertyChanged(nameof(HitCountObject));
		}

		// UI thread
		internal void UpdateSettings_UI(DbgCodeBreakpointSettings newSettings) {
			Context.UIDispatcher.VerifyAccess();
			var oldSettings = settings;
			settings = newSettings;
			if (oldSettings.IsEnabled != newSettings.IsEnabled)
				OnPropertyChanged(nameof(IsEnabled));
			UpdateImageAndMessage_UI();
			if (!LabelsEquals(oldSettings.Labels, newSettings.Labels))
				OnPropertyChanged(nameof(LabelsObject));
			if (oldSettings.Condition != newSettings.Condition)
				OnPropertyChanged(nameof(ConditionObject));
			if (oldSettings.HitCount != newSettings.HitCount)
				OnPropertyChanged(nameof(HitCountObject));
			if (oldSettings.Filter != newSettings.Filter)
				OnPropertyChanged(nameof(FilterObject));
			if (oldSettings.Trace != newSettings.Trace)
				OnPropertyChanged(nameof(WhenHitObject));
		}

		static bool LabelsEquals(ReadOnlyCollection<string> a, ReadOnlyCollection<string> b) {
			if (a is null)
				a = emptyLabels;
			if (b is null)
				b = emptyLabels;
			if (a == b)
				return true;
			if (a.Count != b.Count)
				return false;
			for (int i = 0; i < a.Count; i++) {
				if (!StringComparer.Ordinal.Equals(a[i], b[i]))
					return false;
			}
			return true;
		}
		static readonly ReadOnlyCollection<string> emptyLabels = new ReadOnlyCollection<string>(Array.Empty<string>());

		// UI thread
		internal void UpdateImageAndMessage_UI() {
			Context.UIDispatcher.VerifyAccess();
			var newBreakpointKind = BreakpointImageUtilities.GetBreakpointKind(CodeBreakpoint);
			if (newBreakpointKind != breakpointKind) {
				breakpointKind = newBreakpointKind;
				OnPropertyChanged(nameof(ImageReference));
			}

			var msg = CodeBreakpoint.BoundBreakpointsMessage;
			var newErrorToolTip = msg.Severity == DbgBoundCodeBreakpointSeverity.None ? null : msg.Message;
			if (errorToolTip != newErrorToolTip) {
				errorToolTip = newErrorToolTip;
				OnPropertyChanged(nameof(ErrorToolTip));
			}
		}

		// UI thread
		internal void ClearEditingValueProperties() {
			Context.UIDispatcher.VerifyAccess();
			LabelsEditableValue.IsEditingValue = false;
		}

		// UI thread
		internal void Dispose() {
			Context.UIDispatcher.VerifyAccess();
			BreakpointLocationFormatter.PropertyChanged -= DbgBreakpointLocationFormatter_PropertyChanged;
			BreakpointLocationFormatter.Dispose();
			ClearEditingValueProperties();
		}

		// UI thread
		internal bool IsEditingValues {
			get {
				Context.UIDispatcher.VerifyAccess();
				return LabelsEditableValue.IsEditingValue;
			}
		}
	}
}
