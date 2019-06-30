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
using dnlib.DotNet;
using dnSpy.Contracts.TreeView;

namespace dnSpy.Contracts.Documents.TreeView {
	/// <summary>
	/// A type node
	/// </summary>
	public abstract class TypeNode : DocumentTreeNodeData, IMDTokenNode {
		/// <summary>
		/// Gets the type
		/// </summary>
		public TypeDef TypeDef { get; }

		IMDTokenProvider? IMDTokenNode.Reference => TypeDef;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="type">Type</param>
		protected TypeNode(TypeDef type) => TypeDef = type ?? throw new ArgumentNullException(nameof(type));

		/// <summary>
		/// Creates a <see cref="MethodNode"/>
		/// </summary>
		/// <param name="method">Method</param>
		/// <returns></returns>
		public MethodNode Create(MethodDef method) => Context.DocumentTreeView.Create(method);

		/// <summary>
		/// Creates a <see cref="PropertyNode"/>
		/// </summary>
		/// <param name="property">Property</param>
		/// <returns></returns>
		public PropertyNode Create(PropertyDef property) => Context.DocumentTreeView.Create(property);

		/// <summary>
		/// Creates a <see cref="EventNode"/>
		/// </summary>
		/// <param name="event">Event</param>
		/// <returns></returns>
		public EventNode Create(EventDef @event) => Context.DocumentTreeView.Create(@event);

		/// <summary>
		/// Creates a <see cref="FieldNode"/>
		/// </summary>
		/// <param name="field">Field</param>
		/// <returns></returns>
		public FieldNode Create(FieldDef field) => Context.DocumentTreeView.Create(field);

		/// <summary>
		/// Creates a <see cref="TypeNode"/>
		/// </summary>
		/// <param name="type">Type</param>
		/// <returns></returns>
		public TypeNode Create(TypeDef type) => Context.DocumentTreeView.CreateNested(type);
	}
}
