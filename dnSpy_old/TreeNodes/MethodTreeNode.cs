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
using dnSpy.Contracts.Images;
using dnSpy.NRefactory;
using dnSpy.TreeNodes;
using ICSharpCode.Decompiler;

namespace ICSharpCode.ILSpy.TreeNodes {
	/// <summary>
	/// Tree Node representing a field, method, property, or event.
	/// </summary>
	public sealed class MethodTreeNode : ILSpyTreeNode, IMemberTreeNode {
		readonly MethodDef method;

		public MethodDef MethodDef {
			get { return method; }
		}

		public MethodTreeNode(MethodDef method) {
			if (method == null)
				throw new ArgumentNullException("method");
			this.method = method;
		}

		protected override void Write(ITextOutput output, Language language) {
			Write(output, method, language);
		}

		public static ITextOutput Write(ITextOutput output, MethodDef method, Language language) {
			output.Write(UIUtils.CleanUpIdentifier(method.Name), TextTokenHelper.GetTextTokenType(method));
			output.Write('(', TextTokenType.Operator);
			for (int i = 0; i < method.Parameters.Count; i++) {
				if (method.Parameters[i].IsHiddenThisParameter)
					continue;
				if (method.Parameters[i].MethodSigIndex > 0) {
					output.Write(',', TextTokenType.Operator);
					output.WriteSpace();
				}
				language.TypeToString(output, method.Parameters[i].Type.ToTypeDefOrRef(), false, method.Parameters[i].ParamDef);
			}
			if (method.CallingConvention == CallingConvention.VarArg || method.CallingConvention == CallingConvention.NativeVarArg) {
				if (method.MethodSig.GetParamCount() > 0) {
					output.Write(',', TextTokenType.Operator);
					output.WriteSpace();
				}
				output.Write("...", TextTokenType.Operator);
			}
			output.Write(')', TextTokenType.Operator);
			output.WriteSpace();
			output.Write(':', TextTokenType.Operator);
			output.WriteSpace();
			language.TypeToString(output, method.ReturnType.ToTypeDefOrRef(), false, method.Parameters.ReturnParameter.ParamDef);
			method.MDToken.WriteSuffixString(output);
			return output;
		}

		public override object Icon {
			get { return GetIcon(method, BackgroundType.TreeNode); }
		}

		public static ImageSource GetIcon(MethodDef method, BackgroundType bgType) {
			return FieldTreeNode.GetIcon(GetMemberIcon(method), bgType);
		}

		internal static ImageInfo GetImageInfo(MethodDef method, BackgroundType bgType) {
			return FieldTreeNode.GetImageInfo(GetMemberIcon(method), bgType);
		}

