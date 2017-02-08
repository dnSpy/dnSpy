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

using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnSpy.Contracts.Documents;
using dnSpy.Contracts.Documents.TreeView;
using dnSpy.Contracts.Documents.TreeView.Resources;

namespace dnSpy.Contracts.Search {
	/// <summary>
	/// Filters nodes
	/// </summary>
	public sealed class FlagsDocumentTreeNodeFilter : DocumentTreeNodeFilterBase {
		readonly VisibleMembersFlags flags;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="flags">Flags</param>
		public FlagsDocumentTreeNodeFilter(VisibleMembersFlags flags) {
			this.flags = flags;
		}

#pragma warning disable 1591 // Missing XML comment for publicly visible type or member
		public override DocumentTreeNodeFilterResult GetResult(AssemblyRef asmRef) {
			bool isMatch = (flags & VisibleMembersFlags.AssemblyRef) != 0;
			if (!isMatch)
				return new DocumentTreeNodeFilterResult(FilterType.Hide, isMatch);
			return new DocumentTreeNodeFilterResult(FilterType.Visible, isMatch);
		}

		DocumentTreeNodeFilterResult FilterFile(VisibleMembersFlags thisFlag, VisibleMembersFlags visibleFlags) {
			bool isMatch = (flags & thisFlag) != 0;
			if ((flags & visibleFlags) == 0)
				return new DocumentTreeNodeFilterResult(FilterType.Hide, isMatch);

			if (isMatch)
				return new DocumentTreeNodeFilterResult(FilterType.Visible, isMatch);   // Make sure it's not hidden

			return new DocumentTreeNodeFilterResult(FilterType.Default, isMatch);
		}

		public override DocumentTreeNodeFilterResult GetResult(AssemblyDef asm) {
			var thisFlag = VisibleMembersFlags.AssemblyDef;
			var visibleFlags = thisFlag | VisibleMembersFlags.ModuleDef |
					VisibleMembersFlags.Namespace | VisibleMembersFlags.AnyTypeDef |
					VisibleMembersFlags.FieldDef | VisibleMembersFlags.MethodDef |
					VisibleMembersFlags.InstanceConstructor | VisibleMembersFlags.PropertyDef |
					VisibleMembersFlags.EventDef | VisibleMembersFlags.AssemblyRef |
					VisibleMembersFlags.BaseTypes | VisibleMembersFlags.DerivedTypes |
					VisibleMembersFlags.ModuleRef | VisibleMembersFlags.ResourceList |
					VisibleMembersFlags.MethodBody | VisibleMembersFlags.ParamDefs |
					VisibleMembersFlags.ParamDef | VisibleMembersFlags.Locals |
					VisibleMembersFlags.Local | VisibleMembersFlags.Resource |
					VisibleMembersFlags.ResourceElement | VisibleMembersFlags.Other |
					VisibleMembersFlags.Attributes;
			return FilterFile(thisFlag, visibleFlags);
		}

		public override DocumentTreeNodeFilterResult GetResult(ModuleDef mod) {
			var thisFlag = VisibleMembersFlags.ModuleDef;
			var visibleFlags = thisFlag |
					VisibleMembersFlags.Namespace | VisibleMembersFlags.AnyTypeDef |
					VisibleMembersFlags.FieldDef | VisibleMembersFlags.MethodDef |
					VisibleMembersFlags.InstanceConstructor | VisibleMembersFlags.PropertyDef |
					VisibleMembersFlags.EventDef | VisibleMembersFlags.AssemblyRef |
					VisibleMembersFlags.BaseTypes | VisibleMembersFlags.DerivedTypes |
					VisibleMembersFlags.ModuleRef | VisibleMembersFlags.ResourceList |
					VisibleMembersFlags.MethodBody | VisibleMembersFlags.ParamDefs |
					VisibleMembersFlags.ParamDef | VisibleMembersFlags.Locals |
					VisibleMembersFlags.Local | VisibleMembersFlags.Resource |
					VisibleMembersFlags.ResourceElement | VisibleMembersFlags.Other |
					VisibleMembersFlags.Attributes;
			return FilterFile(thisFlag, visibleFlags);
		}

