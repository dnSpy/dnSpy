/*
    Copyright (C) 2014-2016 de4dot@gmail.com

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

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using dnSpy.Contracts.Language.Intellisense;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Classification;
using dnSpy.Roslyn.Shared.Text.Classification;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Roslyn.Shared.Intellisense.SignatureHelp {
	static class SignatureHelpContentTypes {
#pragma warning disable 0169
		[Export]
		[Name(ContentTypes.CSharpRoslyn + SignatureHelpConstants.ExtendedSignatureHelpContentTypeSuffix)]
		[BaseDefinition(ContentTypes.SignatureHelp)]
		static readonly ContentTypeDefinition CSharpRoslynContentTypeDefinition;

		[Export]
		[Name(ContentTypes.VisualBasicRoslyn + SignatureHelpConstants.ExtendedSignatureHelpContentTypeSuffix)]
		[BaseDefinition(ContentTypes.SignatureHelp)]
		static readonly ContentTypeDefinition VisualBasicRoslynContentTypeDefinition;
#pragma warning restore 0169
	}

	abstract class SignatureHelpTaggerProviderEx : ITaggerProvider {
		readonly IThemeClassificationTypeService themeClassificationTypeService;

		protected SignatureHelpTaggerProviderEx(IThemeClassificationTypeService themeClassificationTypeService) {
			if (themeClassificationTypeService == null)
				throw new ArgumentNullException(nameof(themeClassificationTypeService));
			this.themeClassificationTypeService = themeClassificationTypeService;
		}

		public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag {
			var session = buffer.TryGetSignatureHelpSession();
			if (session == null)
				return null;
			return new SignatureHelpTaggerEx(buffer, themeClassificationTypeService) as ITagger<T>;
		}
	}

	[Export(typeof(ITaggerProvider))]
	[TagType(typeof(IClassificationTag))]
	[ContentType(ContentTypes.CSharpRoslyn + SignatureHelpConstants.ExtendedSignatureHelpContentTypeSuffix)]
	sealed class CSharpSignatureHelpTaggerProviderEx : SignatureHelpTaggerProviderEx {
		[ImportingConstructor]
		CSharpSignatureHelpTaggerProviderEx(IThemeClassificationTypeService themeClassificationTypeService)
			: base(themeClassificationTypeService) {
		}
	}

	[Export(typeof(ITaggerProvider))]
	[TagType(typeof(IClassificationTag))]
	[ContentType(ContentTypes.VisualBasicRoslyn + SignatureHelpConstants.ExtendedSignatureHelpContentTypeSuffix)]
	sealed class VisualBasicSignatureHelpTaggerProviderEx : SignatureHelpTaggerProviderEx {
		[ImportingConstructor]
		VisualBasicSignatureHelpTaggerProviderEx(IThemeClassificationTypeService themeClassificationTypeService)
			: base(themeClassificationTypeService) {
		}
	}

	sealed class SignatureHelpTaggerEx : ITagger<IClassificationTag> {
		public event EventHandler<SnapshotSpanEventArgs> TagsChanged { add { } remove { } }

		readonly ITextBuffer buffer;
		readonly IThemeClassificationTypeService themeClassificationTypeService;

		public SignatureHelpTaggerEx(ITextBuffer buffer, IThemeClassificationTypeService themeClassificationTypeService) {
			if (buffer == null)
				throw new ArgumentNullException(nameof(buffer));
			if (themeClassificationTypeService == null)
				throw new ArgumentNullException(nameof(themeClassificationTypeService));
			this.buffer = buffer;
			this.themeClassificationTypeService = themeClassificationTypeService;
		}

		public IEnumerable<ITagSpan<IClassificationTag>> GetTags(NormalizedSnapshotSpanCollection spans) {
			var context = buffer.TryGetSignatureHelpClassifierContext();
			Debug.Assert(context != null);
			if (context == null || context.Session.IsDismissed)
				yield break;

			if (context.Type == SignatureHelpClassifierContextTypes.ParameterName) {
				var paramContext = (ParameterNameSignatureHelpClassifierContext)context;
				var parameter = paramContext.Parameter as Parameter;
				if (parameter?.Name != null) {
					var snapshot = buffer.CurrentSnapshot;
					var span = new Span(paramContext.NameOffset, parameter.Name.Length);
					Debug.Assert(span.End <= snapshot.Length);
					if (span.End > snapshot.Length)
						yield break;

					var tag = new ClassificationTag(themeClassificationTypeService.GetClassificationType(TextColor.Parameter));
					yield return new TagSpan<IClassificationTag>(new SnapshotSpan(snapshot, span), tag);
				}
			}
			else if (context.Type == SignatureHelpClassifierContextTypes.ParameterDocumentation) {
				var paramContext = (ParameterDocumentationSignatureHelpClassifierContext)context;
				var parameter = paramContext.Parameter as Parameter;
				if (parameter != null) {
					var snapshot = buffer.CurrentSnapshot;
					var snapshotLength = snapshot.Length;
					int pos = 0;
					foreach (var taggedText in parameter.DocumentationTaggedText) {
						var span = new Span(pos, taggedText.Text.Length);
						Debug.Assert(span.End <= snapshotLength);
						if (span.End > snapshotLength)
							yield break;
						var color = TextTagsHelper.ToTextColor(taggedText.Tag);
						var tag = new ClassificationTag(themeClassificationTypeService.GetClassificationType(color));
						yield return new TagSpan<IClassificationTag>(new SnapshotSpan(snapshot, span), tag);
						pos = span.End;
					}
					Debug.Assert(pos == parameter.Documentation.Length);
				}
			}
		}
	}
}
