/*
    Copyright (C) 2014-2019 de4dot@gmail.com

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

namespace dnSpy.Debugger.DotNet.Metadata {
	/// <summary>
	/// Thrown when a type or a member couldn't be found
	/// </summary>
	[Serializable]
	public class MemberNotFoundException : Exception {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="message">Message</param>
		public MemberNotFoundException(string message) : base(message) { }
	}

	/// <summary>
	/// Thrown when a type couldn't be found
	/// </summary>
	[Serializable]
	public sealed class TypeNotFoundException : MemberNotFoundException {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="typeName">Type that couldn't be found</param>
		public TypeNotFoundException(string typeName) : base("Couldn't find type: " + typeName) { }
	}

	/// <summary>
	/// Thrown when a field couldn't be found
	/// </summary>
	[Serializable]
	public sealed class FieldNotFoundException : MemberNotFoundException {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="fieldName">Field that couldn't be found</param>
		public FieldNotFoundException(string fieldName) : base("Couldn't find field: " + fieldName) { }
	}

	/// <summary>
	/// Thrown when a method couldn't be found
	/// </summary>
	[Serializable]
	public sealed class MethodNotFoundException : MemberNotFoundException {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="methodName">Method that couldn't be found</param>
		public MethodNotFoundException(string methodName) : base("Couldn't find method: " + methodName) { }
	}

	/// <summary>
	/// Thrown when a property couldn't be found
	/// </summary>
	[Serializable]
	public sealed class PropertyNotFoundException : MemberNotFoundException {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="propertyName">Property that couldn't be found</param>
		public PropertyNotFoundException(string propertyName) : base("Couldn't find property: " + propertyName) { }
	}

	/// <summary>
	/// Thrown when an event couldn't be found
	/// </summary>
	[Serializable]
	public sealed class EventNotFoundException : MemberNotFoundException {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="eventName">Event that couldn't be found</param>
		public EventNotFoundException(string eventName) : base("Couldn't find event: " + eventName) { }
	}
}
