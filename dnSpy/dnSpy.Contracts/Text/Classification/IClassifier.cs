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

namespace dnSpy.Contracts.Text.Classification {
	/// <summary>
	/// Assigns <see cref="IClassificationType"/> objects to the text in a <see cref="ITextBuffer"/>
	/// </summary>
	public interface IClassifier {
		/// <summary>
		/// Gets all the ClassificationSpan objects that overlap the given range of text
		/// </summary>
		/// <param name="span">Span</param>
		/// <returns></returns>
		IList<ClassificationSpan> GetClassificationSpans(SnapshotSpan span);

		/// <summary>
		/// Ocurs when the classification of a span of text has changed
		/// </summary>
		event EventHandler<ClassificationChangedEventArgs> ClassificationChanged;
	}
}
