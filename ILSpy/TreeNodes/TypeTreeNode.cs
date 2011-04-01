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
using ILSpy.Debugger;
using ILSpy.Debugger.AvalonEdit;
using Mono.Cecil;

namespace ICSharpCode.ILSpy.TreeNodes
{
	public sealed class TypeTreeNode : ILSpyTreeNode, IMemberTreeNode
	{
		readonly TypeDefinition type;
		readonly AssemblyTreeNode parentAssemblyNode;
		
		public TypeTreeNode(TypeDefinition type, AssemblyTreeNode parentAssemblyNode)
		{
			if (parentAssemblyNode == null)
				throw new ArgumentNullException("parentAssemblyNode");
			if (type == null)
				throw new ArgumentNullException("type");
			this.type = type;
			this.parentAssemblyNode = parentAssemblyNode;
			this.LazyLoading = true;
		}
		
		public TypeDefinition TypeDefinition {
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
			get { return HighlightSearchMatch(this.Language.TypeToString(type, includeNamespace: false)); }
		}
		
		public bool IsPublicAPI {
			get {
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
		}
		
		public override FilterResult Filter(FilterSettings settings)
		{
			if (!settings.ShowInternalApi && !IsPublicAPI)
				return FilterResult.Hidden;
			if (settings.SearchTermMatches(type.Name)) {
				if (type.IsNested && !settings.Language.ShowMember(type))
					return FilterResult.Hidden;
				else
					return FilterResult.Match;
			} else {
				return FilterResult.Recurse;
			}
		}
		
		protected override void LoadChildren()
		{
			if (type.BaseType != null || type.HasInterfaces)
				this.Children.Add(new BaseTypesTreeNode(type));
			if (!type.IsSealed)
				this.Children.Add(new DerivedTypesTreeNode(parentAssemblyNode.AssemblyList, type));
			foreach (TypeDefinition nestedType in type.NestedTypes.OrderBy(m => m.Name)) {
				this.Children.Add(new TypeTreeNode(nestedType, parentAssemblyNode));
			}
			foreach (FieldDefinition field in type.Fields.OrderBy(m => m.Name)) {
				this.Children.Add(new FieldTreeNode(field));
			}
			
			// figure out the name of the indexer:
			string defaultMemberName = null;
			var defaultMemberAttribute = type.CustomAttributes.FirstOrDefault(
				a => a.AttributeType.FullName == typeof(System.Reflection.DefaultMemberAttribute).FullName);
			if (defaultMemberAttribute != null && defaultMemberAttribute.ConstructorArguments.Count == 1) {
				defaultMemberName = defaultMemberAttribute.ConstructorArguments[0].Value as string;
			}
			
			foreach (PropertyDefinition property in type.Properties.OrderBy(m => m.Name)) {
				this.Children.Add(new PropertyTreeNode(property, property.Name == defaultMemberName));
			}
			foreach (EventDefinition ev in type.Events.OrderBy(m => m.Name)) {
				this.Children.Add(new EventTreeNode(ev));
			}
			HashSet<MethodDefinition> accessorMethods = type.GetAccessorMethods();
			foreach (MethodDefinition method in type.Methods.OrderBy(m => m.Name)) {
				if (!accessorMethods.Contains(method)) {
					this.Children.Add(new MethodTreeNode(method));
				}
			}
		}
		
		public override void Decompile(Language language, ITextOutput output, DecompilationOptions options)
		{
			DebugData.CurrentType = type;
			language.DecompileType(type, output, options);
		}
		
		#region Icon
		enum ClassType
		{
			Class,
			Enum,
			Struct,
			Interface,
			Delegate
		}
		
		static ClassType GetClassType(TypeDefinition type)
		{
			if (type.IsValueType) {
				if (type.IsEnum)
					return ClassType.Enum;
				else
					return ClassType.Struct;
			} else {
				if (type.IsInterface)
					return ClassType.Interface;
				else if (type.BaseType != null && type.BaseType.FullName == typeof(MulticastDelegate).FullName)
					return ClassType.Delegate;
				else
					return ClassType.Class;
			}
		}
		
		public override object Icon {
			get {
				return GetIcon(type);
			}
		}
		
		public static ImageSource GetIcon(TypeDefinition type)
		{
			switch (type.Attributes & TypeAttributes.VisibilityMask) {
				case TypeAttributes.Public:
				case TypeAttributes.NestedPublic:
					switch (GetClassType(type)) {
							case ClassType.Delegate:  return Images.Delegate;
							case ClassType.Enum:      return Images.Enum;
							case ClassType.Interface: return Images.Interface;
							case ClassType.Struct:    return Images.Struct;
							default:                  return Images.Class;
					}
				case TypeAttributes.NotPublic:
				case TypeAttributes.NestedAssembly:
				case TypeAttributes.NestedFamANDAssem:
					switch (GetClassType(type)) {
							case ClassType.Delegate:  return Images.InternalDelegate;
							case ClassType.Enum:      return Images.InternalEnum;
							case ClassType.Interface: return Images.InternalInterface;
							case ClassType.Struct:    return Images.InternalStruct;
							default:                  return Images.InternalClass;
					}
				case TypeAttributes.NestedFamily:
				case TypeAttributes.NestedFamORAssem:
					switch (GetClassType(type)) {
							case ClassType.Delegate:  return Images.ProtectedDelegate;
							case ClassType.Enum:      return Images.ProtectedEnum;
							case ClassType.Interface: return Images.ProtectedInterface;
							case ClassType.Struct:    return Images.ProtectedStruct;
							default:                  return Images.ProtectedClass;
					}
				case TypeAttributes.NestedPrivate:
					switch (GetClassType(type)) {
							case ClassType.Delegate:  return Images.PrivateDelegate;
							case ClassType.Enum:      return Images.PrivateEnum;
							case ClassType.Interface: return Images.PrivateInterface;
							case ClassType.Struct:    return Images.PrivateStruct;
							default:                  return Images.PrivateClass;
					}
				default:
					throw new NotSupportedException();
			}
		}
		#endregion
		
		MemberReference IMemberTreeNode.Member {
			get { return type; }
		}
	}
}
