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
using System.Collections.Generic;
using dnSpy.Contracts.Files.TreeView;
using dnSpy.Contracts.Files.TreeView.Resources;
using dnSpy.Contracts.TreeView;
using dnSpy.Decompiler;
using dnSpy.Shared.UI.Files.TreeView.Resources;

namespace dnSpy.Files.TreeView {
	enum MemberType {
		NestedTypes,
		Fields,
		Events,
		Properties,
		Methods,
	}

	interface ITreeNodeGroup2 : ITreeNodeGroup {
		new double Order { get; set; }
	}

	sealed class FileTreeNodeGroups : IFileTreeNodeGroups {
		public ITreeNodeGroup GetGroup(FileTreeNodeGroupType type) {
			switch (type) {
			case FileTreeNodeGroupType.AssemblyRefTreeNodeGroupReferences: return AssemblyRefTreeNodeGroupReferences;
			case FileTreeNodeGroupType.AssemblyRefTreeNodeGroupAssemblyRef: return AssemblyRefTreeNodeGroupAssemblyRef;
			case FileTreeNodeGroupType.ModuleRefTreeNodeGroupReferences: return ModuleRefTreeNodeGroupReferences;
			case FileTreeNodeGroupType.ReferencesFolderTreeNodeGroupModule: return ReferencesFolderTreeNodeGroupModule;
			case FileTreeNodeGroupType.ResourcesFolderTreeNodeGroupModule: return ResourcesFolderTreeNodeGroupModule;
			case FileTreeNodeGroupType.NamespaceTreeNodeGroupModule: return NamespaceTreeNodeGroupModule;
			case FileTreeNodeGroupType.TypeTreeNodeGroupNamespace: return TypeTreeNodeGroupNamespace;
			case FileTreeNodeGroupType.TypeTreeNodeGroupType: return TypeTreeNodeGroupType;
			case FileTreeNodeGroupType.BaseTypeFolderTreeNodeGroupType: return BaseTypeFolderTreeNodeGroupType;
			case FileTreeNodeGroupType.BaseTypeTreeNodeGroupBaseType: return BaseTypeTreeNodeGroupBaseType;
			case FileTreeNodeGroupType.InterfaceBaseTypeTreeNodeGroupBaseType: return InterfaceBaseTypeTreeNodeGroupBaseType;
			case FileTreeNodeGroupType.DerivedTypesFolderTreeNodeGroupType: return DerivedTypesFolderTreeNodeGroupType;
			case FileTreeNodeGroupType.MessageTreeNodeGroupDerivedTypes: return MessageTreeNodeGroupDerivedTypes;
			case FileTreeNodeGroupType.DerivedTypeTreeNodeGroupDerivedTypes: return DerivedTypeTreeNodeGroupDerivedTypes;
			case FileTreeNodeGroupType.MethodTreeNodeGroupType: return MethodTreeNodeGroupType;
			case FileTreeNodeGroupType.MethodTreeNodeGroupProperty: return MethodTreeNodeGroupProperty;
			case FileTreeNodeGroupType.MethodTreeNodeGroupEvent: return MethodTreeNodeGroupEvent;
			case FileTreeNodeGroupType.FieldTreeNodeGroupType: return FieldTreeNodeGroupType;
			case FileTreeNodeGroupType.EventTreeNodeGroupType: return EventTreeNodeGroupType;
			case FileTreeNodeGroupType.PropertyTreeNodeGroupType: return PropertyTreeNodeGroupType;
			case FileTreeNodeGroupType.ResourceTreeNodeGroup: return ResourceTreeNodeGroup;
			case FileTreeNodeGroupType.ResourceElementTreeNodeGroup: return ResourceElementTreeNodeGroup;
			default: throw new ArgumentException();
			}
		}

		readonly ITreeNodeGroup2 AssemblyRefTreeNodeGroupReferences = new AssemblyRefTreeNodeGroup(FileTVConstants.ORDER_REFERENCES_ASSEMBLYREF);
		readonly ITreeNodeGroup2 AssemblyRefTreeNodeGroupAssemblyRef = new AssemblyRefTreeNodeGroup(FileTVConstants.ORDER_ASSEMBLYREF_ASSEMBLYREF);

		readonly ITreeNodeGroup2 ModuleRefTreeNodeGroupReferences = new ModuleRefTreeNodeGroup(FileTVConstants.ORDER_REFERENCES_MODULEREF);

		readonly ITreeNodeGroup2 ReferencesFolderTreeNodeGroupModule = new ReferencesFolderTreeNodeGroup(FileTVConstants.ORDER_MODULE_REFERENCES_FOLDER);

