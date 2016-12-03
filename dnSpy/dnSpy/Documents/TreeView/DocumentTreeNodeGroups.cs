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
using System.Collections.Generic;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Documents.TreeView;
using dnSpy.Contracts.Documents.TreeView.Resources;
using dnSpy.Contracts.TreeView;

namespace dnSpy.Documents.TreeView {
	interface ITreeNodeGroup2 : ITreeNodeGroup {
		new double Order { get; set; }
	}

	sealed class DocumentTreeNodeGroups : IDocumentTreeNodeGroups {
		public ITreeNodeGroup GetGroup(DocumentTreeNodeGroupType type) {
			switch (type) {
			case DocumentTreeNodeGroupType.AssemblyRefTreeNodeGroupReferences: return AssemblyRefTreeNodeGroupReferences;
			case DocumentTreeNodeGroupType.AssemblyRefTreeNodeGroupAssemblyRef: return AssemblyRefTreeNodeGroupAssemblyRef;
			case DocumentTreeNodeGroupType.ModuleRefTreeNodeGroupReferences: return ModuleRefTreeNodeGroupReferences;
			case DocumentTreeNodeGroupType.ReferencesFolderTreeNodeGroupModule: return ReferencesFolderTreeNodeGroupModule;
			case DocumentTreeNodeGroupType.ResourcesFolderTreeNodeGroupModule: return ResourcesFolderTreeNodeGroupModule;
			case DocumentTreeNodeGroupType.NamespaceTreeNodeGroupModule: return NamespaceTreeNodeGroupModule;
			case DocumentTreeNodeGroupType.TypeTreeNodeGroupNamespace: return TypeTreeNodeGroupNamespace;
			case DocumentTreeNodeGroupType.TypeTreeNodeGroupType: return TypeTreeNodeGroupType;
			case DocumentTreeNodeGroupType.BaseTypeFolderTreeNodeGroupType: return BaseTypeFolderTreeNodeGroupType;
			case DocumentTreeNodeGroupType.BaseTypeTreeNodeGroupBaseType: return BaseTypeTreeNodeGroupBaseType;
			case DocumentTreeNodeGroupType.InterfaceBaseTypeTreeNodeGroupBaseType: return InterfaceBaseTypeTreeNodeGroupBaseType;
			case DocumentTreeNodeGroupType.DerivedTypesFolderTreeNodeGroupType: return DerivedTypesFolderTreeNodeGroupType;
			case DocumentTreeNodeGroupType.MessageTreeNodeGroupDerivedTypes: return MessageTreeNodeGroupDerivedTypes;
			case DocumentTreeNodeGroupType.DerivedTypeTreeNodeGroupDerivedTypes: return DerivedTypeTreeNodeGroupDerivedTypes;
			case DocumentTreeNodeGroupType.MethodTreeNodeGroupType: return MethodTreeNodeGroupType;
			case DocumentTreeNodeGroupType.MethodTreeNodeGroupProperty: return MethodTreeNodeGroupProperty;
			case DocumentTreeNodeGroupType.MethodTreeNodeGroupEvent: return MethodTreeNodeGroupEvent;
			case DocumentTreeNodeGroupType.FieldTreeNodeGroupType: return FieldTreeNodeGroupType;
			case DocumentTreeNodeGroupType.EventTreeNodeGroupType: return EventTreeNodeGroupType;
			case DocumentTreeNodeGroupType.PropertyTreeNodeGroupType: return PropertyTreeNodeGroupType;
			case DocumentTreeNodeGroupType.ResourceTreeNodeGroup: return ResourceTreeNodeGroup;
			case DocumentTreeNodeGroupType.ResourceElementTreeNodeGroup: return ResourceElementTreeNodeGroup;
			default: throw new ArgumentException();
			}
		}

