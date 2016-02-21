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

namespace dnSpy.Contracts.TextEditor {
	/// <summary>
	/// <see cref="IReplEditor"/> options
	/// </summary>
	public sealed class ReplEditorOptions {
		/// <summary>
		/// Default <see cref="PromptText"/> value
		/// </summary>
		public static readonly string DEFAULT_PROMPT_TEXT = "> ";

		/// <summary>
		/// Default <see cref="ContinueText"/> value
		/// </summary>
		public static readonly string DEFAULT_CONTINUE_TEXT = ". ";

		/// <summary>
		/// Prompt, default is <see cref="DEFAULT_PROMPT_TEXT"/>
		/// </summary>
		public string PromptText {
			get { return promptText ?? DEFAULT_PROMPT_TEXT; }
			set { promptText = value; }
		}
		string promptText;

		/// <summary>
		/// Continue text, default is <see cref="DEFAULT_CONTINUE_TEXT"/>
		/// </summary>
		public string ContinueText {
			get { return continueText ?? DEFAULT_CONTINUE_TEXT; }
			set { continueText = value; }
		}
		string continueText;

		/// <summary>
		/// Command guid of text editor or null
		/// </summary>
		public Guid? TextEditorCommandGuid { get; set; }

		/// <summary>
		/// Command guid of text area or null
		/// </summary>
		public Guid? TextAreaCommandGuid { get; set; }

		/// <summary>
		/// Guid of context menu or null
		/// </summary>
		public Guid? MenuGuid { get; set; }

		/// <summary>
		/// Clones this
		/// </summary>
		/// <returns></returns>
		public ReplEditorOptions Clone() {
			return CopyTo(new ReplEditorOptions());
		}

		ReplEditorOptions CopyTo(ReplEditorOptions other) {
			other.PromptText = PromptText;
			other.ContinueText = ContinueText;
			other.TextEditorCommandGuid = TextEditorCommandGuid;
			other.TextAreaCommandGuid = TextAreaCommandGuid;
			other.MenuGuid = MenuGuid;
			return other;
		}
	}
}
