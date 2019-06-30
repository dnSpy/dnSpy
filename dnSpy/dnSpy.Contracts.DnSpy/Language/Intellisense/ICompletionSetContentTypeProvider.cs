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

using dnSpy.Contracts.Language.Intellisense.Classification;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Contracts.Language.Intellisense {
	/// <summary>
	/// Can be implemented by a <see cref="CompletionSet"/> to return a content type that
	/// should be used when classifying completion items
	/// </summary>
	public interface ICompletionSetContentTypeProvider {
		/// <summary>
		/// Returns the content type or null
		/// </summary>
		/// <param name="contentTypeRegistryService">Content type registry service</param>
		/// <param name="kind">Kind</param>
		/// <returns></returns>
		IContentType? GetContentType(IContentTypeRegistryService contentTypeRegistryService, CompletionClassifierKind kind);
	}
}
