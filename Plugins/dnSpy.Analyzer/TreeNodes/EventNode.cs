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
using dnSpy.Analyzer.Properties;
using dnSpy.Contracts.Highlighting;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Languages;
using dnSpy.Contracts.TreeView;
using dnSpy.Decompiler.Shared;
using dnSpy.Shared.Files.TreeView;
using dnSpy.Shared.Highlighting;

namespace dnSpy.Analyzer.TreeNodes {
	sealed class EventNode : EntityNode {
		readonly EventDef analyzedEvent;
		readonly bool hidesParent;

		public EventNode(EventDef analyzedEvent, bool hidesParent = false) {
			if (analyzedEvent == null)
				throw new ArgumentNullException(nameof(analyzedEvent));
			this.analyzedEvent = analyzedEvent;
			this.hidesParent = hidesParent;
		}

		public override void Initialize() => this.TreeNode.LazyLoading = true;
		public override IMemberRef Member => analyzedEvent;
		public override IMDTokenProvider Reference => analyzedEvent;
		protected override ImageReference GetIcon(IDotNetImageManager dnImgMgr) => dnImgMgr.GetImageReference(analyzedEvent);

		protected override void Write(ISyntaxHighlightOutput output, ILanguage language) {
			if (hidesParent) {
				output.Write("(", BoxedTextTokenKind.Punctuation);
				output.Write(dnSpy_Analyzer_Resources.HidesParent, BoxedTextTokenKind.Text);
				output.Write(")", BoxedTextTokenKind.Punctuation);
				output.WriteSpace();
			}
			language.WriteType(output, analyzedEvent.DeclaringType, true);
			output.Write(".", BoxedTextTokenKind.Operator);
			new NodePrinter().Write(output, language, analyzedEvent, Context.ShowToken);
		}

		public override IEnumerable<ITreeNodeData> CreateChildren() {
			if (analyzedEvent.AddMethod != null)
				yield return new EventAccessorNode(analyzedEvent.AddMethod, dnSpy_Analyzer_Resources.EventAdderTreeNodeName);

			if (analyzedEvent.RemoveMethod != null)
				yield return new EventAccessorNode(analyzedEvent.RemoveMethod, dnSpy_Analyzer_Resources.EventRemoverTreeNodeName);

			foreach (var accessor in analyzedEvent.OtherMethods)
				yield return new EventAccessorNode(accessor, null);

			if (EventFiredByNode.CanShow(analyzedEvent))
				yield return new EventFiredByNode(analyzedEvent);

			if (EventOverridesNode.CanShow(analyzedEvent))
				yield return new EventOverridesNode(analyzedEvent);

			if (InterfaceEventImplementedByNode.CanShow(analyzedEvent))
				yield return new InterfaceEventImplementedByNode(analyzedEvent);
		}

		public static IAnalyzerTreeNodeData TryCreateAnalyzer(IMemberRef member, ILanguage language) {
			if (CanShow(member, language))
				return new EventNode(member as EventDef);
			else
				return null;
		}

		public static bool CanShow(IMemberRef member, ILanguage language) {
			var eventDef = member as EventDef;
			if (eventDef == null)
				return false;

			return !language.ShowMember(eventDef.AddMethod ?? eventDef.RemoveMethod)
				|| EventOverridesNode.CanShow(eventDef);
		}
	}
}
