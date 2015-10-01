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

using System.Diagnostics;
using System.Xml.Linq;
using dndbg.Engine;
using dnSpy.MVVM;
using ICSharpCode.ILSpy;

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
					0;
				if (count == 0)
					return false;
				const int MAX = 6;
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
					OnPropertyChanged("IgnoreBreakInstructions");
				}
			}
		}
		bool ignoreBreakInstructions;

		const string SETTINGS_NAME = "DebuggerSettings";

		void Load() {
			try {
				disableSaveCounter++;

				Load(ILSpySettings.Load());
			}
			finally {
				disableSaveCounter--;
			}
		}

		void Load(ILSpySettings settings) {
			var csx = settings[SETTINGS_NAME];
			UseHexadecimal = (bool?)csx.Attribute("UseHexadecimal") ?? true;
			SyntaxHighlightCallStack = (bool?)csx.Attribute("SyntaxHighlightCallStack") ?? true;
			SyntaxHighlightBreakpoints = (bool?)csx.Attribute("SyntaxHighlightBreakpoints") ?? true;
			SyntaxHighlightThreads = (bool?)csx.Attribute("SyntaxHighlightThreads") ?? true;
			SyntaxHighlightModules = (bool?)csx.Attribute("SyntaxHighlightModules") ?? true;
			SyntaxHighlightLocals = (bool?)csx.Attribute("SyntaxHighlightLocals") ?? true;
			SyntaxHighlightAttach = (bool?)csx.Attribute("SyntaxHighlightAttach") ?? true;
			BreakProcessType = (BreakProcessType)((int?)csx.Attribute("BreakProcessType") ?? (int)BreakProcessType.ModuleCctorOrEntryPoint);
			PropertyEvalAndFunctionCalls = (bool?)csx.Attribute("PropertyEvalAndFunctionCalls") ?? true;
			UseStringConversionFunction = (bool?)csx.Attribute("UseStringConversionFunction") ?? true;
			DebuggerBrowsableAttributesCanHidePropsFields = (bool?)csx.Attribute("DebuggerBrowsableAttributesCanHidePropsFields") ?? true;
			CompilerGeneratedAttributesCanHideFields = (bool?)csx.Attribute("CompilerGeneratedAttributesCanHideFields") ?? true;
			DisableManagedDebuggerDetection = (bool?)csx.Attribute("DisableManagedDebuggerDetection") ?? true;
			IgnoreBreakInstructions = (bool?)csx.Attribute("IgnoreBreakInstructions") ?? false;
		}

		void Save() {
			if (this != DebuggerSettings.Instance)
				return;
			ILSpySettings.Update(root => Save(root));
		}

		void Save(XElement root) {
			if (this != DebuggerSettings.Instance)
				return;
			if (disableSaveCounter != 0)
				return;

			var csx = new XElement(SETTINGS_NAME);
			var existingElement = root.Element(SETTINGS_NAME);
			if (existingElement != null)
				existingElement.ReplaceWith(csx);
			else
				root.Add(csx);

			csx.SetAttributeValue("UseHexadecimal", UseHexadecimal);
			csx.SetAttributeValue("SyntaxHighlightCallStack", SyntaxHighlightCallStack);
			csx.SetAttributeValue("SyntaxHighlightBreakpoints", SyntaxHighlightBreakpoints);
			csx.SetAttributeValue("SyntaxHighlightThreads", SyntaxHighlightThreads);
			csx.SetAttributeValue("SyntaxHighlightModules", SyntaxHighlightModules);
			csx.SetAttributeValue("SyntaxHighlightLocals", SyntaxHighlightLocals);
			csx.SetAttributeValue("SyntaxHighlightAttach", SyntaxHighlightAttach);
			csx.SetAttributeValue("BreakProcessType", (int)BreakProcessType);
			csx.SetAttributeValue("PropertyEvalAndFunctionCalls", PropertyEvalAndFunctionCalls);
			csx.SetAttributeValue("UseStringConversionFunction", UseStringConversionFunction);
			csx.SetAttributeValue("DebuggerBrowsableAttributesCanHidePropsFields", DebuggerBrowsableAttributesCanHidePropsFields);
			csx.SetAttributeValue("CompilerGeneratedAttributesCanHideFields", CompilerGeneratedAttributesCanHideFields);
			csx.SetAttributeValue("DisableManagedDebuggerDetection", DisableManagedDebuggerDetection);
			csx.SetAttributeValue("IgnoreBreakInstructions", IgnoreBreakInstructions);
		}

		public DebuggerSettings CopyTo(DebuggerSettings other) {
			other.UseHexadecimal = this.UseHexadecimal;
			other.SyntaxHighlightCallStack = this.SyntaxHighlightCallStack;
			other.SyntaxHighlightBreakpoints = this.SyntaxHighlightBreakpoints;
			other.SyntaxHighlightThreads = this.SyntaxHighlightThreads;
			other.SyntaxHighlightModules = this.SyntaxHighlightModules;
			other.SyntaxHighlightLocals = this.SyntaxHighlightLocals;
			other.SyntaxHighlightAttach = this.SyntaxHighlightAttach;
			other.BreakProcessType = this.BreakProcessType;
			other.PropertyEvalAndFunctionCalls = this.PropertyEvalAndFunctionCalls;
			other.UseStringConversionFunction = this.UseStringConversionFunction;
			other.DebuggerBrowsableAttributesCanHidePropsFields = this.DebuggerBrowsableAttributesCanHidePropsFields;
			other.CompilerGeneratedAttributesCanHideFields = this.CompilerGeneratedAttributesCanHideFields;
			other.DisableManagedDebuggerDetection = this.DisableManagedDebuggerDetection;
			other.IgnoreBreakInstructions = this.IgnoreBreakInstructions;
			return other;
		}

		public DebuggerSettings Clone() {
			return CopyTo(new DebuggerSettings());
		}

		internal static void WriteNewSettings(XElement root, DebuggerSettings settings) {
			try {
				DebuggerSettings.Instance.disableSaveCounter++;
				settings.CopyTo(DebuggerSettings.Instance);
			}
			finally {
				DebuggerSettings.Instance.disableSaveCounter--;
			}
			DebuggerSettings.Instance.Save(root);
		}
	}
}