		public override DocumentTreeNodeFilterResult GetResult(IDsDocument document) {
			var thisFlag = VisibleMembersFlags.NonNetFile;
			var visibleFlags = thisFlag | VisibleMembersFlags.Other | VisibleMembersFlags.Attributes;
			return FilterFile(thisFlag, visibleFlags);
		}

		public override DocumentTreeNodeFilterResult GetResult(BaseTypeFolderNode node) {
			bool isMatch = (flags & VisibleMembersFlags.BaseTypes) != 0;
			if (!isMatch)
				return new DocumentTreeNodeFilterResult(FilterType.Hide, isMatch);
			return new DocumentTreeNodeFilterResult(FilterType.Visible, isMatch);
		}

		public override DocumentTreeNodeFilterResult GetResult(DerivedTypesFolderNode node) {
			bool isMatch = (flags & VisibleMembersFlags.DerivedTypes) != 0;
			if (!isMatch)
				return new DocumentTreeNodeFilterResult(FilterType.Hide, isMatch);
			return new DocumentTreeNodeFilterResult(FilterType.Visible, isMatch);
		}

		public override DocumentTreeNodeFilterResult GetResult(EventDef evt) {
			var visibleFlags = VisibleMembersFlags.EventDef | VisibleMembersFlags.MethodDef |
								VisibleMembersFlags.MethodBody | VisibleMembersFlags.ParamDefs |
								VisibleMembersFlags.ParamDef | VisibleMembersFlags.Locals |
								VisibleMembersFlags.Local | VisibleMembersFlags.Attributes;
			bool isMatch = (flags & VisibleMembersFlags.EventDef) != 0;
			if ((flags & visibleFlags) == 0)
				return new DocumentTreeNodeFilterResult(FilterType.Hide, isMatch);
			if (isMatch)
				return new DocumentTreeNodeFilterResult(FilterType.Visible, isMatch);   // Make sure it's not hidden
			return new DocumentTreeNodeFilterResult(FilterType.CheckChildren, isMatch);
		}

		public override DocumentTreeNodeFilterResult GetResult(FieldDef field) {
			var visibleFlags = VisibleMembersFlags.FieldDef | VisibleMembersFlags.Attributes;
			bool isMatch = (flags & VisibleMembersFlags.FieldDef) != 0;
			if ((flags & visibleFlags) == 0)
				return new DocumentTreeNodeFilterResult(FilterType.Hide, isMatch);
			if (isMatch)
				return new DocumentTreeNodeFilterResult(FilterType.Visible, isMatch);   // Make sure it's not hidden
			return new DocumentTreeNodeFilterResult(FilterType.CheckChildren, isMatch);
		}

		public override DocumentTreeNodeFilterResult GetResult(MethodDef method) {
			var childrenFlags = VisibleMembersFlags.MethodBody | VisibleMembersFlags.ParamDefs |
								VisibleMembersFlags.ParamDef | VisibleMembersFlags.Locals |
								VisibleMembersFlags.Local | VisibleMembersFlags.Attributes;
			var visibleFlags = childrenFlags | VisibleMembersFlags.MethodDef | VisibleMembersFlags.InstanceConstructor;
			bool isMatch = (flags & VisibleMembersFlags.MethodDef) != 0 ||
							(method.IsInstanceConstructor && (flags & VisibleMembersFlags.InstanceConstructor) != 0);
			if ((flags & visibleFlags) == 0)
				return new DocumentTreeNodeFilterResult(FilterType.Hide, false);
			if (isMatch)
				return new DocumentTreeNodeFilterResult(FilterType.Visible, isMatch);
			if ((flags & childrenFlags) == 0)
				return new DocumentTreeNodeFilterResult(FilterType.Hide, isMatch);
			return new DocumentTreeNodeFilterResult(FilterType.CheckChildren, isMatch);
		}

		public override DocumentTreeNodeFilterResult GetResult(ModuleRef modRef) {
			bool isMatch = (flags & VisibleMembersFlags.ModuleRef) != 0;
			if (!isMatch)
				return new DocumentTreeNodeFilterResult(FilterType.Hide, isMatch);
			return new DocumentTreeNodeFilterResult(FilterType.Visible, isMatch);
		}

