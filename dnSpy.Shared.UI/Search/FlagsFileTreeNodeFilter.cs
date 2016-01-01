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

using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnSpy.Contracts.Files;
using dnSpy.Contracts.Files.TreeView;
using dnSpy.Contracts.Files.TreeView.Resources;

namespace dnSpy.Shared.UI.Search {
	public sealed class FlagsFileTreeNodeFilter : FileTreeNodeFilterBase {
		readonly VisibleMembersFlags flags;

		public FlagsFileTreeNodeFilter(VisibleMembersFlags flags) {
			this.flags = flags;
		}

		public override FileTreeNodeFilterResult GetResult(AssemblyRef asmRef) {
			bool isMatch = (flags & VisibleMembersFlags.AssemblyRef) != 0;
			if (!isMatch)
				return new FileTreeNodeFilterResult(FilterType.Hide, isMatch);
			return new FileTreeNodeFilterResult(FilterType.Visible, isMatch);
		}

		FileTreeNodeFilterResult FilterFile(VisibleMembersFlags thisFlag, VisibleMembersFlags visibleFlags) {
			bool isMatch = (flags & thisFlag) != 0;
			if ((flags & visibleFlags) == 0)
				return new FileTreeNodeFilterResult(FilterType.Hide, isMatch);

			if (isMatch)
				return new FileTreeNodeFilterResult(FilterType.Visible, isMatch);   // Make sure it's not hidden

			return new FileTreeNodeFilterResult(FilterType.Default, isMatch);
		}

		public override FileTreeNodeFilterResult GetResult(AssemblyDef asm) {
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
					VisibleMembersFlags.ResourceElement | VisibleMembersFlags.Other;
			return FilterFile(thisFlag, visibleFlags);
		}

		public override FileTreeNodeFilterResult GetResult(ModuleDef mod) {
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
					VisibleMembersFlags.ResourceElement | VisibleMembersFlags.Other;
			return FilterFile(thisFlag, visibleFlags);
		}

		public override FileTreeNodeFilterResult GetResult(IDnSpyFile file) {
			var thisFlag = VisibleMembersFlags.NonNetFile;
			var visibleFlags = thisFlag | VisibleMembersFlags.Other;
			return FilterFile(thisFlag, visibleFlags);
		}

		public override FileTreeNodeFilterResult GetResult(IBaseTypeFolderNode node) {
			bool isMatch = (flags & VisibleMembersFlags.BaseTypes) != 0;
			if (!isMatch)
				return new FileTreeNodeFilterResult(FilterType.Hide, isMatch);
			return new FileTreeNodeFilterResult(FilterType.Visible, isMatch);
		}

		public override FileTreeNodeFilterResult GetResult(IDerivedTypesFolderNode node) {
			bool isMatch = (flags & VisibleMembersFlags.DerivedTypes) != 0;
			if (!isMatch)
				return new FileTreeNodeFilterResult(FilterType.Hide, isMatch);
			return new FileTreeNodeFilterResult(FilterType.Visible, isMatch);
		}

		public override FileTreeNodeFilterResult GetResult(EventDef evt) {
			var visibleFlags = VisibleMembersFlags.EventDef | VisibleMembersFlags.MethodDef |
								VisibleMembersFlags.MethodBody | VisibleMembersFlags.ParamDefs |
								VisibleMembersFlags.ParamDef | VisibleMembersFlags.Locals |
								VisibleMembersFlags.Local;
			bool isMatch = (flags & VisibleMembersFlags.EventDef) != 0;
			if ((flags & visibleFlags) == 0)
				return new FileTreeNodeFilterResult(FilterType.Hide, isMatch);
			if (isMatch)
				return new FileTreeNodeFilterResult(FilterType.Visible, isMatch);   // Make sure it's not hidden
			return new FileTreeNodeFilterResult(FilterType.CheckChildren, isMatch);
		}

