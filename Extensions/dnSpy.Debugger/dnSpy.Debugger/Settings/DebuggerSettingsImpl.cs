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
using System.ComponentModel;
using System.ComponentModel.Composition;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Settings;

namespace dnSpy.Debugger.Settings {
	class DebuggerSettingsBase : DebuggerSettings {
		readonly object lockObj;

		protected DebuggerSettingsBase() => lockObj = new object();

		public override bool UseHexadecimal {
			get {
				lock (lockObj)
					return useHexadecimal;
			}
			set {
				bool modified;
				lock (lockObj) {
					modified = useHexadecimal != value;
					useHexadecimal = value;
				}
				if (modified)
					OnPropertyChanged(nameof(UseHexadecimal));
			}
		}
		bool useHexadecimal = true;

		public override bool UseDigitSeparators {
			get {
				lock (lockObj)
					return useDigitSeparators;
			}
			set {
				bool modified;
				lock (lockObj) {
					modified = useDigitSeparators != value;
					useDigitSeparators = value;
				}
				if (modified)
					OnPropertyChanged(nameof(UseDigitSeparators));
			}
		}
		bool useDigitSeparators = false;

		public override bool SyntaxHighlight {
			get {
				lock (lockObj)
					return syntaxHighlight;
			}
			set {
				bool modified;
				lock (lockObj) {
					modified = syntaxHighlight != value;
					syntaxHighlight = value;
				}
				if (modified)
					OnPropertyChanged(nameof(SyntaxHighlight));
			}
		}
		bool syntaxHighlight = true;

		public override bool AutoOpenLocalsWindow {
			get {
				lock (lockObj)
					return autoOpenLocalsWindow;
			}
			set {
				bool modified;
				lock (lockObj) {
					modified = autoOpenLocalsWindow != value;
					autoOpenLocalsWindow = value;
				}
				if (modified)
					OnPropertyChanged(nameof(AutoOpenLocalsWindow));
			}
		}
		bool autoOpenLocalsWindow = true;

		public override bool UseMemoryModules {
			get {
				lock (lockObj)
					return useMemoryModules;
			}
			set {
				bool modified;
				lock (lockObj) {
					modified = useMemoryModules != value;
					useMemoryModules = value;
				}
				if (modified)
					OnPropertyChanged(nameof(UseMemoryModules));
			}
		}
		bool useMemoryModules = false;

		public override bool PropertyEvalAndFunctionCalls {
			get {
				lock (lockObj)
					return propertyEvalAndFunctionCalls;
			}
			set {
				bool modified;
				lock (lockObj) {
					modified = propertyEvalAndFunctionCalls != value;
					propertyEvalAndFunctionCalls = value;
				}
				if (modified)
					OnPropertyChanged(nameof(PropertyEvalAndFunctionCalls));
			}
		}
		bool propertyEvalAndFunctionCalls = true;

		public override bool UseStringConversionFunction {
			get {
				lock (lockObj)
					return useStringConversionFunction;
			}
			set {
				bool modified;
				lock (lockObj) {
					modified = useStringConversionFunction != value;
					useStringConversionFunction = value;
				}
				if (modified)
					OnPropertyChanged(nameof(UseStringConversionFunction));
			}
		}
		bool useStringConversionFunction = true;

		public override bool PreventManagedDebuggerDetection {
			get {
				lock (lockObj)
					return preventManagedDebuggerDetection;
			}
			set {
				bool modified;
				lock (lockObj) {
					modified = preventManagedDebuggerDetection != value;
					preventManagedDebuggerDetection = value;
				}
				if (modified)
					OnPropertyChanged(nameof(PreventManagedDebuggerDetection));
			}
		}
		bool preventManagedDebuggerDetection = true;

		public override bool AntiIsDebuggerPresent {
			get {
				lock (lockObj)
					return antiIsDebuggerPresent;
			}
			set {
				bool modified;
				lock (lockObj) {
					modified = antiIsDebuggerPresent != value;
					antiIsDebuggerPresent = value;
				}
				if (modified)
					OnPropertyChanged(nameof(AntiIsDebuggerPresent));
			}
		}
		bool antiIsDebuggerPresent = true;

