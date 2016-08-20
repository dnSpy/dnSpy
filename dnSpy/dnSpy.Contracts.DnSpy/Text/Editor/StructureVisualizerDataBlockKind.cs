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

namespace dnSpy.Contracts.Text.Editor {
	/// <summary>
	/// Block kind
	/// </summary>
	public enum StructureVisualizerDataBlockKind {
		/// <summary>
		/// Not a block
		/// </summary>
		None,

		/// <summary>
		/// Namespace
		/// </summary>
		Namespace,

		/// <summary>
		/// Type
		/// </summary>
		Type,

		/// <summary>
		/// Method
		/// </summary>
		Method,

		/// <summary>
		/// Conditional
		/// </summary>
		Conditional,

		/// <summary>
		/// Loop
		/// </summary>
		Loop,

		/// <summary>
		/// Property
		/// </summary>
		Property,

		/// <summary>
		/// Event
		/// </summary>
		Event,

		/// <summary>
		/// Try
		/// </summary>
		Try,

		/// <summary>
		/// Catch
		/// </summary>
		Catch,

		/// <summary>
		/// Catch filter
		/// </summary>
		Filter,

		/// <summary>
		/// Finally
		/// </summary>
		Finally,

		/// <summary>
		/// Fault
		/// </summary>
		Fault,

		/// <summary>
		/// Other block kind
		/// </summary>
		Other,
	}
}
