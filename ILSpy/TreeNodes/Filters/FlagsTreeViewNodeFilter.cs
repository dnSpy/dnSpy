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

using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using ICSharpCode.ILSpy;
using ICSharpCode.ILSpy.TreeNodes;

namespace dnSpy.Search {
	sealed class FlagsTreeViewNodeFilter : TreeViewNodeFilterBase {
		readonly VisibleMembersFlags flags;

		public override string Text {
			get { return flags.GetListString(); }
		}

		public FlagsTreeViewNodeFilter(VisibleMembersFlags flags) {
			this.flags = flags;
		}

		public override TreeViewNodeFilterResult GetFilterResult(AssemblyRef asmRef) {
			bool isMatch = (flags & VisibleMembersFlags.AssemblyRef) != 0;
			if (!isMatch)
				return new TreeViewNodeFilterResult(FilterResult.Hidden, isMatch);
			return new TreeViewNodeFilterResult(FilterResult.Match, isMatch);
		}

		public override TreeViewNodeFilterResult GetFilterResult(LoadedAssembly asm, AssemblyFilterType type) {
			VisibleMembersFlags thisFlag, visibleFlags;
			switch (type) {
			case AssemblyFilterType.Assembly:
				thisFlag = VisibleMembersFlags.AssemblyDef;
				visibleFlags = thisFlag | VisibleMembersFlags.ModuleDef |
						VisibleMembersFlags.Namespace | VisibleMembersFlags.AnyTypeDef |
						VisibleMembersFlags.FieldDef | VisibleMembersFlags.MethodDef |
						VisibleMembersFlags.InstanceConstructor | VisibleMembersFlags.PropertyDef |
						VisibleMembersFlags.EventDef | VisibleMembersFlags.AssemblyRef |
						VisibleMembersFlags.BaseTypes | VisibleMembersFlags.DerivedTypes |
						VisibleMembersFlags.ModuleRef | VisibleMembersFlags.ResourceList |
						VisibleMembersFlags.MethodBody | VisibleMembersFlags.ParamDefs |
						VisibleMembersFlags.ParamDef | VisibleMembersFlags.Locals |
						VisibleMembersFlags.Local | VisibleMembersFlags.Resource |
						VisibleMembersFlags.ResourceElement;
				break;

			case AssemblyFilterType.NetModule:
				thisFlag = VisibleMembersFlags.ModuleDef;
				visibleFlags = thisFlag |
						VisibleMembersFlags.Namespace | VisibleMembersFlags.AnyTypeDef |
						VisibleMembersFlags.FieldDef | VisibleMembersFlags.MethodDef |
						VisibleMembersFlags.InstanceConstructor | VisibleMembersFlags.PropertyDef |
						VisibleMembersFlags.EventDef | VisibleMembersFlags.AssemblyRef |
						VisibleMembersFlags.BaseTypes | VisibleMembersFlags.DerivedTypes |
						VisibleMembersFlags.ModuleRef | VisibleMembersFlags.ResourceList |
						VisibleMembersFlags.MethodBody | VisibleMembersFlags.ParamDefs |
						VisibleMembersFlags.ParamDef | VisibleMembersFlags.Locals |
						VisibleMembersFlags.Local | VisibleMembersFlags.Resource |
						VisibleMembersFlags.ResourceElement;
				break;

			case AssemblyFilterType.NonNetFile:
			default:
				thisFlag = VisibleMembersFlags.NonNetFile;
				visibleFlags = thisFlag;
				break;
			}
			bool isMatch = (flags & thisFlag) != 0;
			if ((flags & visibleFlags) == 0)
				return new TreeViewNodeFilterResult(FilterResult.Hidden, isMatch);

			if (isMatch)
				return new TreeViewNodeFilterResult(FilterResult.Match, isMatch);   // Make sure it's not hidden

			return new TreeViewNodeFilterResult(null, isMatch);
		}

		public override TreeViewNodeFilterResult GetFilterResult(BaseTypesTreeNode node) {
			bool isMatch = (flags & VisibleMembersFlags.BaseTypes) != 0;
			if (!isMatch)
				return new TreeViewNodeFilterResult(FilterResult.Hidden, isMatch);
			return new TreeViewNodeFilterResult(FilterResult.Match, isMatch);
		}

