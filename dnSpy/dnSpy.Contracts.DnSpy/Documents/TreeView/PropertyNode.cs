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
using dnlib.DotNet;
using dnSpy.Contracts.TreeView;

namespace dnSpy.Contracts.Documents.TreeView {
	/// <summary>
	/// A property node
	/// </summary>
	public abstract class PropertyNode : DocumentTreeNodeData, IMDTokenNode {
		/// <summary>
		/// Gets the property
		/// </summary>
		public PropertyDef PropertyDef { get; }

		IMDTokenProvider IMDTokenNode.Reference => PropertyDef;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="property">Property</param>
		protected PropertyNode(PropertyDef property) {
			if (property == null)
				throw new ArgumentNullException(nameof(property));
			PropertyDef = property;
		}

		/// <summary>
		/// Creates a <see cref="MethodNode"/>, a getter, setter, or an other property method
		/// </summary>
		/// <param name="method">Method</param>
		/// <returns></returns>
		public MethodNode Create(MethodDef method) => Context.DocumentTreeView.CreateProperty(method);
	}
}
