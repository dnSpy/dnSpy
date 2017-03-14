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

using System.ComponentModel.Composition;
using dnSpy.Contracts.Debugger.Exceptions;
using dnSpy.Contracts.Debugger.Text;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Utilities;
using dnSpy.Debugger.Properties;

namespace dnSpy.Debugger.ToolWindows.Exceptions {
	[Export(typeof(ExceptionFormatterProvider))]
	sealed class ExceptionFormatterProvider {
		public ExceptionFormatter Create() => ExceptionFormatter.Create_DONT_USE();
	}

	sealed class ExceptionFormatter {
		ExceptionFormatter() { }
		internal static ExceptionFormatter Create_DONT_USE() => new ExceptionFormatter();

		public void WriteName(ITextColorWriter output, IDebugOutputWriter debugOutputWriter, ExceptionVM vm) =>
			vm.Context.ExceptionFormatterService.WriteName(debugOutputWriter, vm.Definition, includeDescription: true);

		public void WriteGroup(ITextColorWriter output, ExceptionVM vm) {
			if (vm.Context.ExceptionSettingsService.TryGetGroupDefinition(vm.Definition.Id.Group, out var def))
				output.Write(BoxedTextColor.Text, def.DisplayName);
			else
				WriteError(output);
		}

		void WriteError(ITextColorWriter output) => output.Write(BoxedTextColor.Error, "???");

		public void WriteConditions(ITextColorWriter output, ExceptionVM vm) {
			var conditions = vm.Settings.Conditions;
			for (int i = 0; i < conditions.Count; i++) {
				if (i != 0) {
					output.WriteSpace();
					output.Write(BoxedTextColor.Keyword, dnSpy_Debugger_Resources.Exception_Conditions_And);
					output.WriteSpace();
				}
				var cond = conditions[i];
				switch (cond.ConditionType) {
				case DbgExceptionConditionType.ModuleNameEquals:
					WriteQuotedString(output, dnSpy_Debugger_Resources.Exception_Conditions_ModuleNameEquals, cond.Condition);
					break;

				case DbgExceptionConditionType.ModuleNameNotEquals:
					WriteQuotedString(output, dnSpy_Debugger_Resources.Exception_Conditions_ModuleNameNotEquals, cond.Condition);
					break;

				default:
					WriteError(output);
					break;
				}
			}
		}

		void WriteQuotedString(ITextColorWriter output, string text, string s) {
			output.Write(BoxedTextColor.Text, text);
			output.Write(BoxedTextColor.Text, " ");
			output.Write(BoxedTextColor.String, SimpleTypeConverter.ToString(s, true));
		}
	}
}
