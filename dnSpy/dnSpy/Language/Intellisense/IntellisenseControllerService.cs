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

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Editor;
using dnSpy.Text;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Language.Intellisense {
	[Export(typeof(ITextViewConnectionListener))]
	[ContentType(ContentTypes.Any)]
	[TextViewRole(PredefinedTextViewRoles.Editable)]
	[TextViewRole(PredefinedTextViewRoles.EmbeddedPeekTextView)]
	[TextViewRole(PredefinedDsTextViewRoles.CanHaveIntellisenseControllers)]
	sealed class IntellisenseControllerService : ITextViewConnectionListener {
		readonly Lazy<IIntellisenseControllerProvider, IContentTypeMetadata>[] intellisenseControllerProviders;

		[ImportingConstructor]
		IntellisenseControllerService([ImportMany] IEnumerable<Lazy<IIntellisenseControllerProvider, IContentTypeMetadata>> intellisenseControllerProviders) => this.intellisenseControllerProviders = intellisenseControllerProviders.ToArray();

		sealed class TextViewState {
			readonly ITextView textView;
			readonly ControllerInfo[] controllerInfos;

			public TextViewState(ITextView textView, Lazy<IIntellisenseControllerProvider, IContentTypeMetadata>[] intellisenseControllerProviders) {
				this.textView = textView;
				controllerInfos = intellisenseControllerProviders.Select(a => new ControllerInfo(a)).ToArray();
				textView.Closed += TextView_Closed;
			}

			public void SubjectBuffersConnected(ConnectionReason reason, IReadOnlyCollection<ITextBuffer> subjectBuffers) {
				var filteredBuffers = new List<ITextBuffer>(subjectBuffers.Count);
				foreach (var info in controllerInfos) {
					info.FilterBuffers(subjectBuffers, filteredBuffers);
					if (filteredBuffers.Count == 0)
						continue;
					if (info.Controller == null)
						info.Controller = info.Lazy.Value.TryCreateIntellisenseController(textView, filteredBuffers);
					else {
						foreach (var buffer in filteredBuffers)
							info.Controller.ConnectSubjectBuffer(buffer);
					}
				}
			}

			public void SubjectBuffersDisconnected(ConnectionReason reason, IReadOnlyCollection<ITextBuffer> subjectBuffers) {
				var filteredBuffers = new List<ITextBuffer>(subjectBuffers.Count);
				foreach (var info in controllerInfos) {
					if (info.Controller == null)
						continue;
					info.FilterBuffers(subjectBuffers, filteredBuffers);
					if (filteredBuffers.Count == 0)
						continue;
					foreach (var buffer in filteredBuffers)
						info.Controller.DisconnectSubjectBuffer(buffer);
				}
			}

			void TextView_Closed(object sender, EventArgs e) {
				foreach (var info in controllerInfos)
					info.Controller?.Detach(textView);
				textView.Closed -= TextView_Closed;
			}
		}

		sealed class ControllerInfo {
			public Lazy<IIntellisenseControllerProvider, IContentTypeMetadata> Lazy { get; }
			public IIntellisenseController Controller { get; set; }

			public ControllerInfo(Lazy<IIntellisenseControllerProvider, IContentTypeMetadata> lazy) => Lazy = lazy;

			public List<ITextBuffer> FilterBuffers(IReadOnlyCollection<ITextBuffer> buffers, List<ITextBuffer> filteredBuffers) {
				filteredBuffers.Clear();
				foreach (var buffer in buffers) {
					if (buffer.ContentType.IsOfAnyType(Lazy.Metadata.ContentTypes))
						filteredBuffers.Add(buffer);
				}
				return filteredBuffers;
			}
		}

		TextViewState GetTextViewState(ITextView textView) => textView.Properties.GetOrCreateSingletonProperty(typeof(TextViewState), () => new TextViewState(textView, intellisenseControllerProviders));

		public void SubjectBuffersConnected(ITextView textView, ConnectionReason reason, IReadOnlyCollection<ITextBuffer> subjectBuffers) {
			if (textView.IsClosed)
				return;
			GetTextViewState(textView).SubjectBuffersConnected(reason, subjectBuffers);
		}

		public void SubjectBuffersDisconnected(ITextView textView, ConnectionReason reason, IReadOnlyCollection<ITextBuffer> subjectBuffers) =>
			GetTextViewState(textView).SubjectBuffersDisconnected(reason, subjectBuffers);
	}
}
