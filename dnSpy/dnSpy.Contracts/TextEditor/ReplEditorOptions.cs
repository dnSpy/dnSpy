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
		/// Content type or null
		/// </summary>
		public IContentType ContentType { get; set; }

		/// <summary>
		/// Content type guid or null
		/// </summary>
		public Guid? ContentTypeGuid { get; set; }

		/// <summary>
		/// Clones this
		/// </summary>
		/// <returns></returns>
		public ReplEditorOptions Clone() => CopyTo(new ReplEditorOptions());

		ReplEditorOptions CopyTo(ReplEditorOptions other) {
			other.PrimaryPrompt = PrimaryPrompt;
			other.SecondaryPrompt = SecondaryPrompt;
			other.TextEditorCommandGuid = TextEditorCommandGuid;
			other.TextAreaCommandGuid = TextAreaCommandGuid;
			other.MenuGuid = MenuGuid;
			other.ContentType = ContentType;
			other.ContentTypeGuid = ContentTypeGuid;
			return other;
		}
	}
}