		public override DocumentTreeNodeFilterResult GetResult(string ns, IDsDocument owner) {
			var visibleFlags = VisibleMembersFlags.Namespace | VisibleMembersFlags.AnyTypeDef |
					VisibleMembersFlags.FieldDef | VisibleMembersFlags.MethodDef |
					VisibleMembersFlags.InstanceConstructor | VisibleMembersFlags.PropertyDef |
					VisibleMembersFlags.EventDef | VisibleMembersFlags.BaseTypes |
					VisibleMembersFlags.DerivedTypes | VisibleMembersFlags.MethodBody |
					VisibleMembersFlags.ParamDefs | VisibleMembersFlags.ParamDef |
					VisibleMembersFlags.Locals | VisibleMembersFlags.Local |
					VisibleMembersFlags.Attributes;
			bool isMatch = (flags & VisibleMembersFlags.Namespace) != 0;
			if ((flags & visibleFlags) == 0)
				return new DocumentTreeNodeFilterResult(FilterType.Hide, isMatch);
			if (isMatch)
				return new DocumentTreeNodeFilterResult(FilterType.Visible, isMatch);   // Make sure it's not hidden
			return new DocumentTreeNodeFilterResult(FilterType.CheckChildren, isMatch);
		}

		public override DocumentTreeNodeFilterResult GetResult(PropertyDef prop) {
			var visibleFlags = VisibleMembersFlags.PropertyDef | VisibleMembersFlags.MethodDef |
								VisibleMembersFlags.MethodBody | VisibleMembersFlags.ParamDefs |
								VisibleMembersFlags.ParamDef | VisibleMembersFlags.Locals |
								VisibleMembersFlags.Local | VisibleMembersFlags.Attributes;
			bool isMatch = (flags & VisibleMembersFlags.PropertyDef) != 0;
			if ((flags & visibleFlags) == 0)
				return new DocumentTreeNodeFilterResult(FilterType.Hide, isMatch);
			if (isMatch)
				return new DocumentTreeNodeFilterResult(FilterType.Visible, isMatch);   // Make sure it's not hidden
			return new DocumentTreeNodeFilterResult(FilterType.CheckChildren, isMatch);
		}

		public override DocumentTreeNodeFilterResult GetResult(ReferencesFolderNode node) {
			var visibleFlags = VisibleMembersFlags.AssemblyRef | VisibleMembersFlags.ModuleRef;
			const bool isMatch = false;
			if ((flags & visibleFlags) == 0)
				return new DocumentTreeNodeFilterResult(FilterType.Hide, isMatch);
			return new DocumentTreeNodeFilterResult(FilterType.Default, isMatch);
		}

		public override DocumentTreeNodeFilterResult GetResult(ResourcesFolderNode node) {
			var visibleFlags = VisibleMembersFlags.ResourceList | VisibleMembersFlags.Resource |
								VisibleMembersFlags.ResourceElement | VisibleMembersFlags.Attributes;
			bool isMatch = (flags & VisibleMembersFlags.ResourceList) != 0;
			if ((flags & visibleFlags) == 0)
				return new DocumentTreeNodeFilterResult(FilterType.Hide, isMatch);
			if (isMatch)
				return new DocumentTreeNodeFilterResult(FilterType.Visible, isMatch);
			return new DocumentTreeNodeFilterResult(FilterType.CheckChildren, isMatch);
		}

		public override DocumentTreeNodeFilterResult GetResult(ResourceNode node) {
			var visibleFlags = VisibleMembersFlags.Resource | VisibleMembersFlags.ResourceElement |
								VisibleMembersFlags.Attributes;
			bool isMatch = (flags & VisibleMembersFlags.Resource) != 0;
			if ((flags & visibleFlags) == 0)
				return new DocumentTreeNodeFilterResult(FilterType.Hide, isMatch);
			if (isMatch)
				return new DocumentTreeNodeFilterResult(FilterType.Visible, isMatch);
			return new DocumentTreeNodeFilterResult(FilterType.CheckChildren, isMatch);
		}

		public override DocumentTreeNodeFilterResult GetResult(ResourceElementNode node) {
			bool isMatch = (flags & VisibleMembersFlags.ResourceElement) != 0;
			if (!isMatch)
				return new DocumentTreeNodeFilterResult(FilterType.Hide, isMatch);
			return new DocumentTreeNodeFilterResult(FilterType.Visible, isMatch);
		}