		public override TreeViewNodeFilterResult GetFilterResult(DerivedTypesTreeNode node) {
			bool isMatch = (flags & VisibleMembersFlags.DerivedTypes) != 0;
			if (!isMatch)
				return new TreeViewNodeFilterResult(FilterResult.Hidden, isMatch);
			return new TreeViewNodeFilterResult(FilterResult.Match, isMatch);
		}

		public override TreeViewNodeFilterResult GetFilterResult(EventDef evt) {
			var visibleFlags = VisibleMembersFlags.EventDef | VisibleMembersFlags.MethodDef |
								VisibleMembersFlags.MethodBody | VisibleMembersFlags.ParamDefs |
								VisibleMembersFlags.ParamDef | VisibleMembersFlags.Locals |
								VisibleMembersFlags.Local;
			bool isMatch = (flags & VisibleMembersFlags.EventDef) != 0;
			if ((flags & visibleFlags) == 0)
				return new TreeViewNodeFilterResult(FilterResult.Hidden, isMatch);
			if (isMatch)
				return new TreeViewNodeFilterResult(FilterResult.Match, isMatch);   // Make sure it's not hidden
			return new TreeViewNodeFilterResult(FilterResult.Recurse, isMatch);
		}

		public override TreeViewNodeFilterResult GetFilterResult(FieldDef field) {
			bool isMatch = (flags & VisibleMembersFlags.FieldDef) != 0;
			if (!isMatch)
				return new TreeViewNodeFilterResult(FilterResult.Hidden, isMatch);
			return new TreeViewNodeFilterResult(FilterResult.Match, isMatch);
		}

		public override TreeViewNodeFilterResult GetFilterResult(MethodDef method) {
			var childrenFlags = VisibleMembersFlags.MethodBody | VisibleMembersFlags.ParamDefs |
								VisibleMembersFlags.ParamDef | VisibleMembersFlags.Locals |
								VisibleMembersFlags.Local;
			var visibleFlags = childrenFlags | VisibleMembersFlags.MethodDef | VisibleMembersFlags.InstanceConstructor;
			bool isMatch = (flags & VisibleMembersFlags.MethodDef) != 0 ||
							(method.IsInstanceConstructor && (flags & VisibleMembersFlags.InstanceConstructor) != 0);
			if ((flags & visibleFlags) == 0)
				return new TreeViewNodeFilterResult(FilterResult.Hidden, false);
			if (isMatch)
				return new TreeViewNodeFilterResult(FilterResult.Match, isMatch);
			if ((flags & childrenFlags) == 0)
				return new TreeViewNodeFilterResult(FilterResult.Hidden, isMatch);
			return new TreeViewNodeFilterResult(FilterResult.Recurse, isMatch);
		}

		public override TreeViewNodeFilterResult GetFilterResult(ModuleRef modRef) {
			bool isMatch = (flags & VisibleMembersFlags.ModuleRef) != 0;
			if (!isMatch)
				return new TreeViewNodeFilterResult(FilterResult.Hidden, isMatch);
			return new TreeViewNodeFilterResult(FilterResult.Match, isMatch);
		}

		public override TreeViewNodeFilterResult GetFilterResult(string ns, LoadedAssembly owner) {
			var visibleFlags = VisibleMembersFlags.Namespace | VisibleMembersFlags.AnyTypeDef |
					VisibleMembersFlags.FieldDef | VisibleMembersFlags.MethodDef |
					VisibleMembersFlags.InstanceConstructor | VisibleMembersFlags.PropertyDef |
					VisibleMembersFlags.EventDef | VisibleMembersFlags.BaseTypes |
					VisibleMembersFlags.DerivedTypes | VisibleMembersFlags.MethodBody |
					VisibleMembersFlags.ParamDefs | VisibleMembersFlags.ParamDef |
					VisibleMembersFlags.Locals | VisibleMembersFlags.Local;
			bool isMatch = (flags & VisibleMembersFlags.Namespace) != 0;
			if ((flags & visibleFlags) == 0)
				return new TreeViewNodeFilterResult(FilterResult.Hidden, isMatch);
			if (isMatch)
				return new TreeViewNodeFilterResult(FilterResult.Match, isMatch);   // Make sure it's not hidden
			return new TreeViewNodeFilterResult(FilterResult.Recurse, isMatch);
		}

