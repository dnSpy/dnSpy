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
using dnSpy;
using dnSpy.Images;
using dnSpy.NRefactory;
using dnSpy.TreeNodes;
using ICSharpCode.Decompiler;

namespace ICSharpCode.ILSpy.TreeNodes {
	/// <summary>
	/// Represents a field in the TreeView.
	/// </summary>
	public sealed class FieldTreeNode : ILSpyTreeNode, IMemberTreeNode
	{
		readonly FieldDef field;

		public FieldDef FieldDefinition
		{
			get { return field; }
		}

		public FieldTreeNode(FieldDef field)
		{
			if (field == null)
				throw new ArgumentNullException("field");
			this.field = field;
		}

		protected override void Write(ITextOutput output, Language language)
		{
			Write(output, field, language);
		}

		public static ITextOutput Write(ITextOutput output, FieldDef field, Language language)
		{
			output.Write(UIUtils.CleanUpIdentifier(field.Name), TextTokenHelper.GetTextTokenType(field));
			output.WriteSpace();
			output.Write(':', TextTokenType.Operator);
			output.WriteSpace();
			language.TypeToString(output, field.FieldType.ToTypeDefOrRef(), false, field);
			field.MDToken.WriteSuffixString(output);
			return output;
		}

		public override object Icon
		{
			get { return GetIcon(field, BackgroundType.TreeNode); }
		}

		public static ImageSource GetIcon(FieldDef field, BackgroundType bgType)
		{
			return GetIcon(GetMemberIcon(field), bgType);
		}

		internal static ImageInfo GetImageInfo(FieldDef field, BackgroundType bgType)
		{
			return GetImageInfo(GetMemberIcon(field), bgType);
		}

		static MemberIcon GetMemberIcon(FieldDef field)
		{
			var access = GetMemberAccess(field);

			if (field.DeclaringType.IsEnum && !field.IsSpecialName) {
				switch (access) {
				case MemberAccess.Public: return MemberIcon.EnumValue;
				case MemberAccess.Private: return MemberIcon.EnumValuePrivate;
				case MemberAccess.Protected: return MemberIcon.EnumValueProtected;
				case MemberAccess.Internal: return MemberIcon.EnumValueInternal;
				case MemberAccess.CompilerControlled: return MemberIcon.EnumValueCompilerControlled;
				case MemberAccess.ProtectedInternal: return MemberIcon.EnumValueProtectedInternal;
				default:
					Debug.Fail("Invalid MemberAccess");
					goto case MemberAccess.Public;
				}
			}

			if (field.IsLiteral || (field.IsInitOnly && IsDecimalConstant(field))) {
				switch (access) {
				case MemberAccess.Public: return MemberIcon.Literal;
				case MemberAccess.Private: return MemberIcon.LiteralPrivate;
				case MemberAccess.Protected: return MemberIcon.LiteralProtected;
				case MemberAccess.Internal: return MemberIcon.LiteralInternal;
				case MemberAccess.CompilerControlled: return MemberIcon.LiteralCompilerControlled;
				case MemberAccess.ProtectedInternal: return MemberIcon.LiteralProtectedInternal;
				default:
					Debug.Fail("Invalid MemberAccess");
					goto case MemberAccess.Public;
				}
			}
			else if (field.IsInitOnly) {
				switch (access) {
				case MemberAccess.Public: return MemberIcon.FieldReadOnly;
				case MemberAccess.Private: return MemberIcon.FieldReadOnlyPrivate;
				case MemberAccess.Protected: return MemberIcon.FieldReadOnlyProtected;
				case MemberAccess.Internal: return MemberIcon.FieldReadOnlyInternal;
				case MemberAccess.CompilerControlled: return MemberIcon.FieldReadOnlyCompilerControlled;
				case MemberAccess.ProtectedInternal: return MemberIcon.FieldReadOnlyProtectedInternal;
				default:
					Debug.Fail("Invalid MemberAccess");
					goto case MemberAccess.Public;
				}
			}
			else {
				switch (access) {
				case MemberAccess.Public: return MemberIcon.Field;
				case MemberAccess.Private: return MemberIcon.FieldPrivate;
				case MemberAccess.Protected: return MemberIcon.FieldProtected;
				case MemberAccess.Internal: return MemberIcon.FieldInternal;
				case MemberAccess.CompilerControlled: return MemberIcon.FieldCompilerControlled;
				case MemberAccess.ProtectedInternal: return MemberIcon.FieldProtectedInternal;
				default:
					Debug.Fail("Invalid MemberAccess");
					goto case MemberAccess.Public;
				}
			}
		}

		internal static ImageSource GetIcon(MemberIcon icon, BackgroundType bgType)
		{
			return ImageCache.Instance.GetImage(GetImageInfo(icon, bgType));
		}

