/*
    Copyright (C) 2014-2019 de4dot@gmail.com

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
using System.Collections.Generic;
using dnSpy.Contracts.Menus;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Contracts.Text.Editor {
	/// <summary>
	/// <see cref="ICodeEditor"/> options
	/// </summary>
	public sealed class CodeEditorOptions : CommonTextEditorOptions {
		/// <summary>
		/// Text buffer to use or null. Use <see cref="ITextBufferFactoryService"/> to create an instance
		/// </summary>
		public ITextBuffer TextBuffer { get; set; }

		/// <summary>
		/// All <see cref="ITextView"/> roles
		/// </summary>
		public HashSet<string> Roles { get; }

		static readonly string[] defaultRoles = new string[] {
			PredefinedTextViewRoles.Analyzable,
			PredefinedTextViewRoles.Debuggable,
			PredefinedTextViewRoles.Document,
			PredefinedTextViewRoles.Editable,
			PredefinedTextViewRoles.Interactive,
			PredefinedTextViewRoles.PrimaryDocument,
			PredefinedTextViewRoles.Structured,
			PredefinedTextViewRoles.Zoomable,
			PredefinedDsTextViewRoles.CanHaveBackgroundImage,
			PredefinedDsTextViewRoles.CodeEditor,
		};

		/// <summary>
		/// Constructor
		/// </summary>
		public CodeEditorOptions() {
			Roles = new HashSet<string>(defaultRoles, StringComparer.InvariantCultureIgnoreCase);
			MenuGuid = new Guid(MenuConstants.GUIDOBJ_CODE_EDITOR_GUID);
		}

		/// <summary>
		/// Clones this
		/// </summary>
		/// <returns></returns>
		public new CodeEditorOptions Clone() => CopyTo(new CodeEditorOptions());

		CodeEditorOptions CopyTo(CodeEditorOptions other) {
			base.CopyTo(other);
			other.TextBuffer = TextBuffer;
			other.Roles.Clear();
			foreach (var r in Roles)
				other.Roles.Add(r);
			return other;
		}
	}
}
