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

using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Contracts.Text.Editor {
	/// <summary>
	/// <see cref="IReplEditor"/> options
	/// </summary>
	public sealed class ReplEditorOptions : CommonTextEditorOptions {
		/// <summary>
		/// Default <see cref="PrimaryPrompt"/> value
		/// </summary>
		public static readonly string DEFAULT_PRIMARY_PROMPT_TEXT = "> ";

		/// <summary>
		/// Default <see cref="SecondaryPrompt"/> value
		/// </summary>
		public static readonly string DEFAULT_SECONDARY_PROMPT_TEXT = ". ";

		/// <summary>
		/// Primary prompt, default is <see cref="DEFAULT_PRIMARY_PROMPT_TEXT"/>
		/// </summary>
		public string PrimaryPrompt {
			get { return primaryPrompt ?? DEFAULT_PRIMARY_PROMPT_TEXT; }
			set { primaryPrompt = value; }
		}
		string primaryPrompt;

		/// <summary>
		/// Secondary prompt text, default is <see cref="DEFAULT_SECONDARY_PROMPT_TEXT"/>
		/// </summary>
		public string SecondaryPrompt {
			get { return secondaryPrompt ?? DEFAULT_SECONDARY_PROMPT_TEXT; }
			set { secondaryPrompt = value; }
		}
		string secondaryPrompt;

		/// <summary>
		/// All <see cref="ITextView"/> roles
		/// </summary>
		public HashSet<string> Roles { get; }

		static readonly string[] defaultRoles = new string[] {
			PredefinedTextViewRoles.Analyzable,
			PredefinedTextViewRoles.Editable,
			PredefinedTextViewRoles.Interactive,
			PredefinedTextViewRoles.Zoomable,
			PredefinedDnSpyTextViewRoles.ReplEditor,
			PredefinedDnSpyTextViewRoles.CanHaveCurrentLineHighlighter,
			PredefinedDnSpyTextViewRoles.CustomLineNumberMargin,
		};

		/// <summary>
		/// Constructor
		/// </summary>
		public ReplEditorOptions() {
			Roles = new HashSet<string>(defaultRoles, StringComparer.InvariantCultureIgnoreCase);
		}

		/// <summary>
		/// Clones this
		/// </summary>
		/// <returns></returns>
		public new ReplEditorOptions Clone() => CopyTo(new ReplEditorOptions());

		ReplEditorOptions CopyTo(ReplEditorOptions other) {
			base.CopyTo(other);
			other.PrimaryPrompt = PrimaryPrompt;
			other.SecondaryPrompt = SecondaryPrompt;
			other.Roles.Clear();
			foreach (var r in Roles)
				other.Roles.Add(r);
			return other;
		}
	}
}
