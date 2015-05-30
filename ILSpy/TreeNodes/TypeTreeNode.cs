// Copyright (c) 2011 AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Media;

using ICSharpCode.Decompiler;
using ICSharpCode.TreeView;
using dnlib.DotNet;

namespace ICSharpCode.ILSpy.TreeNodes
{
	enum MemberAccess
	{
		Public,
		Private,
		Protected,
		Internal,
		CompilerControlled,
		ProtectedInternal,
	}

	public sealed class TypeTreeNode : ILSpyTreeNode, IMemberTreeNode
	{
		readonly TypeDef type;
		readonly AssemblyTreeNode parentAssemblyNode;
		
		public TypeTreeNode(TypeDef type, AssemblyTreeNode parentAssemblyNode)
		{
			if (parentAssemblyNode == null)
				throw new ArgumentNullException("parentAssemblyNode");
			if (type == null)
				throw new ArgumentNullException("type");
			this.type = type;
			this.parentAssemblyNode = parentAssemblyNode;
			this.LazyLoading = true;
		}
		
		public TypeDef TypeDefinition {
			get { return type; }
		}
		
		public AssemblyTreeNode ParentAssemblyNode {
			get { return parentAssemblyNode; }
		}
		
		public string Name {
			get { return type.Name; }
		}
		
		public string Namespace {
			get { return type.Namespace; }
		}
		
		public override object Text {
			get { return ToString(Language); }
		}

		public override string ToString(Language language)
		{
			return CleanUpName(language.FormatTypeName(type)) + type.MDToken.ToSuffixString();
		}
		
		public override bool IsPublicAPI {
			get { return IsPublicAPIInternal(type); }
		}

		internal static bool IsPublicAPIInternal(TypeDef type)
		{
			switch (type.Attributes & TypeAttributes.VisibilityMask) {
			case TypeAttributes.Public:
			case TypeAttributes.NestedPublic:
			case TypeAttributes.NestedFamily:
			case TypeAttributes.NestedFamORAssem:
				return true;
			default:
				return false;
			}
		}
		
		public override FilterResult Filter(FilterSettings settings)
		{
			var res = settings.Filter.GetFilterResult(this.TypeDefinition);
			if (res.FilterResult != null)
				return res.FilterResult.Value;
			if (settings.SearchTermMatches(type.Name)) {
				if (settings.Language.ShowMember(type))
					return FilterResult.Match;
				else
					return FilterResult.Hidden;
			} else {
				return FilterResult.Recurse;
			}
		}
		
		protected override Type[] ChildTypeOrder {
			get { return typeOrder; }
		}

		static readonly Type[] typeOrder = new Type[] {
			typeof(BaseTypesTreeNode),	// InvalidateInterfacesNode() assumes this is first
			typeof(DerivedTypesTreeNode),
			typeof(TypeTreeNode),
			typeof(FieldTreeNode),
			typeof(PropertyTreeNode),
			typeof(EventTreeNode),
			typeof(MethodTreeNode),
		};
		protected override void LoadChildren()
		{
			// Make sure the order below matches typeOrder above
			this.Children.Add(new BaseTypesTreeNode(type));
			this.Children.Add(new DerivedTypesTreeNode(parentAssemblyNode.AssemblyList, type));
			foreach (TypeDef nestedType in type.NestedTypes.OrderBy(m => m.Name.String, NestedTypeStringComparer)) {
				this.Children.Add(new TypeTreeNode(nestedType, parentAssemblyNode));
			}
			foreach (FieldDef field in type.Fields.OrderBy(m => m.Name.String, FieldStringComparer)) {
				this.Children.Add(new FieldTreeNode(field));
			}
			
			foreach (PropertyDef property in type.Properties.OrderBy(m => m.Name.String, PropertyStringComparer)) {
				this.Children.Add(new PropertyTreeNode(property));
			}
			foreach (EventDef ev in type.Events.OrderBy(m => m.Name.String, EventStringComparer)) {
				this.Children.Add(new EventTreeNode(ev));
			}
			HashSet<MethodDef> accessorMethods = type.GetAccessorMethods();
			foreach (MethodDef method in type.Methods.OrderBy(m => m.Name.String, MethodStringComparer)) {
				if (!accessorMethods.Contains(method)) {
					this.Children.Add(new MethodTreeNode(method));
				}
			}
		}
		static readonly StringComparer NestedTypeStringComparer = StringComparer.OrdinalIgnoreCase;
		static readonly StringComparer FieldStringComparer = StringComparer.OrdinalIgnoreCase;
		static readonly StringComparer PropertyStringComparer = StringComparer.OrdinalIgnoreCase;
		static readonly StringComparer EventStringComparer = StringComparer.OrdinalIgnoreCase;
		static readonly StringComparer MethodStringComparer = StringComparer.OrdinalIgnoreCase;

