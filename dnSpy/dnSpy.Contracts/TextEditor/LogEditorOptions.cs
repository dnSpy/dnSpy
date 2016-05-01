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
using dnSpy.Contracts.Menus;

namespace dnSpy.Contracts.TextEditor {
	/// <summary>
	/// <see cref="ILogEditor"/> options
	/// </summary>
	public sealed class LogEditorOptions {
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
		/// Creates <see cref="GuidObject"/>s, can be null
		/// </summary>
		public Func<GuidObject, bool, IEnumerable<GuidObject>> CreateGuidObjects { get; set; }

		/// <summary>
		/// Clones this
		/// </summary>
		/// <returns></returns>
		public LogEditorOptions Clone() {
			return CopyTo(new LogEditorOptions());
		}

		LogEditorOptions CopyTo(LogEditorOptions other) {
			other.TextEditorCommandGuid = TextEditorCommandGuid;
			other.TextAreaCommandGuid = TextAreaCommandGuid;
			other.MenuGuid = MenuGuid;
			other.ContentType = ContentType;
			other.CreateGuidObjects = CreateGuidObjects;
			return other;
		}
	}
}