		readonly ITreeNodeGroup2 AssemblyRefTreeNodeGroupReferences = new AssemblyRefTreeNodeGroup(DocumentTreeViewConstants.ORDER_REFERENCES_ASSEMBLYREF);
		readonly ITreeNodeGroup2 AssemblyRefTreeNodeGroupAssemblyRef = new AssemblyRefTreeNodeGroup(DocumentTreeViewConstants.ORDER_ASSEMBLYREF_ASSEMBLYREF);

		readonly ITreeNodeGroup2 ModuleRefTreeNodeGroupReferences = new ModuleRefTreeNodeGroup(DocumentTreeViewConstants.ORDER_REFERENCES_MODULEREF);

		readonly ITreeNodeGroup2 ReferencesFolderTreeNodeGroupModule = new ReferencesFolderTreeNodeGroup(DocumentTreeViewConstants.ORDER_MODULE_REFERENCES_FOLDER);

		readonly ITreeNodeGroup2 ResourcesFolderTreeNodeGroupModule = new ResourcesFolderTreeNodeGroup(DocumentTreeViewConstants.ORDER_MODULE_RESOURCES_FOLDER);

		readonly ITreeNodeGroup2 NamespaceTreeNodeGroupModule = new NamespaceTreeNodeGroup(DocumentTreeViewConstants.ORDER_MODULE_NAMESPACE);

		readonly ITreeNodeGroup2 TypeTreeNodeGroupNamespace = new TypeTreeNodeGroup(DocumentTreeViewConstants.ORDER_NAMESPACE_TYPE);
		readonly ITreeNodeGroup2 TypeTreeNodeGroupType = new TypeTreeNodeGroup(DocumentTreeViewConstants.ORDER_TYPE_TYPE);

		readonly ITreeNodeGroup2 BaseTypeFolderTreeNodeGroupType = new BaseTypeFolderTreeNodeGroup(DocumentTreeViewConstants.ORDER_TYPE_BASE);

		readonly ITreeNodeGroup2 BaseTypeTreeNodeGroupBaseType = new BaseTypeTreeNodeGroup(DocumentTreeViewConstants.ORDER_BASETYPEFOLDER_BASETYPE);
		readonly ITreeNodeGroup2 InterfaceBaseTypeTreeNodeGroupBaseType = new BaseTypeTreeNodeGroup(DocumentTreeViewConstants.ORDER_BASETYPEFOLDER_INTERFACE);

		readonly ITreeNodeGroup2 DerivedTypesFolderTreeNodeGroupType = new DerivedTypesFolderTreeNodeGroup(DocumentTreeViewConstants.ORDER_TYPE_DERIVED);

		readonly ITreeNodeGroup2 MessageTreeNodeGroupDerivedTypes = new MessageTreeNodeGroup(DocumentTreeViewConstants.ORDER_DERIVEDTYPES_TEXT);
		readonly ITreeNodeGroup2 DerivedTypeTreeNodeGroupDerivedTypes = new DerivedTypeTreeNodeGroup(DocumentTreeViewConstants.ORDER_DERIVEDTYPES_TYPE);

		readonly ITreeNodeGroup2 MethodTreeNodeGroupType = new MethodTreeNodeGroup(DocumentTreeViewConstants.ORDER_TYPE_METHOD);
		readonly ITreeNodeGroup2 MethodTreeNodeGroupProperty = new MethodTreeNodeGroup(DocumentTreeViewConstants.ORDER_PROPERTY_METHOD);
		readonly ITreeNodeGroup2 MethodTreeNodeGroupEvent = new MethodTreeNodeGroup(DocumentTreeViewConstants.ORDER_EVENT_METHOD);

		readonly ITreeNodeGroup2 FieldTreeNodeGroupType = new FieldTreeNodeGroup(DocumentTreeViewConstants.ORDER_TYPE_FIELD);

		readonly ITreeNodeGroup2 EventTreeNodeGroupType = new EventTreeNodeGroup(DocumentTreeViewConstants.ORDER_TYPE_EVENT);

