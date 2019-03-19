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
	/// Thrown when a type or member couldn't be resolved
	/// </summary>
	[Serializable]
	public class ResolveException : Exception {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="message">Message</param>
		public ResolveException(string message) : base(message) { }
	}

	/// <summary>
	/// Thrown when a type couldn't be resolved
	/// </summary>
	[Serializable]
	public sealed class TypeResolveException : ResolveException {
		/// <summary>
		/// Gets the type that couldn't be resolved
		/// </summary>
		public DmdType Type { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="type">Type that couldn't be resolved</param>
		public TypeResolveException(DmdType type) : base("Couldn't resolve type: " + type.ToString()) => Type = type;
	}

	/// <summary>
	/// Thrown when a field couldn't be resolved
	/// </summary>
	[Serializable]
	public sealed class FieldResolveException : ResolveException {
		/// <summary>
		/// Gets the field that couldn't be resolved
		/// </summary>
		public DmdFieldInfo Field { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="field">Field that couldn't be resolved</param>
		public FieldResolveException(DmdFieldInfo field) : base("Couldn't resolve field: " + field.ToString() + ", type: " + field.DeclaringType.ToString()) => Field = field;
	}

	/// <summary>
	/// Thrown when a method couldn't be resolved
	/// </summary>
	[Serializable]
	public sealed class MethodResolveException : ResolveException {
		/// <summary>
		/// Gets the method that couldn't be resolved
		/// </summary>
		public DmdMethodBase Method { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="method">Method that couldn't be resolved</param>
		public MethodResolveException(DmdMethodBase method) : base("Couldn't resolve method: " + method.ToString() + ", type: " + method.DeclaringType.ToString()) => Method = method;
	}
}
