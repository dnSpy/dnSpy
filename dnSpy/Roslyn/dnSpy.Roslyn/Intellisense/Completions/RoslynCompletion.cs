/*
    Copyright (C) 2014-2019 de4dot@gmail.com

    This file is part of dnSpy

    dnSpy is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    dnSpy is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with dnSpy.  If not, see <http://www.gnu.org/licenses/>.
*/

using System.Collections.Generic;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Language.Intellisense;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Tags;
using Microsoft.VisualStudio.Language.Intellisense;

namespace dnSpy.Roslyn.Intellisense.Completions {
	sealed class RoslynCompletion : DsCompletion, ICustomCommit {
		public CompletionItem CompletionItem { get; }
		public RoslynCompletionSet? CompletionSet { get; set; }

		public override string Description {
			// Need to return a non-empty string or no tooltip is shown
			get => ".";
			set { }
		}

		public RoslynCompletion(CompletionItem completionItem)
			: base(completionItem.DisplayText, completionItem.FilterText) =>
			CompletionItem = completionItem;

		protected override ImageReference GetImageReference() => CompletionImageHelper.GetImageReference(CompletionItem.Tags) ?? default;

		public override IEnumerable<CompletionIcon>? AttributeIcons {
			get => GetAttributeIcons();
			set { }
		}

		IEnumerable<CompletionIcon>? GetAttributeIcons() {
			if (CompletionItem.Tags.Contains(WellKnownTags.Warning))
				return new[] { new DsCompletionIcon(DsImages.StatusWarning) };
			return null;
		}

		void ICustomCommit.Commit() => CompletionSet!.Commit(this);
	}
}
