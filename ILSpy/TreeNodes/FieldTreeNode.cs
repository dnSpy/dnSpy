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
using System.Windows.Media;
using ICSharpCode.Decompiler;
using Mono.Cecil;

namespace ICSharpCode.ILSpy.TreeNodes
{
	/// <summary>
	/// Represents a field in the TreeView.
	/// </summary>
	public sealed class FieldTreeNode : ILSpyTreeNode, IMemberTreeNode
	{
		readonly FieldDefinition field;

		public FieldDefinition FieldDefinition
		{
			get { return field; }
		}

		public FieldTreeNode(FieldDefinition field)
		{
			if (field == null)
				throw new ArgumentNullException("field");
			this.field = field;
		}

		public override object Text
		{
			get {
				return HighlightSearchMatch(
					field.Name,
					" : " + this.Language.TypeToString(field.FieldType, false, field) + field.MetadataToken.ToSuffixString()
				);
			}
		}

		public override object Icon
		{
			get { return GetIcon(field); }
		}

		public static ImageSource GetIcon(FieldDefinition field)
		{
			if (field.DeclaringType.IsEnum && !field.Attributes.HasFlag(FieldAttributes.SpecialName))
				return Images.GetIcon(MemberIcon.EnumValue, GetOverlayIcon(field.Attributes), false);

			if (field.IsLiteral)
				return Images.GetIcon(MemberIcon.Literal, GetOverlayIcon(field.Attributes), false);
			else if (field.IsInitOnly) {
				if (IsDecimalConstant(field))
					return Images.GetIcon(MemberIcon.Literal, GetOverlayIcon(field.Attributes), false);
				else
					return Images.GetIcon(MemberIcon.FieldReadOnly, GetOverlayIcon(field.Attributes), field.IsStatic);
			} else
				return Images.GetIcon(MemberIcon.Field, GetOverlayIcon(field.Attributes), field.IsStatic);
		}

		private static bool IsDecimalConstant(FieldDefinition field)
		{
			var fieldType = field.FieldType;
			if (fieldType.Name == "Decimal" && fieldType.Namespace == "System") {
				if (field.HasCustomAttributes) {
					var attrs = field.CustomAttributes;
					for (int i = 0; i < attrs.Count; i++) {
						var attrType = attrs[i].AttributeType;
						if (attrType.Name == "DecimalConstantAttribute" && attrType.Namespace == "System.Runtime.CompilerServices")
							return true;
					}
				}
			}
			return false;
		}

		private static AccessOverlayIcon GetOverlayIcon(FieldAttributes fieldAttributes)
		{
			switch (fieldAttributes & FieldAttributes.FieldAccessMask) {
				case FieldAttributes.Public:
					return AccessOverlayIcon.Public;
				case FieldAttributes.Assembly:
				case FieldAttributes.FamANDAssem:
					return AccessOverlayIcon.Internal;
				case FieldAttributes.Family:
					return AccessOverlayIcon.Protected;
				case FieldAttributes.FamORAssem:
					return AccessOverlayIcon.ProtectedInternal;
				case FieldAttributes.Private:
					return AccessOverlayIcon.Private;
				case FieldAttributes.CompilerControlled:
					return AccessOverlayIcon.CompilerControlled;
				default:
					throw new NotSupportedException();
			}
		}

		public override FilterResult Filter(FilterSettings settings)
		{
			if (settings.SearchTermMatches(field.Name) && settings.Language.ShowMember(field))
				return FilterResult.Match;
			else
				return FilterResult.Hidden;
		}

		public override void Decompile(Language language, ITextOutput output, DecompilationOptions options)
		{
			language.DecompileField(field, output, options);
		}
		
		public override bool IsPublicAPI {
			get {
				return field.IsPublic || field.IsFamily || field.IsFamilyOrAssembly;
			}
		}

		MemberReference IMemberTreeNode.Member
		{
			get { return field; }
		}
	}
}
