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
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

using ICSharpCode.Decompiler;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.TypeSystem;
using Mono.Cecil;

namespace ICSharpCode.ILSpy.Bookmarks
{
	/// <summary>
	/// Bookmark used to give additional operations for class members.
	/// Does not derive from SDBookmark because it is not stored in the central BookmarkManager,
	/// but only in the document's BookmarkManager.
	/// </summary>
	public class MemberBookmark : IBookmark
	{
		MemberReference member;
		
		public MemberReference Member {
			get {
				return member;
			}
		}
		
		public MemberBookmark(MemberReference member, int line)
		{
			this.member = member;
			LineNumber = line;
		}
		
		public virtual ImageSource Image {
			get {
				if (member is FieldDefinition)
					return GetOverlayedImage(member as FieldDefinition, MemberIcon.Field);
				
				if (member is PropertyDefinition)
					return GetOverlayedImage(member as PropertyDefinition, MemberIcon.Property);
				
				if (member is EventDefinition)
					return GetOverlayedImage(member as EventDefinition, MemberIcon.Event);
				
				if (member is MethodDefinition)
					return GetOverlayedImage(member as MethodDefinition, MemberIcon.Method);
				
				if (member is TypeDefinition)
					return GetOverlayedImage(member as TypeDefinition);
				
				return null;
			}
		}
		
		public int LineNumber {
			get; private set;
		}
		
		public virtual void MouseDown(MouseButtonEventArgs e)
		{
		}
		
		public virtual void MouseUp(MouseButtonEventArgs e)
		{
		}
		
		int IBookmark.ZOrder {
			get { return -10; }
		}
		
		bool IBookmark.CanDragDrop {
			get { return false; }
		}
		
		void IBookmark.Drop(int lineNumber)
		{
			throw new NotSupportedException();
		}
		
		#region Overlayed images
		
		internal ImageSource GetOverlayedImage(TypeDefinition typeDef)
		{
			TypeIcon icon = TypeIcon.Class;
			if (typeDef.IsEnum)
				icon = TypeIcon.Enum;			
			if (typeDef.IsValueType)
				icon = TypeIcon.Struct;			
			if (typeDef.IsInterface)
				icon = TypeIcon.Interface;			
			if (typeDef.BaseType.FullName == "System.MulticastDelegate" || typeDef.BaseType.FullName == "System.Delegate")
				icon = TypeIcon.Delegate;
			
			bool isStatic = false;
			AccessOverlayIcon overlayIcon = AccessOverlayIcon.Private;
			
			if (typeDef.IsNestedPrivate)
				overlayIcon = AccessOverlayIcon.Public;
			else if (typeDef.IsNestedAssembly || typeDef.IsNestedFamilyAndAssembly || typeDef.IsNotPublic)
				overlayIcon = AccessOverlayIcon.Internal;
			else if (typeDef.IsNestedFamily)
				overlayIcon = AccessOverlayIcon.Protected;
			else if (typeDef.IsNestedFamilyOrAssembly)
				overlayIcon = AccessOverlayIcon.ProtectedInternal;
			else if (typeDef.IsPublic || typeDef.IsNestedPublic)
				overlayIcon = AccessOverlayIcon.Public;
			
			if (typeDef.IsAbstract && typeDef.IsSealed)
				isStatic = true;
			
			return Images.GetIcon(icon, overlayIcon, isStatic);
		}
		
		ImageSource GetOverlayedImage(FieldDefinition fieldDef, MemberIcon icon)
		{
			bool isStatic = false;
			AccessOverlayIcon overlayIcon = AccessOverlayIcon.Public;
			
			if (fieldDef.IsPrivate)
				overlayIcon = AccessOverlayIcon.Private;
			else if (fieldDef.IsAssembly || fieldDef.IsFamilyAndAssembly)
				overlayIcon = AccessOverlayIcon.Internal;
			else if (fieldDef.IsFamily)
				overlayIcon = AccessOverlayIcon.Protected;
			else if (fieldDef.IsFamilyOrAssembly)
				overlayIcon = AccessOverlayIcon.ProtectedInternal;
			else if (fieldDef.IsPublic)
				overlayIcon = AccessOverlayIcon.Public;
			
			if (fieldDef.IsStatic)
				isStatic = true;
			
			return Images.GetIcon(icon, overlayIcon, isStatic);
		}
		
		ImageSource GetOverlayedImage(MethodDefinition methodDef, MemberIcon icon)
		{
			bool isStatic = false;
			AccessOverlayIcon overlayIcon = AccessOverlayIcon.Public;
			
			if (methodDef == null)
				return Images.GetIcon(icon, overlayIcon, isStatic);;

			if (methodDef.IsPrivate)
				overlayIcon = AccessOverlayIcon.Private;
			else if (methodDef.IsAssembly || methodDef.IsFamilyAndAssembly)
				overlayIcon = AccessOverlayIcon.Internal;
			else if (methodDef.IsFamily)
				overlayIcon = AccessOverlayIcon.Protected;
			else if (methodDef.IsFamilyOrAssembly)
				overlayIcon = AccessOverlayIcon.ProtectedInternal;
			else if (methodDef.IsPublic)
				overlayIcon = AccessOverlayIcon.Public;
			
			if (methodDef.IsStatic)
				isStatic = true;
			
			return Images.GetIcon(icon, overlayIcon, isStatic);
		}
		
		ImageSource GetOverlayedImage(PropertyDefinition propDef, MemberIcon icon)
		{
			bool isStatic = false;
			AccessOverlayIcon overlayIcon = AccessOverlayIcon.Public;
			
			return Images.GetIcon(propDef.IsIndexer() ? MemberIcon.Indexer : icon, overlayIcon, isStatic);
		}
		
		ImageSource GetOverlayedImage(EventDefinition eventDef, MemberIcon icon)
		{
			bool isStatic = false;
			AccessOverlayIcon overlayIcon = AccessOverlayIcon.Public;

			return Images.GetIcon(icon, overlayIcon, isStatic);
		}
		
		#endregion
	}
	
	public class TypeBookmark : MemberBookmark
	{
		public TypeBookmark(MemberReference member, int line) : base (member, line)
		{
		}
		
		public override ImageSource Image {
			get {
				if (Member is TypeDefinition) {
					return GetOverlayedImage(Member as TypeDefinition);
				}
				
				return null;
			}
		}
	}
}
