// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Media;

using ICSharpCode.TreeView;
using Mono.Cecil;

namespace ICSharpCode.ILSpy
{
	sealed class TypeTreeNode : SharpTreeNode
	{
		readonly TypeDefinition type;
		
		public TypeTreeNode(TypeDefinition type)
		{
			if (type == null)
				throw new ArgumentNullException("type");
			this.type = type;
			
			this.LazyLoading = true;
		}
		
		public string Name {
			get { return type.Name; }
		}
		
		public string Namespace {
			get { return type.Namespace; }
		}
		
		public override object Text {
			get { return type.Name; }
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
		
		protected override void LoadChildren()
		{
			this.Children.Add(new BaseTypesTreeNode(type));
			foreach (TypeDefinition nestedType in type.NestedTypes) {
				this.Children.Add(new TypeTreeNode(nestedType));
			}
			foreach (FieldDefinition field in type.Fields) {
				this.Children.Add(new FieldTreeNode(field));
			}
			foreach (MethodDefinition method in type.Methods) {
				this.Children.Add(new MethodTreeNode(method));
			}
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
	}
}