		public override FileTreeNodeFilterResult GetResult(FieldDef field) {
			bool isMatch = (flags & VisibleMembersFlags.FieldDef) != 0;
			if (!isMatch)
				return new FileTreeNodeFilterResult(FilterType.Hide, isMatch);
			return new FileTreeNodeFilterResult(FilterType.Visible, isMatch);
		}

		public override FileTreeNodeFilterResult GetResult(MethodDef method) {
			var childrenFlags = VisibleMembersFlags.MethodBody | VisibleMembersFlags.ParamDefs |
								VisibleMembersFlags.ParamDef | VisibleMembersFlags.Locals |
								VisibleMembersFlags.Local;
			var visibleFlags = childrenFlags | VisibleMembersFlags.MethodDef | VisibleMembersFlags.InstanceConstructor;
			bool isMatch = (flags & VisibleMembersFlags.MethodDef) != 0 ||
							(method.IsInstanceConstructor && (flags & VisibleMembersFlags.InstanceConstructor) != 0);
			if ((flags & visibleFlags) == 0)
				return new FileTreeNodeFilterResult(FilterType.Hide, false);
			if (isMatch)
				return new FileTreeNodeFilterResult(FilterType.Visible, isMatch);
			if ((flags & childrenFlags) == 0)
				return new FileTreeNodeFilterResult(FilterType.Hide, isMatch);
			return new FileTreeNodeFilterResult(FilterType.CheckChildren, isMatch);
		}

		public override FileTreeNodeFilterResult GetResult(ModuleRef modRef) {
			bool isMatch = (flags & VisibleMembersFlags.ModuleRef) != 0;
			if (!isMatch)
				return new FileTreeNodeFilterResult(FilterType.Hide, isMatch);
			return new FileTreeNodeFilterResult(FilterType.Visible, isMatch);
		}

		public override FileTreeNodeFilterResult GetResult(string ns, IDnSpyFile owner) {
			var visibleFlags = VisibleMembersFlags.Namespace | VisibleMembersFlags.AnyTypeDef |
					VisibleMembersFlags.FieldDef | VisibleMembersFlags.MethodDef |
					VisibleMembersFlags.InstanceConstructor | VisibleMembersFlags.PropertyDef |
					VisibleMembersFlags.EventDef | VisibleMembersFlags.BaseTypes |
					VisibleMembersFlags.DerivedTypes | VisibleMembersFlags.MethodBody |
					VisibleMembersFlags.ParamDefs | VisibleMembersFlags.ParamDef |
					VisibleMembersFlags.Locals | VisibleMembersFlags.Local;
			bool isMatch = (flags & VisibleMembersFlags.Namespace) != 0;
			if ((flags & visibleFlags) == 0)
				return new FileTreeNodeFilterResult(FilterType.Hide, isMatch);
			if (isMatch)
				return new FileTreeNodeFilterResult(FilterType.Visible, isMatch);   // Make sure it's not hidden
			return new FileTreeNodeFilterResult(FilterType.CheckChildren, isMatch);
		}

		public override FileTreeNodeFilterResult GetResult(PropertyDef prop) {
			var visibleFlags = VisibleMembersFlags.PropertyDef | VisibleMembersFlags.MethodDef |
								VisibleMembersFlags.MethodBody | VisibleMembersFlags.ParamDefs |
								VisibleMembersFlags.ParamDef | VisibleMembersFlags.Locals |
								VisibleMembersFlags.Local;
			bool isMatch = (flags & VisibleMembersFlags.PropertyDef) != 0;
			if ((flags & visibleFlags) == 0)
				return new FileTreeNodeFilterResult(FilterType.Hide, isMatch);
			if (isMatch)
				return new FileTreeNodeFilterResult(FilterType.Visible, isMatch);   // Make sure it's not hidden
			return new FileTreeNodeFilterResult(FilterType.CheckChildren, isMatch);
		}

		public override FileTreeNodeFilterResult GetResult(IReferencesFolderNode node) {
			var visibleFlags = VisibleMembersFlags.AssemblyRef | VisibleMembersFlags.ModuleRef;
			const bool isMatch = false;
			if ((flags & visibleFlags) == 0)
				return new FileTreeNodeFilterResult(FilterType.Hide, isMatch);
			return new FileTreeNodeFilterResult(FilterType.Default, isMatch);
		}

