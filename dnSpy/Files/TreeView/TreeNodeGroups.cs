/*
    Copyright (C) 2014-2015 de4dot@gmail.com

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
using dnSpy.Contracts.Files.TreeView;
using dnSpy.Contracts.TreeView;
using dnSpy.Decompiler;

namespace dnSpy.Files.TreeView {
	static class TreeNodeGroups {
		public static readonly ITreeNodeGroup AssemblyRefTreeNodeGroupReferences = new AssemblyRefTreeNodeGroup(FileTVConstants.ORDER_REFERENCES_ASSEMBLYREF);
		public static readonly ITreeNodeGroup AssemblyRefTreeNodeGroupAssemblyRef = new AssemblyRefTreeNodeGroup(FileTVConstants.ORDER_ASSEMBLYREF_ASSEMBLYREF);

		public static readonly ITreeNodeGroup ModuleRefTreeNodeGroupReferences = new ModuleRefTreeNodeGroup(FileTVConstants.ORDER_REFERENCES_MODULEREF);

		public static readonly ITreeNodeGroup ReferencesTreeNodeGroupModule = new ReferencesTreeNodeGroup(FileTVConstants.ORDER_MODULE_REFERENCES);

		public static readonly ITreeNodeGroup ResourcesTreeNodeGroupModule = new ResourcesTreeNodeGroup(FileTVConstants.ORDER_MODULE_RESOURCES);

		public static readonly ITreeNodeGroup NamespaceTreeNodeGroupModule = new NamespaceTreeNodeGroup(FileTVConstants.ORDER_MODULE_NAMESPACE);

		public static readonly ITreeNodeGroup TypeTreeNodeGroupNamespace = new TypeTreeNodeGroup(FileTVConstants.ORDER_NAMESPACE_TYPE);
		public static readonly ITreeNodeGroup TypeTreeNodeGroupType = new TypeTreeNodeGroup(FileTVConstants.ORDER_TYPE_TYPE);

		public static readonly ITreeNodeGroup BaseTypeFolderTreeNodeGroupType = new BaseTypeFolderTreeNodeGroup(FileTVConstants.ORDER_TYPE_BASE);

		public static readonly ITreeNodeGroup BaseTypeTreeNodeGroupBaseType = new BaseTypeTreeNodeGroup(FileTVConstants.ORDER_BASETYPEFOLDER_BASETYPE);
		public static readonly ITreeNodeGroup InterfaceBaseTypeTreeNodeGroupBaseType = new BaseTypeTreeNodeGroup(FileTVConstants.ORDER_BASETYPEFOLDER_INTERFACE);

		public static readonly ITreeNodeGroup DerivedTypesFolderTreeNodeGroupType = new DerivedTypesFolderTreeNodeGroup(FileTVConstants.ORDER_TYPE_DERIVED);

		public static readonly ITreeNodeGroup MessageTreeNodeGroupDerivedTypes = new MessageTreeNodeGroup(FileTVConstants.ORDER_DERIVEDTYPES_TEXT);
		public static readonly ITreeNodeGroup DerivedTypeTreeNodeGroupDerivedTypes = new DerivedTypeTreeNodeGroup(FileTVConstants.ORDER_DERIVEDTYPES_TYPE);

		public static readonly ITreeNodeGroup MethodTreeNodeGroupType = new MethodTreeNodeGroup(FileTVConstants.ORDER_TYPE_METHOD);
		public static readonly ITreeNodeGroup MethodTreeNodeGroupProperty = new MethodTreeNodeGroup(FileTVConstants.ORDER_PROPERTY_METHOD);
		public static readonly ITreeNodeGroup MethodTreeNodeGroupEvent = new MethodTreeNodeGroup(FileTVConstants.ORDER_EVENT_METHOD);

		public static readonly ITreeNodeGroup FieldTreeNodeGroupType = new FieldTreeNodeGroup(FileTVConstants.ORDER_TYPE_FIELD);

		public static readonly ITreeNodeGroup EventTreeNodeGroupType = new EventTreeNodeGroup(FileTVConstants.ORDER_TYPE_EVENT);

		public static readonly ITreeNodeGroup PropertyTreeNodeGroupType = new PropertyTreeNodeGroup(FileTVConstants.ORDER_TYPE_PROPERTY);
	}

	sealed class AssemblyRefTreeNodeGroup : ITreeNodeGroup {
		readonly double order;

		public AssemblyRefTreeNodeGroup(double order) {
			this.order = order;
		}

		public double Order {
			get { return order; }
		}

		public int Compare(ITreeNodeData x, ITreeNodeData y) {
			if (x == y) return 0;
			var a = x as IAssemblyReferenceNode;
			var b = y as IAssemblyReferenceNode;
			if (a == null) return -1;
			if (b == null) return 1;
			return StringComparer.OrdinalIgnoreCase.Compare(a.AssemblyRef.FullName, b.AssemblyRef.FullName);
		}
	}

	sealed class ModuleRefTreeNodeGroup : ITreeNodeGroup {
		readonly double order;

		public ModuleRefTreeNodeGroup(double order) {
			this.order = order;
		}

		public double Order {
			get { return order; }
		}

		public int Compare(ITreeNodeData x, ITreeNodeData y) {
			if (x == y) return 0;
			var a = x as IModuleReferenceNode;
			var b = y as IModuleReferenceNode;
			if (a == null) return -1;
			if (b == null) return 1;
			return StringComparer.OrdinalIgnoreCase.Compare(a.ModuleRef.FullName, b.ModuleRef.FullName);
		}
	}

	sealed class ReferencesTreeNodeGroup : ITreeNodeGroup {
		readonly double order;

		public ReferencesTreeNodeGroup(double order) {
			this.order = order;
		}

		public double Order {
			get { return order; }
		}

		public int Compare(ITreeNodeData x, ITreeNodeData y) {
			if (x == y) return 0;
			var a = x as IReferencesNode;
			var b = y as IReferencesNode;
			if (a == null) return -1;
			if (b == null) return 1;
			return -1;
		}
	}

	sealed class ResourcesTreeNodeGroup : ITreeNodeGroup {
		readonly double order;

		public ResourcesTreeNodeGroup(double order) {
			this.order = order;
		}

		public double Order {
			get { return order; }
		}

		public int Compare(ITreeNodeData x, ITreeNodeData y) {
			if (x == y) return 0;
			var a = x as IResourcesNode;
			var b = y as IResourcesNode;
			if (a == null) return -1;
			if (b == null) return 1;
			return -1;
		}
	}

	sealed class BaseTypeFolderTreeNodeGroup : ITreeNodeGroup {
		readonly double order;

		public BaseTypeFolderTreeNodeGroup(double order) {
			this.order = order;
		}

		public double Order {
			get { return order; }
		}

		public int Compare(ITreeNodeData x, ITreeNodeData y) {
			if (x == y) return 0;
			var a = x as IBaseTypeFolderNode;
			var b = y as IBaseTypeFolderNode;
			if (a == null) return -1;
			if (b == null) return 1;
			return -1;
		}
	}

	sealed class DerivedTypesFolderTreeNodeGroup : ITreeNodeGroup {
		readonly double order;

		public DerivedTypesFolderTreeNodeGroup(double order) {
			this.order = order;
		}

		public double Order {
			get { return order; }
		}

		public int Compare(ITreeNodeData x, ITreeNodeData y) {
			if (x == y) return 0;
			var a = x as IDerivedTypesFolderNode;
			var b = y as IDerivedTypesFolderNode;
			if (a == null) return -1;
			if (b == null) return 1;
			return -1;
		}
	}

	sealed class MessageTreeNodeGroup : ITreeNodeGroup {
		readonly double order;

		public MessageTreeNodeGroup(double order) {
			this.order = order;
		}

		public double Order {
			get { return order; }
		}

		public int Compare(ITreeNodeData x, ITreeNodeData y) {
			if (x == y) return 0;
			var a = x as IMessageNode;
			var b = y as IMessageNode;
			if (a == null) return -1;
			if (b == null) return 1;
			return StringComparer.OrdinalIgnoreCase.Compare(a.Text, b.Text);
		}
	}

	sealed class DerivedTypeTreeNodeGroup : ITreeNodeGroup {
		readonly double order;

		public DerivedTypeTreeNodeGroup(double order) {
			this.order = order;
		}

		public double Order {
			get { return order; }
		}

		public int Compare(ITreeNodeData x, ITreeNodeData y) {
			if (x == y) return 0;
			var a = x as IDerivedTypeNode;
			var b = y as IDerivedTypeNode;
			if (a == null) return -1;
			if (b == null) return 1;
			string an = a.TypeDef.Name;
			string bn = b.TypeDef.Name;
			return StringComparer.OrdinalIgnoreCase.Compare(an, bn);
		}
	}

	sealed class NamespaceTreeNodeGroup : ITreeNodeGroup {
		readonly double order;

		public NamespaceTreeNodeGroup(double order) {
			this.order = order;
		}

		public double Order {
			get { return order; }
		}

		public int Compare(ITreeNodeData x, ITreeNodeData y) {
			if (x == y) return 0;
			var a = x as INamespaceNode;
			var b = y as INamespaceNode;
			if (a == null) return -1;
			if (b == null) return 1;
			return StringComparer.OrdinalIgnoreCase.Compare(a.Name, b.Name);
		}
	}

	sealed class BaseTypeTreeNodeGroup : ITreeNodeGroup {
		readonly double order;

		public BaseTypeTreeNodeGroup(double order) {
			this.order = order;
		}

		public double Order {
			get { return order; }
		}

		public int Compare(ITreeNodeData x, ITreeNodeData y) {
			if (x == y) return 0;
			var a = x as IBaseTypeNode;
			var b = y as IBaseTypeNode;
			if (a == null) return -1;
			if (b == null) return 1;
			string an = a.TypeDefOrRef.Name;
			string bn = b.TypeDefOrRef.Name;
			return StringComparer.OrdinalIgnoreCase.Compare(an, bn);
		}
	}

	sealed class TypeTreeNodeGroup : ITreeNodeGroup {
		readonly double order;

		public TypeTreeNodeGroup(double order) {
			this.order = order;
		}

		public double Order {
			get { return order; }
		}

		public int Compare(ITreeNodeData x, ITreeNodeData y) {
			if (x == y) return 0;
			var a = x as ITypeNode;
			var b = y as ITypeNode;
			if (a == null) return -1;
			if (b == null) return 1;
			var an = a.TypeDef.FullName;
			var bn = b.TypeDef.FullName;
			return StringComparer.OrdinalIgnoreCase.Compare(an, bn);
		}
	}

	sealed class MethodTreeNodeGroup : ITreeNodeGroup {
		readonly double order;

		public MethodTreeNodeGroup(double order) {
			this.order = order;
		}

		public double Order {
			get { return order; }
		}

		public int Compare(ITreeNodeData x, ITreeNodeData y) {
			if (x == y) return 0;
			var a = x as IMethodNode;
			var b = y as IMethodNode;
			if (a == null) return -1;
			if (b == null) return 1;
			return MethodDefComparer.Instance.Compare(a.MethodDef, b.MethodDef);
		}
	}

	sealed class FieldTreeNodeGroup : ITreeNodeGroup {
		readonly double order;

		public FieldTreeNodeGroup(double order) {
			this.order = order;
		}

		public double Order {
			get { return order; }
		}

		public int Compare(ITreeNodeData x, ITreeNodeData y) {
			if (x == y) return 0;
			var a = x as IFieldNode;
			var b = y as IFieldNode;
			if (a == null) return -1;
			if (b == null) return 1;
			return FieldDefComparer.Instance.Compare(a.FieldDef, b.FieldDef);
		}
	}

	sealed class EventTreeNodeGroup : ITreeNodeGroup {
		readonly double order;

		public EventTreeNodeGroup(double order) {
			this.order = order;
		}

		public double Order {
			get { return order; }
		}

		public int Compare(ITreeNodeData x, ITreeNodeData y) {
			if (x == y) return 0;
			var a = x as IEventNode;
			var b = y as IEventNode;
			if (a == null) return -1;
			if (b == null) return 1;
			return EventDefComparer.Instance.Compare(a.EventDef, b.EventDef);
		}
	}

	sealed class PropertyTreeNodeGroup : ITreeNodeGroup {
		readonly double order;

		public PropertyTreeNodeGroup(double order) {
			this.order = order;
		}

		public double Order {
			get { return order; }
		}

		public int Compare(ITreeNodeData x, ITreeNodeData y) {
			if (x == y) return 0;
			var a = x as IPropertyNode;
			var b = y as IPropertyNode;
			if (a == null) return -1;
			if (b == null) return 1;
			return PropertyDefComparer.Instance.Compare(a.PropertyDef, b.PropertyDef);
		}
	}
}
