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
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Settings;

namespace dnSpy.Debugger.Settings {
	class DebuggerSettingsBase : DebuggerSettings {
		protected virtual void OnModified() { }

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
				if (modified) {
					OnPropertyChanged(nameof(UseHexadecimal));
					OnModified();
				}
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
				if (modified) {
					OnPropertyChanged(nameof(UseDigitSeparators));
					OnModified();
				}
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
				if (modified) {
					OnPropertyChanged(nameof(SyntaxHighlight));
					OnModified();
				}
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
				if (modified) {
					OnPropertyChanged(nameof(AutoOpenLocalsWindow));
					OnModified();
				}
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
				if (modified) {
					OnPropertyChanged(nameof(UseMemoryModules));
					OnModified();
				}
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
				if (modified) {
					OnPropertyChanged(nameof(PropertyEvalAndFunctionCalls));
					OnModified();
				}
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
				if (modified) {
					OnPropertyChanged(nameof(UseStringConversionFunction));
					OnModified();
				}
			}
		}
		bool useStringConversionFunction = true;

		public override bool DisableManagedDebuggerDetection {
			get {
				lock (lockObj)
					return disableManagedDebuggerDetection;
			}
			set {
				bool modified;
				lock (lockObj) {
					modified = disableManagedDebuggerDetection != value;
					disableManagedDebuggerDetection = value;
				}
				if (modified) {
					OnPropertyChanged(nameof(DisableManagedDebuggerDetection));
					OnModified();
				}
			}
		}
		bool disableManagedDebuggerDetection = true;

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
				if (modified) {
					OnPropertyChanged(nameof(IgnoreBreakInstructions));
					OnModified();
				}
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
				if (modified) {
					OnPropertyChanged(nameof(BreakAllProcesses));
					OnModified();
				}
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
				if (modified) {
					OnPropertyChanged(nameof(EnableManagedDebuggingAssistants));
					OnModified();
				}
			}
		}
		bool enableManagedDebuggingAssistants = false;

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
				if (modified) {
					OnPropertyChanged(nameof(HighlightChangedVariables));
					OnModified();
				}
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
				if (modified) {
					OnPropertyChanged(nameof(ShowRawStructureOfObjects));
					OnModified();
				}
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
				if (modified) {
					OnPropertyChanged(nameof(SortParameters));
					OnModified();
				}
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
				if (modified) {
					OnPropertyChanged(nameof(SortLocals));
					OnModified();
				}
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
				if (modified) {
					OnPropertyChanged(nameof(GroupParametersAndLocalsTogether));
					OnModified();
				}
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
				if (modified) {
					OnPropertyChanged(nameof(ShowCompilerGeneratedVariables));
					OnModified();
				}
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
				if (modified) {
					OnPropertyChanged(nameof(ShowDecompilerGeneratedVariables));
					OnModified();
				}
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
				if (modified) {
					OnPropertyChanged(nameof(HideCompilerGeneratedMembers));
					OnModified();
				}
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
				if (modified) {
					OnPropertyChanged(nameof(RespectHideMemberAttributes));
					OnModified();
				}
			}
		}
		bool respectHideMemberAttributes = true;

		public DebuggerSettingsBase Clone() => CopyTo(new DebuggerSettingsBase());

		public DebuggerSettingsBase CopyTo(DebuggerSettingsBase other) {
			other.UseHexadecimal = UseHexadecimal;
			other.SyntaxHighlight = SyntaxHighlight;
			other.UseDigitSeparators = UseDigitSeparators;
			other.AutoOpenLocalsWindow = AutoOpenLocalsWindow;
			other.UseMemoryModules = UseMemoryModules;
			other.PropertyEvalAndFunctionCalls = PropertyEvalAndFunctionCalls;
			other.UseStringConversionFunction = UseStringConversionFunction;
			other.DisableManagedDebuggerDetection = DisableManagedDebuggerDetection;
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

			disableSave = true;
			var sect = settingsService.GetOrCreateSection(SETTINGS_GUID);
			UseHexadecimal = sect.Attribute<bool?>(nameof(UseHexadecimal)) ?? UseHexadecimal;
			SyntaxHighlight = sect.Attribute<bool?>(nameof(SyntaxHighlight)) ?? SyntaxHighlight;
			UseDigitSeparators = sect.Attribute<bool?>(nameof(UseDigitSeparators)) ?? UseDigitSeparators;
			AutoOpenLocalsWindow = sect.Attribute<bool?>(nameof(AutoOpenLocalsWindow)) ?? AutoOpenLocalsWindow;
			UseMemoryModules = sect.Attribute<bool?>(nameof(UseMemoryModules)) ?? UseMemoryModules;
			PropertyEvalAndFunctionCalls = sect.Attribute<bool?>(nameof(PropertyEvalAndFunctionCalls)) ?? PropertyEvalAndFunctionCalls;
			UseStringConversionFunction = sect.Attribute<bool?>(nameof(UseStringConversionFunction)) ?? UseStringConversionFunction;
			DisableManagedDebuggerDetection = sect.Attribute<bool?>(nameof(DisableManagedDebuggerDetection)) ?? DisableManagedDebuggerDetection;
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
			disableSave = false;
		}
		readonly bool disableSave;

		protected override void OnModified() {
			if (disableSave)
				return;
			var sect = settingsService.RecreateSection(SETTINGS_GUID);
			sect.Attribute(nameof(UseHexadecimal), UseHexadecimal);
			sect.Attribute(nameof(SyntaxHighlight), SyntaxHighlight);
			sect.Attribute(nameof(UseDigitSeparators), UseDigitSeparators);
			sect.Attribute(nameof(AutoOpenLocalsWindow), AutoOpenLocalsWindow);
			sect.Attribute(nameof(UseMemoryModules), UseMemoryModules);
			sect.Attribute(nameof(PropertyEvalAndFunctionCalls), PropertyEvalAndFunctionCalls);
			sect.Attribute(nameof(UseStringConversionFunction), UseStringConversionFunction);
			sect.Attribute(nameof(DisableManagedDebuggerDetection), DisableManagedDebuggerDetection);
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
		}
	}
}