		public override DocumentTreeNodeFilterResult GetResultOther(DocumentTreeNodeData node) {
			bool isMatch = (flags & VisibleMembersFlags.Other) != 0;
			if (!isMatch)
				return new DocumentTreeNodeFilterResult(FilterType.Hide, isMatch);
			return new DocumentTreeNodeFilterResult(FilterType.Visible, isMatch);
		}

		public override DocumentTreeNodeFilterResult GetResult(TypeDef type) {
			var childrenFlags = VisibleMembersFlags.AnyTypeDef | VisibleMembersFlags.FieldDef |
					VisibleMembersFlags.MethodDef | VisibleMembersFlags.InstanceConstructor |
					VisibleMembersFlags.PropertyDef | VisibleMembersFlags.EventDef |
					VisibleMembersFlags.BaseTypes | VisibleMembersFlags.DerivedTypes |
					VisibleMembersFlags.MethodBody | VisibleMembersFlags.ParamDefs |
					VisibleMembersFlags.ParamDef | VisibleMembersFlags.Locals |
					VisibleMembersFlags.Local | VisibleMembersFlags.Attributes;
			var visibleFlags = VisibleMembersFlags.AnyTypeDef | childrenFlags;
			if ((flags & visibleFlags) == 0)
				return new DocumentTreeNodeFilterResult(FilterType.Hide, false);
			if ((flags & VisibleMembersFlags.GenericTypeDef) != 0 && type.GenericParameters.Count > 0)
				return new DocumentTreeNodeFilterResult(FilterType.Visible, true);
			else if ((flags & VisibleMembersFlags.NonGenericTypeDef) != 0 && type.GenericParameters.Count == 0)
				return new DocumentTreeNodeFilterResult(FilterType.Visible, true);
			else if ((flags & VisibleMembersFlags.EnumTypeDef) != 0 && type.IsEnum)
				return new DocumentTreeNodeFilterResult(FilterType.Visible, true);
			else if ((flags & VisibleMembersFlags.InterfaceTypeDef) != 0 && type.IsInterface)
				return new DocumentTreeNodeFilterResult(FilterType.Visible, true);
			else if ((flags & VisibleMembersFlags.ClassTypeDef) != 0 && !type.IsValueType && !type.IsInterface)
				return new DocumentTreeNodeFilterResult(FilterType.Visible, true);
			else if ((flags & VisibleMembersFlags.StructTypeDef) != 0 && type.IsValueType && !type.IsEnum)
				return new DocumentTreeNodeFilterResult(FilterType.Visible, true);
			else if ((flags & VisibleMembersFlags.DelegateTypeDef) != 0 && IsDelegate(type))
				return new DocumentTreeNodeFilterResult(FilterType.Visible, true);
			else if ((flags & VisibleMembersFlags.TypeDef) != 0)
				return new DocumentTreeNodeFilterResult(FilterType.Visible, true);
			else if ((flags & childrenFlags) == 0)
				return new DocumentTreeNodeFilterResult(FilterType.Hide, false);

			var defaultValue = new DocumentTreeNodeFilterResult(FilterType.CheckChildren, false);
			if (type.HasNestedTypes)
				return defaultValue;
			if ((flags & VisibleMembersFlags.FieldDef) != 0 && type.HasFields)
				return defaultValue;
			if ((flags & VisibleMembersFlags.MethodDef) != 0 && type.HasMethods)
				return defaultValue;
			if ((flags & VisibleMembersFlags.InstanceConstructor) != 0 && HasInstanceConstructors(type))
				return defaultValue;
			if ((flags & VisibleMembersFlags.PropertyDef) != 0 && type.HasProperties)
				return defaultValue;
			if ((flags & VisibleMembersFlags.EventDef) != 0 && type.HasEvents)
				return defaultValue;
			if ((flags & VisibleMembersFlags.BaseTypes) != 0 || (flags & VisibleMembersFlags.DerivedTypes) != 0)
				return defaultValue;
			if ((flags & VisibleMembersFlags.MethodBody) != 0 && HasMethodBodies(type))
				return defaultValue;
			if ((flags & (VisibleMembersFlags.ParamDefs | VisibleMembersFlags.ParamDef)) != 0 && HasParamDefs(type))
				return defaultValue;
			if ((flags & (VisibleMembersFlags.Locals | VisibleMembersFlags.Local)) != 0 && HasLocals(type))
				return defaultValue;

			return new DocumentTreeNodeFilterResult(FilterType.Hide, false);
		}

