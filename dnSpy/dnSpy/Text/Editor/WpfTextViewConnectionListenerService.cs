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
using System.Collections.ObjectModel;
using System.Linq;
using dnSpy.Text.MEF;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Text.Editor {
	sealed class WpfTextViewConnectionListenerService {
		readonly IWpfTextView wpfTextView;
		readonly ListenerInfo[] listenerInfos;

		public WpfTextViewConnectionListenerService(IWpfTextView wpfTextView, Lazy<IWpfTextViewConnectionListener, IContentTypeAndTextViewRoleMetadata>[] wpfTextViewConnectionListeners, Lazy<ITextViewConnectionListener, IContentTypeAndTextViewRoleMetadata>[] textViewConnectionListeners) {
			if (wpfTextViewConnectionListeners == null)
				throw new ArgumentNullException(nameof(wpfTextViewConnectionListeners));
			this.wpfTextView = wpfTextView ?? throw new ArgumentNullException(nameof(wpfTextView));
			var list = new List<ListenerInfo>();
			list.AddRange(wpfTextViewConnectionListeners.Where(a => wpfTextView.Roles.ContainsAny(a.Metadata.TextViewRoles)).Select(a => new WpfTextViewListenerInfo(a)));
			list.AddRange(textViewConnectionListeners.Where(a => wpfTextView.Roles.ContainsAny(a.Metadata.TextViewRoles)).Select(a => new TextViewListenerInfo(a)));
			listenerInfos = list.ToArray();
			wpfTextView.Closed += WpfTextView_Closed;
			wpfTextView.TextDataModel.ContentTypeChanged += TextDataModel_ContentTypeChanged;
			InitializeListeners();
		}

		abstract class ListenerInfo {
			public Collection<ITextBuffer> Buffers { get; }
			public abstract IContentTypeAndTextViewRoleMetadata Metadata { get; }
			protected ListenerInfo() => Buffers = new Collection<ITextBuffer>();
			public abstract void SubjectBuffersConnected(IWpfTextView textView, ConnectionReason reason, Collection<ITextBuffer> subjectBuffers);
			public abstract void SubjectBuffersDisconnected(IWpfTextView textView, ConnectionReason reason, Collection<ITextBuffer> subjectBuffers);
		}

		sealed class WpfTextViewListenerInfo : ListenerInfo {
			Lazy<IWpfTextViewConnectionListener, IContentTypeAndTextViewRoleMetadata> Lazy { get; }
			public override IContentTypeAndTextViewRoleMetadata Metadata => Lazy.Metadata;
			public WpfTextViewListenerInfo(Lazy<IWpfTextViewConnectionListener, IContentTypeAndTextViewRoleMetadata> lazy) => Lazy = lazy;
			public override void SubjectBuffersConnected(IWpfTextView textView, ConnectionReason reason, Collection<ITextBuffer> subjectBuffers) =>
				Lazy.Value.SubjectBuffersConnected(textView, reason, subjectBuffers);
			public override void SubjectBuffersDisconnected(IWpfTextView textView, ConnectionReason reason, Collection<ITextBuffer> subjectBuffers) =>
				Lazy.Value.SubjectBuffersDisconnected(textView, reason, subjectBuffers);
		}

		sealed class TextViewListenerInfo : ListenerInfo {
			Lazy<ITextViewConnectionListener, IContentTypeAndTextViewRoleMetadata> Lazy { get; }
			public override IContentTypeAndTextViewRoleMetadata Metadata => Lazy.Metadata;
			public TextViewListenerInfo(Lazy<ITextViewConnectionListener, IContentTypeAndTextViewRoleMetadata> lazy) => Lazy = lazy;
			public override void SubjectBuffersConnected(IWpfTextView textView, ConnectionReason reason, Collection<ITextBuffer> subjectBuffers) =>
				Lazy.Value.SubjectBuffersConnected(textView, reason, subjectBuffers);
			public override void SubjectBuffersDisconnected(IWpfTextView textView, ConnectionReason reason, Collection<ITextBuffer> subjectBuffers) =>
				Lazy.Value.SubjectBuffersDisconnected(textView, reason, subjectBuffers);
		}

		void InitializeListeners() {
			var buffer = wpfTextView.TextBuffer;
			var contentType = wpfTextView.TextDataModel.ContentType;
			foreach (var info in listenerInfos) {
				if (contentType.IsOfAnyType(info.Metadata.ContentTypes)) {
					info.Buffers.Add(buffer);
					info.SubjectBuffersConnected(wpfTextView, ConnectionReason.TextViewLifetime, info.Buffers);
				}
			}
		}

		void TextDataModel_ContentTypeChanged(object sender, TextDataModelContentTypeChangedEventArgs e) {
			var buffer = wpfTextView.TextBuffer;
			var coll = new Collection<ITextBuffer> { buffer };
			foreach (var info in listenerInfos) {
				var isBefore = e.BeforeContentType.IsOfAnyType(info.Metadata.ContentTypes);
				var isAfter = e.AfterContentType.IsOfAnyType(info.Metadata.ContentTypes);
				if ((isAfter && isBefore) || isAfter) {
					if (!info.Buffers.Contains(buffer)) {
						info.Buffers.Add(buffer);
						info.SubjectBuffersConnected(wpfTextView, ConnectionReason.ContentTypeChange, coll);
					}
				}
				else if (isBefore) {
					if (info.Buffers.Contains(buffer)) {
						info.Buffers.Remove(buffer);
						info.SubjectBuffersDisconnected(wpfTextView, ConnectionReason.ContentTypeChange, coll);
					}
				}
			}
		}

		void WpfTextView_Closed(object sender, EventArgs e) {
			foreach (var info in listenerInfos) {
				if (info.Buffers.Count > 0) {
					info.SubjectBuffersDisconnected(wpfTextView, ConnectionReason.TextViewLifetime, info.Buffers);
					info.Buffers.Clear();
				}
			}

			wpfTextView.Closed -= WpfTextView_Closed;
			wpfTextView.TextDataModel.ContentTypeChanged -= TextDataModel_ContentTypeChanged;
		}
	}
}