		readonly ITreeNodeGroup2 PropertyTreeNodeGroupType = new PropertyTreeNodeGroup(DocumentTreeViewConstants.ORDER_TYPE_PROPERTY);

		readonly ITreeNodeGroup2 ResourceTreeNodeGroup = new ResourceTreeNodeGroup(DocumentTreeViewConstants.ORDER_RESOURCE);
		readonly ITreeNodeGroup2 ResourceElementTreeNodeGroup = new ResourceElementTreeNodeGroup(DocumentTreeViewConstants.ORDER_RESOURCE_ELEM);

		public void SetMemberOrder(MemberKind[] newOrders) {
			if (newOrders == null)
				throw new ArgumentNullException(nameof(newOrders));

			var infos = new Tuple<double, MemberKind, ITreeNodeGroup2>[] {
				Tuple.Create(DocumentTreeViewConstants.ORDER_TYPE_METHOD, MemberKind.Methods, MethodTreeNodeGroupType),
				Tuple.Create(DocumentTreeViewConstants.ORDER_TYPE_PROPERTY, MemberKind.Properties, PropertyTreeNodeGroupType),
				Tuple.Create(DocumentTreeViewConstants.ORDER_TYPE_EVENT, MemberKind.Events, EventTreeNodeGroupType),
				Tuple.Create(DocumentTreeViewConstants.ORDER_TYPE_FIELD, MemberKind.Fields, FieldTreeNodeGroupType),
				Tuple.Create(DocumentTreeViewConstants.ORDER_TYPE_TYPE, MemberKind.NestedTypes, TypeTreeNodeGroupType),
			};
			if (infos.Length != newOrders.Length)
				throw new ArgumentException();

			var dict = new Dictionary<MemberKind, ITreeNodeGroup2>(infos.Length);
			foreach (var info in infos)
				dict[info.Item2] = info.Item3;
			for (int i = 0; i < newOrders.Length; i++)
				dict[newOrders[i]].Order = infos[i].Item1;
		}
	}

	sealed class AssemblyRefTreeNodeGroup : ITreeNodeGroup2 {
		public AssemblyRefTreeNodeGroup(double order) {
			Order = order;
		}

		public double Order { get; set; }

		public int Compare(TreeNodeData x, TreeNodeData y) {
			if (x == y) return 0;
			var a = x as AssemblyReferenceNode;
			var b = y as AssemblyReferenceNode;
			if (a == null) return -1;
			if (b == null) return 1;
			return StringComparer.OrdinalIgnoreCase.Compare(a.AssemblyRef.FullName, b.AssemblyRef.FullName);
		}
	}

	sealed class ModuleRefTreeNodeGroup : ITreeNodeGroup2 {
		public ModuleRefTreeNodeGroup(double order) {
			Order = order;
		}

		public double Order { get; set; }

		public int Compare(TreeNodeData x, TreeNodeData y) {
			if (x == y) return 0;
			var a = x as ModuleReferenceNode;
			var b = y as ModuleReferenceNode;
			if (a == null) return -1;
			if (b == null) return 1;
			return StringComparer.OrdinalIgnoreCase.Compare(a.ModuleRef.FullName, b.ModuleRef.FullName);
		}
	}

	sealed class ReferencesFolderTreeNodeGroup : ITreeNodeGroup2 {
		public ReferencesFolderTreeNodeGroup(double order) {
			Order = order;
		}

		public double Order { get; set; }

		public int Compare(TreeNodeData x, TreeNodeData y) {
			if (x == y) return 0;
			var a = x as ReferencesFolderNode;
			var b = y as ReferencesFolderNode;
			if (a == null) return -1;
			if (b == null) return 1;
			return -1;
		}
	}

	sealed class ResourcesFolderTreeNodeGroup : ITreeNodeGroup2 {
		public ResourcesFolderTreeNodeGroup(double order) {
			Order = order;
		}