		public override TreeViewNodeFilterResult GetFilterResult(PropertyDef prop) {
			var visibleFlags = VisibleMembersFlags.PropertyDef | VisibleMembersFlags.MethodDef |
								VisibleMembersFlags.MethodBody | VisibleMembersFlags.ParamDefs |
								VisibleMembersFlags.ParamDef | VisibleMembersFlags.Locals |
								VisibleMembersFlags.Local;
			bool isMatch = (flags & VisibleMembersFlags.PropertyDef) != 0;
			if ((flags & visibleFlags) == 0)
				return new TreeViewNodeFilterResult(FilterResult.Hidden, isMatch);
			if (isMatch)
				return new TreeViewNodeFilterResult(FilterResult.Match, isMatch);   // Make sure it's not hidden
			return new TreeViewNodeFilterResult(FilterResult.Recurse, isMatch);
		}

		public override TreeViewNodeFilterResult GetFilterResult(ReferenceFolderTreeNode node) {
			var visibleFlags = VisibleMembersFlags.AssemblyRef | VisibleMembersFlags.ModuleRef;
			const bool isMatch = false;
			if ((flags & visibleFlags) == 0)
				return new TreeViewNodeFilterResult(FilterResult.Hidden, isMatch);
			return new TreeViewNodeFilterResult(null, isMatch);
		}

		public override TreeViewNodeFilterResult GetFilterResult(ResourceListTreeNode node) {
			var visibleFlags = VisibleMembersFlags.Resource | VisibleMembersFlags.ResourceElement;
			bool isMatch = (flags & VisibleMembersFlags.ResourceList) != 0;
			if ((flags & visibleFlags) == 0)
				return new TreeViewNodeFilterResult(FilterResult.Hidden, isMatch);
			if (isMatch)
				return new TreeViewNodeFilterResult(FilterResult.Match, isMatch);
			return new TreeViewNodeFilterResult(FilterResult.Recurse, isMatch);
		}

		public override TreeViewNodeFilterResult GetFilterResult(ResourceTreeNode node) {
			var visibleFlags = VisibleMembersFlags.ResourceElement;
			bool isMatch = (flags & VisibleMembersFlags.Resource) != 0;
			if ((flags & visibleFlags) == 0)
				return new TreeViewNodeFilterResult(FilterResult.Hidden, isMatch);
			if (isMatch)
				return new TreeViewNodeFilterResult(FilterResult.Match, isMatch);
			return new TreeViewNodeFilterResult(FilterResult.Recurse, isMatch);
		}

		public override TreeViewNodeFilterResult GetFilterResult(ResourceElementTreeNode node) {
			bool isMatch = (flags & VisibleMembersFlags.ResourceElement) != 0;
			if (!isMatch)
				return new TreeViewNodeFilterResult(FilterResult.Hidden, isMatch);
			return new TreeViewNodeFilterResult(FilterResult.Match, isMatch);
		}

		public override TreeViewNodeFilterResult GetFilterResult(TypeDef type) {
			var childrenFlags = VisibleMembersFlags.AnyTypeDef | VisibleMembersFlags.FieldDef |
					VisibleMembersFlags.MethodDef | VisibleMembersFlags.InstanceConstructor |
					VisibleMembersFlags.PropertyDef | VisibleMembersFlags.EventDef |
					VisibleMembersFlags.BaseTypes | VisibleMembersFlags.DerivedTypes |
					VisibleMembersFlags.MethodBody | VisibleMembersFlags.ParamDefs |
					VisibleMembersFlags.ParamDef | VisibleMembersFlags.Locals |
					VisibleMembersFlags.Local;
			var visibleFlags = VisibleMembersFlags.AnyTypeDef | childrenFlags;
			if ((flags & visibleFlags) == 0)
				return new TreeViewNodeFilterResult(FilterResult.Hidden, false);
			if ((flags & VisibleMembersFlags.GenericTypeDef) != 0 && type.GenericParameters.Count > 0)
				return new TreeViewNodeFilterResult(FilterResult.Match, true);
			else if ((flags & VisibleMembersFlags.NonGenericTypeDef) != 0 && type.GenericParameters.Count == 0)
				return new TreeViewNodeFilterResult(FilterResult.Match, true);
			else if ((flags & VisibleMembersFlags.EnumTypeDef) != 0 && type.IsEnum)
				return new TreeViewNodeFilterResult(FilterResult.Match, true);
			else if ((flags & VisibleMembersFlags.InterfaceTypeDef) != 0 && type.IsInterface)
				return new TreeViewNodeFilterResult(FilterResult.Match, true);
			else if ((flags & VisibleMembersFlags.ClassTypeDef) != 0 && !type.IsValueType && !type.IsInterface)
				return new TreeViewNodeFilterResult(FilterResult.Match, true);
			else if ((flags & VisibleMembersFlags.StructTypeDef) != 0 && type.IsValueType && !type.IsEnum)
				return new TreeViewNodeFilterResult(FilterResult.Match, true);
			else if ((flags & VisibleMembersFlags.DelegateTypeDef) != 0 && TypeTreeNode.IsDelegate(type))
				return new TreeViewNodeFilterResult(FilterResult.Match, true);
			else if ((flags & VisibleMembersFlags.TypeDef) != 0)
				return new TreeViewNodeFilterResult(FilterResult.Match, true);
			else if ((flags & childrenFlags) == 0)
				return new TreeViewNodeFilterResult(FilterResult.Hidden, false);

			var defaultValue = new TreeViewNodeFilterResult(FilterResult.Recurse, false);
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

			return new TreeViewNodeFilterResult(FilterResult.Hidden, false);
		}