		protected override int GetNewChildIndex(SharpTreeNode node)
		{
			if (node is TypeTreeNode)
				return GetNewChildIndex(node, NestedTypeStringComparer, n => ((TypeTreeNode)n).TypeDefinition.Name);
			if (node is FieldTreeNode)
				return GetNewChildIndex(node, FieldStringComparer, n => ((FieldTreeNode)n).FieldDefinition.Name);
			if (node is PropertyTreeNode)
				return GetNewChildIndex(node, PropertyStringComparer, n => ((PropertyTreeNode)n).PropertyDefinition.Name);
			if (node is EventTreeNode)
				return GetNewChildIndex(node, EventStringComparer, n => ((EventTreeNode)n).EventDefinition.Name);
			if (node is MethodTreeNode)
				return GetNewChildIndex(node, MethodStringComparer, n => ((MethodTreeNode)n).MethodDefinition.Name);
			return base.GetNewChildIndex(node);
		}
		
		public override void Decompile(Language language, ITextOutput output, DecompilationOptions options)
		{
			language.DecompileType(type, output, options);
		}

		#region Icon
		public override object Icon
		{
			get { return GetIcon(type, BackgroundType.TreeNode); }
		}

		public static ImageSource GetIcon(TypeDef type, BackgroundType bgType)
		{
			return GetIcon(GetTypeIcon(type), bgType);
		}

		internal static ImageSource GetIcon(TypeIcon typeIcon, BackgroundType bgType)
		{
			return ImageCache.Instance.GetImage(GetImageInfo(typeIcon, bgType));
		}

		internal static ImageInfo GetImageInfo(TypeDef type, BackgroundType bgType)
		{
			return GetImageInfo(GetTypeIcon(type), bgType);
		}

		internal static ImageInfo GetImageInfo(TypeIcon typeIcon, BackgroundType bgType)
		{
			switch (typeIcon) {
			case TypeIcon.StaticClass:				return new ImageInfo("StaticClass", bgType);
			case TypeIcon.Class:					return new ImageInfo("Class", bgType);
			case TypeIcon.ClassPrivate:				return new ImageInfo("ClassPrivate", bgType);
			case TypeIcon.ClassProtected:			return new ImageInfo("ClassProtected", bgType);
			case TypeIcon.ClassInternal:			return new ImageInfo("ClassInternal", bgType);
			case TypeIcon.ClassProtectedInternal:	return new ImageInfo("ClassProtectedInternal", bgType);
			case TypeIcon.Enum:						return new ImageInfo("Enum", bgType);
			case TypeIcon.EnumPrivate:				return new ImageInfo("EnumPrivate", bgType);
			case TypeIcon.EnumProtected:			return new ImageInfo("EnumProtected", bgType);
			case TypeIcon.EnumInternal:				return new ImageInfo("EnumInternal", bgType);
			case TypeIcon.EnumProtectedInternal:	return new ImageInfo("EnumProtectedInternal", bgType);
			case TypeIcon.Struct:					return new ImageInfo("Struct", bgType);
			case TypeIcon.StructPrivate:			return new ImageInfo("StructPrivate", bgType);
			case TypeIcon.StructProtected:			return new ImageInfo("StructProtected", bgType);
			case TypeIcon.StructInternal:			return new ImageInfo("StructInternal", bgType);
			case TypeIcon.StructProtectedInternal:	return new ImageInfo("StructProtectedInternal", bgType);
			case TypeIcon.Interface:				return new ImageInfo("Interface", bgType);
			case TypeIcon.InterfacePrivate:			return new ImageInfo("InterfacePrivate", bgType);
			case TypeIcon.InterfaceProtected:		return new ImageInfo("InterfaceProtected", bgType);
			case TypeIcon.InterfaceInternal:		return new ImageInfo("InterfaceInternal", bgType);
			case TypeIcon.InterfaceProtectedInternal:return new ImageInfo("InterfaceProtectedInternal", bgType);
			case TypeIcon.Delegate:					return new ImageInfo("Delegate", bgType);
			case TypeIcon.DelegatePrivate:			return new ImageInfo("DelegatePrivate", bgType);
			case TypeIcon.DelegateProtected:		return new ImageInfo("DelegateProtected", bgType);
			case TypeIcon.DelegateInternal:			return new ImageInfo("DelegateInternal", bgType);
			case TypeIcon.DelegateProtectedInternal:return new ImageInfo("DelegateProtectedInternal", bgType);
			case TypeIcon.Exception:				return new ImageInfo("Exception", bgType);
			case TypeIcon.ExceptionPrivate:			return new ImageInfo("ExceptionPrivate", bgType);
			case TypeIcon.ExceptionProtected:		return new ImageInfo("ExceptionProtected", bgType);
			case TypeIcon.ExceptionInternal:		return new ImageInfo("ExceptionInternal", bgType);
			case TypeIcon.ExceptionProtectedInternal:return new ImageInfo("ExceptionProtectedInternal", bgType);
			case TypeIcon.Generic:					return new ImageInfo("Generic", bgType);
			case TypeIcon.GenericPrivate:			return new ImageInfo("GenericPrivate", bgType);
			case TypeIcon.GenericProtected:			return new ImageInfo("GenericProtected", bgType);
			case TypeIcon.GenericInternal:			return new ImageInfo("GenericInternal", bgType);
			case TypeIcon.GenericProtectedInternal:	return new ImageInfo("GenericProtectedInternal", bgType);
			default:
				Debug.Fail("Unknown type");
				goto case TypeIcon.Class;
			}
		}