		public double Order { get; set; }

		public int Compare(TreeNodeData x, TreeNodeData y) {
			if (x == y) return 0;
			var a = x as ResourcesFolderNode;
			var b = y as ResourcesFolderNode;
			if (a == null) return -1;
			if (b == null) return 1;
			return -1;
		}
	}

	sealed class BaseTypeFolderTreeNodeGroup : ITreeNodeGroup2 {
		public BaseTypeFolderTreeNodeGroup(double order) {
			Order = order;
		}

		public double Order { get; set; }

		public int Compare(TreeNodeData x, TreeNodeData y) {
			if (x == y) return 0;
			var a = x as BaseTypeFolderNode;
			var b = y as BaseTypeFolderNode;
			if (a == null) return -1;
			if (b == null) return 1;
			return -1;
		}
	}

	sealed class DerivedTypesFolderTreeNodeGroup : ITreeNodeGroup2 {
		public DerivedTypesFolderTreeNodeGroup(double order) {
			Order = order;
		}

		public double Order { get; set; }

		public int Compare(TreeNodeData x, TreeNodeData y) {
			if (x == y) return 0;
			var a = x as DerivedTypesFolderNode;
			var b = y as DerivedTypesFolderNode;
			if (a == null) return -1;
			if (b == null) return 1;
			return -1;
		}
	}

	sealed class MessageTreeNodeGroup : ITreeNodeGroup2 {
		public MessageTreeNodeGroup(double order) {
			Order = order;
		}

		public double Order { get; set; }

		public int Compare(TreeNodeData x, TreeNodeData y) {
			if (x == y) return 0;
			var a = x as MessageNode;
			var b = y as MessageNode;
			if (a == null) return -1;
			if (b == null) return 1;
			return 0;
		}
	}

	sealed class DerivedTypeTreeNodeGroup : ITreeNodeGroup2 {
		public DerivedTypeTreeNodeGroup(double order) {
			Order = order;
		}

		public double Order { get; set; }

		public int Compare(TreeNodeData x, TreeNodeData y) {
			if (x == y) return 0;
			var a = x as DerivedTypeNode;
			var b = y as DerivedTypeNode;
			if (a == null) return -1;
			if (b == null) return 1;
			string an = a.TypeDef.Name;
			string bn = b.TypeDef.Name;
			return StringComparer.OrdinalIgnoreCase.Compare(an, bn);
		}
	}

	sealed class NamespaceTreeNodeGroup : ITreeNodeGroup2 {
		public NamespaceTreeNodeGroup(double order) {
			Order = order;
		}

		public double Order { get; set; }

		public int Compare(TreeNodeData x, TreeNodeData y) {
			if (x == y) return 0;
			var a = x as NamespaceNode;
			var b = y as NamespaceNode;
			if (a == null) return -1;
			if (b == null) return 1;
			return StringComparer.OrdinalIgnoreCase.Compare(a.Name, b.Name);
		}
	}

	sealed class BaseTypeTreeNodeGroup : ITreeNodeGroup2 {
		public BaseTypeTreeNodeGroup(double order) {
			Order = order;
		}

		public double Order { get; set; }

		public int Compare(TreeNodeData x, TreeNodeData y) {
			if (x == y) return 0;
			var a = x as BaseTypeNode;
			var b = y as BaseTypeNode;
			if (a == null) return -1;
			if (b == null) return 1;
			string an = a.TypeDefOrRef.Name;
			string bn = b.TypeDefOrRef.Name;
			return StringComparer.OrdinalIgnoreCase.Compare(an, bn);
		}
	}

	sealed class TypeTreeNodeGroup : ITreeNodeGroup2 {
		public TypeTreeNodeGroup(double order) {
			Order = order;
		}

		public double Order { get; set; }

