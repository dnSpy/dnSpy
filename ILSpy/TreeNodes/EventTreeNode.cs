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
using ICSharpCode.Decompiler;
using Mono.Cecil;

namespace ICSharpCode.ILSpy.TreeNodes
{
	/// <summary>
	/// Represents an event in the TreeView.
	/// </summary>
	public sealed class EventTreeNode : ILSpyTreeNode, IMemberTreeNode
	{
		readonly EventDefinition ev;
		
		public EventTreeNode(EventDefinition ev)
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
		
		public EventDefinition EventDefinition {
			get { return ev; }
		}
		
		public override object Text {
			get { return HighlightSearchMatch(ev.Name, " : " + this.Language.TypeToString(ev.EventType, false, ev)); }
		}
		
		public override object Icon
		{
			get { return Images.GetIcon(MemberIcon.Event, GetOverlayIcon(ev.AddMethod.Attributes), ev.AddMethod.IsStatic); }
		}

		private static OverlayIcon GetOverlayIcon(MethodAttributes methodAttributes)
		{
			switch (methodAttributes & MethodAttributes.MemberAccessMask) {
				case MethodAttributes.Public:
					return OverlayIcon.Public;
				case MethodAttributes.Assembly:
				case MethodAttributes.FamANDAssem:
					return OverlayIcon.Internal;
				case MethodAttributes.Family:
				case MethodAttributes.FamORAssem:
					return OverlayIcon.Protected;
				case MethodAttributes.Private:
					return OverlayIcon.Private;
				default:
					throw new NotSupportedException();
			}
		}

		public override FilterResult Filter(FilterSettings settings)
		{
			if (settings.SearchTermMatches(ev.Name) && settings.Language.ShowMember(ev))
				return FilterResult.Match;
			else
				return FilterResult.Hidden;
		}
		
		public override void Decompile(Language language, ITextOutput output, DecompilationOptions options)
		{
			language.DecompileEvent(ev, output, options);
		}
		
		MemberReference IMemberTreeNode.Member {
			get { return ev; }
		}
	}
}
