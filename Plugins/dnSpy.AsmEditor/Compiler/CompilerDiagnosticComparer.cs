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
using dnSpy.Contracts.AsmEditor.Compiler;

namespace dnSpy.AsmEditor.Compiler {
	sealed class CompilerDiagnosticComparer : IComparer<CompilerDiagnostic> {
		public static readonly CompilerDiagnosticComparer Instance = new CompilerDiagnosticComparer();

		public int Compare(CompilerDiagnostic x, CompilerDiagnostic y) =>
			GetOrder(x.Severity) - GetOrder(y.Severity);

		int GetOrder(CompilerDiagnosticSeverity severity) {
			switch (severity) {
			case CompilerDiagnosticSeverity.Error:	return 0;
			case CompilerDiagnosticSeverity.Warning:return 1;
			case CompilerDiagnosticSeverity.Info:	return 2;
			case CompilerDiagnosticSeverity.Hidden:	return 3;
			default: Debug.Fail($"Unknown severity: {severity}"); return int.MaxValue;
			}
		}
	}
}
