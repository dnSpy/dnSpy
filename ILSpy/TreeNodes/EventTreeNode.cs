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
using ICSharpCode.Decompiler;
using dnlib.DotNet;

namespace ICSharpCode.ILSpy.TreeNodes
{
	/// <summary>
	/// Represents an event in the TreeView.
	/// </summary>
	public sealed class EventTreeNode : ILSpyTreeNode, IMemberTreeNode
	{
		readonly EventDef ev;
		
		public EventTreeNode(EventDef ev)
		{
			if (ev == null)
				throw new ArgumentNullException("ev");
			this.ev = ev;
			
			if (ev.AddMethod != null)
				this.Children.Add(new MethodTreeNode(ev.AddMethod));
			if (ev.RemoveMethod != null)
				this.Children.Add(new MethodTreeNode(ev.RemoveMethod));
			if (ev.InvokeMethod != null)
				this.Children.Add(new MethodTreeNode(ev.InvokeMethod));
			if (ev.HasOtherMethods) {
				foreach (var m in ev.OtherMethods)
					this.Children.Add(new MethodTreeNode(m));
			}
		}
		
		public EventDef EventDefinition
		{
			get { return ev; }
		}
		
		public override object Text
		{
			get { return ToString(Language); }
		}

		public override string ToString(Language language)
		{
			return GetText(ev, language) + ev.MDToken.ToSuffixString();
		}

		public static object GetText(EventDef eventDef, Language language)
		{
			return CleanUpName(eventDef.Name) + CleanUpName(" : " + language.TypeToString(eventDef.EventType, false, eventDef));
		}
		
		public override object Icon
		{
			get { return GetIcon(ev, BackgroundType.TreeNode); }
		}

		public static ImageSource GetIcon(EventDef eventDef, BackgroundType bgType)
		{
			return FieldTreeNode.GetIcon(GetMemberIcon(eventDef), bgType);
		}

		internal static ImageInfo GetImageInfo(EventDef eventDef, BackgroundType bgType)
		{
			return FieldTreeNode.GetImageInfo(GetMemberIcon(eventDef), bgType);
		}

		static MemberIcon GetMemberIcon(EventDef eventDef)
		{
			MethodDef method = eventDef.AddMethod ?? eventDef.RemoveMethod;
			if (method == null)
				return MemberIcon.Event;

			var access = MethodTreeNode.GetMemberAccess(method);
			if (method.IsStatic) {
				switch (access) {
				case MemberAccess.Public: return MemberIcon.StaticEvent;
				case MemberAccess.Private: return MemberIcon.StaticEventPrivate;
				case MemberAccess.Protected: return MemberIcon.StaticEventProtected;
				case MemberAccess.Internal: return MemberIcon.StaticEventInternal;
				case MemberAccess.CompilerControlled: return MemberIcon.StaticEventCompilerControlled;
				case MemberAccess.ProtectedInternal: return MemberIcon.StaticEventProtectedInternal;
				default:
					Debug.Fail("Invalid MemberAccess");
					goto case MemberAccess.Public;
				}
			}

			if (method.IsVirtual) {
				switch (access) {
				case MemberAccess.Public: return MemberIcon.VirtualEvent;
				case MemberAccess.Private: return MemberIcon.VirtualEventPrivate;
				case MemberAccess.Protected: return MemberIcon.VirtualEventProtected;
				case MemberAccess.Internal: return MemberIcon.VirtualEventInternal;
				case MemberAccess.CompilerControlled: return MemberIcon.VirtualEventCompilerControlled;
				case MemberAccess.ProtectedInternal: return MemberIcon.VirtualEventProtectedInternal;
				default:
					Debug.Fail("Invalid MemberAccess");
					goto case MemberAccess.Public;
				}
			}

			switch (access) {
			case MemberAccess.Public: return MemberIcon.Event;
			case MemberAccess.Private: return MemberIcon.EventPrivate;
			case MemberAccess.Protected: return MemberIcon.EventProtected;
			case MemberAccess.Internal: return MemberIcon.EventInternal;
			case MemberAccess.CompilerControlled: return MemberIcon.EventCompilerControlled;
			case MemberAccess.ProtectedInternal: return MemberIcon.EventProtectedInternal;
			default:
				Debug.Fail("Invalid MemberAccess");
				goto case MemberAccess.Public;
			}
		}

		public override FilterResult Filter(FilterSettings settings)
		{
			var res = settings.Filter.GetFilterResult(this.EventDefinition);
			if (res.FilterResult != null)
				return res.FilterResult.Value;
			if (settings.SearchTermMatches(ev.Name) && settings.Language.ShowMember(ev))
				return FilterResult.Match;
			else
				return FilterResult.Hidden;
		}
		
		public override void Decompile(Language language, ITextOutput output, DecompilationOptions options)
		{
			language.DecompileEvent(ev, output, options);
		}
		
		
		public override bool IsPublicAPI {
			get { return IsPublicAPIInternal(ev); }
		}

		internal static bool IsPublicAPIInternal(EventDef ev)
		{
			MethodDef accessor = ev.AddMethod ?? ev.RemoveMethod;
			return accessor != null && (accessor.IsPublic || accessor.IsFamilyOrAssembly || accessor.IsFamily);
		}
		
		IMemberRef IMemberTreeNode.Member
		{
			get { return ev; }
		}

		IMDTokenProvider ITokenTreeNode.MDTokenProvider {
			get { return ev; }
		}

		public override NodePathName NodePathName {
			get { return new NodePathName("event", ev.FullName); }
		}
	}
}