		public override bool AntiCheckRemoteDebuggerPresent {
			get {
				lock (lockObj)
					return antiCheckRemoteDebuggerPresent;
			}
			set {
				bool modified;
				lock (lockObj) {
					modified = antiCheckRemoteDebuggerPresent != value;
					antiCheckRemoteDebuggerPresent = value;
				}
				if (modified)
					OnPropertyChanged(nameof(AntiCheckRemoteDebuggerPresent));
			}
		}
		bool antiCheckRemoteDebuggerPresent = true;

		public override bool IgnoreBreakInstructions {
			get {
				lock (lockObj)
					return ignoreBreakInstructions;
			}
			set {
				bool modified;
				lock (lockObj) {
					modified = ignoreBreakInstructions != value;
					ignoreBreakInstructions = value;
				}
				if (modified)
					OnPropertyChanged(nameof(IgnoreBreakInstructions));
			}
		}
		bool ignoreBreakInstructions = false;

		public override bool BreakAllProcesses {
			get {
				lock (lockObj)
					return breakAllProcesses;
			}
			set {
				bool modified;
				lock (lockObj) {
					modified = breakAllProcesses != value;
					breakAllProcesses = value;
				}
				if (modified)
					OnPropertyChanged(nameof(BreakAllProcesses));
			}
		}
		bool breakAllProcesses = true;

		public override bool EnableManagedDebuggingAssistants {
			get {
				lock (lockObj)
					return enableManagedDebuggingAssistants;
			}
			set {
				bool modified;
				lock (lockObj) {
					modified = enableManagedDebuggingAssistants != value;
					enableManagedDebuggingAssistants = value;
				}
				if (modified)
					OnPropertyChanged(nameof(EnableManagedDebuggingAssistants));
			}
		}
		bool enableManagedDebuggingAssistants = true;

		public override bool HighlightChangedVariables {
			get {
				lock (lockObj)
					return highlightChangedVariables;
			}
			set {
				bool modified;
				lock (lockObj) {
					modified = highlightChangedVariables != value;
					highlightChangedVariables = value;
				}
				if (modified)
					OnPropertyChanged(nameof(HighlightChangedVariables));
			}
		}
		bool highlightChangedVariables = true;

		public override bool ShowRawStructureOfObjects {
			get {
				lock (lockObj)
					return showRawStructureOfObjects;
			}
			set {
				bool modified;
				lock (lockObj) {
					modified = showRawStructureOfObjects != value;
					showRawStructureOfObjects = value;
				}
				if (modified)
					OnPropertyChanged(nameof(ShowRawStructureOfObjects));
			}
		}
		bool showRawStructureOfObjects = false;

		public override bool SortParameters {
			get {
				lock (lockObj)
					return sortParameters;
			}
			set {
				bool modified;
				lock (lockObj) {
					modified = sortParameters != value;
					sortParameters = value;
				}
				if (modified)
					OnPropertyChanged(nameof(SortParameters));
			}
		}
		bool sortParameters = false;

		public override bool SortLocals {
			get {
				lock (lockObj)
					return sortLocals;
			}
			set {
				bool modified;
				lock (lockObj) {
					modified = sortLocals != value;
					sortLocals = value;
				}
				if (modified)
					OnPropertyChanged(nameof(SortLocals));
			}
		}
		bool sortLocals = false;

		public override bool GroupParametersAndLocalsTogether {
			get {
				lock (lockObj)
					return groupParametersAndLocalsTogether;
			}
			set {
				bool modified;
				lock (lockObj) {
					modified = groupParametersAndLocalsTogether != value;
					groupParametersAndLocalsTogether = value;
				}
				if (modified)
					OnPropertyChanged(nameof(GroupParametersAndLocalsTogether));
			}
		}
		bool groupParametersAndLocalsTogether = false;