		static TypeIcon GetTypeIcon(TypeDef type)
		{
			var memType = GetMemberAccess(type);
			if (Decompiler.DnlibExtensions.IsValueType(type)) {
				if (type.IsEnum) {
					switch (memType) {
					case MemberAccess.Public: return TypeIcon.Enum;
					case MemberAccess.Private: return TypeIcon.EnumPrivate;
					case MemberAccess.Protected: return TypeIcon.EnumProtected;
					case MemberAccess.Internal: return TypeIcon.EnumInternal;
					case MemberAccess.ProtectedInternal: return TypeIcon.EnumProtectedInternal;
					default:
						Debug.Fail("Invalid MemberAccess");
						goto case MemberAccess.Public;
					}
				}
				else {
					switch (memType) {
					case MemberAccess.Public: return TypeIcon.Struct;
					case MemberAccess.Private: return TypeIcon.StructPrivate;
					case MemberAccess.Protected: return TypeIcon.StructProtected;
					case MemberAccess.Internal: return TypeIcon.StructInternal;
					case MemberAccess.ProtectedInternal: return TypeIcon.StructProtectedInternal;
					default:
						Debug.Fail("Invalid MemberAccess");
						goto case MemberAccess.Public;
					}
				}
			}
			else {
				if (type.IsInterface) {
					switch (memType) {
					case MemberAccess.Public: return TypeIcon.Interface;
					case MemberAccess.Private: return TypeIcon.InterfacePrivate;
					case MemberAccess.Protected: return TypeIcon.InterfaceProtected;
					case MemberAccess.Internal: return TypeIcon.InterfaceInternal;
					case MemberAccess.ProtectedInternal: return TypeIcon.InterfaceProtectedInternal;
					default:
						Debug.Fail("Invalid MemberAccess");
						goto case MemberAccess.Public;
					}
				}
				else if (IsDelegate(type)) {
					switch (memType) {
					case MemberAccess.Public: return TypeIcon.Delegate;
					case MemberAccess.Private: return TypeIcon.DelegatePrivate;
					case MemberAccess.Protected: return TypeIcon.DelegateProtected;
					case MemberAccess.Internal: return TypeIcon.DelegateInternal;
					case MemberAccess.ProtectedInternal: return TypeIcon.DelegateProtectedInternal;
					default:
						Debug.Fail("Invalid MemberAccess");
						goto case MemberAccess.Public;
					}
				}
				else if (IsException(type)) {
					switch (memType) {
					case MemberAccess.Public: return TypeIcon.Exception;
					case MemberAccess.Private: return TypeIcon.ExceptionPrivate;
					case MemberAccess.Protected: return TypeIcon.ExceptionProtected;
					case MemberAccess.Internal: return TypeIcon.ExceptionInternal;
					case MemberAccess.ProtectedInternal: return TypeIcon.ExceptionProtectedInternal;
					default:
						Debug.Fail("Invalid MemberAccess");
						goto case MemberAccess.Public;
					}
				}
				else if (type.GenericParameters.Count > 0) {
					switch (memType) {
					case MemberAccess.Public: return TypeIcon.Generic;
					case MemberAccess.Private: return TypeIcon.GenericPrivate;
					case MemberAccess.Protected: return TypeIcon.GenericProtected;
					case MemberAccess.Internal: return TypeIcon.GenericInternal;
					case MemberAccess.ProtectedInternal: return TypeIcon.GenericProtectedInternal;
					default:
						Debug.Fail("Invalid MemberAccess");
						goto case MemberAccess.Public;
					}
				}
				else if (IsStaticClass(type))
					return TypeIcon.StaticClass;
				else {
					switch (memType) {
					case MemberAccess.Public: return TypeIcon.Class;
					case MemberAccess.Private: return TypeIcon.ClassPrivate;
					case MemberAccess.Protected: return TypeIcon.ClassProtected;
					case MemberAccess.Internal: return TypeIcon.ClassInternal;
					case MemberAccess.ProtectedInternal: return TypeIcon.ClassProtectedInternal;
					default:
						Debug.Fail("Invalid MemberAccess");
						goto case MemberAccess.Public;
					}
				}
			}
		}

