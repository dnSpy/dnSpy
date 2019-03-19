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

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Contracts.Text.Editor {
	/// <summary>
	/// dnSpy <see cref="ITextEditorFactoryService"/> interface
	/// </summary>
	public interface IDsTextEditorFactoryService : ITextEditorFactoryService {
		/// <summary>
		/// Creates a new <see cref="IDsWpfTextView"/> instance with content type text
		/// </summary>
		/// <param name="options">Options or null</param>
		/// <returns></returns>
		IDsWpfTextView CreateTextView(TextViewCreatorOptions options);

		/// <summary>
		/// Creates a new <see cref="IDsWpfTextView"/> instance using <paramref name="textBuffer"/>
		/// </summary>
		/// <param name="textBuffer">Text buffer</param>
		/// <param name="options">Options or null</param>
		/// <returns></returns>
		IDsWpfTextView CreateTextView(ITextBuffer textBuffer, TextViewCreatorOptions options);

		/// <summary>
		/// Creates a new <see cref="IDsWpfTextView"/> instance using <paramref name="textBuffer"/>
		/// </summary>
		/// <param name="textBuffer">Text buffer</param>
		/// <param name="roles">Roles</param>
		/// <param name="options">Options or null</param>
		/// <returns></returns>
		IDsWpfTextView CreateTextView(ITextBuffer textBuffer, ITextViewRoleSet roles, TextViewCreatorOptions options);

		/// <summary>
		/// Creates a new <see cref="IDsWpfTextView"/> instance using <paramref name="textBuffer"/>
		/// </summary>
		/// <param name="textBuffer">Text buffer</param>
		/// <param name="roles">Roles</param>
		/// <param name="parentOptions">Parent options</param>
		/// <param name="options">Options or null</param>
		/// <returns></returns>
		IDsWpfTextView CreateTextView(ITextBuffer textBuffer, ITextViewRoleSet roles, IEditorOptions parentOptions, TextViewCreatorOptions options);

		/// <summary>
		/// Creates a new <see cref="IDsWpfTextView"/> instance using <paramref name="dataModel"/>
		/// </summary>
		/// <param name="dataModel">Data model</param>
		/// <param name="roles">Roles</param>
		/// <param name="parentOptions">Parent options</param>
		/// <param name="options">Options or null</param>
		/// <returns></returns>
		IDsWpfTextView CreateTextView(ITextDataModel dataModel, ITextViewRoleSet roles, IEditorOptions parentOptions, TextViewCreatorOptions options);

		/// <summary>
		/// Creates a new <see cref="IDsWpfTextView"/> instance using <paramref name="viewModel"/>
		/// </summary>
		/// <param name="viewModel">View model</param>
		/// <param name="roles">Roles</param>
		/// <param name="parentOptions">Parent options</param>
		/// <param name="options">Options or null</param>
		/// <returns></returns>
		IDsWpfTextView CreateTextView(ITextViewModel viewModel, ITextViewRoleSet roles, IEditorOptions parentOptions, TextViewCreatorOptions options);

		/// <summary>
		/// Creates a new <see cref="IDsWpfTextViewHost"/> instance
		/// </summary>
		/// <param name="wpfTextView">Text view</param>
		/// <param name="setFocus">true to set focus</param>
		/// <returns></returns>
		IDsWpfTextViewHost CreateTextViewHost(IDsWpfTextView wpfTextView, bool setFocus);
	}
}