		public override bool ShowCompilerGeneratedVariables {
			get {
				lock (lockObj)
					return showCompilerGeneratedVariables;
			}
			set {
				bool modified;
				lock (lockObj) {
					modified = showCompilerGeneratedVariables != value;
					showCompilerGeneratedVariables = value;
				}
				if (modified)
					OnPropertyChanged(nameof(ShowCompilerGeneratedVariables));
			}
		}
		bool showCompilerGeneratedVariables = false;

		public override bool ShowDecompilerGeneratedVariables {
			get {
				lock (lockObj)
					return showDecompilerGeneratedVariables;
			}
			set {
				bool modified;
				lock (lockObj) {
					modified = showDecompilerGeneratedVariables != value;
					showDecompilerGeneratedVariables = value;
				}
				if (modified)
					OnPropertyChanged(nameof(ShowDecompilerGeneratedVariables));
			}
		}
		bool showDecompilerGeneratedVariables = true;

		public override bool HideCompilerGeneratedMembers {
			get {
				lock (lockObj)
					return hideCompilerGeneratedMembers;
			}
			set {
				bool modified;
				lock (lockObj) {
					modified = hideCompilerGeneratedMembers != value;
					hideCompilerGeneratedMembers = value;
				}
				if (modified)
					OnPropertyChanged(nameof(HideCompilerGeneratedMembers));
			}
		}
		bool hideCompilerGeneratedMembers = true;

		public override bool RespectHideMemberAttributes {
			get {
				lock (lockObj)
					return respectHideMemberAttributes;
			}
			set {
				bool modified;
				lock (lockObj) {
					modified = respectHideMemberAttributes != value;
					respectHideMemberAttributes = value;
				}
				if (modified)
					OnPropertyChanged(nameof(RespectHideMemberAttributes));
			}
		}
		bool respectHideMemberAttributes = true;

		public override bool HideDeprecatedError {
			get {
				lock (lockObj)
					return hideDeprecatedError;
			}
			set {
				bool modified;
				lock (lockObj) {
					modified = hideDeprecatedError != value;
					hideDeprecatedError = value;
				}
				if (modified)
					OnPropertyChanged(nameof(HideDeprecatedError));
			}
		}
		bool hideDeprecatedError = false;

		public override bool SuppressJITOptimization_SystemModules {
			get {
				lock (lockObj)
					return suppressJITOptimization_SystemModules;
			}
			set {
				bool modified;
				lock (lockObj) {
					modified = suppressJITOptimization_SystemModules != value;
					suppressJITOptimization_SystemModules = value;
				}
				if (modified)
					OnPropertyChanged(nameof(SuppressJITOptimization_SystemModules));
			}
		}
		bool suppressJITOptimization_SystemModules = true;

		public override bool SuppressJITOptimization_ProgramModules {
			get {
				lock (lockObj)
					return suppressJITOptimization_ProgramModules;
			}
			set {
				bool modified;
				lock (lockObj) {
					modified = suppressJITOptimization_ProgramModules != value;
					suppressJITOptimization_ProgramModules = value;
				}
				if (modified)
					OnPropertyChanged(nameof(SuppressJITOptimization_ProgramModules));
			}
		}
		bool suppressJITOptimization_ProgramModules = true;

		public override bool FocusActiveProcess {
			get {
				lock (lockObj)
					return focusActiveProcess;
			}
			set {
				bool modified;
				lock (lockObj) {
					modified = focusActiveProcess != value;
					focusActiveProcess = value;
				}
				if (modified)
					OnPropertyChanged(nameof(FocusActiveProcess));
			}
		}
		bool focusActiveProcess = true;

		public override bool FocusDebuggerWhenProcessBreaks {
			get {
				lock (lockObj)
					return focusDebuggerWhenProcessBreaks;
			}
			set {
				bool modified;
				lock (lockObj) {
					modified = focusDebuggerWhenProcessBreaks != value;
					focusDebuggerWhenProcessBreaks = value;
				}
				if (modified)
					OnPropertyChanged(nameof(FocusDebuggerWhenProcessBreaks));
			}
		}
		bool focusDebuggerWhenProcessBreaks = true;

