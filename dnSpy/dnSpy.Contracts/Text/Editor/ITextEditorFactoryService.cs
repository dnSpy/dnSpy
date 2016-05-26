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

namespace dnSpy.Contracts.Text.Editor {
	/// <summary>
	/// <see cref="ITextView"/> factory
	/// </summary>
	public interface ITextEditorFactoryService {
		/// <summary>
		/// All predefined roles
		/// </summary>
		ITextViewRoleSet AllPredefinedRoles { get; }

		/// <summary>
		/// Default roles
		/// </summary>
		ITextViewRoleSet DefaultRoles { get; }

		/// <summary>
		/// No roles
		/// </summary>
		ITextViewRoleSet NoRoles { get; }

		/// <summary>
		/// Creates a new <see cref="ITextViewRoleSet"/>
		/// </summary>
		/// <param name="roles">Roles</param>
		/// <returns></returns>
		ITextViewRoleSet CreateTextViewRoleSet(IEnumerable<string> roles);

		/// <summary>
		/// Creates a new <see cref="ITextViewRoleSet"/>
		/// </summary>
		/// <param name="roles">Roles</param>
		/// <returns></returns>
		ITextViewRoleSet CreateTextViewRoleSet(params string[] roles);

		/// <summary>
		/// Creates a new <see cref="IWpfTextView"/> instance with content type <see cref="ContentTypes.TEXT"/>
		/// </summary>
		/// <param name="options">Options or null</param>
		/// <returns></returns>
		IWpfTextView CreateTextView(TextViewCreatorOptions options = null);

		/// <summary>
		/// Creates a new <see cref="IWpfTextView"/> instance using <paramref name="textBuffer"/>
		/// </summary>
		/// <param name="textBuffer">Text buffer</param>
		/// <param name="options">Options or null</param>
		/// <returns></returns>
		IWpfTextView CreateTextView(ITextBuffer textBuffer, TextViewCreatorOptions options = null);

		/// <summary>
		/// Creates a new <see cref="IWpfTextView"/> instance using <paramref name="textBuffer"/>
		/// </summary>
		/// <param name="textBuffer">Text buffer</param>
		/// <param name="roles">Roles</param>
		/// <param name="options">Options or null</param>
		/// <returns></returns>
		IWpfTextView CreateTextView(ITextBuffer textBuffer, ITextViewRoleSet roles, TextViewCreatorOptions options = null);

		/// <summary>
		/// Creates a new <see cref="IWpfTextView"/> instance using <paramref name="textBuffer"/>
		/// </summary>
		/// <param name="textBuffer">Text buffer</param>
		/// <param name="roles">Roles</param>
		/// <param name="parentOptions">Parent options</param>
		/// <param name="options">Options or null</param>
		/// <returns></returns>
		IWpfTextView CreateTextView(ITextBuffer textBuffer, ITextViewRoleSet roles, IEditorOptions parentOptions, TextViewCreatorOptions options = null);

		/// <summary>
		/// Creates a new <see cref="IWpfTextView"/> instance using <paramref name="dataModel"/>
		/// </summary>
		/// <param name="dataModel">Data model</param>
		/// <param name="roles">Roles</param>
		/// <param name="parentOptions">Parent options</param>
		/// <param name="options">Options or null</param>
		/// <returns></returns>
		IWpfTextView CreateTextView(ITextDataModel dataModel, ITextViewRoleSet roles, IEditorOptions parentOptions, TextViewCreatorOptions options = null);

		/// <summary>
		/// Creates a new <see cref="IWpfTextView"/> instance using <paramref name="viewModel"/>
		/// </summary>
		/// <param name="viewModel">View model</param>
		/// <param name="roles">Roles</param>
		/// <param name="parentOptions">Parent options</param>
		/// <param name="options">Options or null</param>
		/// <returns></returns>
		IWpfTextView CreateTextView(ITextViewModel viewModel, ITextViewRoleSet roles, IEditorOptions parentOptions, TextViewCreatorOptions options = null);

		/// <summary>
		/// Creates a new <see cref="IWpfTextViewHost"/> instance
		/// </summary>
		/// <param name="wpfTextView">Text view</param>
		/// <param name="setFocus">true to give the host focus</param>
		/// <returns></returns>
		IWpfTextViewHost CreateTextViewHost(IWpfTextView wpfTextView, bool setFocus);

		/// <summary>
		/// Raised when a new <see cref="ITextView"/> has been created
		/// </summary>
		event EventHandler<TextViewCreatedEventArgs> TextViewCreated;
	}
}
