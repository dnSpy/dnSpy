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
	/// <see cref="IClassificationType"/> registry service
	/// </summary>
	public interface IClassificationTypeRegistryService {
		/// <summary>
		/// Creates a classification type
		/// </summary>
		/// <param name="type">Classfication type</param>
		/// <param name="baseTypes">Base types</param>
		/// <returns></returns>
		IClassificationType CreateClassificationType(string type, IEnumerable<IClassificationType> baseTypes);

		/// <summary>
		/// Creates a classification type
		/// </summary>
		/// <param name="type">Classfication type</param>
		/// <param name="baseTypes">Base types</param>
		/// <returns></returns>
		IClassificationType CreateClassificationType(Guid type, IEnumerable<IClassificationType> baseTypes);

		/// <summary>
		/// Creates a transient classification type
		/// </summary>
		/// <param name="baseTypes">Base types</param>
		/// <returns></returns>
		IClassificationType CreateTransientClassificationType(IEnumerable<IClassificationType> baseTypes);

		/// <summary>
		/// Creates a transient classification type
		/// </summary>
		/// <param name="baseTypes">Base types</param>
		/// <returns></returns>
		IClassificationType CreateTransientClassificationType(params IClassificationType[] baseTypes);

		/// <summary>
		/// Gets a <see cref="IClassificationType"/> or null if it doesn't exist
		/// </summary>
		/// <param name="type">Classification type</param>
		/// <returns></returns>
		IClassificationType GetClassificationType(string type);

		/// <summary>
		/// Gets a <see cref="IClassificationType"/> or null if it doesn't exist
		/// </summary>
		/// <param name="type">Classification type</param>
		/// <returns></returns>
		IClassificationType GetClassificationType(Guid type);
	}
}