		public int Compare(TreeNodeData x, TreeNodeData y) {
			if (x == y) return 0;
			var a = x as TypeNode;
			var b = y as TypeNode;
			if (a == null) return -1;
			if (b == null) return 1;
			var an = a.TypeDef.FullName;
			var bn = b.TypeDef.FullName;
			return StringComparer.OrdinalIgnoreCase.Compare(an, bn);
		}
	}

	sealed class MethodTreeNodeGroup : ITreeNodeGroup2 {
		public MethodTreeNodeGroup(double order) {
			Order = order;
		}

		public double Order { get; set; }

		public int Compare(TreeNodeData x, TreeNodeData y) {
			if (x == y) return 0;
			var a = x as MethodNode;
			var b = y as MethodNode;
			if (a == null) return -1;
			if (b == null) return 1;
			return MethodDefComparer.Instance.Compare(a.MethodDef, b.MethodDef);
		}
	}

	sealed class FieldTreeNodeGroup : ITreeNodeGroup2 {
		public FieldTreeNodeGroup(double order) {
			Order = order;
		}

		public double Order { get; set; }

		public int Compare(TreeNodeData x, TreeNodeData y) {
			if (x == y) return 0;
			var a = x as FieldNode;
			var b = y as FieldNode;
			if (a == null) return -1;
			if (b == null) return 1;
			return FieldDefComparer.Instance.Compare(a.FieldDef, b.FieldDef);
		}
	}

	sealed class EventTreeNodeGroup : ITreeNodeGroup2 {
		public EventTreeNodeGroup(double order) {
			Order = order;
		}

		public double Order { get; set; }

		public int Compare(TreeNodeData x, TreeNodeData y) {
			if (x == y) return 0;
			var a = x as EventNode;
			var b = y as EventNode;
			if (a == null) return -1;
			if (b == null) return 1;
			return EventDefComparer.Instance.Compare(a.EventDef, b.EventDef);
		}
	}

	sealed class PropertyTreeNodeGroup : ITreeNodeGroup2 {
		public PropertyTreeNodeGroup(double order) {
			Order = order;
		}

		public double Order { get; set; }

		public int Compare(TreeNodeData x, TreeNodeData y) {
			if (x == y) return 0;
			var a = x as PropertyNode;
			var b = y as PropertyNode;
			if (a == null) return -1;
			if (b == null) return 1;
			return PropertyDefComparer.Instance.Compare(a.PropertyDef, b.PropertyDef);
		}
	}

	sealed class ResourceTreeNodeGroup : ITreeNodeGroup2 {
		public ResourceTreeNodeGroup(double order) {
			Order = order;
		}

		public double Order { get; set; }

		public int Compare(TreeNodeData x, TreeNodeData y) {
			if (x == y) return 0;
			var a = x as ResourceNode;
			var b = y as ResourceNode;
			if (a == null) return -1;
			if (b == null) return 1;
			int c = StringComparer.OrdinalIgnoreCase.Compare(a.Resource.Name, b.Resource.Name);
			if (c != 0) return c;
			return a.Resource.MDToken.Raw.CompareTo(b.Resource.MDToken.Raw);
		}
	}

	sealed class ResourceElementTreeNodeGroup : ITreeNodeGroup2 {
		public ResourceElementTreeNodeGroup(double order) {
			Order = order;
		}

		public double Order { get; set; }

		public int Compare(TreeNodeData x, TreeNodeData y) {
			if (x == y) return 0;
			var a = x as ResourceElementNode;
			var b = y as ResourceElementNode;
			if (a == null) return -1;
			if (b == null) return 1;
			int c = StringComparer.OrdinalIgnoreCase.Compare(a.ResourceElement.Name, b.ResourceElement.Name);
			if (c != 0) return c;
			int cx = (int)a.ResourceElement.ResourceData.Code.FixUserType();
			int cy = (int)b.ResourceElement.ResourceData.Code.FixUserType();
			return cx.CompareTo(cy);
		}
	}
}