		internal static ImageInfo GetImageInfo(MemberIcon icon, BackgroundType bgType)
		{
			switch (icon) {
			case MemberIcon.EnumValue:							return new ImageInfo("EnumValue", bgType);
			case MemberIcon.EnumValuePrivate:					return new ImageInfo("EnumValuePrivate", bgType);
			case MemberIcon.EnumValueProtected:					return new ImageInfo("EnumValueProtected", bgType);
			case MemberIcon.EnumValueInternal:					return new ImageInfo("EnumValueInternal", bgType);
			case MemberIcon.EnumValueCompilerControlled:		return new ImageInfo("EnumValueCompilerControlled", bgType);
			case MemberIcon.EnumValueProtectedInternal:			return new ImageInfo("EnumValueProtectedInternal", bgType);
			case MemberIcon.Literal:							return new ImageInfo("Literal", bgType);
			case MemberIcon.LiteralPrivate:						return new ImageInfo("LiteralPrivate", bgType);
			case MemberIcon.LiteralProtected:					return new ImageInfo("LiteralProtected", bgType);
			case MemberIcon.LiteralInternal:					return new ImageInfo("LiteralInternal", bgType);
			case MemberIcon.LiteralCompilerControlled:			return new ImageInfo("LiteralCompilerControlled", bgType);
			case MemberIcon.LiteralProtectedInternal:			return new ImageInfo("LiteralProtectedInternal", bgType);
			case MemberIcon.FieldReadOnly:						return new ImageInfo("FieldReadOnly", bgType);
			case MemberIcon.FieldReadOnlyPrivate:				return new ImageInfo("FieldReadOnlyPrivate", bgType);
			case MemberIcon.FieldReadOnlyProtected:				return new ImageInfo("FieldReadOnlyProtected", bgType);
			case MemberIcon.FieldReadOnlyInternal:				return new ImageInfo("FieldReadOnlyInternal", bgType);
			case MemberIcon.FieldReadOnlyCompilerControlled:	return new ImageInfo("FieldReadOnlyCompilerControlled", bgType);
			case MemberIcon.FieldReadOnlyProtectedInternal:		return new ImageInfo("FieldReadOnlyProtectedInternal", bgType);
			case MemberIcon.Field:								return new ImageInfo("Field", bgType);
			case MemberIcon.FieldPrivate:						return new ImageInfo("FieldPrivate", bgType);
			case MemberIcon.FieldProtected:						return new ImageInfo("FieldProtected", bgType);
			case MemberIcon.FieldInternal:						return new ImageInfo("FieldInternal", bgType);
			case MemberIcon.FieldCompilerControlled:			return new ImageInfo("FieldCompilerControlled", bgType);
			case MemberIcon.FieldProtectedInternal:				return new ImageInfo("FieldProtectedInternal", bgType);
			case MemberIcon.Operator:							return new ImageInfo("Operator", bgType);
			case MemberIcon.OperatorPrivate:					return new ImageInfo("OperatorPrivate", bgType);
			case MemberIcon.OperatorProtected:					return new ImageInfo("OperatorProtected", bgType);
			case MemberIcon.OperatorInternal:					return new ImageInfo("OperatorInternal", bgType);
			case MemberIcon.OperatorCompilerControlled:			return new ImageInfo("OperatorCompilerControlled", bgType);
			case MemberIcon.OperatorProtectedInternal:			return new ImageInfo("OperatorProtectedInternal", bgType);
			case MemberIcon.ExtensionMethod:					return new ImageInfo("ExtensionMethod", bgType);
			case MemberIcon.ExtensionMethodPrivate:				return new ImageInfo("ExtensionMethodPrivate", bgType);
			case MemberIcon.ExtensionMethodProtected:			return new ImageInfo("ExtensionMethodProtected", bgType);
			case MemberIcon.ExtensionMethodInternal:			return new ImageInfo("ExtensionMethodInternal", bgType);
			case MemberIcon.ExtensionMethodCompilerControlled:	return new ImageInfo("ExtensionMethodCompilerControlled", bgType);
			case MemberIcon.ExtensionMethodProtectedInternal:	return new ImageInfo("ExtensionMethodProtectedInternal", bgType);
			case MemberIcon.Constructor:						return new ImageInfo("Constructor", bgType);
			case MemberIcon.ConstructorPrivate:					return new ImageInfo("ConstructorPrivate", bgType);
			case MemberIcon.ConstructorProtected:				return new ImageInfo("ConstructorProtected", bgType);
			case MemberIcon.ConstructorInternal:				return new ImageInfo("ConstructorInternal", bgType);
			case MemberIcon.ConstructorCompilerControlled:		return new ImageInfo("ConstructorCompilerControlled", bgType);
			case MemberIcon.ConstructorProtectedInternal:		return new ImageInfo("ConstructorProtectedInternal", bgType);
			case MemberIcon.PInvokeMethod:						return new ImageInfo("PInvokeMethod", bgType);
			case MemberIcon.PInvokeMethodPrivate:				return new ImageInfo("PInvokeMethodPrivate", bgType);
			case MemberIcon.PInvokeMethodProtected:				return new ImageInfo("PInvokeMethodProtected", bgType);
			case MemberIcon.PInvokeMethodInternal:				return new ImageInfo("PInvokeMethodInternal", bgType);
			case MemberIcon.PInvokeMethodCompilerControlled:	return new ImageInfo("PInvokeMethodCompilerControlled", bgType);
			case MemberIcon.PInvokeMethodProtectedInternal:		return new ImageInfo("PInvokeMethodProtectedInternal", bgType);
			case MemberIcon.StaticMethod:						return new ImageInfo("StaticMethod", bgType);
			case MemberIcon.StaticMethodPrivate:				return new ImageInfo("StaticMethodPrivate", bgType);
			case MemberIcon.StaticMethodProtected:				return new ImageInfo("StaticMethodProtected", bgType);
			case MemberIcon.StaticMethodInternal:				return new ImageInfo("StaticMethodInternal", bgType);
			case MemberIcon.StaticMethodCompilerControlled:		return new ImageInfo("StaticMethodCompilerControlled", bgType);
			case MemberIcon.StaticMethodProtectedInternal:		return new ImageInfo("StaticMethodProtectedInternal", bgType);
			case MemberIcon.VirtualMethod:						return new ImageInfo("VirtualMethod", bgType);
			case MemberIcon.VirtualMethodPrivate:				return new ImageInfo("VirtualMethodPrivate", bgType);
			case MemberIcon.VirtualMethodProtected:				return new ImageInfo("VirtualMethodProtected", bgType);
			case MemberIcon.VirtualMethodInternal:				return new ImageInfo("VirtualMethodInternal", bgType);
			case MemberIcon.VirtualMethodCompilerControlled:	return new ImageInfo("VirtualMethodCompilerControlled", bgType);
			case MemberIcon.VirtualMethodProtectedInternal:		return new ImageInfo("VirtualMethodProtectedInternal", bgType);
			case MemberIcon.Method:								return new ImageInfo("Method", bgType);
			case MemberIcon.MethodPrivate: 						return new ImageInfo("MethodPrivate", bgType);
			case MemberIcon.MethodProtected: 					return new ImageInfo("MethodProtected", bgType);
			case MemberIcon.MethodInternal:						return new ImageInfo("MethodInternal", bgType);
			case MemberIcon.MethodCompilerControlled:			return new ImageInfo("MethodCompilerControlled", bgType);
			case MemberIcon.MethodProtectedInternal:			return new ImageInfo("MethodProtectedInternal", bgType);
			case MemberIcon.StaticProperty:						return new ImageInfo("StaticProperty", bgType);
			case MemberIcon.StaticPropertyPrivate:				return new ImageInfo("StaticPropertyPrivate", bgType);
			case MemberIcon.StaticPropertyProtected:			return new ImageInfo("StaticPropertyProtected", bgType);
			case MemberIcon.StaticPropertyInternal:				return new ImageInfo("StaticPropertyInternal", bgType);
			case MemberIcon.StaticPropertyCompilerControlled:	return new ImageInfo("StaticPropertyCompilerControlled", bgType);
			case MemberIcon.StaticPropertyProtectedInternal:	return new ImageInfo("StaticPropertyProtectedInternal", bgType);
			case MemberIcon.VirtualProperty:					return new ImageInfo("VirtualProperty", bgType);
			case MemberIcon.VirtualPropertyPrivate:				return new ImageInfo("VirtualPropertyPrivate", bgType);
			case MemberIcon.VirtualPropertyProtected:			return new ImageInfo("VirtualPropertyProtected", bgType);
			case MemberIcon.VirtualPropertyInternal:			return new ImageInfo("VirtualPropertyInternal", bgType);
			case MemberIcon.VirtualPropertyCompilerControlled:	return new ImageInfo("VirtualPropertyCompilerControlled", bgType);
			case MemberIcon.VirtualPropertyProtectedInternal:	return new ImageInfo("VirtualPropertyProtectedInternal", bgType);
			case MemberIcon.Property:							return new ImageInfo("Property", bgType);
			case MemberIcon.PropertyPrivate:					return new ImageInfo("PropertyPrivate", bgType);
			case MemberIcon.PropertyProtected:					return new ImageInfo("PropertyProtected", bgType);
			case MemberIcon.PropertyInternal:					return new ImageInfo("PropertyInternal", bgType);
			case MemberIcon.PropertyCompilerControlled:			return new ImageInfo("PropertyCompilerControlled", bgType);
			case MemberIcon.PropertyProtectedInternal:			return new ImageInfo("PropertyProtectedInternal", bgType);
			case MemberIcon.StaticEvent:						return new ImageInfo("StaticEvent", bgType);
			case MemberIcon.StaticEventPrivate:					return new ImageInfo("StaticEventPrivate", bgType);
			case MemberIcon.StaticEventProtected:				return new ImageInfo("StaticEventProtected", bgType);
			case MemberIcon.StaticEventInternal:				return new ImageInfo("StaticEventInternal", bgType);
			case MemberIcon.StaticEventCompilerControlled:		return new ImageInfo("StaticEventCompilerControlled", bgType);
			case MemberIcon.StaticEventProtectedInternal:		return new ImageInfo("StaticEventProtectedInternal", bgType);
			case MemberIcon.VirtualEvent:						return new ImageInfo("VirtualEvent", bgType);
			case MemberIcon.VirtualEventPrivate:				return new ImageInfo("VirtualEventPrivate", bgType);
			case MemberIcon.VirtualEventProtected:				return new ImageInfo("VirtualEventProtected", bgType);
			case MemberIcon.VirtualEventInternal:				return new ImageInfo("VirtualEventInternal", bgType);
			case MemberIcon.VirtualEventCompilerControlled:		return new ImageInfo("VirtualEventCompilerControlled", bgType);
			case MemberIcon.VirtualEventProtectedInternal:		return new ImageInfo("VirtualEventProtectedInternal", bgType);
			case MemberIcon.Event:								return new ImageInfo("Event", bgType);
			case MemberIcon.EventPrivate:						return new ImageInfo("EventPrivate", bgType);
			case MemberIcon.EventProtected:						return new ImageInfo("EventProtected", bgType);
			case MemberIcon.EventInternal:						return new ImageInfo("EventInternal", bgType);
			case MemberIcon.EventCompilerControlled:			return new ImageInfo("EventCompilerControlled", bgType);
			case MemberIcon.EventProtectedInternal:				return new ImageInfo("EventProtectedInternal", bgType);

			default:
				Debug.Fail("Invalid MemberIcon");
				goto case MemberIcon.EnumValue;
			}
		}

