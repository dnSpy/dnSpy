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
using System.Diagnostics;
using System.Windows.Media;
using dnlib.DotNet;
using dnSpy.Files;
using dnSpy.Images;
using dnSpy.NRefactory;
using dnSpy.TreeNodes;
using ICSharpCode.Decompiler;

namespace ICSharpCode.ILSpy.TreeNodes {
	/// <summary>
	/// Represents a property in the TreeView.
	/// </summary>
	public sealed class PropertyTreeNode : ILSpyTreeNode, IMemberTreeNode
	{
		readonly PropertyDef property;
		readonly bool isIndexer;

		static DnSpyFileList GetDnSpyFileList(ILSpyTreeNode node) {
			if (node == null)
				return null;
			var asmNode = GetNode<AssemblyTreeNode>(node);
			if (asmNode == null)
				return null;
			return asmNode.DnSpyFileList;
		}

		public PropertyTreeNode(PropertyDef property, ILSpyTreeNode owner)
		{
			if (property == null)
				throw new ArgumentNullException("property");
			this.property = property;
			var list = GetDnSpyFileList(owner ?? this);
			using (list == null ? null : list.DisableAssemblyLoad()) {
				this.isIndexer = property.IsIndexer();
			}

			if (property.GetMethod != null)
				this.Children.Add(new MethodTreeNode(property.GetMethod));
			if (property.SetMethod != null)
				this.Children.Add(new MethodTreeNode(property.SetMethod));
			if (property.HasOtherMethods) {
				foreach (var m in property.OtherMethods)
					this.Children.Add(new MethodTreeNode(m));
			}
			
		}

		public PropertyDef PropertyDef {
			get { return property; }
		}

		protected override void Write(ITextOutput output, Language language)
		{
			Write(output, property, language);
		}

		public static ITextOutput Write(ITextOutput output, PropertyDef property, Language language, bool? isIndexer = null)
		{
			language.FormatPropertyName(output, property, isIndexer);
			output.WriteSpace();
			output.Write(':', TextTokenType.Operator);
			output.WriteSpace();
			language.TypeToString(output, property.PropertySig.GetRetType().ToTypeDefOrRef(), false, property);
			property.MDToken.WriteSuffixString(output);
			return output;
		}
		
		public override object Icon
		{
			get { return GetIcon(property, BackgroundType.TreeNode); }
		}

		internal static ImageInfo GetImageInfo(PropertyDef property, BackgroundType bgType)
		{
			return FieldTreeNode.GetImageInfo(GetMemberIcon(property), bgType);
		}

		public static ImageSource GetIcon(PropertyDef property, BackgroundType bgType)
		{
			return FieldTreeNode.GetIcon(GetMemberIcon(property), bgType);
		}

		static MemberIcon GetMemberIcon(PropertyDef property)
		{
			MethodDef method = GetMostAccessibleMethod(property);
			if (method == null)
				return MemberIcon.Property;

			var access = MethodTreeNode.GetMemberAccess(method);
			if (method.IsStatic) {
				switch (access) {
				case MemberAccess.Public: return MemberIcon.StaticProperty;
				case MemberAccess.Private: return MemberIcon.StaticPropertyPrivate;
				case MemberAccess.Protected: return MemberIcon.StaticPropertyProtected;
				case MemberAccess.Internal: return MemberIcon.StaticPropertyInternal;
				case MemberAccess.CompilerControlled: return MemberIcon.StaticPropertyCompilerControlled;
				case MemberAccess.ProtectedInternal: return MemberIcon.StaticPropertyProtectedInternal;
				default:
					Debug.Fail("Invalid MemberAccess");
					goto case MemberAccess.Public;
				}
			}

			if (method.IsVirtual) {
				switch (access) {
				case MemberAccess.Public: return MemberIcon.VirtualProperty;
				case MemberAccess.Private: return MemberIcon.VirtualPropertyPrivate;
				case MemberAccess.Protected: return MemberIcon.VirtualPropertyProtected;
				case MemberAccess.Internal: return MemberIcon.VirtualPropertyInternal;
				case MemberAccess.CompilerControlled: return MemberIcon.VirtualPropertyCompilerControlled;
				case MemberAccess.ProtectedInternal: return MemberIcon.VirtualPropertyProtectedInternal;
				default:
					Debug.Fail("Invalid MemberAccess");
					goto case MemberAccess.Public;
				}
			}

			switch (access) {
			case MemberAccess.Public: return MemberIcon.Property;
			case MemberAccess.Private: return MemberIcon.PropertyPrivate;
			case MemberAccess.Protected: return MemberIcon.PropertyProtected;
			case MemberAccess.Internal: return MemberIcon.PropertyInternal;
			case MemberAccess.CompilerControlled: return MemberIcon.PropertyCompilerControlled;
			case MemberAccess.ProtectedInternal: return MemberIcon.PropertyProtectedInternal;
			default:
				Debug.Fail("Invalid MemberAccess");
				goto case MemberAccess.Public;
			}
		}

		private static MethodDef GetMostAccessibleMethod(PropertyDef property)
		{
			MethodDef result = null;

			// Method access is defined from inaccessible (lowest) to public (highest)
			// in numeric order, so we can do an integer comparison of the masked attribute
			int accessLevel = 0;

			if (property.GetMethod != null) {
				int methodAccessLevel = (int)(property.GetMethod.Attributes & MethodAttributes.MemberAccessMask);
				if (accessLevel < methodAccessLevel) {
					accessLevel = methodAccessLevel;
					result = property.GetMethod;
				}
			}

			if (property.SetMethod != null) {
				int methodAccessLevel = (int)(property.SetMethod.Attributes & MethodAttributes.MemberAccessMask);
				if (accessLevel < methodAccessLevel) {
					accessLevel = methodAccessLevel;
					result = property.SetMethod;
				}
			}

			if (property.HasOtherMethods) {
				foreach (var m in property.OtherMethods) {
					int methodAccessLevel = (int)(m.Attributes & MethodAttributes.MemberAccessMask);
					if (accessLevel < methodAccessLevel) {
						accessLevel = methodAccessLevel;
						result = m;
					}
				}
			}

			return result;
		}

		public override FilterResult Filter(FilterSettings settings)
		{
			var res = settings.Filter.GetFilterResult(this.PropertyDef);
			if (res.FilterResult != null)
				return res.FilterResult.Value;
			if (settings.SearchTermMatches(property.Name) && settings.Language.ShowMember(property))
				return FilterResult.Match;
			else
				return FilterResult.Hidden;
		}

		public override void Decompile(Language language, ITextOutput output, DecompilationOptions options)
		{
			language.DecompileProperty(property, output, options);
		}
		
		public override bool IsPublicAPI {
			get { return IsPublicAPIInternal(property); }
		}

		internal static bool IsPublicAPIInternal(PropertyDef property)
		{
			var m = GetMostAccessibleMethod(property);
			var attr = m == null ? 0 : m.Attributes;
			switch (attr & MethodAttributes.MemberAccessMask) {
			case MethodAttributes.Public:
			case MethodAttributes.Family:
			case MethodAttributes.FamORAssem:
				return true;
			default:
				return false;
			}
		}

		IMemberRef IMemberTreeNode.Member
		{
			get { return property; }
		}

		IMDTokenProvider ITokenTreeNode.MDTokenProvider {
			get { return property; }
		}

		public override NodePathName NodePathName {
			get { return new NodePathName("property", property.FullName); }
		}
	}
}
