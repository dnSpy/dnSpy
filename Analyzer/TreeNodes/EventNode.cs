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
using dnlib.DotNet;
using dnSpy.Contracts.Highlighting;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Languages;
using dnSpy.Contracts.TreeView;
using dnSpy.NRefactory;
using dnSpy.Shared.UI.Files.TreeView;
using dnSpy.Shared.UI.Highlighting;
using ICSharpCode.Decompiler;

namespace dnSpy.Analyzer.TreeNodes {
	sealed class EventNode : EntityNode {
		readonly EventDef analyzedEvent;
		readonly bool hidesParent;

		public EventNode(EventDef analyzedEvent, bool hidesParent = false) {
			if (analyzedEvent == null)
				throw new ArgumentNullException("analyzedEvent");
			this.analyzedEvent = analyzedEvent;
			this.hidesParent = hidesParent;
		}

		public override void Initialize() {
			this.TreeNode.LazyLoading = true;
		}

		public override IMemberRef Member {
			get { return analyzedEvent; }
		}

		public override IMDTokenProvider Reference {
			get { return analyzedEvent; }
		}

		protected override ImageReference GetIcon(IDotNetImageManager dnImgMgr) {
			return dnImgMgr.GetImageReference(analyzedEvent);
		}

		protected override void Write(ISyntaxHighlightOutput output, ILanguage language) {
			if (hidesParent) {
				output.Write("(", TextTokenType.Operator);
				output.Write("hides", TextTokenType.Text);
				output.Write(")", TextTokenType.Operator);
				output.WriteSpace();
			}
			language.WriteType(output, analyzedEvent.DeclaringType, true);
			output.Write(".", TextTokenType.Operator);
			new NodePrinter().Write(output, language, analyzedEvent, Context.ShowToken);
		}

		public override IEnumerable<ITreeNodeData> CreateChildren() {
			if (analyzedEvent.AddMethod != null)
				yield return new EventAccessorNode(analyzedEvent.AddMethod, "add");

			if (analyzedEvent.RemoveMethod != null)
				yield return new EventAccessorNode(analyzedEvent.RemoveMethod, "remove");

			foreach (var accessor in analyzedEvent.OtherMethods)
				yield return new EventAccessorNode(accessor, null);

			if (EventFiredByNode.CanShow(analyzedEvent))
				yield return new EventFiredByNode(analyzedEvent);

			if (EventOverridesNode.CanShow(analyzedEvent))
				yield return new EventOverridesNode(analyzedEvent);

			if (InterfaceEventImplementedByNode.CanShow(analyzedEvent))
				yield return new InterfaceEventImplementedByNode(analyzedEvent);
		}

		public static IAnalyzerTreeNodeData TryCreateAnalyzer(IMemberRef member, ILanguage language, DecompilerSettings decompilerSettings) {
			if (CanShow(member, language, decompilerSettings))
				return new EventNode(member as EventDef);
			else
				return null;
		}

		public static bool CanShow(IMemberRef member, ILanguage language, DecompilerSettings decompilerSettings) {
			var eventDef = member as EventDef;
			if (eventDef == null)
				return false;

			return !language.ShowMember(eventDef.AddMethod ?? eventDef.RemoveMethod, decompilerSettings.Clone())
				|| EventOverridesNode.CanShow(eventDef);
		}
	}
}