		public override FileTreeNodeFilterResult GetResult(IResourcesFolderNode node) {
			var visibleFlags = VisibleMembersFlags.ResourceList | VisibleMembersFlags.Resource |
								VisibleMembersFlags.ResourceElement;
			bool isMatch = (flags & VisibleMembersFlags.ResourceList) != 0;
			if ((flags & visibleFlags) == 0)
				return new FileTreeNodeFilterResult(FilterType.Hide, isMatch);
			if (isMatch)
				return new FileTreeNodeFilterResult(FilterType.Visible, isMatch);
			return new FileTreeNodeFilterResult(FilterType.CheckChildren, isMatch);
		}

		public override FileTreeNodeFilterResult GetResult(IResourceNode node) {
			var visibleFlags = VisibleMembersFlags.Resource | VisibleMembersFlags.ResourceElement;
			bool isMatch = (flags & VisibleMembersFlags.Resource) != 0;
			if ((flags & visibleFlags) == 0)
				return new FileTreeNodeFilterResult(FilterType.Hide, isMatch);
			if (isMatch)
				return new FileTreeNodeFilterResult(FilterType.Visible, isMatch);
			return new FileTreeNodeFilterResult(FilterType.CheckChildren, isMatch);
		}

		public override FileTreeNodeFilterResult GetResult(IResourceElementNode node) {
			bool isMatch = (flags & VisibleMembersFlags.ResourceElement) != 0;
			if (!isMatch)
				return new FileTreeNodeFilterResult(FilterType.Hide, isMatch);
			return new FileTreeNodeFilterResult(FilterType.Visible, isMatch);
		}

		public override FileTreeNodeFilterResult GetResult(IFileTreeNodeData node) {
			bool isMatch = (flags & VisibleMembersFlags.Other) != 0;
			if (!isMatch)
				return new FileTreeNodeFilterResult(FilterType.Hide, isMatch);
			return new FileTreeNodeFilterResult(FilterType.Visible, isMatch);
		}

		public override FileTreeNodeFilterResult GetResult(TypeDef type) {
			var childrenFlags = VisibleMembersFlags.AnyTypeDef | VisibleMembersFlags.FieldDef |
					VisibleMembersFlags.MethodDef | VisibleMembersFlags.InstanceConstructor |
					VisibleMembersFlags.PropertyDef | VisibleMembersFlags.EventDef |
					VisibleMembersFlags.BaseTypes | VisibleMembersFlags.DerivedTypes |
					VisibleMembersFlags.MethodBody | VisibleMembersFlags.ParamDefs |
					VisibleMembersFlags.ParamDef | VisibleMembersFlags.Locals |
					VisibleMembersFlags.Local;
			var visibleFlags = VisibleMembersFlags.AnyTypeDef | childrenFlags;
			if ((flags & visibleFlags) == 0)
				return new FileTreeNodeFilterResult(FilterType.Hide, false);
			if ((flags & VisibleMembersFlags.GenericTypeDef) != 0 && type.GenericParameters.Count > 0)
				return new FileTreeNodeFilterResult(FilterType.Visible, true);
			else if ((flags & VisibleMembersFlags.NonGenericTypeDef) != 0 && type.GenericParameters.Count == 0)
				return new FileTreeNodeFilterResult(FilterType.Visible, true);
			else if ((flags & VisibleMembersFlags.EnumTypeDef) != 0 && type.IsEnum)
				return new FileTreeNodeFilterResult(FilterType.Visible, true);
			else if ((flags & VisibleMembersFlags.InterfaceTypeDef) != 0 && type.IsInterface)
				return new FileTreeNodeFilterResult(FilterType.Visible, true);
			else if ((flags & VisibleMembersFlags.ClassTypeDef) != 0 && !type.IsValueType && !type.IsInterface)
				return new FileTreeNodeFilterResult(FilterType.Visible, true);
			else if ((flags & VisibleMembersFlags.StructTypeDef) != 0 && type.IsValueType && !type.IsEnum)
				return new FileTreeNodeFilterResult(FilterType.Visible, true);
			else if ((flags & VisibleMembersFlags.DelegateTypeDef) != 0 && IsDelegate(type))
				return new FileTreeNodeFilterResult(FilterType.Visible, true);
			else if ((flags & VisibleMembersFlags.TypeDef) != 0)
				return new FileTreeNodeFilterResult(FilterType.Visible, true);
			else if ((flags & childrenFlags) == 0)
				return new FileTreeNodeFilterResult(FilterType.Hide, false);

			var defaultValue = new FileTreeNodeFilterResult(FilterType.CheckChildren, false);
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

			return new FileTreeNodeFilterResult(FilterType.Hide, false);
		}

