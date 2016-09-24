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
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading;
using dnSpy.Roslyn.Internal.SignatureHelp;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;

namespace dnSpy.Roslyn.Shared.Intellisense.SignatureHelp {
	sealed class Signature : ISignature {
		public ITrackingSpan ApplicableToSpan { get; }
		public string Content { get; }
		public string PrettyPrintedContent { get; }
		public IList<TaggedText> ContentTaggedText { get; }
		public IList<TaggedText> PrettyPrintedContentTaggedText { get; }
		public string Documentation => null;
		public ReadOnlyCollection<IParameter> Parameters { get; }
		public bool IsSelected { get; }

		public IParameter CurrentParameter {
			get { return currentParameter; }
			set {
				if (value == null)
					throw new ArgumentNullException(nameof(value));
				var newParam = value as Parameter;
				if (newParam == null)
					throw new ArgumentException();
				if (currentParameter == newParam)
					return;
				var oldParam = currentParameter;
				currentParameter = newParam;
				CurrentParameterChanged?.Invoke(this, new CurrentParameterChangedEventArgs(oldParam, currentParameter));
			}
		}
		Parameter currentParameter;

		public event EventHandler<CurrentParameterChangedEventArgs> CurrentParameterChanged;

		readonly SignatureHelpItem item;

		struct Builder {
			public string Content { get; }
			public string PrettyPrintedContent { get; }
			public IList<TaggedText> ContentTaggedText { get; }
			public IList<TaggedText> PrettyPrintedContentTaggedText { get; }
			public List<IParameter> Parameters { get; }

			readonly SignatureHelpItem item;
			readonly StringBuilder content;
			readonly List<TaggedText> contentTagged;

			public Builder(Signature signature, SignatureHelpItem item, int? selectedParameter) {
				this = default(Builder);
				this.item = item;
				this.content = new StringBuilder();
				this.contentTagged = new List<TaggedText>();
				this.Parameters = new List<IParameter>();

				Add(item.PrefixDisplayParts);
				int pi = 0;
				foreach (var parameter in item.Parameters) {
					if (pi > 0)
						Add(item.SeparatorDisplayParts);
					pi++;
					Add(parameter.PrefixDisplayParts);
					int paramStart = content.Length;
					if (parameter.IsOptional)
						Add(new TaggedText(TextTags.Punctuation, "["));
					Add(parameter.DisplayParts);
					if (parameter.IsOptional)
						Add(new TaggedText(TextTags.Punctuation, "]"));
					int paramEnd = content.Length;
					Add(parameter.SuffixDisplayParts);

					var locus = Span.FromBounds(paramStart, paramEnd);
					var prettyPrintedLocus = Span.FromBounds(paramStart, paramEnd);
					Parameters.Add(new Parameter(signature, parameter, locus, prettyPrintedLocus));
				}
				Add(item.SuffixDisplayParts);
				if (selectedParameter != null && (uint)selectedParameter.Value < (uint)item.Parameters.Length) {
					var parameter = item.Parameters[selectedParameter.Value];
					Add(parameter.SelectedDisplayParts);
				}
				Add(item.DescriptionParts);
				int docCount = 0;
				foreach (var taggedText in item.DocumentationFactory(CancellationToken.None)) {
					if (docCount == 0)
						Add(new TaggedText(TextTags.LineBreak, "\r\n"));
					docCount++;
					Add(taggedText);
				}

				Content = content.ToString();
				ContentTaggedText = contentTagged;
				PrettyPrintedContent = Content;
				PrettyPrintedContentTaggedText = ContentTaggedText;
			}

			void Add(TaggedText taggedText) {
				content.Append(taggedText.Text);
				contentTagged.Add(taggedText);
			}

			void Add(IList<TaggedText> parts) {
				for (int i = 0; i < parts.Count; i++) {
					var taggedText = parts[i];
					content.Append(taggedText.Text);
					contentTagged.Add(taggedText);
				}
			}

			void Add(ImmutableArray<TaggedText> parts) {
				foreach (var taggedText in parts) {
					content.Append(taggedText.Text);
					contentTagged.Add(taggedText);
				}
			}
		}

		public Signature(ITrackingSpan applicableToSpan, SignatureHelpItem item, bool isSelected, int? selectedParameter) {
			if (applicableToSpan == null)
				throw new ArgumentNullException(nameof(applicableToSpan));
			if (item == null)
				throw new ArgumentNullException(nameof(item));
			IsSelected = isSelected;
			ApplicableToSpan = applicableToSpan;
			this.item = item;

			var builder = new Builder(this, item, selectedParameter);

			Content = builder.Content;
			ContentTaggedText = builder.ContentTaggedText;
			PrettyPrintedContent = builder.PrettyPrintedContent;
			PrettyPrintedContentTaggedText = builder.PrettyPrintedContentTaggedText;
			Parameters = new ReadOnlyCollection<IParameter>(builder.Parameters);

			if (selectedParameter != null) {
				if ((uint)selectedParameter.Value < (uint)Parameters.Count)
					CurrentParameter = Parameters[selectedParameter.Value];
				else if (item.IsVariadic && Parameters.Count > 0)
					CurrentParameter = Parameters[Parameters.Count - 1];
			}
		}
	}
}