		static bool HasInstanceConstructors(TypeDef type) {
			return type.Methods.Any(m => m.IsInstanceConstructor);
		}

		static bool HasMethodBodies(TypeDef type) {
			foreach (var method in type.Methods) {
				bool hasBody = method.Body != null;
				ICSharpCode.ILSpy.TreeNodes.Analyzer.Helpers.FreeMethodBody(method);
				if (hasBody)
					return true;
			}
			return false;
		}

		static bool HasParamDefs(TypeDef type) {
			return type.Methods.Any(m => m.HasParamDefs);
		}

		static bool HasLocals(TypeDef type) {
			foreach (var method in type.Methods) {
				bool hasLocal = method.Body != null && method.Body.HasVariables;
				ICSharpCode.ILSpy.TreeNodes.Analyzer.Helpers.FreeMethodBody(method);
				if (hasLocal)
					return true;
			}
			return false;
		}

		public override TreeViewNodeFilterResult GetFilterResultBody(MethodDef method) {
			bool isMatch = (flags & VisibleMembersFlags.MethodBody) != 0;
			if (!isMatch)
				return new TreeViewNodeFilterResult(FilterResult.Hidden, isMatch);
			return new TreeViewNodeFilterResult(FilterResult.Match, isMatch);
		}

		public override TreeViewNodeFilterResult GetFilterResultParamDefs(MethodDef method) {
			var visibleFlags = VisibleMembersFlags.ParamDefs | VisibleMembersFlags.ParamDef;
			bool isMatch = (flags & VisibleMembersFlags.ParamDefs) != 0;
			if ((flags & visibleFlags) == 0)
				return new TreeViewNodeFilterResult(FilterResult.Hidden, isMatch);
			if (isMatch)
				return new TreeViewNodeFilterResult(FilterResult.Match, isMatch);   // Make sure it's not hidden
			return new TreeViewNodeFilterResult(FilterResult.Recurse, isMatch);
		}

		public override TreeViewNodeFilterResult GetFilterResult(MethodDef method, ParamDef param) {
			bool isMatch = (flags & VisibleMembersFlags.ParamDef) != 0;
			if (!isMatch)
				return new TreeViewNodeFilterResult(FilterResult.Hidden, isMatch);
			return new TreeViewNodeFilterResult(FilterResult.Match, isMatch);
		}

		public override TreeViewNodeFilterResult GetFilterResultLocals(MethodDef method) {
			var visibleFlags = VisibleMembersFlags.Locals | VisibleMembersFlags.Local;
			bool isMatch = (flags & VisibleMembersFlags.Locals) != 0;
			if ((flags & visibleFlags) == 0)
				return new TreeViewNodeFilterResult(FilterResult.Hidden, isMatch);
			if (isMatch)
				return new TreeViewNodeFilterResult(FilterResult.Match, isMatch);   // Make sure it's not hidden
			return new TreeViewNodeFilterResult(FilterResult.Recurse, isMatch);
		}

		public override TreeViewNodeFilterResult GetFilterResult(MethodDef method, Local local) {
			bool isMatch = (flags & VisibleMembersFlags.Local) != 0;
			if (!isMatch)
				return new TreeViewNodeFilterResult(FilterResult.Hidden, isMatch);
			return new TreeViewNodeFilterResult(FilterResult.Match, isMatch);
		}
	}
}