		public override bool ShowReturnValues {
			get {
				lock (lockObj)
					return showReturnValues;
			}
			set {
				bool modified;
				lock (lockObj) {
					modified = showReturnValues != value;
					showReturnValues = value;
				}
				if (modified)
					OnPropertyChanged(nameof(ShowReturnValues));
			}
		}
		bool showReturnValues = true;

		public override bool RedirectGuiConsoleOutput {
			get {
				lock (lockObj)
					return redirectGuiConsoleOutput;
			}
			set {
				bool modified;
				lock (lockObj) {
					modified = redirectGuiConsoleOutput != value;
					redirectGuiConsoleOutput = value;
				}
				if (modified)
					OnPropertyChanged(nameof(RedirectGuiConsoleOutput));
			}
		}
		bool redirectGuiConsoleOutput = true;

		public override bool ShowOnlyPublicMembers {
			get {
				lock (lockObj)
					return showOnlyPublicMembers;
			}
			set {
				bool modified;
				lock (lockObj) {
					modified = showOnlyPublicMembers != value;
					showOnlyPublicMembers = value;
				}
				if (modified)
					OnPropertyChanged(nameof(ShowOnlyPublicMembers));
			}
		}
		bool showOnlyPublicMembers = false;

		public override bool ShowRawLocals {
			get {
				lock (lockObj)
					return showRawLocals;
			}
			set {
				bool modified;
				lock (lockObj) {
					modified = showRawLocals != value;
					showRawLocals = value;
				}
				if (modified)
					OnPropertyChanged(nameof(ShowRawLocals));
			}
		}
		bool showRawLocals = false;

		public override bool AsyncDebugging {
			get {
				lock (lockObj)
					return asyncDebugging;
			}
			set {
				bool modified;
				lock (lockObj) {
					modified = asyncDebugging != value;
					asyncDebugging = value;
				}
				if (modified)
					OnPropertyChanged(nameof(AsyncDebugging));
			}
		}
		bool asyncDebugging = true;

		public override bool StepOverPropertiesAndOperators {
			get {
				lock (lockObj)
					return stepOverPropertiesAndOperators;
			}
			set {
				bool modified;
				lock (lockObj) {
					modified = stepOverPropertiesAndOperators != value;
					stepOverPropertiesAndOperators = value;
				}
				if (modified)
					OnPropertyChanged(nameof(StepOverPropertiesAndOperators));
			}
		}
		bool stepOverPropertiesAndOperators = true;

		public override bool IgnoreUnhandledExceptions {
			get {
				lock (lockObj)
					return ignoreUnhandledExceptions;
			}
			set {
				bool modified;
				lock (lockObj) {
					modified = ignoreUnhandledExceptions != value;
					ignoreUnhandledExceptions = value;
				}
				if (modified)
					OnPropertyChanged(nameof(IgnoreUnhandledExceptions));
			}
		}
		bool ignoreUnhandledExceptions;

		public override bool FullString {
			get {
				lock (lockObj)
					return fullString;
			}
			set {
				bool modified;
				lock (lockObj) {
					modified = fullString != value;
					fullString = value;
				}
				if (modified)
					OnPropertyChanged(nameof(FullString));
			}
		}
		bool fullString;

		public DebuggerSettingsBase Clone() => CopyTo(new DebuggerSettingsBase());

