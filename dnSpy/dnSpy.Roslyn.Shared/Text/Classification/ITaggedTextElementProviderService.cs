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

using dnSpy.Contracts.Text.Classification;

namespace dnSpy.Roslyn.Shared.Text.Classification {
	interface ITaggedTextElementProviderService {
		/// <summary>
		/// Creates a <see cref="ITaggedTextElementProvider"/>
		/// </summary>
		/// <param name="classifiers">Classifiers to use. The context passed to them will be a <see cref="TaggedTextClassifierContext"/></param>
		/// <param name="category">Category, eg. <see cref="AppearanceCategoryConstants.CodeCompletionToolTip"/></param>
		/// <returns></returns>
		ITaggedTextElementProvider Create(ITextClassifier[] classifiers, string category);
	}
}
