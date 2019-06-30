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
using System.Diagnostics;
using dnlib.DotNet;
using dnSpy.Contracts.TreeView;

namespace dnSpy.Contracts.Documents.TreeView {
	/// <summary>
	/// Type References node
	/// </summary>
	public abstract class TypeReferencesFolderNode : DocumentTreeNodeData {
	}

	/// <summary>
	/// TypeSpec node
	/// </summary>
	public abstract class TypeSpecsFolderNode : DocumentTreeNodeData {
	}

	/// <summary>
	/// Method References node
	/// </summary>
	public abstract class MethodReferencesFolderNode : DocumentTreeNodeData {
	}

	/// <summary>
	/// Field References node
	/// </summary>
	public abstract class FieldReferencesFolderNode : DocumentTreeNodeData {
	}

	/// <summary>
	/// Property References node
	/// </summary>
	public abstract class PropertyReferencesFolderNode : DocumentTreeNodeData {
	}

	/// <summary>
	/// Event References node
	/// </summary>
	public abstract class EventReferencesFolderNode : DocumentTreeNodeData {
	}

	/// <summary>
	/// Type reference node
	/// </summary>
	public abstract class TypeReferenceNode : DocumentTreeNodeData, IMDTokenNode {
		/// <summary>
		/// Gets the type reference
		/// </summary>
		public ITypeDefOrRef TypeRef { get; }

		IMDTokenProvider? IMDTokenNode.Reference => TypeRef;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="type">Type ref or type spec</param>
		protected TypeReferenceNode(ITypeDefOrRef type) {
			Debug.Assert(type is TypeRef || type is TypeSpec);
			TypeRef = type ?? throw new ArgumentNullException(nameof(type));
		}
	}

	/// <summary>
	/// Method reference node
	/// </summary>
	public abstract class MethodReferenceNode : DocumentTreeNodeData, IMDTokenNode {
		/// <summary>
		/// Gets the method reference
		/// </summary>
		public IMethod MethodRef { get; }

		IMDTokenProvider? IMDTokenNode.Reference => MethodRef;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="method">Method ref</param>
		protected MethodReferenceNode(IMethod method) {
			Debug.Assert((method is MemberRef && ((MemberRef)method).IsMethodRef) || method is MethodSpec || method is MethodDef);
			MethodRef = method ?? throw new ArgumentNullException(nameof(method));
		}
	}

	/// <summary>
	/// Field reference node
	/// </summary>
	public abstract class FieldReferenceNode : DocumentTreeNodeData, IMDTokenNode {
		/// <summary>
		/// Gets the field reference
		/// </summary>
		public MemberRef FieldRef { get; }

		IMDTokenProvider? IMDTokenNode.Reference => FieldRef;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="field">Field ref</param>
		protected FieldReferenceNode(MemberRef field) {
			Debug.Assert(field.IsFieldRef);
			FieldRef = field ?? throw new ArgumentNullException(nameof(field));
		}
	}

	/// <summary>
	/// Property reference node
	/// </summary>
	public abstract class PropertyReferenceNode : DocumentTreeNodeData, IMDTokenNode {
		/// <summary>
		/// Gets the property reference
		/// </summary>
		public IMethod PropertyRef { get; }

		IMDTokenProvider? IMDTokenNode.Reference => PropertyRef;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="method">Property ref</param>
		protected PropertyReferenceNode(IMethod method) {
			Debug.Assert((method is MemberRef && ((MemberRef)method).IsMethodRef) || method is MethodSpec || method is MethodDef);
			PropertyRef = method ?? throw new ArgumentNullException(nameof(method));
		}
	}

	/// <summary>
	/// Event reference node
	/// </summary>
	public abstract class EventReferenceNode : DocumentTreeNodeData, IMDTokenNode {
		/// <summary>
		/// Gets the event reference
		/// </summary>
		public IMethod EventRef { get; }

		IMDTokenProvider? IMDTokenNode.Reference => EventRef;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="method">Event ref</param>
		protected EventReferenceNode(IMethod method) {
			Debug.Assert((method is MemberRef && ((MemberRef)method).IsMethodRef) || method is MethodSpec || method is MethodDef);
			EventRef = method ?? throw new ArgumentNullException(nameof(method));
		}
	}
}
