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

namespace dnSpy.Contracts.Text {
	/// <summary>
	/// Mapping span
	/// </summary>
	public interface IMappingSpan {
		/// <summary>
		/// Gets the <see cref="ITextBuffer"/> from which this span was created
		/// </summary>
		ITextBuffer AnchorBuffer { get; }

		/// <summary>
		/// Gets the <see cref="IMappingPoint"/> for the start of this span
		/// </summary>
		IMappingPoint Start { get; }

		/// <summary>
		/// Gets the <see cref="IMappingPoint"/> for the end of this span
		/// </summary>
		IMappingPoint End { get; }

		/// <summary>
		/// Maps the span to a particular <see cref="ITextBuffer"/>
		/// </summary>
		/// <param name="targetBuffer">Target buffer</param>
		/// <returns></returns>
		NormalizedSnapshotSpanCollection GetSpans(ITextBuffer targetBuffer);

		/// <summary>
		/// Maps the span to a particular <see cref="ITextSnapshot"/>
		/// </summary>
		/// <param name="targetSnapshot">Target snapshot</param>
		/// <returns></returns>
		NormalizedSnapshotSpanCollection GetSpans(ITextSnapshot targetSnapshot);

		/// <summary>
		/// Maps the span to a matching <see cref="ITextBuffer"/>
		/// </summary>
		/// <param name="match">The predicate used to identify the <see cref="ITextBuffer"/></param>
		/// <returns></returns>
		NormalizedSnapshotSpanCollection GetSpans(Predicate<ITextBuffer> match);
	}
}
