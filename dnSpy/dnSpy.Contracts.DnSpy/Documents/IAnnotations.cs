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

namespace dnSpy.Contracts.Documents {
	/// <summary>
	/// Add/remove annotations
	/// </summary>
	public interface IAnnotations {
		/// <summary>
		/// Adds an annotation and returns it
		/// </summary>
		/// <typeparam name="T">Type</typeparam>
		/// <param name="annotation">Value to add</param>
		/// <returns></returns>
		T AddAnnotation<T>(T annotation) where T : class;

		/// <summary>
		/// Gets the first annotation of a certain type or null if none was found
		/// </summary>
		/// <typeparam name="T">Type</typeparam>
		/// <returns></returns>
		T Annotation<T>() where T : class;

		/// <summary>
		/// Gets all annotations of a certain type
		/// </summary>
		/// <typeparam name="T">Type</typeparam>
		/// <returns></returns>
		IEnumerable<T> Annotations<T>() where T : class;

		/// <summary>
		/// Removes all annotations of a certain type
		/// </summary>
		/// <typeparam name="T">Type</typeparam>
		void RemoveAnnotations<T>() where T : class;
	}
}