		public DebuggerSettingsBase CopyTo(DebuggerSettingsBase other) {
			other.UseHexadecimal = UseHexadecimal;
			other.SyntaxHighlight = SyntaxHighlight;
			other.UseDigitSeparators = UseDigitSeparators;
			other.AutoOpenLocalsWindow = AutoOpenLocalsWindow;
			other.UseMemoryModules = UseMemoryModules;
			other.PropertyEvalAndFunctionCalls = PropertyEvalAndFunctionCalls;
			other.UseStringConversionFunction = UseStringConversionFunction;
			other.PreventManagedDebuggerDetection = PreventManagedDebuggerDetection;
			other.AntiIsDebuggerPresent = AntiIsDebuggerPresent;
			other.AntiCheckRemoteDebuggerPresent = AntiCheckRemoteDebuggerPresent;
			other.IgnoreBreakInstructions = IgnoreBreakInstructions;
			other.BreakAllProcesses = BreakAllProcesses;
			other.EnableManagedDebuggingAssistants = EnableManagedDebuggingAssistants;
			other.HighlightChangedVariables = HighlightChangedVariables;
			other.ShowRawStructureOfObjects = ShowRawStructureOfObjects;
			other.SortParameters = SortParameters;
			other.SortLocals = SortLocals;
			other.GroupParametersAndLocalsTogether = GroupParametersAndLocalsTogether;
			other.ShowCompilerGeneratedVariables = ShowCompilerGeneratedVariables;
			other.ShowDecompilerGeneratedVariables = ShowDecompilerGeneratedVariables;
			other.HideCompilerGeneratedMembers = HideCompilerGeneratedMembers;
			other.RespectHideMemberAttributes = RespectHideMemberAttributes;
			other.HideDeprecatedError = HideDeprecatedError;
			other.SuppressJITOptimization_SystemModules = SuppressJITOptimization_SystemModules;
			other.SuppressJITOptimization_ProgramModules = SuppressJITOptimization_ProgramModules;
			other.FocusActiveProcess = FocusActiveProcess;
			other.FocusDebuggerWhenProcessBreaks = FocusDebuggerWhenProcessBreaks;
			other.ShowReturnValues = ShowReturnValues;
			other.RedirectGuiConsoleOutput = RedirectGuiConsoleOutput;
			other.ShowOnlyPublicMembers = ShowOnlyPublicMembers;
			other.ShowRawLocals = ShowRawLocals;
			other.AsyncDebugging = AsyncDebugging;
			other.StepOverPropertiesAndOperators = StepOverPropertiesAndOperators;
			other.IgnoreUnhandledExceptions = IgnoreUnhandledExceptions;
			other.FullString = FullString;
			return other;
		}
	}

	[Export(typeof(DebuggerSettingsImpl))]
	[Export(typeof(DebuggerSettings))]
	sealed class DebuggerSettingsImpl : DebuggerSettingsBase {
		static readonly Guid SETTINGS_GUID = new Guid("91F1ED94-1BEA-4853-9240-B542A7D022CA");

		readonly ISettingsService settingsService;

