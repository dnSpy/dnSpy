/*
    Copyright (C) 2014-2016 de4dot@gmail.com

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
using System.Diagnostics;
using dndbg.Engine;
using dnSpy.Contracts.Settings;
using dnSpy.Shared.MVVM;

namespace dnSpy.Debugger {
	interface IDebuggerSettings : INotifyPropertyChanged {
		bool UseHexadecimal { get; }
		bool SyntaxHighlightCallStack { get; }
		bool SyntaxHighlightBreakpoints { get; }
		bool SyntaxHighlightThreads { get; }
		bool SyntaxHighlightModules { get; }
		bool SyntaxHighlightLocals { get; }
		bool SyntaxHighlightAttach { get; }
		bool SyntaxHighlightExceptions { get; }
		BreakProcessKind BreakProcessKind { get; }
		bool PropertyEvalAndFunctionCalls { get; }
		bool UseStringConversionFunction { get; }
		bool CanEvaluateToString { get; }
		bool DebuggerBrowsableAttributesCanHidePropsFields { get; }
		bool CompilerGeneratedAttributesCanHideFields { get; }
		bool DisableManagedDebuggerDetection { get; }
		bool IgnoreBreakInstructions { get; }
		bool AutoOpenLocalsWindow { get; }
		bool UseMemoryModules { get; }
		string CoreCLRDbgShimFilename { get; }
		string CoreCLRDbgShimFilename32 { get; }
		string CoreCLRDbgShimFilename64 { get; }
	}

	class DebuggerSettings : ViewModelBase, IDebuggerSettings {
		protected virtual void OnModified() { }

		public bool UseHexadecimal {
			get { return useHexadecimal; }
			set {
				if (useHexadecimal != value) {
					useHexadecimal = value;
					OnPropertyChanged(nameof(UseHexadecimal));
					OnModified();
				}
			}
		}
		bool useHexadecimal = true;

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
					OnPropertyChanged(nameof(SyntaxHighlightCallStack));
					OnPropertyChanged(nameof(SyntaxHighlight));
					OnModified();
				}
			}
		}
		bool syntaxHighlightCallStack = true;

		public bool SyntaxHighlightBreakpoints {
			get { return syntaxHighlightBreakpoints; }
			set {
				if (syntaxHighlightBreakpoints != value) {
					syntaxHighlightBreakpoints = value;
					OnPropertyChanged(nameof(SyntaxHighlightBreakpoints));
					OnPropertyChanged(nameof(SyntaxHighlight));
					OnModified();
				}
			}
		}
		bool syntaxHighlightBreakpoints = true;

		public bool SyntaxHighlightThreads {
			get { return syntaxHighlightThreads; }
			set {
				if (syntaxHighlightThreads != value) {
					syntaxHighlightThreads = value;
					OnPropertyChanged(nameof(SyntaxHighlightThreads));
					OnPropertyChanged(nameof(SyntaxHighlight));
					OnModified();
				}
			}
		}
		bool syntaxHighlightThreads = true;

		public bool SyntaxHighlightModules {
			get { return syntaxHighlightModules; }
			set {
				if (syntaxHighlightModules != value) {
					syntaxHighlightModules = value;
					OnPropertyChanged(nameof(SyntaxHighlightModules));
					OnPropertyChanged(nameof(SyntaxHighlight));
					OnModified();
				}
			}
		}
		bool syntaxHighlightModules = true;

		public bool SyntaxHighlightLocals {
			get { return syntaxHighlightLocals; }
			set {
				if (syntaxHighlightLocals != value) {
					syntaxHighlightLocals = value;
					OnPropertyChanged(nameof(SyntaxHighlightLocals));
					OnPropertyChanged(nameof(SyntaxHighlight));
					OnModified();
				}
			}
		}
		bool syntaxHighlightLocals = true;

		public bool SyntaxHighlightAttach {
			get { return syntaxHighlightAttach; }
			set {
				if (syntaxHighlightAttach != value) {
					syntaxHighlightAttach = value;
					OnPropertyChanged(nameof(SyntaxHighlightAttach));
					OnPropertyChanged(nameof(SyntaxHighlight));
					OnModified();
				}
			}
		}
		bool syntaxHighlightAttach = true;

		public bool SyntaxHighlightExceptions {
			get { return syntaxHighlightExceptions; }
			set {
				if (syntaxHighlightExceptions != value) {
					syntaxHighlightExceptions = value;
					OnPropertyChanged(nameof(SyntaxHighlightExceptions));
					OnPropertyChanged(nameof(SyntaxHighlight));
					OnModified();
				}
			}
		}
		bool syntaxHighlightExceptions = true;

		public BreakProcessKind BreakProcessKind {
			get { return breakProcessKind; }
			set {
				if (breakProcessKind != value) {
					breakProcessKind = value;
					OnPropertyChanged(nameof(BreakProcessKind));
					OnModified();
				}
			}
		}
		BreakProcessKind breakProcessKind = BreakProcessKind.ModuleCctorOrEntryPoint;

		public bool PropertyEvalAndFunctionCalls {
			get { return propertyEvalAndFunctionCalls; }
			set {
				if (propertyEvalAndFunctionCalls != value) {
					propertyEvalAndFunctionCalls = value;
					OnPropertyChanged(nameof(PropertyEvalAndFunctionCalls));
					OnModified();
				}
			}
		}
		bool propertyEvalAndFunctionCalls = true;

		public bool UseStringConversionFunction {
			get { return useStringConversionFunction; }
			set {
				if (useStringConversionFunction != value) {
					useStringConversionFunction = value;
					OnPropertyChanged(nameof(UseStringConversionFunction));
					OnModified();
				}
			}
		}
		bool useStringConversionFunction = true;

		public bool CanEvaluateToString => PropertyEvalAndFunctionCalls && UseStringConversionFunction;

		public bool DebuggerBrowsableAttributesCanHidePropsFields {
			get { return debuggerBrowsableAttributesCanHidePropsFields; }
			set {
				if (debuggerBrowsableAttributesCanHidePropsFields != value) {
					debuggerBrowsableAttributesCanHidePropsFields = value;
					OnPropertyChanged(nameof(DebuggerBrowsableAttributesCanHidePropsFields));
					OnModified();
				}
			}
		}
		bool debuggerBrowsableAttributesCanHidePropsFields = true;

		public bool CompilerGeneratedAttributesCanHideFields {
			get { return compilerGeneratedAttributesCanHideFields; }
			set {
				if (compilerGeneratedAttributesCanHideFields != value) {
					compilerGeneratedAttributesCanHideFields = value;
					OnPropertyChanged(nameof(CompilerGeneratedAttributesCanHideFields));
					OnModified();
				}
			}
		}
		bool compilerGeneratedAttributesCanHideFields = true;

		public bool DisableManagedDebuggerDetection {
			get { return disableManagedDebuggerDetection; }
			set {
				if (disableManagedDebuggerDetection != value) {
					disableManagedDebuggerDetection = value;
					OnPropertyChanged(nameof(DisableManagedDebuggerDetection));
					OnModified();
				}
			}
		}
		bool disableManagedDebuggerDetection = true;

		public bool IgnoreBreakInstructions {
			get { return ignoreBreakInstructions; }
			set {
				if (ignoreBreakInstructions != value) {
					ignoreBreakInstructions = value;
					OnPropertyChanged(nameof(IgnoreBreakInstructions));
					OnModified();
				}
			}
		}
		bool ignoreBreakInstructions = false;

		public bool AutoOpenLocalsWindow {
			get { return autoOpenLocalsWindow; }
			set {
				if (autoOpenLocalsWindow != value) {
					autoOpenLocalsWindow = value;
					OnPropertyChanged(nameof(AutoOpenLocalsWindow));
					OnModified();
				}
			}
		}
		bool autoOpenLocalsWindow = true;

		public bool UseMemoryModules {
			get { return useMemoryModules; }
			set {
				if (useMemoryModules != value) {
					useMemoryModules = value;
					OnPropertyChanged(nameof(UseMemoryModules));
					OnModified();
				}
			}
		}
		bool useMemoryModules = false;

		public string CoreCLRDbgShimFilename {
			get { return IntPtr.Size == 4 ? CoreCLRDbgShimFilename32 : CoreCLRDbgShimFilename64; }
			set {
				if (IntPtr.Size == 4)
					CoreCLRDbgShimFilename32 = value;
				else
					CoreCLRDbgShimFilename64 = value;
			}
		}

		public string CoreCLRDbgShimFilename32 {
			get { return coreCLRDbgShimFilename32; }
			set {
				if (coreCLRDbgShimFilename32 != value) {
					coreCLRDbgShimFilename32 = value;
					OnPropertyChanged(nameof(CoreCLRDbgShimFilename32));
					if (IntPtr.Size == 4)
						OnPropertyChanged(nameof(CoreCLRDbgShimFilename));
					OnModified();
				}
			}
		}
		string coreCLRDbgShimFilename32 = string.Empty;

		public string CoreCLRDbgShimFilename64 {
			get { return coreCLRDbgShimFilename64; }
			set {
				if (coreCLRDbgShimFilename64 != value) {
					coreCLRDbgShimFilename64 = value;
					OnPropertyChanged(nameof(CoreCLRDbgShimFilename64));
					if (IntPtr.Size == 8)
						OnPropertyChanged(nameof(CoreCLRDbgShimFilename));
					OnModified();
				}
			}
		}
		string coreCLRDbgShimFilename64 = string.Empty;

		public DebuggerSettings Clone() => CopyTo(new DebuggerSettings());

		public DebuggerSettings CopyTo(DebuggerSettings other) {
			other.UseHexadecimal = this.UseHexadecimal;
			other.SyntaxHighlightCallStack = this.SyntaxHighlightCallStack;
			other.SyntaxHighlightBreakpoints = this.SyntaxHighlightBreakpoints;
			other.SyntaxHighlightThreads = this.SyntaxHighlightThreads;
			other.SyntaxHighlightModules = this.SyntaxHighlightModules;
			other.SyntaxHighlightLocals = this.SyntaxHighlightLocals;
			other.SyntaxHighlightAttach = this.SyntaxHighlightAttach;
			other.SyntaxHighlightExceptions = this.SyntaxHighlightExceptions;
			other.BreakProcessKind = this.BreakProcessKind;
			other.PropertyEvalAndFunctionCalls = this.PropertyEvalAndFunctionCalls;
			other.UseStringConversionFunction = this.UseStringConversionFunction;
			other.DebuggerBrowsableAttributesCanHidePropsFields = this.DebuggerBrowsableAttributesCanHidePropsFields;
			other.CompilerGeneratedAttributesCanHideFields = this.CompilerGeneratedAttributesCanHideFields;
			other.DisableManagedDebuggerDetection = this.DisableManagedDebuggerDetection;
			other.IgnoreBreakInstructions = this.IgnoreBreakInstructions;
			other.AutoOpenLocalsWindow = this.AutoOpenLocalsWindow;
			other.UseMemoryModules = this.UseMemoryModules;
			other.CoreCLRDbgShimFilename32 = this.CoreCLRDbgShimFilename32;
			other.CoreCLRDbgShimFilename64 = this.CoreCLRDbgShimFilename64;
			return other;
		}
	}

	[Export, Export(typeof(IDebuggerSettings)), PartCreationPolicy(CreationPolicy.Shared)]
	sealed class DebuggerSettingsImpl : DebuggerSettings {
		static readonly Guid SETTINGS_GUID = new Guid("91F1ED94-1BEA-4853-9240-B542A7D022CA");

		readonly ISettingsManager settingsManager;

		[ImportingConstructor]
		DebuggerSettingsImpl(ISettingsManager settingsManager) {
			this.settingsManager = settingsManager;

			this.disableSave = true;
			var sect = settingsManager.GetOrCreateSection(SETTINGS_GUID);
			UseHexadecimal = sect.Attribute<bool?>(nameof(UseHexadecimal)) ?? UseHexadecimal;
			SyntaxHighlightCallStack = sect.Attribute<bool?>(nameof(SyntaxHighlightCallStack)) ?? SyntaxHighlightCallStack;
			SyntaxHighlightBreakpoints = sect.Attribute<bool?>(nameof(SyntaxHighlightBreakpoints)) ?? SyntaxHighlightBreakpoints;
			SyntaxHighlightThreads = sect.Attribute<bool?>(nameof(SyntaxHighlightThreads)) ?? SyntaxHighlightThreads;
			SyntaxHighlightModules = sect.Attribute<bool?>(nameof(SyntaxHighlightModules)) ?? SyntaxHighlightModules;
			SyntaxHighlightLocals = sect.Attribute<bool?>(nameof(SyntaxHighlightLocals)) ?? SyntaxHighlightLocals;
			SyntaxHighlightAttach = sect.Attribute<bool?>(nameof(SyntaxHighlightAttach)) ?? SyntaxHighlightAttach;
			SyntaxHighlightExceptions = sect.Attribute<bool?>(nameof(SyntaxHighlightExceptions)) ?? SyntaxHighlightExceptions;
			BreakProcessKind = sect.Attribute<BreakProcessKind?>(nameof(BreakProcessKind)) ?? BreakProcessKind;
			PropertyEvalAndFunctionCalls = sect.Attribute<bool?>(nameof(PropertyEvalAndFunctionCalls)) ?? PropertyEvalAndFunctionCalls;
			UseStringConversionFunction = sect.Attribute<bool?>(nameof(UseStringConversionFunction)) ?? UseStringConversionFunction;
			DebuggerBrowsableAttributesCanHidePropsFields = sect.Attribute<bool?>(nameof(DebuggerBrowsableAttributesCanHidePropsFields)) ?? DebuggerBrowsableAttributesCanHidePropsFields;
			CompilerGeneratedAttributesCanHideFields = sect.Attribute<bool?>(nameof(CompilerGeneratedAttributesCanHideFields)) ?? CompilerGeneratedAttributesCanHideFields;
			DisableManagedDebuggerDetection = sect.Attribute<bool?>(nameof(DisableManagedDebuggerDetection)) ?? DisableManagedDebuggerDetection;
			IgnoreBreakInstructions = sect.Attribute<bool?>(nameof(IgnoreBreakInstructions)) ?? IgnoreBreakInstructions;
			AutoOpenLocalsWindow = sect.Attribute<bool?>(nameof(AutoOpenLocalsWindow)) ?? AutoOpenLocalsWindow;
			UseMemoryModules = sect.Attribute<bool?>(nameof(UseMemoryModules)) ?? UseMemoryModules;
			CoreCLRDbgShimFilename32 = sect.Attribute<string>(nameof(CoreCLRDbgShimFilename32)) ?? CoreCLRDbgShimFilename32;
			CoreCLRDbgShimFilename64 = sect.Attribute<string>(nameof(CoreCLRDbgShimFilename64)) ?? CoreCLRDbgShimFilename64;
			this.disableSave = false;
		}
		readonly bool disableSave;

		protected override void OnModified() {
			if (disableSave)
				return;
			var sect = settingsManager.RecreateSection(SETTINGS_GUID);
			sect.Attribute(nameof(UseHexadecimal), UseHexadecimal);
			sect.Attribute(nameof(SyntaxHighlightCallStack), SyntaxHighlightCallStack);
			sect.Attribute(nameof(SyntaxHighlightBreakpoints), SyntaxHighlightBreakpoints);
			sect.Attribute(nameof(SyntaxHighlightThreads), SyntaxHighlightThreads);
			sect.Attribute(nameof(SyntaxHighlightModules), SyntaxHighlightModules);
			sect.Attribute(nameof(SyntaxHighlightLocals), SyntaxHighlightLocals);
			sect.Attribute(nameof(SyntaxHighlightAttach), SyntaxHighlightAttach);
			sect.Attribute(nameof(SyntaxHighlightExceptions), SyntaxHighlightExceptions);
			sect.Attribute(nameof(BreakProcessKind), BreakProcessKind);
			sect.Attribute(nameof(PropertyEvalAndFunctionCalls), PropertyEvalAndFunctionCalls);
			sect.Attribute(nameof(UseStringConversionFunction), UseStringConversionFunction);
			sect.Attribute(nameof(DebuggerBrowsableAttributesCanHidePropsFields), DebuggerBrowsableAttributesCanHidePropsFields);
			sect.Attribute(nameof(CompilerGeneratedAttributesCanHideFields), CompilerGeneratedAttributesCanHideFields);
			sect.Attribute(nameof(DisableManagedDebuggerDetection), DisableManagedDebuggerDetection);
			sect.Attribute(nameof(IgnoreBreakInstructions), IgnoreBreakInstructions);
			sect.Attribute(nameof(AutoOpenLocalsWindow), AutoOpenLocalsWindow);
			sect.Attribute(nameof(UseMemoryModules), UseMemoryModules);
			sect.Attribute(nameof(CoreCLRDbgShimFilename32), CoreCLRDbgShimFilename32);
			sect.Attribute(nameof(CoreCLRDbgShimFilename64), CoreCLRDbgShimFilename64);
		}
	}
}