		readonly ITreeNodeGroup2 ResourcesFolderTreeNodeGroupModule = new ResourcesFolderTreeNodeGroup(FileTVConstants.ORDER_MODULE_RESOURCES_FOLDER);

		readonly ITreeNodeGroup2 NamespaceTreeNodeGroupModule = new NamespaceTreeNodeGroup(FileTVConstants.ORDER_MODULE_NAMESPACE);

		readonly ITreeNodeGroup2 TypeTreeNodeGroupNamespace = new TypeTreeNodeGroup(FileTVConstants.ORDER_NAMESPACE_TYPE);
		readonly ITreeNodeGroup2 TypeTreeNodeGroupType = new TypeTreeNodeGroup(FileTVConstants.ORDER_TYPE_TYPE);

		readonly ITreeNodeGroup2 BaseTypeFolderTreeNodeGroupType = new BaseTypeFolderTreeNodeGroup(FileTVConstants.ORDER_TYPE_BASE);

		readonly ITreeNodeGroup2 BaseTypeTreeNodeGroupBaseType = new BaseTypeTreeNodeGroup(FileTVConstants.ORDER_BASETYPEFOLDER_BASETYPE);
		readonly ITreeNodeGroup2 InterfaceBaseTypeTreeNodeGroupBaseType = new BaseTypeTreeNodeGroup(FileTVConstants.ORDER_BASETYPEFOLDER_INTERFACE);

		readonly ITreeNodeGroup2 DerivedTypesFolderTreeNodeGroupType = new DerivedTypesFolderTreeNodeGroup(FileTVConstants.ORDER_TYPE_DERIVED);

		readonly ITreeNodeGroup2 MessageTreeNodeGroupDerivedTypes = new MessageTreeNodeGroup(FileTVConstants.ORDER_DERIVEDTYPES_TEXT);
		readonly ITreeNodeGroup2 DerivedTypeTreeNodeGroupDerivedTypes = new DerivedTypeTreeNodeGroup(FileTVConstants.ORDER_DERIVEDTYPES_TYPE);

		readonly ITreeNodeGroup2 MethodTreeNodeGroupType = new MethodTreeNodeGroup(FileTVConstants.ORDER_TYPE_METHOD);
		readonly ITreeNodeGroup2 MethodTreeNodeGroupProperty = new MethodTreeNodeGroup(FileTVConstants.ORDER_PROPERTY_METHOD);
		readonly ITreeNodeGroup2 MethodTreeNodeGroupEvent = new MethodTreeNodeGroup(FileTVConstants.ORDER_EVENT_METHOD);

		readonly ITreeNodeGroup2 FieldTreeNodeGroupType = new FieldTreeNodeGroup(FileTVConstants.ORDER_TYPE_FIELD);

		readonly ITreeNodeGroup2 EventTreeNodeGroupType = new EventTreeNodeGroup(FileTVConstants.ORDER_TYPE_EVENT);

		readonly ITreeNodeGroup2 PropertyTreeNodeGroupType = new PropertyTreeNodeGroup(FileTVConstants.ORDER_TYPE_PROPERTY);

		readonly ITreeNodeGroup2 ResourceTreeNodeGroup = new ResourceTreeNodeGroup(FileTVConstants.ORDER_RESOURCE);
		readonly ITreeNodeGroup2 ResourceElementTreeNodeGroup = new ResourceElementTreeNodeGroup(FileTVConstants.ORDER_RESOURCE_ELEM);

		public void SetMemberOrder(MemberType[] newOrders) {
			if (newOrders == null)
				throw new ArgumentNullException();

			var infos = new Tuple<double, MemberType, ITreeNodeGroup2>[] {
				Tuple.Create(FileTVConstants.ORDER_TYPE_METHOD, MemberType.Methods, MethodTreeNodeGroupType),
				Tuple.Create(FileTVConstants.ORDER_TYPE_PROPERTY, MemberType.Properties, PropertyTreeNodeGroupType),
				Tuple.Create(FileTVConstants.ORDER_TYPE_EVENT, MemberType.Events, EventTreeNodeGroupType),
				Tuple.Create(FileTVConstants.ORDER_TYPE_FIELD, MemberType.Fields, FieldTreeNodeGroupType),
				Tuple.Create(FileTVConstants.ORDER_TYPE_TYPE, MemberType.NestedTypes, TypeTreeNodeGroupType),
			};
			if (infos.Length != newOrders.Length)
				throw new ArgumentException();

			var dict = new Dictionary<MemberType, ITreeNodeGroup2>(infos.Length);
			foreach (var info in infos)
				dict[info.Item2] = info.Item3;
			for (int i = 0; i < newOrders.Length; i++)
				dict[newOrders[i]].Order = infos[i].Item1;
		}
	}