		static bool IsDelegate(TypeDef type) => type.BaseType != null && type.BaseType.FullName == "System.MulticastDelegate" && type.BaseType.DefinitionAssembly.IsCorLib();

		static bool HasInstanceConstructors(TypeDef type) => type.Methods.Any(m => m.IsInstanceConstructor);

		static bool HasMethodBodies(TypeDef type) {
			foreach (var method in type.Methods) {
				if (method.Body != null)
					return true;
			}
			return false;
		}

		static bool HasParamDefs(TypeDef type) => type.Methods.Any(m => m.HasParamDefs);

		static bool HasLocals(TypeDef type) {
			foreach (var method in type.Methods) {
				if (method.Body != null && method.Body.HasVariables)
					return true;
			}
			return false;
		}

		public override DocumentTreeNodeFilterResult GetResultBody(MethodDef method) {
			bool isMatch = (flags & VisibleMembersFlags.MethodBody) != 0;
			if (!isMatch)
				return new DocumentTreeNodeFilterResult(FilterType.Hide, isMatch);
			return new DocumentTreeNodeFilterResult(FilterType.Visible, isMatch);
		}

		public override DocumentTreeNodeFilterResult GetResultParamDefs(MethodDef method) {
			var visibleFlags = VisibleMembersFlags.ParamDefs | VisibleMembersFlags.ParamDef |
								VisibleMembersFlags.Attributes;
			bool isMatch = (flags & VisibleMembersFlags.ParamDefs) != 0;
			if ((flags & visibleFlags) == 0)
				return new DocumentTreeNodeFilterResult(FilterType.Hide, isMatch);
			if (isMatch)
				return new DocumentTreeNodeFilterResult(FilterType.Visible, isMatch);   // Make sure it's not hidden
			return new DocumentTreeNodeFilterResult(FilterType.CheckChildren, isMatch);
		}

		public override DocumentTreeNodeFilterResult GetResult(MethodDef method, ParamDef param) {
			var visibleFlags = VisibleMembersFlags.ParamDef | VisibleMembersFlags.Attributes;
			bool isMatch = (flags & VisibleMembersFlags.ParamDef) != 0;
			if ((flags & visibleFlags) == 0)
				return new DocumentTreeNodeFilterResult(FilterType.Hide, isMatch);
			if (isMatch)
				return new DocumentTreeNodeFilterResult(FilterType.Visible, isMatch);   // Make sure it's not hidden
			return new DocumentTreeNodeFilterResult(FilterType.CheckChildren, isMatch);
		}

		public override DocumentTreeNodeFilterResult GetResultLocals(MethodDef method) {
			var visibleFlags = VisibleMembersFlags.Locals | VisibleMembersFlags.Local;
			bool isMatch = (flags & VisibleMembersFlags.Locals) != 0;
			if ((flags & visibleFlags) == 0)
				return new DocumentTreeNodeFilterResult(FilterType.Hide, isMatch);
			if (isMatch)
				return new DocumentTreeNodeFilterResult(FilterType.Visible, isMatch);   // Make sure it's not hidden
			return new DocumentTreeNodeFilterResult(FilterType.CheckChildren, isMatch);
		}

		public override DocumentTreeNodeFilterResult GetResult(MethodDef method, Local local) {
			bool isMatch = (flags & VisibleMembersFlags.Local) != 0;
			if (!isMatch)
				return new DocumentTreeNodeFilterResult(FilterType.Hide, isMatch);
			return new DocumentTreeNodeFilterResult(FilterType.Visible, isMatch);
		}

		public override DocumentTreeNodeFilterResult GetResultAttributes(IHasCustomAttribute hca) {
			bool isMatch = (flags & VisibleMembersFlags.Attributes) != 0;
			if (!isMatch)
				return new DocumentTreeNodeFilterResult(FilterType.Hide, isMatch);
			return new DocumentTreeNodeFilterResult(FilterType.Visible, isMatch);
		}
#pragma warning restore 1591 // Missing XML comment for publicly visible type or member
	}
}
