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

using Microsoft.VisualStudio.Text;

namespace dnSpy.Contracts.Text.Editor.Roslyn {
	/// <summary>
	/// <see cref="IRoslynCodeEditor"/> options
	/// </summary>
	public sealed class RoslynCodeEditorOptions : CommonTextEditorOptions {
		/// <summary>
		/// Text buffer to use or null
		/// </summary>
		public ITextBuffer TextBuffer { get; set; }

		/// <summary>
		/// Clones this
		/// </summary>
		/// <returns></returns>
		public new RoslynCodeEditorOptions Clone() => CopyTo(new RoslynCodeEditorOptions());

		RoslynCodeEditorOptions CopyTo(RoslynCodeEditorOptions other) {
			base.CopyTo(other);
			other.TextBuffer = TextBuffer;
			return other;
		}

		/// <summary>
		/// Creates a new <see cref="CodeEditorOptions"/> instance
		/// </summary>
		/// <returns></returns>
		public CodeEditorOptions ToCodeEditorOptions() {
			var res = new CodeEditorOptions {
				TextBuffer = TextBuffer,
			};
			CopyTo(res);
			return res;
		}
	}
}
