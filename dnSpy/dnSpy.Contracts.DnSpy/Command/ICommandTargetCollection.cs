/*
    Copyright (C) 2014-2017 de4dot@gmail.com

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

namespace dnSpy.Contracts.Command {
	/// <summary>
	/// Allows adding and removing <see cref="ICommandTargetFilter"/>s
	/// </summary>
	public interface ICommandTargetCollection : ICommandTarget {
		/// <summary>
		/// Adds a new filter
		/// </summary>
		/// <param name="filter">Filter to add</param>
		/// <param name="order">Order, eg. <see cref="CommandTargetFilterOrder.TextEditor"/></param>
		void AddFilter(ICommandTargetFilter filter, double order);

		/// <summary>
		/// Removes an added filter
		/// </summary>
		/// <param name="filter">Filter to remove</param>
		void RemoveFilter(ICommandTargetFilter filter);
	}
}