		static MemberAccess GetMemberAccess(TypeDef type)
		{
			switch (type.Visibility) {
			case TypeAttributes.Public:
			case TypeAttributes.NestedPublic:
				return MemberAccess.Public;
			case TypeAttributes.NotPublic:
			case TypeAttributes.NestedAssembly:
			case TypeAttributes.NestedFamANDAssem:
				return MemberAccess.Internal;
			case TypeAttributes.NestedFamily:
				return MemberAccess.Protected;
			case TypeAttributes.NestedFamORAssem:
				return MemberAccess.ProtectedInternal;
			case TypeAttributes.NestedPrivate:
				return MemberAccess.Private;
			default:
				return MemberAccess.Public;
			}
		}

		internal static bool IsDelegate(TypeDef type)
		{
			return type.BaseType != null && type.BaseType.FullName == typeof(MulticastDelegate).FullName && type.BaseType.DefinitionAssembly.IsCorLib();
		}

		static bool IsException(TypeDef type)
		{
			if (IsSystemException(type))
				return true;
			while (type != null) {
				if (IsSystemException(type.BaseType))
					return true;
				type = type.BaseType.Resolve();
			}
			return false;
		}

		static bool IsSystemException(ITypeDefOrRef type)
		{
			return type != null &&
				type.DeclaringType == null &&
				type.Namespace == "System" &&
				type.Name == "Exception" &&
				type.DefinitionAssembly.IsCorLib();
		}

		private static bool IsStaticClass(TypeDef type)
		{
			return type.IsSealed && type.IsAbstract;
		}

		#endregion
		
		IMemberRef IMemberTreeNode.Member {
			get { return type; }
		}

		IMDTokenProvider ITokenTreeNode.MDTokenProvider {
			get { return type; }
		}

		public override NodePathName NodePathName {
			get { return new NodePathName("type", type.Namespace + "." + type.Name); }
		}

		AssemblyTreeNode GetAssemblyIfNonNestedType()
		{
			var nsNode = Parent as NamespaceTreeNode;
			if (nsNode == null)
				return null;
			return nsNode.Parent as AssemblyTreeNode;
		}

		internal void OnBeforeRemoved()
		{
			var asmNode = GetAssemblyIfNonNestedType();
			if (asmNode != null)
				asmNode.OnRemoved(this);
		}

		internal void OnReadded()
		{
			var asmNode = GetAssemblyIfNonNestedType();
			if (asmNode != null)
				asmNode.OnReadded(this);
		}

		internal void InvalidateInterfacesNode()
		{
			if (Children.Count == 0)
				return;
			Debug.Assert(Children[0] is BaseTypesTreeNode);
			if (!(Children[0] is BaseTypesTreeNode))
				throw new InvalidOperationException();
			Children[0] = new BaseTypesTreeNode(type);
		}
	}
}
