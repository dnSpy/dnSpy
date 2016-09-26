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
using System.Diagnostics;
using dnSpy.Contracts.TreeView;

namespace dnSpy.Contracts.Documents.TreeView {
	/// <summary>
	/// Node path name
	/// </summary>
	public struct NodePathName : IEquatable<NodePathName> {
		/// <summary>
		/// Gets the guid
		/// </summary>
		public Guid Guid { get; }

		/// <summary>
		/// Gets the name
		/// </summary>
		public string Name { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="guid">Guid of node (<see cref="ITreeNodeData.Guid"/>)</param>
		/// <param name="name">Extra data if needed or null</param>
		public NodePathName(Guid guid, string name = null) {
			Debug.Assert(guid != System.Guid.Empty);
			this.Guid = guid;
			this.Name = name ?? string.Empty;
		}

		/// <summary>
		/// Equals()
		/// </summary>
		/// <param name="other">Other instance</param>
		/// <returns></returns>
		public bool Equals(NodePathName other) => Guid.Equals(other.Guid) && StringComparer.Ordinal.Equals(Name, other.Name);

		/// <summary>
		/// Equals()
		/// </summary>
		/// <param name="obj">Other instance</param>
		/// <returns></returns>
		public override bool Equals(object obj) {
			if (obj is NodePathName)
				return Equals((NodePathName)obj);
			return false;
		}

		/// <summary>
		/// GetHashCode()
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode() => Guid.GetHashCode() ^ StringComparer.Ordinal.GetHashCode(Name);

		/// <summary>
		/// ToString()
		/// </summary>
		/// <returns></returns>
		public override string ToString() {
			if (string.IsNullOrEmpty(Name))
				return Guid.ToString();
			return string.Format("{0} - {1}", Guid, Name);
		}
	}
}
