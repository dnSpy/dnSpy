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
}
