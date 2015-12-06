/*
    Copyright (C) 2014-2015 de4dot@gmail.com

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
using System.Diagnostics;
using dndbg.Engine;
using dnSpy.Contracts;
using dnSpy.Shared.UI.MVVM;

namespace dnSpy.Debugger {
	sealed class DebuggerSettings : ViewModelBase {
		public static readonly DebuggerSettings Instance = new DebuggerSettings();
		int disableSaveCounter;

		DebuggerSettings() {
			Load();
		}

		public bool UseHexadecimal {
			get { return useHexadecimal; }
			set {
				if (useHexadecimal != value) {
					useHexadecimal = value;
					Save();
					OnPropertyChanged("UseHexadecimal");
				}
			}
		}
		bool useHexadecimal;

		public bool? SyntaxHighlight {
			get {
				int count =
					(SyntaxHighlightCallStack ? 1 : 0) +
					(SyntaxHighlightBreakpoints ? 1 : 0) +
					(SyntaxHighlightThreads ? 1 : 0) +
					(SyntaxHighlightModules ? 1 : 0) +
					(SyntaxHighlightLocals ? 1 : 0) +
					(SyntaxHighlightAttach ? 1 : 0) +
					(SyntaxHighlightExceptions ? 1 : 0) +
					0;
				if (count == 0)
					return false;
				const int MAX = 7;
				if (count == MAX)
					return true;
				Debug.Assert(count < MAX);
				return null;
			}
			set {
				if (value != null) {
					SyntaxHighlightCallStack = value.Value;
					SyntaxHighlightBreakpoints = value.Value;
					SyntaxHighlightThreads = value.Value;
					SyntaxHighlightModules = value.Value;
					SyntaxHighlightLocals = value.Value;
					SyntaxHighlightAttach = value.Value;
					SyntaxHighlightExceptions = value.Value;
				}
			}
		}

		public bool SyntaxHighlightCallStack {
			get { return syntaxHighlightCallStack; }
			set {
				if (syntaxHighlightCallStack != value) {
					syntaxHighlightCallStack = value;
					Save();
					OnPropertyChanged("SyntaxHighlightCallStack");
					OnPropertyChanged("SyntaxHighlight");
				}
			}
		}
		bool syntaxHighlightCallStack;

		public bool SyntaxHighlightBreakpoints {
			get { return syntaxHighlightBreakpoints; }
			set {
				if (syntaxHighlightBreakpoints != value) {
					syntaxHighlightBreakpoints = value;
					Save();
					OnPropertyChanged("SyntaxHighlightBreakpoints");
					OnPropertyChanged("SyntaxHighlight");
				}
			}
		}
		bool syntaxHighlightBreakpoints;

		public bool SyntaxHighlightThreads {
			get { return syntaxHighlightThreads; }
			set {
				if (syntaxHighlightThreads != value) {
					syntaxHighlightThreads = value;
					Save();
					OnPropertyChanged("SyntaxHighlightThreads");
					OnPropertyChanged("SyntaxHighlight");
				}
			}
		}
		bool syntaxHighlightThreads;

		public bool SyntaxHighlightModules {
			get { return syntaxHighlightModules; }
			set {
				if (syntaxHighlightModules != value) {
					syntaxHighlightModules = value;
					Save();
					OnPropertyChanged("SyntaxHighlightModules");
					OnPropertyChanged("SyntaxHighlight");
				}
			}
		}
		bool syntaxHighlightModules;

		public bool SyntaxHighlightLocals {
			get { return syntaxHighlightLocals; }
			set {
				if (syntaxHighlightLocals != value) {
					syntaxHighlightLocals = value;
					Save();
					OnPropertyChanged("SyntaxHighlightLocals");
					OnPropertyChanged("SyntaxHighlight");
				}
			}
		}
		bool syntaxHighlightLocals;

		public bool SyntaxHighlightAttach {
			get { return syntaxHighlightAttach; }
			set {
				if (syntaxHighlightAttach != value) {
					syntaxHighlightAttach = value;
					Save();
					OnPropertyChanged("SyntaxHighlightAttach");
					OnPropertyChanged("SyntaxHighlight");
				}
			}
		}
		bool syntaxHighlightAttach;

		public bool SyntaxHighlightExceptions {
			get { return syntaxHighlightExceptions; }
			set {
				if (syntaxHighlightExceptions != value) {
					syntaxHighlightExceptions = value;
					Save();
					OnPropertyChanged("SyntaxHighlightExceptions");
					OnPropertyChanged("SyntaxHighlight");
				}
			}
		}
		bool syntaxHighlightExceptions;

		public BreakProcessType BreakProcessType {
			get { return breakProcessType; }
			set {
				if (breakProcessType != value) {
					breakProcessType = value;
					Save();
					OnPropertyChanged("BreakProcessType");
				}
			}
		}
		BreakProcessType breakProcessType;

		public bool PropertyEvalAndFunctionCalls {
			get { return propertyEvalAndFunctionCalls; }
			set {
				if (propertyEvalAndFunctionCalls != value) {
					propertyEvalAndFunctionCalls = value;
					Save();
					OnPropertyChanged("PropertyEvalAndFunctionCalls");
				}
			}
		}
		bool propertyEvalAndFunctionCalls;

		public bool UseStringConversionFunction {
			get { return useStringConversionFunction; }
			set {
				if (useStringConversionFunction != value) {
					useStringConversionFunction = value;
					Save();
					OnPropertyChanged("UseStringConversionFunction");
				}
			}
		}
		bool useStringConversionFunction;

		public bool CanEvaluateToString {
			get { return PropertyEvalAndFunctionCalls && UseStringConversionFunction; }
		}

		public bool DebuggerBrowsableAttributesCanHidePropsFields {
			get { return debuggerBrowsableAttributesCanHidePropsFields; }
			set {
				if (debuggerBrowsableAttributesCanHidePropsFields != value) {
					debuggerBrowsableAttributesCanHidePropsFields = value;
					Save();
					OnPropertyChanged("DebuggerBrowsableAttributesCanHidePropsFields");
				}
			}
		}
		bool debuggerBrowsableAttributesCanHidePropsFields;

		public bool CompilerGeneratedAttributesCanHideFields {
			get { return compilerGeneratedAttributesCanHideFields; }
			set {
				if (compilerGeneratedAttributesCanHideFields != value) {
					compilerGeneratedAttributesCanHideFields = value;
					Save();
					OnPropertyChanged("CompilerGeneratedAttributesCanHideFields");
				}
			}
		}
		bool compilerGeneratedAttributesCanHideFields;

		public bool DisableManagedDebuggerDetection {
			get { return disableManagedDebuggerDetection; }
			set {
				if (disableManagedDebuggerDetection != value) {
					disableManagedDebuggerDetection = value;
					Save();
					OnPropertyChanged("DisableManagedDebuggerDetection");
				}
			}
		}
		bool disableManagedDebuggerDetection;

		public bool IgnoreBreakInstructions {
			get { return ignoreBreakInstructions; }
			set {
				if (ignoreBreakInstructions != value) {
					ignoreBreakInstructions = value;
					Save();
					OnPropertyChanged("IgnoreBreakInstructions");
				}
			}
		}
		bool ignoreBreakInstructions;

		public bool AutoOpenLocalsWindow {
			get { return autoOpenLocalsWindow; }
			set {
				if (autoOpenLocalsWindow != value) {
					autoOpenLocalsWindow = value;
					Save();
					OnPropertyChanged("AutoOpenLocalsWindow");
				}
			}
		}
		bool autoOpenLocalsWindow;

		public bool UseMemoryModules {
			get { return useMemoryModules; }
			set {
				if (useMemoryModules != value) {
					useMemoryModules = value;
					Save();
					OnPropertyChanged("UseMemoryModules");
				}
			}
		}
		bool useMemoryModules;

		public string CoreCLRDbgShimFilename {
			get { return coreCLRDbgShimFilename; }
			set {
				if (coreCLRDbgShimFilename != value) {
					coreCLRDbgShimFilename = value;
					Save();
					OnPropertyChanged("CoreCLRDbgShimFilename");
				}
			}
		}
		string coreCLRDbgShimFilename;

		static readonly Guid SETTINGS_GUID = new Guid("102DCD2E-BE0A-477C-B4D0-600C5CA28A6A");

		void Load() {
			try {
				disableSaveCounter++;

				Load2();
			}
			finally {
				disableSaveCounter--;
			}
		}

		void Load2() {
			var section = DnSpy.App.SettingsManager.GetOrCreateSection(SETTINGS_GUID);
			UseHexadecimal = section.Attribute<bool?>("UseHexadecimal") ?? true;
			SyntaxHighlightCallStack = section.Attribute<bool?>("SyntaxHighlightCallStack") ?? true;
			SyntaxHighlightBreakpoints = section.Attribute<bool?>("SyntaxHighlightBreakpoints") ?? true;
			SyntaxHighlightThreads = section.Attribute<bool?>("SyntaxHighlightThreads") ?? true;
			SyntaxHighlightModules = section.Attribute<bool?>("SyntaxHighlightModules") ?? true;
			SyntaxHighlightLocals = section.Attribute<bool?>("SyntaxHighlightLocals") ?? true;
			SyntaxHighlightAttach = section.Attribute<bool?>("SyntaxHighlightAttach") ?? true;
			SyntaxHighlightExceptions = section.Attribute<bool?>("SyntaxHighlightExceptions") ?? true;
			BreakProcessType = section.Attribute<BreakProcessType?>("BreakProcessType") ?? BreakProcessType.ModuleCctorOrEntryPoint;
			PropertyEvalAndFunctionCalls = section.Attribute<bool?>("PropertyEvalAndFunctionCalls") ?? true;
			UseStringConversionFunction = section.Attribute<bool?>("UseStringConversionFunction") ?? true;
			DebuggerBrowsableAttributesCanHidePropsFields = section.Attribute<bool?>("DebuggerBrowsableAttributesCanHidePropsFields") ?? true;
			CompilerGeneratedAttributesCanHideFields = section.Attribute<bool?>("CompilerGeneratedAttributesCanHideFields") ?? true;
			DisableManagedDebuggerDetection = section.Attribute<bool?>("DisableManagedDebuggerDetection") ?? true;
			IgnoreBreakInstructions = section.Attribute<bool?>("IgnoreBreakInstructions") ?? false;
			AutoOpenLocalsWindow = section.Attribute<bool?>("AutoOpenLocalsWindow") ?? true;
			UseMemoryModules = section.Attribute<bool?>("UseMemoryModules") ?? false;
			CoreCLRDbgShimFilename = section.Attribute<string>("CoreCLRDbgShimFilename") ?? string.Empty;
		}

		void Save() {
			if (this != DebuggerSettings.Instance)
				return;
			if (disableSaveCounter != 0)
				return;

			var section = DnSpy.App.SettingsManager.CreateSection(SETTINGS_GUID);
			section.Attribute("UseHexadecimal", UseHexadecimal);
			section.Attribute("SyntaxHighlightCallStack", SyntaxHighlightCallStack);
			section.Attribute("SyntaxHighlightBreakpoints", SyntaxHighlightBreakpoints);
			section.Attribute("SyntaxHighlightThreads", SyntaxHighlightThreads);
			section.Attribute("SyntaxHighlightModules", SyntaxHighlightModules);
			section.Attribute("SyntaxHighlightLocals", SyntaxHighlightLocals);
			section.Attribute("SyntaxHighlightAttach", SyntaxHighlightAttach);
			section.Attribute("SyntaxHighlightExceptions", SyntaxHighlightExceptions);
			section.Attribute("BreakProcessType", BreakProcessType);
			section.Attribute("PropertyEvalAndFunctionCalls", PropertyEvalAndFunctionCalls);
			section.Attribute("UseStringConversionFunction", UseStringConversionFunction);
			section.Attribute("DebuggerBrowsableAttributesCanHidePropsFields", DebuggerBrowsableAttributesCanHidePropsFields);
			section.Attribute("CompilerGeneratedAttributesCanHideFields", CompilerGeneratedAttributesCanHideFields);
			section.Attribute("DisableManagedDebuggerDetection", DisableManagedDebuggerDetection);
			section.Attribute("IgnoreBreakInstructions", IgnoreBreakInstructions);
			section.Attribute("AutoOpenLocalsWindow", AutoOpenLocalsWindow);
			section.Attribute("UseMemoryModules", UseMemoryModules);
			section.Attribute("CoreCLRDbgShimFilename", CoreCLRDbgShimFilename);
		}

		public DebuggerSettings CopyTo(DebuggerSettings other) {
			other.UseHexadecimal = this.UseHexadecimal;
			other.SyntaxHighlightCallStack = this.SyntaxHighlightCallStack;
			other.SyntaxHighlightBreakpoints = this.SyntaxHighlightBreakpoints;
			other.SyntaxHighlightThreads = this.SyntaxHighlightThreads;
			other.SyntaxHighlightModules = this.SyntaxHighlightModules;
			other.SyntaxHighlightLocals = this.SyntaxHighlightLocals;
			other.SyntaxHighlightAttach = this.SyntaxHighlightAttach;
			other.SyntaxHighlightExceptions = this.SyntaxHighlightExceptions;
			other.BreakProcessType = this.BreakProcessType;
			other.PropertyEvalAndFunctionCalls = this.PropertyEvalAndFunctionCalls;
			other.UseStringConversionFunction = this.UseStringConversionFunction;
			other.DebuggerBrowsableAttributesCanHidePropsFields = this.DebuggerBrowsableAttributesCanHidePropsFields;
			other.CompilerGeneratedAttributesCanHideFields = this.CompilerGeneratedAttributesCanHideFields;
			other.DisableManagedDebuggerDetection = this.DisableManagedDebuggerDetection;
			other.IgnoreBreakInstructions = this.IgnoreBreakInstructions;
			other.AutoOpenLocalsWindow = this.AutoOpenLocalsWindow;
			other.UseMemoryModules = this.UseMemoryModules;
			other.CoreCLRDbgShimFilename = this.CoreCLRDbgShimFilename;
			return other;
		}

		public DebuggerSettings Clone() {
			return CopyTo(new DebuggerSettings());
		}

		internal static void WriteNewSettings(DebuggerSettings settings) {
			try {
				DebuggerSettings.Instance.disableSaveCounter++;
				settings.CopyTo(DebuggerSettings.Instance);
			}
			finally {
				DebuggerSettings.Instance.disableSaveCounter--;
			}
			DebuggerSettings.Instance.Save();
		}
	}
}
