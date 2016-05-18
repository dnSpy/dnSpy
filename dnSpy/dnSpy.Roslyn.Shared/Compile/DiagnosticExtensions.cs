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

using System.Collections.Generic;
using System.Diagnostics;
using dnSpy.Contracts.AsmEditor.Compile;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace dnSpy.Roslyn.Shared.Compile {
	static class DiagnosticExtensions {
		public static IEnumerable<CompilerDiagnostic> ToCompilerDiagnostics(this IEnumerable<Diagnostic> diagnostics) {
			foreach (var d in diagnostics) {
				var severity = d.Severity.ToCompilerDiagnosticSeverity();
				var description = d.GetMessage();
				var id = d.Id;
				string filename;
				LineLocationSpan? lineLocationSpan;
				if (d.Location.IsInSource) {
					var pos = d.Location.GetLineSpan();
					filename = pos.Path;
					lineLocationSpan = pos.ToLineLocationSpan();
				}
				else {
					filename = null;
					lineLocationSpan = null;
				}
				yield return new CompilerDiagnostic(severity, description, id, filename, lineLocationSpan);
			}
		}

		static CompilerDiagnosticSeverity ToCompilerDiagnosticSeverity(this DiagnosticSeverity severity) {
			switch (severity) {
			case DiagnosticSeverity.Hidden:	return CompilerDiagnosticSeverity.Hidden;
			case DiagnosticSeverity.Info:	return CompilerDiagnosticSeverity.Info;
			case DiagnosticSeverity.Warning:return CompilerDiagnosticSeverity.Warning;
			case DiagnosticSeverity.Error:	return CompilerDiagnosticSeverity.Error;
			default:
				Debug.Fail($"Unknown severity: {severity}");
				return CompilerDiagnosticSeverity.Hidden;
			}
		}

		static LineLocationSpan ToLineLocationSpan(this FileLinePositionSpan pos) =>
			new LineLocationSpan(pos.StartLinePosition.ToLineLocation(), pos.EndLinePosition.ToLineLocation());

		static LineLocation ToLineLocation(this LinePosition pos) => new LineLocation(pos.Line + 1, pos.Character + 1);
	}
}
