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
using System.Linq;
using System.Windows.Media;

using ICSharpCode.Decompiler;
using ICSharpCode.TreeView;
using dnlib.DotNet;

namespace ICSharpCode.ILSpy.TreeNodes
{
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
			typeof(BaseTypesTreeNode),
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
			get { return GetIcon(type); }
		}

		public static ImageSource GetIcon(TypeDef type)
		{
			TypeIcon typeIcon = GetTypeIcon(type);
			AccessOverlayIcon overlayIcon = GetOverlayIcon(type);

			return Images.GetIcon(typeIcon, overlayIcon);
		}

		static TypeIcon GetTypeIcon(TypeDef type)
		{
			if (Decompiler.DnlibExtensions.IsValueType(type)) {
				if (type.IsEnum)
					return TypeIcon.Enum;
				else
					return TypeIcon.Struct;
			} else {
				if (type.IsInterface)
					return TypeIcon.Interface;
				else if (IsDelegate(type))
					return TypeIcon.Delegate;
				else if (IsStaticClass(type))
					return TypeIcon.StaticClass;
				else
					return TypeIcon.Class;
			}
		}

		private static AccessOverlayIcon GetOverlayIcon(TypeDef type)
		{
			AccessOverlayIcon overlay;
			switch (type.Attributes & TypeAttributes.VisibilityMask) {
				case TypeAttributes.Public:
				case TypeAttributes.NestedPublic:
					overlay = AccessOverlayIcon.Public;
					break;
				case TypeAttributes.NotPublic:
				case TypeAttributes.NestedAssembly:
				case TypeAttributes.NestedFamANDAssem:
					overlay = AccessOverlayIcon.Internal;
					break;
				case TypeAttributes.NestedFamily:
				case TypeAttributes.NestedFamORAssem:
					overlay = AccessOverlayIcon.Protected;
					break;
				case TypeAttributes.NestedPrivate:
					overlay = AccessOverlayIcon.Private;
					break;
				default:
					overlay = AccessOverlayIcon.Public;
					break;
			}
			return overlay;
		}

		internal static bool IsDelegate(TypeDef type)
		{
			return type.BaseType != null && type.BaseType.FullName == typeof(MulticastDelegate).FullName && type.BaseType.Module.Assembly.IsCorLib();
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
	}
}
