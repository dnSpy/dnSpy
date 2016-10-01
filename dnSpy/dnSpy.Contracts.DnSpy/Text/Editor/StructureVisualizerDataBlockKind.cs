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
		/// Reference type
		/// </summary>
		Type,

		/// <summary>
		/// Module
		/// </summary>
		Module,

		/// <summary>
		/// Value type
		/// </summary>
		ValueType,

		/// <summary>
		/// Interface
		/// </summary>
		Interface,

		/// <summary>
		/// Method
		/// </summary>
		Method,

		/// <summary>
		/// Accessor
		/// </summary>
		Accessor,

		/// <summary>
		/// Anonymous method
		/// </summary>
		AnonymousMethod,

		/// <summary>
		/// Constructor
		/// </summary>
		Constructor,

		/// <summary>
		/// Destructor
		/// </summary>
		Destructor,

		/// <summary>
		/// Operator
		/// </summary>
		Operator,

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
		/// Lock
		/// </summary>
		Lock,

		/// <summary>
		/// Using
		/// </summary>
		Using,

		/// <summary>
		/// Fixed
		/// </summary>
		Fixed,

		/// <summary>
		/// Switch
		/// </summary>
		Switch,

		/// <summary>
		/// Case
		/// </summary>
		Case,

		/// <summary>
		/// Local function
		/// </summary>
		LocalFunction,

		/// <summary>
		/// Other block kind
		/// </summary>
		Other,
	}
}