	sealed class AssemblyRefTreeNodeGroup : ITreeNodeGroup2 {
		public AssemblyRefTreeNodeGroup(double order) {
			this.order = order;
		}

		public double Order {
			get { return order; }
			set { order = value; }
		}
		double order;

		public int Compare(ITreeNodeData x, ITreeNodeData y) {
			if (x == y) return 0;
			var a = x as IAssemblyReferenceNode;
			var b = y as IAssemblyReferenceNode;
			if (a == null) return -1;
			if (b == null) return 1;
			return StringComparer.OrdinalIgnoreCase.Compare(a.AssemblyRef.FullName, b.AssemblyRef.FullName);
		}
	}

	sealed class ModuleRefTreeNodeGroup : ITreeNodeGroup2 {
		public ModuleRefTreeNodeGroup(double order) {
			this.order = order;
		}

		public double Order {
			get { return order; }
			set { order = value; }
		}
		double order;

		public int Compare(ITreeNodeData x, ITreeNodeData y) {
			if (x == y) return 0;
			var a = x as IModuleReferenceNode;
			var b = y as IModuleReferenceNode;
			if (a == null) return -1;
			if (b == null) return 1;
			return StringComparer.OrdinalIgnoreCase.Compare(a.ModuleRef.FullName, b.ModuleRef.FullName);
		}
	}

	sealed class ReferencesFolderTreeNodeGroup : ITreeNodeGroup2 {
		public ReferencesFolderTreeNodeGroup(double order) {
			this.order = order;
		}

		public double Order {
			get { return order; }
			set { order = value; }
		}
		double order;

		public int Compare(ITreeNodeData x, ITreeNodeData y) {
			if (x == y) return 0;
			var a = x as IReferencesFolderNode;
			var b = y as IReferencesFolderNode;
			if (a == null) return -1;
			if (b == null) return 1;
			return -1;
		}
	}

	sealed class ResourcesFolderTreeNodeGroup : ITreeNodeGroup2 {
		public ResourcesFolderTreeNodeGroup(double order) {
			this.order = order;
		}

		public double Order {
			get { return order; }
			set { order = value; }
		}
		double order;

		public int Compare(ITreeNodeData x, ITreeNodeData y) {
			if (x == y) return 0;
			var a = x as IResourcesFolderNode;
			var b = y as IResourcesFolderNode;
			if (a == null) return -1;
			if (b == null) return 1;
			return -1;
		}
	}

	sealed class BaseTypeFolderTreeNodeGroup : ITreeNodeGroup2 {
		public BaseTypeFolderTreeNodeGroup(double order) {
			this.order = order;
		}

		public double Order {
			get { return order; }
			set { order = value; }
		}
		double order;

		public int Compare(ITreeNodeData x, ITreeNodeData y) {
			if (x == y) return 0;
			var a = x as IBaseTypeFolderNode;
			var b = y as IBaseTypeFolderNode;
			if (a == null) return -1;
			if (b == null) return 1;
			return -1;
		}
	}

	sealed class DerivedTypesFolderTreeNodeGroup : ITreeNodeGroup2 {
		public DerivedTypesFolderTreeNodeGroup(double order) {
			this.order = order;
		}

		public double Order {
			get { return order; }
			set { order = value; }
		}
		double order;

		public int Compare(ITreeNodeData x, ITreeNodeData y) {
			if (x == y) return 0;
			var a = x as IDerivedTypesFolderNode;
			var b = y as IDerivedTypesFolderNode;
			if (a == null) return -1;
			if (b == null) return 1;
			return -1;
		}
	}

	sealed class MessageTreeNodeGroup : ITreeNodeGroup2 {
		public MessageTreeNodeGroup(double order) {
			this.order = order;
		}

		public double Order {
			get { return order; }
			set { order = value; }
		}
		double order;

		public int Compare(ITreeNodeData x, ITreeNodeData y) {
			if (x == y) return 0;
			var a = x as IMessageNode;
			var b = y as IMessageNode;
			if (a == null) return -1;
			if (b == null) return 1;
			return StringComparer.OrdinalIgnoreCase.Compare(a.Text, b.Text);
		}
	}

	sealed class DerivedTypeTreeNodeGroup : ITreeNodeGroup2 {
		public DerivedTypeTreeNodeGroup(double order) {
			this.order = order;
		}

		public double Order {
			get { return order; }
			set { order = value; }
		}
		double order;

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

