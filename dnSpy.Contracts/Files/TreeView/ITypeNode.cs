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

using dnlib.DotNet;
using dnSpy.Contracts.TreeView;

namespace dnSpy.Contracts.Files.TreeView {
	/// <summary>
	/// A type node
	/// </summary>
	public interface ITypeNode : IFileTreeNodeData, IMDTokenNode {
		/// <summary>
		/// Gets the type
		/// </summary>
		TypeDef TypeDef { get; }

		/// <summary>
		/// Creates a <see cref="IMethodNode"/>
		/// </summary>
		/// <param name="method">Method</param>
		/// <returns></returns>
		IMethodNode Create(MethodDef method);

		/// <summary>
		/// Creates a <see cref="IPropertyNode"/>
		/// </summary>
		/// <param name="property">Property</param>
		/// <returns></returns>
		IPropertyNode Create(PropertyDef property);

		/// <summary>
		/// Creates a <see cref="IEventNode"/>
		/// </summary>
		/// <param name="event">Event</param>
		/// <returns></returns>
		IEventNode Create(EventDef @event);

		/// <summary>
		/// Creates a <see cref="IFieldNode"/>
		/// </summary>
		/// <param name="field">Field</param>
		/// <returns></returns>
		IFieldNode Create(FieldDef field);

		/// <summary>
		/// Creates a <see cref="ITypeNode"/>
		/// </summary>
		/// <param name="type">Type</param>
		/// <returns></returns>
		ITypeNode Create(TypeDef type);
	}
}
