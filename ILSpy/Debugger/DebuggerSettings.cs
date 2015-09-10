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

using System.Xml.Linq;
using dndbg.Engine;
using dnSpy.MVVM;
using ICSharpCode.ILSpy;

namespace dnSpy.Debugger {
	sealed class DebuggerSettings : ViewModelBase {
		public static readonly DebuggerSettings Instance = new DebuggerSettings();
		int disableSaveCounter;

		DebuggerSettings() {
			LoadSettings();
		}

		public bool UseHexadecimal {
			get { return useHexadecimal; }
			set {
				if (useHexadecimal != value) {
					useHexadecimal = value;
					SaveSettings();
					OnPropertyChanged("UseHexadecimal");
				}
			}
		}
		bool useHexadecimal;

		public bool SyntaxHighlightCallStack {
			get { return syntaxHighlightCallStack; }
			set {
				if (syntaxHighlightCallStack != value) {
					syntaxHighlightCallStack = value;
					SaveSettings();
					OnPropertyChanged("SyntaxHighlightCallStack");
				}
			}
		}
		bool syntaxHighlightCallStack;

		public bool SyntaxHighlightBreakpoints {
			get { return syntaxHighlightBreakpoints; }
			set {
				if (syntaxHighlightBreakpoints != value) {
					syntaxHighlightBreakpoints = value;
					SaveSettings();
					OnPropertyChanged("SyntaxHighlightBreakpoints");
				}
			}
		}
		bool syntaxHighlightBreakpoints;

		public BreakProcessType BreakProcessType {
			get { return breakProcessType; }
			set {
				if (breakProcessType != value) {
					breakProcessType = value;
					SaveSettings();
					OnPropertyChanged("BreakProcessType");
				}
			}
		}
		BreakProcessType breakProcessType;

		const string SETTINGS_NAME = "DebuggerSettings";

		void LoadSettings() {
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
			BreakProcessType = (BreakProcessType)((int?)csx.Attribute("BreakProcessType") ?? (int)BreakProcessType.ModuleCctorOrEntryPoint);
		}

		void SaveSettings() {
			if (this != Instance)
				return;
			ILSpySettings.Update(root => Save(root));
		}

		void Save(XElement root) {
			if (this != Instance)
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
			csx.SetAttributeValue("BreakProcessType", (int)BreakProcessType);
		}

		public DebuggerSettings CopyTo(DebuggerSettings other) {
			other.UseHexadecimal = this.UseHexadecimal;
			other.SyntaxHighlightCallStack = this.SyntaxHighlightCallStack;
			other.SyntaxHighlightBreakpoints = this.SyntaxHighlightBreakpoints;
			other.BreakProcessType = this.BreakProcessType;
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
