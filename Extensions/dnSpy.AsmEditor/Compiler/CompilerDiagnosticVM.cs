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

using System.Diagnostics;
using System.IO;
using System.Text;
using dnSpy.AsmEditor.Properties;
using dnSpy.Contracts.AsmEditor.Compiler;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.MVVM;

namespace dnSpy.AsmEditor.Compiler {
	sealed class CompilerDiagnosticVM : ViewModelBase {
		public CompilerDiagnostic Diagnostic { get; }

		public ImageReference ImageReference { get; }
		public string Code => Diagnostic.Id;
		public string Description => Diagnostic.Description;
		public string File => GetFilename(Diagnostic.Filename);
		public string FullPath => Diagnostic.Filename;
		public string Line => Diagnostic.LineLocationSpan == null ? string.Empty : (Diagnostic.LineLocationSpan.Value.StartLinePosition.Line + 1).ToString();
		public LineLocationSpan? LineLocationSpan => Diagnostic.LineLocationSpan;

		public CompilerDiagnosticVM(CompilerDiagnostic diag, ImageReference imageReference) {
			Diagnostic = diag;
			ImageReference = imageReference;
		}

		public void WriteTo(StringBuilder sb) {
			WriteSeverity(sb);
			sb.Append('\t');
			sb.Append(Code);
			sb.Append('\t');
			sb.Append(Description);
			sb.Append('\t');
			sb.Append(FullPath ?? string.Empty);
			sb.Append('\t');
			sb.Append(Line);
		}

		void WriteSeverity(StringBuilder sb) {
			switch (Diagnostic.Severity) {
			case CompilerDiagnosticSeverity.Hidden:	sb.Append(dnSpy_AsmEditor_Resources.StatusHidden); break;
			case CompilerDiagnosticSeverity.Info:	sb.Append(dnSpy_AsmEditor_Resources.StatusInfo); break;
			case CompilerDiagnosticSeverity.Warning:sb.Append(dnSpy_AsmEditor_Resources.StatusWarning); break;
			case CompilerDiagnosticSeverity.Error:	sb.Append(dnSpy_AsmEditor_Resources.StatusError); break;
			default: Debug.Fail($"Unknown severity {Diagnostic.Severity}"); sb.Append("???"); break;
			}
		}

		static string GetFilename(string filename) {
			if (filename == null)
				return string.Empty;
			try {
				return Path.GetFileName(filename);
			}
			catch {
			}
			return filename;
		}
	}
}
