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

using System.Collections.Generic;

namespace dnSpy.Text.Classification {
	/// <summary>
	/// Creates a <see cref="ITextClassifier"/> that aggregates and normalizes
	/// all <see cref="ITextClassifier"/> contributions.
	/// </summary>
	interface ITextClassifierAggregatorService {
		/// <summary>
		/// Creates a <see cref="ITextClassifier"/> aggregator. The returned <see cref="ITextClassifier"/>
		/// implements <see cref="System.IDisposable"/> and must be <see cref="System.IDisposable.Dispose"/>'d.
		/// </summary>
		/// <param name="textClassifiers">Classifiers to use</param>
		/// <returns></returns>
		ITextClassifier Create(IEnumerable<ITextClassifier> textClassifiers);
	}
}