		[ImportingConstructor]
		DebuggerSettingsImpl(ISettingsService settingsService) {
			this.settingsService = settingsService;

			var sect = settingsService.GetOrCreateSection(SETTINGS_GUID);
			UseHexadecimal = sect.Attribute<bool?>(nameof(UseHexadecimal)) ?? UseHexadecimal;
			SyntaxHighlight = sect.Attribute<bool?>(nameof(SyntaxHighlight)) ?? SyntaxHighlight;
			UseDigitSeparators = sect.Attribute<bool?>(nameof(UseDigitSeparators)) ?? UseDigitSeparators;
			AutoOpenLocalsWindow = sect.Attribute<bool?>(nameof(AutoOpenLocalsWindow)) ?? AutoOpenLocalsWindow;
			UseMemoryModules = sect.Attribute<bool?>(nameof(UseMemoryModules)) ?? UseMemoryModules;
			PropertyEvalAndFunctionCalls = sect.Attribute<bool?>(nameof(PropertyEvalAndFunctionCalls)) ?? PropertyEvalAndFunctionCalls;
			UseStringConversionFunction = sect.Attribute<bool?>(nameof(UseStringConversionFunction)) ?? UseStringConversionFunction;
			PreventManagedDebuggerDetection = sect.Attribute<bool?>(nameof(PreventManagedDebuggerDetection)) ?? PreventManagedDebuggerDetection;
			AntiIsDebuggerPresent = sect.Attribute<bool?>(nameof(AntiIsDebuggerPresent)) ?? AntiIsDebuggerPresent;
			AntiCheckRemoteDebuggerPresent = sect.Attribute<bool?>(nameof(AntiCheckRemoteDebuggerPresent)) ?? AntiCheckRemoteDebuggerPresent;
			IgnoreBreakInstructions = sect.Attribute<bool?>(nameof(IgnoreBreakInstructions)) ?? IgnoreBreakInstructions;
			BreakAllProcesses = sect.Attribute<bool?>(nameof(BreakAllProcesses)) ?? BreakAllProcesses;
			EnableManagedDebuggingAssistants = sect.Attribute<bool?>(nameof(EnableManagedDebuggingAssistants)) ?? EnableManagedDebuggingAssistants;
			HighlightChangedVariables = sect.Attribute<bool?>(nameof(HighlightChangedVariables)) ?? HighlightChangedVariables;
			ShowRawStructureOfObjects = sect.Attribute<bool?>(nameof(ShowRawStructureOfObjects)) ?? ShowRawStructureOfObjects;
			SortParameters = sect.Attribute<bool?>(nameof(SortParameters)) ?? SortParameters;
			SortLocals = sect.Attribute<bool?>(nameof(SortLocals)) ?? SortLocals;
			GroupParametersAndLocalsTogether = sect.Attribute<bool?>(nameof(GroupParametersAndLocalsTogether)) ?? GroupParametersAndLocalsTogether;
			ShowCompilerGeneratedVariables = sect.Attribute<bool?>(nameof(ShowCompilerGeneratedVariables)) ?? ShowCompilerGeneratedVariables;
			ShowDecompilerGeneratedVariables = sect.Attribute<bool?>(nameof(ShowDecompilerGeneratedVariables)) ?? ShowDecompilerGeneratedVariables;
			HideCompilerGeneratedMembers = sect.Attribute<bool?>(nameof(HideCompilerGeneratedMembers)) ?? HideCompilerGeneratedMembers;
			RespectHideMemberAttributes = sect.Attribute<bool?>(nameof(RespectHideMemberAttributes)) ?? RespectHideMemberAttributes;
			HideDeprecatedError = sect.Attribute<bool?>(nameof(HideDeprecatedError)) ?? HideDeprecatedError;
			SuppressJITOptimization_SystemModules = sect.Attribute<bool?>(nameof(SuppressJITOptimization_SystemModules)) ?? SuppressJITOptimization_SystemModules;
			SuppressJITOptimization_ProgramModules = sect.Attribute<bool?>(nameof(SuppressJITOptimization_ProgramModules)) ?? SuppressJITOptimization_ProgramModules;
			FocusActiveProcess = sect.Attribute<bool?>(nameof(FocusActiveProcess)) ?? FocusActiveProcess;
			FocusDebuggerWhenProcessBreaks = sect.Attribute<bool?>(nameof(FocusDebuggerWhenProcessBreaks)) ?? FocusDebuggerWhenProcessBreaks;
			ShowReturnValues = sect.Attribute<bool?>(nameof(ShowReturnValues)) ?? ShowReturnValues;
			RedirectGuiConsoleOutput = sect.Attribute<bool?>(nameof(RedirectGuiConsoleOutput)) ?? RedirectGuiConsoleOutput;
			ShowOnlyPublicMembers = sect.Attribute<bool?>(nameof(ShowOnlyPublicMembers)) ?? ShowOnlyPublicMembers;
			ShowRawLocals = sect.Attribute<bool?>(nameof(ShowRawLocals)) ?? ShowRawLocals;
			AsyncDebugging = sect.Attribute<bool?>(nameof(AsyncDebugging)) ?? AsyncDebugging;
			StepOverPropertiesAndOperators = sect.Attribute<bool?>(nameof(StepOverPropertiesAndOperators)) ?? StepOverPropertiesAndOperators;
			IgnoreUnhandledExceptions = sect.Attribute<bool?>(nameof(IgnoreUnhandledExceptions)) ?? IgnoreUnhandledExceptions;
			FullString = sect.Attribute<bool?>(nameof(FullString)) ?? FullString;
			PropertyChanged += DebuggerSettingsImpl_PropertyChanged;
		}