		static MemberIcon GetMemberIcon(MethodDef method) {
			var access = GetMemberAccess(method);

			if (method.IsSpecialName && method.Name.StartsWith("op_", StringComparison.Ordinal)) {
				switch (access) {
				case MemberAccess.Public: return MemberIcon.Operator;
				case MemberAccess.Private: return MemberIcon.OperatorPrivate;
				case MemberAccess.Protected: return MemberIcon.OperatorProtected;
				case MemberAccess.Internal: return MemberIcon.OperatorInternal;
				case MemberAccess.CompilerControlled: return MemberIcon.OperatorCompilerControlled;
				case MemberAccess.ProtectedInternal: return MemberIcon.OperatorProtectedInternal;
				default:
					Debug.Fail("Invalid MemberAccess");
					goto case MemberAccess.Public;
				}
			}

			if (method.IsStatic && method.CustomAttributes.IsDefined("System.Runtime.CompilerServices.ExtensionAttribute")) {
				switch (access) {
				case MemberAccess.Public: return MemberIcon.ExtensionMethod;
				case MemberAccess.Private: return MemberIcon.ExtensionMethodPrivate;
				case MemberAccess.Protected: return MemberIcon.ExtensionMethodProtected;
				case MemberAccess.Internal: return MemberIcon.ExtensionMethodInternal;
				case MemberAccess.CompilerControlled: return MemberIcon.ExtensionMethodCompilerControlled;
				case MemberAccess.ProtectedInternal: return MemberIcon.ExtensionMethodProtectedInternal;
				default:
					Debug.Fail("Invalid MemberAccess");
					goto case MemberAccess.Public;
				}
			}

			if (method.IsConstructor) {
				switch (access) {
				case MemberAccess.Public: return MemberIcon.Constructor;
				case MemberAccess.Private: return MemberIcon.ConstructorPrivate;
				case MemberAccess.Protected: return MemberIcon.ConstructorProtected;
				case MemberAccess.Internal: return MemberIcon.ConstructorInternal;
				case MemberAccess.CompilerControlled: return MemberIcon.ConstructorCompilerControlled;
				case MemberAccess.ProtectedInternal: return MemberIcon.ConstructorProtectedInternal;
				default:
					Debug.Fail("Invalid MemberAccess");
					goto case MemberAccess.Public;
				}
			}

			if (method.HasImplMap) {
				switch (access) {
				case MemberAccess.Public: return MemberIcon.PInvokeMethod;
				case MemberAccess.Private: return MemberIcon.PInvokeMethodPrivate;
				case MemberAccess.Protected: return MemberIcon.PInvokeMethodProtected;
				case MemberAccess.Internal: return MemberIcon.PInvokeMethodInternal;
				case MemberAccess.CompilerControlled: return MemberIcon.PInvokeMethodCompilerControlled;
				case MemberAccess.ProtectedInternal: return MemberIcon.PInvokeMethodProtectedInternal;
				default:
					Debug.Fail("Invalid MemberAccess");
					goto case MemberAccess.Public;
				}
			}

			if (method.IsStatic) {
				switch (access) {
				case MemberAccess.Public: return MemberIcon.StaticMethod;
				case MemberAccess.Private: return MemberIcon.StaticMethodPrivate;
				case MemberAccess.Protected: return MemberIcon.StaticMethodProtected;
				case MemberAccess.Internal: return MemberIcon.StaticMethodInternal;
				case MemberAccess.CompilerControlled: return MemberIcon.StaticMethodCompilerControlled;
				case MemberAccess.ProtectedInternal: return MemberIcon.StaticMethodProtectedInternal;
				default:
					Debug.Fail("Invalid MemberAccess");
					goto case MemberAccess.Public;
				}
			}

			if (method.IsVirtual) {
				switch (access) {
				case MemberAccess.Public: return MemberIcon.VirtualMethod;
				case MemberAccess.Private: return MemberIcon.VirtualMethodPrivate;
				case MemberAccess.Protected: return MemberIcon.VirtualMethodProtected;
				case MemberAccess.Internal: return MemberIcon.VirtualMethodInternal;
				case MemberAccess.CompilerControlled: return MemberIcon.VirtualMethodCompilerControlled;
				case MemberAccess.ProtectedInternal: return MemberIcon.VirtualMethodProtectedInternal;
				default:
					Debug.Fail("Invalid MemberAccess");
					goto case MemberAccess.Public;
				}
			}
			switch (access) {
			case MemberAccess.Public: return MemberIcon.Method;
			case MemberAccess.Private: return MemberIcon.MethodPrivate;
			case MemberAccess.Protected: return MemberIcon.MethodProtected;
			case MemberAccess.Internal: return MemberIcon.MethodInternal;
			case MemberAccess.CompilerControlled: return MemberIcon.MethodCompilerControlled;
			case MemberAccess.ProtectedInternal: return MemberIcon.MethodProtectedInternal;
			default:
				Debug.Fail("Invalid MemberAccess");
				goto case MemberAccess.Public;
			}
		}

		internal static MemberAccess GetMemberAccess(MethodDef method) {
			return GetMemberAccess(method.Access);
		}

		public static MemberAccess GetMemberAccess(MethodAttributes attrs) {
			switch (attrs & MethodAttributes.MemberAccessMask) {
			case MethodAttributes.Public:
				return MemberAccess.Public;
			case MethodAttributes.Assembly:
			case MethodAttributes.FamANDAssem:
				return MemberAccess.Internal;
			case MethodAttributes.Family:
				return MemberAccess.Protected;
			case MethodAttributes.FamORAssem:
				return MemberAccess.ProtectedInternal;
			case MethodAttributes.Private:
				return MemberAccess.Private;
			case MethodAttributes.CompilerControlled:
				return MemberAccess.CompilerControlled;
			default:
				return MemberAccess.Public;
			}
		}

		public override void Decompile(Language language, ITextOutput output, DecompilationOptions options) {
			language.DecompileMethod(method, output, options);
		}

		public override FilterResult Filter(FilterSettings settings) {
			var res = settings.Filter.GetFilterResult(this.MethodDef);
			if (res.FilterResult != null)
				return res.FilterResult.Value;
			if (settings.SearchTermMatches(method.Name) && settings.Language.ShowMember(method))
				return FilterResult.Match;
			else
				return FilterResult.Hidden;
		}

		public override bool IsPublicAPI {
			get { return IsPublicAPIInternal(method); }
		}

		internal static bool IsPublicAPIInternal(MethodDef method) {
			return method.IsPublic || method.IsFamily || method.IsFamilyOrAssembly;
		}

		IMemberRef IMemberTreeNode.Member {
			get { return method; }
		}

		IMDTokenProvider ITokenTreeNode.MDTokenProvider {
			get { return method; }
		}

		public override NodePathName NodePathName {
			get { return new NodePathName("method", method.FullName); }
		}
	}
}
