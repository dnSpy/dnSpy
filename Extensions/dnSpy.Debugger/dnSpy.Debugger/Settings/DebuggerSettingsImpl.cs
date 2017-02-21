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

		public DebuggerSettings Clone() => CopyTo(new DebuggerSettingsBase());

		public DebuggerSettings CopyTo(DebuggerSettings other) {
			other.UseHexadecimal = UseHexadecimal;
			other.SyntaxHighlight = SyntaxHighlight;
			other.PropertyEvalAndFunctionCalls = PropertyEvalAndFunctionCalls;
			other.UseStringConversionFunction = UseStringConversionFunction;
			other.DisableManagedDebuggerDetection = DisableManagedDebuggerDetection;
			other.IgnoreBreakInstructions = IgnoreBreakInstructions;
			other.AutoOpenLocalsWindow = AutoOpenLocalsWindow;
			other.UseMemoryModules = UseMemoryModules;
			return other;
		}
	}

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
			PropertyEvalAndFunctionCalls = sect.Attribute<bool?>(nameof(PropertyEvalAndFunctionCalls)) ?? PropertyEvalAndFunctionCalls;
			UseStringConversionFunction = sect.Attribute<bool?>(nameof(UseStringConversionFunction)) ?? UseStringConversionFunction;
			DisableManagedDebuggerDetection = sect.Attribute<bool?>(nameof(DisableManagedDebuggerDetection)) ?? DisableManagedDebuggerDetection;
			IgnoreBreakInstructions = sect.Attribute<bool?>(nameof(IgnoreBreakInstructions)) ?? IgnoreBreakInstructions;
			AutoOpenLocalsWindow = sect.Attribute<bool?>(nameof(AutoOpenLocalsWindow)) ?? AutoOpenLocalsWindow;
			UseMemoryModules = sect.Attribute<bool?>(nameof(UseMemoryModules)) ?? UseMemoryModules;
			disableSave = false;
		}
		readonly bool disableSave;

		protected override void OnModified() {
			if (disableSave)
				return;
			var sect = settingsService.RecreateSection(SETTINGS_GUID);
			sect.Attribute(nameof(UseHexadecimal), UseHexadecimal);
			sect.Attribute(nameof(SyntaxHighlight), SyntaxHighlight);
			sect.Attribute(nameof(PropertyEvalAndFunctionCalls), PropertyEvalAndFunctionCalls);
			sect.Attribute(nameof(UseStringConversionFunction), UseStringConversionFunction);
			sect.Attribute(nameof(DisableManagedDebuggerDetection), DisableManagedDebuggerDetection);
			sect.Attribute(nameof(IgnoreBreakInstructions), IgnoreBreakInstructions);
			sect.Attribute(nameof(AutoOpenLocalsWindow), AutoOpenLocalsWindow);
			sect.Attribute(nameof(UseMemoryModules), UseMemoryModules);
		}
	}
}
