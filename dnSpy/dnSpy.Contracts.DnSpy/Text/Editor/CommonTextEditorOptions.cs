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

using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Contracts.Text.Editor {
	/// <summary>
	/// Common text editor options
	/// </summary>
	public class CommonTextEditorOptions : TextViewCreatorOptions {
		/// <summary>
		/// Content type or null
		/// </summary>
		public IContentType ContentType { get; set; }

		/// <summary>
		/// Content type string or null
		/// </summary>
		public string ContentTypeString { get; set; }

		/// <summary>
		/// Clones this
		/// </summary>
		/// <returns></returns>
		public new CommonTextEditorOptions Clone() => CopyTo(new CommonTextEditorOptions());

		/// <summary>
		/// Copy this to <paramref name="other"/>
		/// </summary>
		/// <param name="other">Other instance</param>
		/// <returns></returns>
		public CommonTextEditorOptions CopyTo(CommonTextEditorOptions other) {
			base.CopyTo(other);
			other.ContentType = ContentType;
			other.ContentTypeString = ContentTypeString;
			return other;
		}
	}
}
