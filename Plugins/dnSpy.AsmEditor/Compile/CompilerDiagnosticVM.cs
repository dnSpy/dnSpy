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

using System.Diagnostics;
using System.IO;
using System.Text;
using dnSpy.AsmEditor.Properties;
using dnSpy.Shared.MVVM;

namespace dnSpy.AsmEditor.Compile {
	sealed class CompilerDiagnosticVM : ViewModelBase {
		readonly CompilerDiagnostic diag;

		public object ImageObj { get; }
		public string Code => diag.Id;
		public string Description => diag.Description;
		public string File => GetFilename(diag.Filename);
		public string Line => diag.LineLocationSpan == null ? string.Empty : diag.LineLocationSpan.Value.StartLinePosition.Line.ToString();
		public LineLocationSpan? LineLocationSpan => diag.LineLocationSpan;

		public CompilerDiagnosticVM(CompilerDiagnostic diag, object image) {
			this.diag = diag;
			ImageObj = image;
		}

		public void WriteTo(StringBuilder sb) {
			WriteSeverity(sb);
			sb.Append('\t');
			sb.Append(Code);
			sb.Append('\t');
			sb.Append(Description);
			sb.Append('\t');
			sb.Append(diag.Filename ?? string.Empty);
			sb.Append('\t');
			sb.Append(Line);
		}

		void WriteSeverity(StringBuilder sb) {
			switch (diag.Severity) {
			case CompilerDiagnosticSeverity.Hidden:	sb.Append(dnSpy_AsmEditor_Resources.StatusHidden); break;
			case CompilerDiagnosticSeverity.Info:	sb.Append(dnSpy_AsmEditor_Resources.StatusInfo); break;
			case CompilerDiagnosticSeverity.Warning:sb.Append(dnSpy_AsmEditor_Resources.StatusWarning); break;
			case CompilerDiagnosticSeverity.Error:	sb.Append(dnSpy_AsmEditor_Resources.StatusError); break;
			default: Debug.Fail($"Unknown severity {diag.Severity}"); sb.Append("???"); break;
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
