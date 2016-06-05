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

namespace dnSpy.Contracts.Text.Editor.Operations {
	/// <summary>
	/// <see cref="ITextStructureNavigator"/> selector service
	/// </summary>
	public interface ITextStructureNavigatorSelectorService {
		/// <summary>
		/// Creates a new <see cref="ITextStructureNavigator"/> for the specified <see cref="ITextBuffer"/> by using the specified <see cref="IContentType"/> to select the navigator
		/// </summary>
		/// <param name="textBuffer">Text buffer</param>
		/// <param name="contentType">Content type</param>
		/// <returns></returns>
		ITextStructureNavigator CreateTextStructureNavigator(ITextBuffer textBuffer, IContentType contentType);

		/// <summary>
		/// Creates a new <see cref="ITextStructureNavigator"/> for the specified <see cref="ITextBuffer"/> by using the specified <see cref="IContentType"/> to select the navigator
		/// </summary>
		/// <param name="textBuffer">Text buffer</param>
		/// <param name="contentType">Content type, eg. <see cref="ContentTypes.PLAIN_TEXT"/></param>
		/// <returns></returns>
		ITextStructureNavigator CreateTextStructureNavigator(ITextBuffer textBuffer, Guid contentType);

		/// <summary>
		/// Gets a <see cref="ITextStructureNavigator"/> for the specified <see cref="ITextBuffer"/>, either by creating a new one or by using a cached value
		/// </summary>
		/// <param name="textBuffer">Text buffer</param>
		/// <returns></returns>
		ITextStructureNavigator GetTextStructureNavigator(ITextBuffer textBuffer);
	}
}