		private static bool IsDecimalConstant(FieldDef field)
		{
			var fieldType = field.FieldType;
			if (fieldType != null && fieldType.DefinitionAssembly.IsCorLib() && fieldType.TypeName == "Decimal" && fieldType.Namespace == "System") {
				if (field.HasCustomAttributes) {
					var attrs = field.CustomAttributes;
					for (int i = 0; i < attrs.Count; i++) {
						var attrType = attrs[i].AttributeType;
						if (attrType != null && attrType.Name == "DecimalConstantAttribute" && attrType.Namespace == "System.Runtime.CompilerServices")
							return true;
					}
				}
			}
			return false;
		}

		static MemberAccess GetMemberAccess(FieldDef field)
		{
			return GetMemberAccess(field.Access);
		}

		internal static MemberAccess GetMemberAccess(FieldAttributes attrs)
		{
			switch (attrs & FieldAttributes.FieldAccessMask) {
			case FieldAttributes.Public:
				return MemberAccess.Public;
			case FieldAttributes.Assembly:
			case FieldAttributes.FamANDAssem:
				return MemberAccess.Internal;
			case FieldAttributes.Family:
				return MemberAccess.Protected;
			case FieldAttributes.FamORAssem:
				return MemberAccess.ProtectedInternal;
			case FieldAttributes.Private:
				return MemberAccess.Private;
			case FieldAttributes.CompilerControlled:
				return MemberAccess.CompilerControlled;
			default:
				return MemberAccess.Public;
			}
		}

		public override FilterResult Filter(FilterSettings settings)
		{
			var res = settings.Filter.GetFilterResult(this.FieldDefinition);
			if (res.FilterResult != null)
				return res.FilterResult.Value;
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
			get { return IsPublicAPIInternal(field); }
		}

		internal static bool IsPublicAPIInternal(FieldDef field)
		{
			return field.IsPublic || field.IsFamily || field.IsFamilyOrAssembly;
		}

		IMemberRef IMemberTreeNode.Member
		{
			get { return field; }
		}

		IMDTokenProvider ITokenTreeNode.MDTokenProvider {
			get { return field; }
		}

		public override NodePathName NodePathName {
			get { return new NodePathName("field", field.FullName); }
		}
	}
}