		void DebuggerSettingsImpl_PropertyChanged(object? sender, PropertyChangedEventArgs e) {
			var sect = settingsService.RecreateSection(SETTINGS_GUID);
			sect.Attribute(nameof(UseHexadecimal), UseHexadecimal);
			sect.Attribute(nameof(SyntaxHighlight), SyntaxHighlight);
			sect.Attribute(nameof(UseDigitSeparators), UseDigitSeparators);
			sect.Attribute(nameof(AutoOpenLocalsWindow), AutoOpenLocalsWindow);
			sect.Attribute(nameof(UseMemoryModules), UseMemoryModules);
			sect.Attribute(nameof(PropertyEvalAndFunctionCalls), PropertyEvalAndFunctionCalls);
			sect.Attribute(nameof(UseStringConversionFunction), UseStringConversionFunction);
			sect.Attribute(nameof(PreventManagedDebuggerDetection), PreventManagedDebuggerDetection);
			sect.Attribute(nameof(AntiIsDebuggerPresent), AntiIsDebuggerPresent);
			sect.Attribute(nameof(AntiCheckRemoteDebuggerPresent), AntiCheckRemoteDebuggerPresent);
			sect.Attribute(nameof(IgnoreBreakInstructions), IgnoreBreakInstructions);
			sect.Attribute(nameof(BreakAllProcesses), BreakAllProcesses);
			sect.Attribute(nameof(EnableManagedDebuggingAssistants), EnableManagedDebuggingAssistants);
			sect.Attribute(nameof(HighlightChangedVariables), HighlightChangedVariables);
			sect.Attribute(nameof(ShowRawStructureOfObjects), ShowRawStructureOfObjects);
			sect.Attribute(nameof(SortParameters), SortParameters);
			sect.Attribute(nameof(SortLocals), SortLocals);
			sect.Attribute(nameof(GroupParametersAndLocalsTogether), GroupParametersAndLocalsTogether);
			sect.Attribute(nameof(ShowCompilerGeneratedVariables), ShowCompilerGeneratedVariables);
			sect.Attribute(nameof(ShowDecompilerGeneratedVariables), ShowDecompilerGeneratedVariables);
			sect.Attribute(nameof(HideCompilerGeneratedMembers), HideCompilerGeneratedMembers);
			sect.Attribute(nameof(RespectHideMemberAttributes), RespectHideMemberAttributes);
			sect.Attribute(nameof(HideDeprecatedError), HideDeprecatedError);
			sect.Attribute(nameof(SuppressJITOptimization_SystemModules), SuppressJITOptimization_SystemModules);
			sect.Attribute(nameof(SuppressJITOptimization_ProgramModules), SuppressJITOptimization_ProgramModules);
			sect.Attribute(nameof(FocusActiveProcess), FocusActiveProcess);
			sect.Attribute(nameof(FocusDebuggerWhenProcessBreaks), FocusDebuggerWhenProcessBreaks);
			sect.Attribute(nameof(ShowReturnValues), ShowReturnValues);
			sect.Attribute(nameof(RedirectGuiConsoleOutput), RedirectGuiConsoleOutput);
			sect.Attribute(nameof(ShowOnlyPublicMembers), ShowOnlyPublicMembers);
			sect.Attribute(nameof(ShowRawLocals), ShowRawLocals);
			sect.Attribute(nameof(AsyncDebugging), AsyncDebugging);
			sect.Attribute(nameof(StepOverPropertiesAndOperators), StepOverPropertiesAndOperators);
			sect.Attribute(nameof(IgnoreUnhandledExceptions), IgnoreUnhandledExceptions);
			sect.Attribute(nameof(FullString), FullString);
		}
	}
}
