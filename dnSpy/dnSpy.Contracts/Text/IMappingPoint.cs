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
	/// Mapping point
	/// </summary>
	public interface IMappingPoint {
		/// <summary>
		/// The <see cref="ITextBuffer"/> from which this point was created
		/// </summary>
		ITextBuffer AnchorBuffer { get; }

		/// <summary>
		/// Maps the point to a particular <see cref="ITextBuffer"/>
		/// </summary>
		/// <param name="targetBuffer">Target buffer</param>
		/// <param name="affinity">Affinity</param>
		/// <returns></returns>
		SnapshotPoint? GetPoint(ITextBuffer targetBuffer, PositionAffinity affinity);

		/// <summary>
		/// Maps the point to a particular <see cref="ITextSnapshot"/>
		/// </summary>
		/// <param name="targetSnapshot">Target snapshot</param>
		/// <param name="affinity">Affinity</param>
		/// <returns></returns>
		SnapshotPoint? GetPoint(ITextSnapshot targetSnapshot, PositionAffinity affinity);

		/// <summary>
		/// Maps the point to a matching <see cref="ITextBuffer"/>
		/// </summary>
		/// <param name="match">The predicate used to match the <see cref="ITextBuffer"/></param>
		/// <param name="affinity">Affinity</param>
		/// <returns></returns>
		SnapshotPoint? GetPoint(Predicate<ITextBuffer> match, PositionAffinity affinity);
	}
}
