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
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Linq;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Editor;
using dnSpy.Text;
using dnSpy.Text.MEF;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Language.Intellisense {
	[Export(typeof(IWpfTextViewConnectionListener))]
	[ContentType(ContentTypes.Any)]
	[TextViewRole(PredefinedTextViewRoles.Editable)]
	[TextViewRole(PredefinedTextViewRoles.EmbeddedPeekTextView)]
	[TextViewRole(PredefinedDsTextViewRoles.CanHaveIntellisenseControllers)]
	sealed class IntellisenseControllerService : IWpfTextViewConnectionListener {
		readonly Lazy<IIntellisenseControllerProvider, IContentTypeMetadata>[] intellisenseControllerProviders;

		[ImportingConstructor]
		IntellisenseControllerService([ImportMany] IEnumerable<Lazy<IIntellisenseControllerProvider, IContentTypeMetadata>> intellisenseControllerProviders) {
			this.intellisenseControllerProviders = intellisenseControllerProviders.ToArray();
		}

		sealed class TextViewState {
			readonly IWpfTextView wpfTextView;
			readonly ControllerInfo[] controllerInfos;

			public TextViewState(IWpfTextView wpfTextView, Lazy<IIntellisenseControllerProvider, IContentTypeMetadata>[] intellisenseControllerProviders) {
				this.wpfTextView = wpfTextView;
				this.controllerInfos = intellisenseControllerProviders.Select(a => new ControllerInfo(a)).ToArray();
				wpfTextView.Closed += WpfTextView_Closed;
			}

			public void SubjectBuffersConnected(ConnectionReason reason, Collection<ITextBuffer> subjectBuffers) {
				var filteredBuffers = new List<ITextBuffer>(subjectBuffers.Count);
				foreach (var info in controllerInfos) {
					info.FilterBuffers(subjectBuffers, filteredBuffers);
					if (filteredBuffers.Count == 0)
						continue;
					if (info.Controller == null)
						info.Controller = info.Lazy.Value.TryCreateIntellisenseController(wpfTextView, filteredBuffers);
					else {
						foreach (var buffer in filteredBuffers)
							info.Controller.ConnectSubjectBuffer(buffer);
					}
				}
			}

			public void SubjectBuffersDisconnected(ConnectionReason reason, Collection<ITextBuffer> subjectBuffers) {
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

			void WpfTextView_Closed(object sender, EventArgs e) {
				foreach (var info in controllerInfos)
					info.Controller?.Detach(wpfTextView);
				wpfTextView.Closed -= WpfTextView_Closed;
			}
		}

		sealed class ControllerInfo {
			public Lazy<IIntellisenseControllerProvider, IContentTypeMetadata> Lazy { get; }
			public IIntellisenseController Controller { get; set; }

			public ControllerInfo(Lazy<IIntellisenseControllerProvider, IContentTypeMetadata> lazy) {
				Lazy = lazy;
			}

			public List<ITextBuffer> FilterBuffers(IList<ITextBuffer> buffers, List<ITextBuffer> filteredBuffers) {
				filteredBuffers.Clear();
				foreach (var buffer in buffers) {
					if (buffer.ContentType.IsOfAnyType(Lazy.Metadata.ContentTypes))
						filteredBuffers.Add(buffer);
				}
				return filteredBuffers;
			}
		}

		TextViewState GetTextViewState(IWpfTextView textView) => textView.Properties.GetOrCreateSingletonProperty(typeof(TextViewState), () => new TextViewState(textView, intellisenseControllerProviders));

		public void SubjectBuffersConnected(IWpfTextView textView, ConnectionReason reason, Collection<ITextBuffer> subjectBuffers) {
			if (textView.IsClosed)
				return;
			GetTextViewState(textView).SubjectBuffersConnected(reason, subjectBuffers);
		}

		public void SubjectBuffersDisconnected(IWpfTextView textView, ConnectionReason reason, Collection<ITextBuffer> subjectBuffers) =>
			GetTextViewState(textView).SubjectBuffersDisconnected(reason, subjectBuffers);
	}
}
