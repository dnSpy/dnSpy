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
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using dnlib.DotNet;
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
			case DocumentTreeNodeGroupType.TypeReferenceTreeNodeGroupTypeReferences: return TypeReferenceTreeNodeGroupTypeReferences;
			case DocumentTreeNodeGroupType.TypeSpecsFolderTreeNodeGroupTypeReference: return TypeSpecsFolderTreeNodeGroupTypeReference;
			case DocumentTreeNodeGroupType.MethodReferencesFolderTreeNodeGroupTypeReference: return MethodReferencesFolderTreeNodeGroupTypeReference;
			case DocumentTreeNodeGroupType.FieldReferencesFolderTreeNodeGroupTypeReference: return FieldReferencesFolderTreeNodeGroupTypeReference;
			case DocumentTreeNodeGroupType.PropertyReferencesFolderTreeNodeGroupTypeReference: return PropertyReferencesFolderTreeNodeGroupTypeReference;
			case DocumentTreeNodeGroupType.EventReferencesFolderTreeNodeGroupTypeReference: return EventReferencesFolderTreeNodeGroupTypeReference;
			case DocumentTreeNodeGroupType.TypeSpecTreeNodeGroupTypeSpecsFolder: return TypeSpecTreeNodeGroupTypeSpecsFolder;
			case DocumentTreeNodeGroupType.MethodReferenceTreeNodeGroupMethodReferencesFolder: return MethodReferenceTreeNodeGroupMethodReferencesFolder;
			case DocumentTreeNodeGroupType.FieldReferenceTreeNodeGroupFieldReferencesFolder: return FieldReferenceTreeNodeGroupFieldReferencesFolder;
			case DocumentTreeNodeGroupType.PropertyReferenceTreeNodeGroupPropertyReferencesFolder: return PropertyReferenceTreeNodeGroupPropertyReferencesFolder;
			case DocumentTreeNodeGroupType.EventReferenceTreeNodeGroupEventReferencesFolder: return EventReferenceTreeNodeGroupEventReferencesFolder;
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

		readonly ITreeNodeGroup2 TypeReferenceTreeNodeGroupTypeReferences = new TypeReferenceTreeNodeGroup(DocumentTreeViewConstants.ORDER_TYPEREFS_TYPEREF);

		readonly ITreeNodeGroup2 TypeSpecsFolderTreeNodeGroupTypeReference = new TypeSpecsFolderTreeNodeGroup(DocumentTreeViewConstants.ORDER_TYPEREF_TYPESPECFOLDER);
		readonly ITreeNodeGroup2 MethodReferencesFolderTreeNodeGroupTypeReference = new MethodReferencesFolderTreeNodeGroup(DocumentTreeViewConstants.ORDER_TYPEREF_METHODREFFOLDER);
		readonly ITreeNodeGroup2 PropertyReferencesFolderTreeNodeGroupTypeReference = new PropertyReferencesFolderTreeNodeGroup(DocumentTreeViewConstants.ORDER_TYPEREF_PROPERTYREFFOLDER);
		readonly ITreeNodeGroup2 EventReferencesFolderTreeNodeGroupTypeReference = new EventReferencesFolderTreeNodeGroup(DocumentTreeViewConstants.ORDER_TYPEREF_EVENTREFFOLDER);
		readonly ITreeNodeGroup2 FieldReferencesFolderTreeNodeGroupTypeReference = new FieldReferencesFolderTreeNodeGroup(DocumentTreeViewConstants.ORDER_TYPEREF_FIELDREFFOLDER);

		readonly ITreeNodeGroup2 TypeSpecTreeNodeGroupTypeSpecsFolder = new TypeReferenceTreeNodeGroup(DocumentTreeViewConstants.ORDER_TYPESPECS_TYPESPEC);
		readonly ITreeNodeGroup2 MethodReferenceTreeNodeGroupMethodReferencesFolder = new MethodReferenceTreeNodeGroup(DocumentTreeViewConstants.ORDER_METHODREFS_METHODREF);
		readonly ITreeNodeGroup2 PropertyReferenceTreeNodeGroupPropertyReferencesFolder = new PropertyReferenceTreeNodeGroup(DocumentTreeViewConstants.ORDER_EVENTREFS_EVENTREF);
		readonly ITreeNodeGroup2 EventReferenceTreeNodeGroupEventReferencesFolder = new EventReferenceTreeNodeGroup(DocumentTreeViewConstants.ORDER_FIELDREFS_FIELDREF);
		readonly ITreeNodeGroup2 FieldReferenceTreeNodeGroupFieldReferencesFolder = new FieldReferenceTreeNodeGroup(DocumentTreeViewConstants.ORDER_FIELDREFS_FIELDREF);

		public void SetMemberOrder(MemberKind[] newOrders) {
			if (newOrders is null)
				throw new ArgumentNullException(nameof(newOrders));

			var infos = new (double order, MemberKind kind, ITreeNodeGroup2 group)[] {
				(DocumentTreeViewConstants.ORDER_TYPE_METHOD, MemberKind.Methods, MethodTreeNodeGroupType),
				(DocumentTreeViewConstants.ORDER_TYPE_PROPERTY, MemberKind.Properties, PropertyTreeNodeGroupType),
				(DocumentTreeViewConstants.ORDER_TYPE_EVENT, MemberKind.Events, EventTreeNodeGroupType),
				(DocumentTreeViewConstants.ORDER_TYPE_FIELD, MemberKind.Fields, FieldTreeNodeGroupType),
				(DocumentTreeViewConstants.ORDER_TYPE_TYPE, MemberKind.NestedTypes, TypeTreeNodeGroupType),
			};
			if (infos.Length != newOrders.Length)
				throw new ArgumentException();

			var dict = new Dictionary<MemberKind, ITreeNodeGroup2>(infos.Length);
			foreach (var info in infos)
				dict[info.kind] = info.group;
			for (int i = 0; i < newOrders.Length; i++)
				dict[newOrders[i]].Order = infos[i].order;
		}
	}

	sealed class AssemblyRefTreeNodeGroup : ITreeNodeGroup2 {
		public AssemblyRefTreeNodeGroup(double order) => Order = order;

		public double Order { get; set; }

		public int Compare([AllowNull] TreeNodeData x, [AllowNull] TreeNodeData y) {
			if (x == y) return 0;
			var a = x as AssemblyReferenceNode;
			var b = y as AssemblyReferenceNode;
			if (a is null) return -1;
			if (b is null) return 1;
			return StringComparer.OrdinalIgnoreCase.Compare(a.AssemblyRef.FullName, b.AssemblyRef.FullName);
		}
	}

	sealed class ModuleRefTreeNodeGroup : ITreeNodeGroup2 {
		public ModuleRefTreeNodeGroup(double order) => Order = order;

		public double Order { get; set; }

		public int Compare([AllowNull] TreeNodeData x, [AllowNull] TreeNodeData y) {
			if (x == y) return 0;
			var a = x as ModuleReferenceNode;
			var b = y as ModuleReferenceNode;
			if (a is null) return -1;
			if (b is null) return 1;
			return StringComparer.OrdinalIgnoreCase.Compare(a.ModuleRef.FullName, b.ModuleRef.FullName);
		}
	}

	sealed class ReferencesFolderTreeNodeGroup : ITreeNodeGroup2 {
		public ReferencesFolderTreeNodeGroup(double order) => Order = order;

		public double Order { get; set; }

		public int Compare([AllowNull] TreeNodeData x, [AllowNull] TreeNodeData y) {
			if (x == y) return 0;
			var a = x as ReferencesFolderNode;
			var b = y as ReferencesFolderNode;
			if (a is null) return -1;
			if (b is null) return 1;
			return -1;
		}
	}

	sealed class ResourcesFolderTreeNodeGroup : ITreeNodeGroup2 {
		public ResourcesFolderTreeNodeGroup(double order) => Order = order;

		public double Order { get; set; }

		public int Compare([AllowNull] TreeNodeData x, [AllowNull] TreeNodeData y) {
			if (x == y) return 0;
			var a = x as ResourcesFolderNode;
			var b = y as ResourcesFolderNode;
			if (a is null) return -1;
			if (b is null) return 1;
			return -1;
		}
	}

	sealed class BaseTypeFolderTreeNodeGroup : ITreeNodeGroup2 {
		public BaseTypeFolderTreeNodeGroup(double order) => Order = order;

		public double Order { get; set; }

		public int Compare([AllowNull] TreeNodeData x, [AllowNull] TreeNodeData y) {
			if (x == y) return 0;
			var a = x as BaseTypeFolderNode;
			var b = y as BaseTypeFolderNode;
			if (a is null) return -1;
			if (b is null) return 1;
			return -1;
		}
	}

	sealed class DerivedTypesFolderTreeNodeGroup : ITreeNodeGroup2 {
		public DerivedTypesFolderTreeNodeGroup(double order) => Order = order;

		public double Order { get; set; }

		public int Compare([AllowNull] TreeNodeData x, [AllowNull] TreeNodeData y) {
			if (x == y) return 0;
			var a = x as DerivedTypesFolderNode;
			var b = y as DerivedTypesFolderNode;
			if (a is null) return -1;
			if (b is null) return 1;
			return -1;
		}
	}

	sealed class MessageTreeNodeGroup : ITreeNodeGroup2 {
		public MessageTreeNodeGroup(double order) => Order = order;

		public double Order { get; set; }

		public int Compare([AllowNull] TreeNodeData x, [AllowNull] TreeNodeData y) {
			if (x == y) return 0;
			var a = x as MessageNode;
			var b = y as MessageNode;
			if (a is null) return -1;
			if (b is null) return 1;
			return 0;
		}
	}

	sealed class DerivedTypeTreeNodeGroup : ITreeNodeGroup2 {
		public DerivedTypeTreeNodeGroup(double order) => Order = order;

		public double Order { get; set; }

		public int Compare([AllowNull] TreeNodeData x, [AllowNull] TreeNodeData y) {
			if (x == y) return 0;
			var a = x as DerivedTypeNode;
			var b = y as DerivedTypeNode;
			if (a is null) return -1;
			if (b is null) return 1;
			string an = a.TypeDef.Name;
			string bn = b.TypeDef.Name;
			return StringComparer.OrdinalIgnoreCase.Compare(an, bn);
		}
	}

	sealed class NamespaceTreeNodeGroup : ITreeNodeGroup2 {
		public NamespaceTreeNodeGroup(double order) => Order = order;

		public double Order { get; set; }

		public int Compare([AllowNull] TreeNodeData x, [AllowNull] TreeNodeData y) {
			if (x == y) return 0;
			var a = x as NamespaceNode;
			var b = y as NamespaceNode;
			if (a is null) return -1;
			if (b is null) return 1;
			return StringComparer.OrdinalIgnoreCase.Compare(a.Name, b.Name);
		}
	}

	sealed class BaseTypeTreeNodeGroup : ITreeNodeGroup2 {
		public BaseTypeTreeNodeGroup(double order) => Order = order;

		public double Order { get; set; }

		public int Compare([AllowNull] TreeNodeData x, [AllowNull] TreeNodeData y) {
			if (x == y) return 0;
			var a = x as BaseTypeNode;
			var b = y as BaseTypeNode;
			if (a is null) return -1;
			if (b is null) return 1;
			string an = a.TypeDefOrRef.Name;
			string bn = b.TypeDefOrRef.Name;
			return StringComparer.OrdinalIgnoreCase.Compare(an, bn);
		}
	}

	sealed class TypeTreeNodeGroup : ITreeNodeGroup2 {
		public TypeTreeNodeGroup(double order) => Order = order;

		public double Order { get; set; }

		public int Compare([AllowNull] TreeNodeData x, [AllowNull] TreeNodeData y) {
			if (x == y) return 0;
			var a = x as TypeNode;
			var b = y as TypeNode;
			if (a is null) return -1;
			if (b is null) return 1;
			var an = a.TypeDef.FullName;
			var bn = b.TypeDef.FullName;
			return StringComparer.OrdinalIgnoreCase.Compare(an, bn);
		}
	}

	sealed class TypeReferenceTreeNodeGroup : ITreeNodeGroup2 {
		readonly StringBuilder sb = new StringBuilder();

		public TypeReferenceTreeNodeGroup(double order) => Order = order;

		public double Order { get; set; }

		public int Compare([AllowNull] TreeNodeData x, [AllowNull] TreeNodeData y) {
			if (x == y) return 0;
			var a = x as TypeReferenceNode;
			var b = y as TypeReferenceNode;
			if (a is null) return -1;
			if (b is null) return 1;

			var ar = GetTypeDefOrRef(a.TypeRef);
			var br = GetTypeDefOrRef(b.TypeRef);
			var an = GetName(ar);
			var bn = GetName(br);
			int c = StringComparer.OrdinalIgnoreCase.Compare(an, bn);
			if (c != 0) return c;
			c = GetOrder(a.TypeRef) - GetOrder(b.TypeRef);
			if (c != 0) return c;
			return a.TypeRef.MDToken.ToInt32() - b.TypeRef.MDToken.ToInt32();
		}

		string GetName(ITypeDefOrRef tdr) {
			if (tdr.DeclaringType is null)
				return tdr.Name;
			int parents = 0;
			ITypeDefOrRef parent;
			for (parent = tdr.DeclaringType; parent is not null; parent = parent.DeclaringType)
				parents++;
			bool needSep = false;
			// parents should be small so we don't need to use a List<T>/Stack<T>
			while (parents >= 0) {
				parent = tdr;
				for (int i = 0; i < parents; i++) {
					parent = tdr.DeclaringType;
					Debug2.Assert(parent is not null);
				}
				if (needSep)
					sb.Append('.');
				sb.Append(parent.Name.String);
				needSep = true;
				parents--;
			}
			var res = sb.ToString();
			sb.Clear();
			return res;
		}

		int GetOrder(ITypeDefOrRef typeRef) {
			if (typeRef is TypeDef)
				return 0;
			if (typeRef is TypeRef)
				return 1;
			return 2;
		}

		static ITypeDefOrRef GetTypeDefOrRef(ITypeDefOrRef typeRef) {
			if (typeRef is TypeSpec ts) {
				var sig = ts.TypeSig.RemovePinnedAndModifiers();
				if (sig is TypeDefOrRefSig tdrs)
					return tdrs.TypeDefOrRef ?? typeRef;
				if (sig is GenericInstSig gis)
					return gis.GenericType?.TypeDefOrRef ?? typeRef;
			}
			return typeRef;
		}
	}

	sealed class MethodTreeNodeGroup : ITreeNodeGroup2 {
		public MethodTreeNodeGroup(double order) => Order = order;

		public double Order { get; set; }

		public int Compare([AllowNull] TreeNodeData x, [AllowNull] TreeNodeData y) {
			if (x == y) return 0;
			var a = x as MethodNode;
			var b = y as MethodNode;
			if (a is null) return -1;
			if (b is null) return 1;
			return MethodDefComparer.Instance.Compare(a.MethodDef, b.MethodDef);
		}
	}

	sealed class FieldTreeNodeGroup : ITreeNodeGroup2 {
		public FieldTreeNodeGroup(double order) => Order = order;

		public double Order { get; set; }

		public int Compare([AllowNull] TreeNodeData x, [AllowNull] TreeNodeData y) {
			if (x == y) return 0;
			var a = x as FieldNode;
			var b = y as FieldNode;
			if (a is null) return -1;
			if (b is null) return 1;
			return FieldDefComparer.Instance.Compare(a.FieldDef, b.FieldDef);
		}
	}

	sealed class EventTreeNodeGroup : ITreeNodeGroup2 {
		public EventTreeNodeGroup(double order) => Order = order;

		public double Order { get; set; }

		public int Compare([AllowNull] TreeNodeData x, [AllowNull] TreeNodeData y) {
			if (x == y) return 0;
			var a = x as EventNode;
			var b = y as EventNode;
			if (a is null) return -1;
			if (b is null) return 1;
			return EventDefComparer.Instance.Compare(a.EventDef, b.EventDef);
		}
	}

	sealed class PropertyTreeNodeGroup : ITreeNodeGroup2 {
		public PropertyTreeNodeGroup(double order) => Order = order;

		public double Order { get; set; }

		public int Compare([AllowNull] TreeNodeData x, [AllowNull] TreeNodeData y) {
			if (x == y) return 0;
			var a = x as PropertyNode;
			var b = y as PropertyNode;
			if (a is null) return -1;
			if (b is null) return 1;
			return PropertyDefComparer.Instance.Compare(a.PropertyDef, b.PropertyDef);
		}
	}

	sealed class ResourceTreeNodeGroup : ITreeNodeGroup2 {
		public ResourceTreeNodeGroup(double order) => Order = order;

		public double Order { get; set; }

		public int Compare([AllowNull] TreeNodeData x, [AllowNull] TreeNodeData y) {
			if ((object?)x == y) return 0;
			if (x is null) return -1;
			if (y is null) return 1;
			var ra = ResourceNode.GetResource((DocumentTreeNodeData)x);
			var rb = ResourceNode.GetResource((DocumentTreeNodeData)y);
			if (ra is null) return -1;
			if (rb is null) return 1;
			int c = StringComparer.OrdinalIgnoreCase.Compare(ra.Name, rb.Name);
			if (c != 0) return c;
			return ra.MDToken.Raw.CompareTo(rb.MDToken.Raw);
		}
	}

	sealed class ResourceElementTreeNodeGroup : ITreeNodeGroup2 {
		public ResourceElementTreeNodeGroup(double order) => Order = order;

		public double Order { get; set; }

		public int Compare([AllowNull] TreeNodeData x, [AllowNull] TreeNodeData y) {
			if ((object?)x == y) return 0;
			if (x is null) return -1;
			if (y is null) return 1;
			var ra = ResourceElementNode.GetResourceElement((DocumentTreeNodeData)x);
			var rb = ResourceElementNode.GetResourceElement((DocumentTreeNodeData)y);
			if (ra is null) return -1;
			if (rb is null) return 1;
			int c = StringComparer.OrdinalIgnoreCase.Compare(ra.Name, rb.Name);
			if (c != 0) return c;
			int cx = (int)ra.ResourceData.Code.FixUserType();
			int cy = (int)rb.ResourceData.Code.FixUserType();
			return cx.CompareTo(cy);
		}
	}

	sealed class TypeSpecsFolderTreeNodeGroup : ITreeNodeGroup2 {
		public TypeSpecsFolderTreeNodeGroup(double order) => Order = order;

		public double Order { get; set; }

		public int Compare([AllowNull] TreeNodeData x, [AllowNull] TreeNodeData y) {
			if (x == y) return 0;
			var a = x as TypeSpecsFolderNode;
			var b = y as TypeSpecsFolderNode;
			if (a is null) return -1;
			if (b is null) return 1;
			return -1;
		}
	}

	sealed class MethodReferencesFolderTreeNodeGroup : ITreeNodeGroup2 {
		public MethodReferencesFolderTreeNodeGroup(double order) => Order = order;

		public double Order { get; set; }

		public int Compare([AllowNull] TreeNodeData x, [AllowNull] TreeNodeData y) {
			if (x == y) return 0;
			var a = x as MethodReferencesFolderNode;
			var b = y as MethodReferencesFolderNode;
			if (a is null) return -1;
			if (b is null) return 1;
			return -1;
		}
	}

	sealed class PropertyReferencesFolderTreeNodeGroup : ITreeNodeGroup2 {
		public PropertyReferencesFolderTreeNodeGroup(double order) => Order = order;

		public double Order { get; set; }

		public int Compare([AllowNull] TreeNodeData x, [AllowNull] TreeNodeData y) {
			if (x == y) return 0;
			var a = x as PropertyReferencesFolderNode;
			var b = y as PropertyReferencesFolderNode;
			if (a is null) return -1;
			if (b is null) return 1;
			return -1;
		}
	}

	sealed class EventReferencesFolderTreeNodeGroup : ITreeNodeGroup2 {
		public EventReferencesFolderTreeNodeGroup(double order) => Order = order;

		public double Order { get; set; }

		public int Compare([AllowNull] TreeNodeData x, [AllowNull] TreeNodeData y) {
			if (x == y) return 0;
			var a = x as EventReferencesFolderNode;
			var b = y as EventReferencesFolderNode;
			if (a is null) return -1;
			if (b is null) return 1;
			return -1;
		}
	}

	sealed class FieldReferencesFolderTreeNodeGroup : ITreeNodeGroup2 {
		public FieldReferencesFolderTreeNodeGroup(double order) => Order = order;

		public double Order { get; set; }

		public int Compare([AllowNull] TreeNodeData x, [AllowNull] TreeNodeData y) {
			if (x == y) return 0;
			var a = x as FieldReferencesFolderNode;
			var b = y as FieldReferencesFolderNode;
			if (a is null) return -1;
			if (b is null) return 1;
			return -1;
		}
	}

	sealed class MethodReferenceTreeNodeGroup : ITreeNodeGroup2 {
		public MethodReferenceTreeNodeGroup(double order) => Order = order;

		public double Order { get; set; }

		public int Compare([AllowNull] TreeNodeData x, [AllowNull] TreeNodeData y) {
			if (x == y) return 0;
			var a = x as MethodReferenceNode;
			var b = y as MethodReferenceNode;
			if (a is null) return -1;
			if (b is null) return 1;
			return MethodRefComparer.Instance.Compare(a.MethodRef, b.MethodRef);
		}
	}

	sealed class PropertyReferenceTreeNodeGroup : ITreeNodeGroup2 {
		public PropertyReferenceTreeNodeGroup(double order) => Order = order;

		public double Order { get; set; }

		public int Compare([AllowNull] TreeNodeData x, [AllowNull] TreeNodeData y) {
			if (x == y) return 0;
			var a = x as PropertyReferenceNode;
			var b = y as PropertyReferenceNode;
			if (a is null) return -1;
			if (b is null) return 1;
			return PropertyRefComparer.Instance.Compare(a.PropertyRef, b.PropertyRef);
		}
	}

	sealed class EventReferenceTreeNodeGroup : ITreeNodeGroup2 {
		public EventReferenceTreeNodeGroup(double order) => Order = order;

		public double Order { get; set; }

		public int Compare([AllowNull] TreeNodeData x, [AllowNull] TreeNodeData y) {
			if (x == y) return 0;
			var a = x as EventReferenceNode;
			var b = y as EventReferenceNode;
			if (a is null) return -1;
			if (b is null) return 1;
			return EventRefComparer.Instance.Compare(a.EventRef, b.EventRef);
		}
	}

	sealed class FieldReferenceTreeNodeGroup : ITreeNodeGroup2 {
		public FieldReferenceTreeNodeGroup(double order) => Order = order;

		public double Order { get; set; }

		public int Compare([AllowNull] TreeNodeData x, [AllowNull] TreeNodeData y) {
			if (x == y) return 0;
			var a = x as FieldReferenceNode;
			var b = y as FieldReferenceNode;
			if (a is null) return -1;
			if (b is null) return 1;
			return MemberRefComparer.Instance.Compare(a.FieldRef, b.FieldRef);
		}
	}
}
