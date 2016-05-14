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
using System.Text;

namespace dnSpy.AsmEditor.Compile {
	sealed class CompilerDiagnostic {
		/// <summary>
		/// Gets the severity
		/// </summary>
		public CompilerDiagnosticSeverity Severity { get; }

		/// <summary>
		/// Description
		/// </summary>
		public string Description { get; }

		/// <summary>
		/// Id, eg. CS0001
		/// </summary>
		public string Id { get; }

		/// <summary>
		/// Filename or null
		/// </summary>
		public string Filename { get; }

		/// <summary>
		/// Location in the file or null
		/// </summary>
		public LineLocationSpan? LineLocationSpan { get; }

		public CompilerDiagnostic(CompilerDiagnosticSeverity severity, string description, string id, string filename, LineLocationSpan? lineLocationSpan) {
			Severity = severity;
			Description = description ?? string.Empty;
			Id = id ?? string.Empty;
			Filename = filename;
			LineLocationSpan = lineLocationSpan;
		}

		public override string ToString() {
			var sb = new StringBuilder();
			sb.Append(Filename ?? "???");
			if (LineLocationSpan != null)
				sb.Append(LineLocationSpan.Value.StartLinePosition.ToString());
			sb.Append(": ");
			switch (Severity) {
			case CompilerDiagnosticSeverity.Hidden: sb.Append("hidden"); break;
			case CompilerDiagnosticSeverity.Info: sb.Append("info"); break;
			case CompilerDiagnosticSeverity.Warning: sb.Append("warning"); break;
			case CompilerDiagnosticSeverity.Error: sb.Append("error"); break;
			default: Debug.Fail($"Unknown severity {Severity}"); sb.Append("???"); break;
			}
			sb.Append(' ');
			sb.Append(Id);
			sb.Append(": ");
			sb.Append(Description);
			return sb.ToString();
		}
	}

	enum CompilerDiagnosticSeverity {
		Hidden,
		Info,
		Warning,
		Error,
	}

	struct LineLocation {
		public int Line { get; }
		public int Character { get; }

		public LineLocation(int line, int character) {
			Line = line;
			Character = character;
		}

		public override string ToString() => $"({Line},{Character})";
	}

	struct LineLocationSpan {
		public LineLocation StartLinePosition { get; }
		public LineLocation EndLinePosition { get; }

		public LineLocationSpan(LineLocation startLinePosition, LineLocation endLinePosition) {
			StartLinePosition = startLinePosition;
			EndLinePosition = endLinePosition;
		}

		public override string ToString() => $"{StartLinePosition}-{EndLinePosition}";
	}
}