	sealed class NamespaceTreeNodeGroup : ITreeNodeGroup2 {
		public NamespaceTreeNodeGroup(double order) {
			this.order = order;
		}

		public double Order {
			get { return order; }
			set { order = value; }
		}
		double order;

		public int Compare(ITreeNodeData x, ITreeNodeData y) {
			if (x == y) return 0;
			var a = x as INamespaceNode;
			var b = y as INamespaceNode;
			if (a == null) return -1;
			if (b == null) return 1;
			return StringComparer.OrdinalIgnoreCase.Compare(a.Name, b.Name);
		}
	}

	sealed class BaseTypeTreeNodeGroup : ITreeNodeGroup2 {
		public BaseTypeTreeNodeGroup(double order) {
			this.order = order;
		}

		public double Order {
			get { return order; }
			set { order = value; }
		}
		double order;

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

	sealed class TypeTreeNodeGroup : ITreeNodeGroup2 {
		public TypeTreeNodeGroup(double order) {
			this.order = order;
		}

		public double Order {
			get { return order; }
			set { order = value; }
		}
		double order;

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

	sealed class MethodTreeNodeGroup : ITreeNodeGroup2 {
		public MethodTreeNodeGroup(double order) {
			this.order = order;
		}

		public double Order {
			get { return order; }
			set { order = value; }
		}
		double order;

		public int Compare(ITreeNodeData x, ITreeNodeData y) {
			if (x == y) return 0;
			var a = x as IMethodNode;
			var b = y as IMethodNode;
			if (a == null) return -1;
			if (b == null) return 1;
			return MethodDefComparer.Instance.Compare(a.MethodDef, b.MethodDef);
		}
	}

	sealed class FieldTreeNodeGroup : ITreeNodeGroup2 {
		public FieldTreeNodeGroup(double order) {
			this.order = order;
		}

		public double Order {
			get { return order; }
			set { order = value; }
		}
		double order;

		public int Compare(ITreeNodeData x, ITreeNodeData y) {
			if (x == y) return 0;
			var a = x as IFieldNode;
			var b = y as IFieldNode;
			if (a == null) return -1;
			if (b == null) return 1;
			return FieldDefComparer.Instance.Compare(a.FieldDef, b.FieldDef);
		}
	}

	sealed class EventTreeNodeGroup : ITreeNodeGroup2 {
		public EventTreeNodeGroup(double order) {
			this.order = order;
		}

		public double Order {
			get { return order; }
			set { order = value; }
		}
		double order;

		public int Compare(ITreeNodeData x, ITreeNodeData y) {
			if (x == y) return 0;
			var a = x as IEventNode;
			var b = y as IEventNode;
			if (a == null) return -1;
			if (b == null) return 1;
			return EventDefComparer.Instance.Compare(a.EventDef, b.EventDef);
		}
	}

	sealed class PropertyTreeNodeGroup : ITreeNodeGroup2 {
		public PropertyTreeNodeGroup(double order) {
			this.order = order;
		}

		public double Order {
			get { return order; }
			set { order = value; }
		}
		double order;

		public int Compare(ITreeNodeData x, ITreeNodeData y) {
			if (x == y) return 0;
			var a = x as IPropertyNode;
			var b = y as IPropertyNode;
			if (a == null) return -1;
			if (b == null) return 1;
			return PropertyDefComparer.Instance.Compare(a.PropertyDef, b.PropertyDef);
		}
	}

	sealed class ResourceTreeNodeGroup : ITreeNodeGroup2 {
		public ResourceTreeNodeGroup(double order) {
			this.order = order;
		}

		public double Order {
			get { return order; }
			set { order = value; }
		}
		double order;

		public int Compare(ITreeNodeData x, ITreeNodeData y) {
			if (x == y) return 0;
			var a = x as IResourceNode;
			var b = y as IResourceNode;
			if (a == null) return -1;
			if (b == null) return 1;
			int c = StringComparer.OrdinalIgnoreCase.Compare(a.Resource.Name, b.Resource.Name);
			if (c != 0) return c;
			return a.Resource.MDToken.Raw.CompareTo(b.Resource.MDToken.Raw);
		}
	}

	sealed class ResourceElementTreeNodeGroup : ITreeNodeGroup2 {
		public ResourceElementTreeNodeGroup(double order) {
			this.order = order;
		}

		public double Order {
			get { return order; }
			set { order = value; }
		}
		double order;

		public int Compare(ITreeNodeData x, ITreeNodeData y) {
			if (x == y) return 0;
			var a = x as IResourceElementNode;
			var b = y as IResourceElementNode;
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