		static bool IsDelegate(TypeDef type) {
			return type.BaseType != null && type.BaseType.FullName == "System.MulticastDelegate" && type.BaseType.DefinitionAssembly.IsCorLib();
		}

		static bool HasInstanceConstructors(TypeDef type) {
			return type.Methods.Any(m => m.IsInstanceConstructor);
		}

		static bool HasMethodBodies(TypeDef type) {
			foreach (var method in type.Methods) {
				if (method.Body != null)
					return true;
			}
			return false;
		}

		static bool HasParamDefs(TypeDef type) {
			return type.Methods.Any(m => m.HasParamDefs);
		}

		static bool HasLocals(TypeDef type) {
			foreach (var method in type.Methods) {
				if (method.Body != null && method.Body.HasVariables)
					return true;
			}
			return false;
		}

		public override FileTreeNodeFilterResult GetResultBody(MethodDef method) {
			bool isMatch = (flags & VisibleMembersFlags.MethodBody) != 0;
			if (!isMatch)
				return new FileTreeNodeFilterResult(FilterType.Hide, isMatch);
			return new FileTreeNodeFilterResult(FilterType.Visible, isMatch);
		}

		public override FileTreeNodeFilterResult GetResultParamDefs(MethodDef method) {
			var visibleFlags = VisibleMembersFlags.ParamDefs | VisibleMembersFlags.ParamDef;
			bool isMatch = (flags & VisibleMembersFlags.ParamDefs) != 0;
			if ((flags & visibleFlags) == 0)
				return new FileTreeNodeFilterResult(FilterType.Hide, isMatch);
			if (isMatch)
				return new FileTreeNodeFilterResult(FilterType.Visible, isMatch);   // Make sure it's not hidden
			return new FileTreeNodeFilterResult(FilterType.CheckChildren, isMatch);
		}

		public override FileTreeNodeFilterResult GetResult(MethodDef method, ParamDef param) {
			bool isMatch = (flags & VisibleMembersFlags.ParamDef) != 0;
			if (!isMatch)
				return new FileTreeNodeFilterResult(FilterType.Hide, isMatch);
			return new FileTreeNodeFilterResult(FilterType.Visible, isMatch);
		}

		public override FileTreeNodeFilterResult GetResultLocals(MethodDef method) {
			var visibleFlags = VisibleMembersFlags.Locals | VisibleMembersFlags.Local;
			bool isMatch = (flags & VisibleMembersFlags.Locals) != 0;
			if ((flags & visibleFlags) == 0)
				return new FileTreeNodeFilterResult(FilterType.Hide, isMatch);
			if (isMatch)
				return new FileTreeNodeFilterResult(FilterType.Visible, isMatch);   // Make sure it's not hidden
			return new FileTreeNodeFilterResult(FilterType.CheckChildren, isMatch);
		}

		public override FileTreeNodeFilterResult GetResult(MethodDef method, Local local) {
			bool isMatch = (flags & VisibleMembersFlags.Local) != 0;
			if (!isMatch)
				return new FileTreeNodeFilterResult(FilterType.Hide, isMatch);
			return new FileTreeNodeFilterResult(FilterType.Visible, isMatch);
		}
	}
}
