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
using System.Collections.ObjectModel;
using System.Linq;
using dnSpy.Text.MEF;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Text.Editor {
	sealed class WpfTextViewConnectionListenerService {
		readonly IWpfTextView wpfTextView;
		readonly ListenerInfo[] listenerInfos;

		public WpfTextViewConnectionListenerService(IWpfTextView wpfTextView, Lazy<IWpfTextViewConnectionListener, IContentTypeAndTextViewRoleMetadata>[] wpfTextViewConnectionListeners) {
			if (wpfTextView == null)
				throw new ArgumentNullException(nameof(wpfTextView));
			if (wpfTextViewConnectionListeners == null)
				throw new ArgumentNullException(nameof(wpfTextViewConnectionListeners));
			this.wpfTextView = wpfTextView;
			this.listenerInfos = wpfTextViewConnectionListeners.Where(a => wpfTextView.Roles.ContainsAny(a.Metadata.TextViewRoles)).Select(a => new ListenerInfo(a)).ToArray();
			wpfTextView.Closed += WpfTextView_Closed;
			wpfTextView.TextDataModel.ContentTypeChanged += TextDataModel_ContentTypeChanged;
			InitializeListeners();
		}

		sealed class ListenerInfo {
			public Lazy<IWpfTextViewConnectionListener, IContentTypeAndTextViewRoleMetadata> Lazy { get; }
			public Collection<ITextBuffer> Buffers { get; }
			public ListenerInfo(Lazy<IWpfTextViewConnectionListener, IContentTypeAndTextViewRoleMetadata> lazy) {
				Lazy = lazy;
				Buffers = new Collection<ITextBuffer>();
			}
		}

		void InitializeListeners() {
			var buffer = wpfTextView.TextBuffer;
			var contentType = wpfTextView.TextDataModel.ContentType;
			foreach (var info in listenerInfos) {
				if (contentType.IsOfAnyType(info.Lazy.Metadata.ContentTypes)) {
					info.Buffers.Add(buffer);
					info.Lazy.Value.SubjectBuffersConnected(wpfTextView, ConnectionReason.TextViewLifetime, info.Buffers);
				}
			}
		}

		void TextDataModel_ContentTypeChanged(object sender, TextDataModelContentTypeChangedEventArgs e) {
			var buffer = wpfTextView.TextBuffer;
			var coll = new Collection<ITextBuffer> { buffer };
			foreach (var info in listenerInfos) {
				var isBefore = e.BeforeContentType.IsOfAnyType(info.Lazy.Metadata.ContentTypes);
				var isAfter = e.AfterContentType.IsOfAnyType(info.Lazy.Metadata.ContentTypes);
				if ((isAfter && isBefore) || isAfter) {
					if (!info.Buffers.Contains(buffer)) {
						info.Buffers.Add(buffer);
						info.Lazy.Value.SubjectBuffersConnected(wpfTextView, ConnectionReason.ContentTypeChange, coll);
					}
				}
				else if (isBefore) {
					if (info.Buffers.Contains(buffer)) {
						info.Buffers.Remove(buffer);
						info.Lazy.Value.SubjectBuffersDisconnected(wpfTextView, ConnectionReason.ContentTypeChange, coll);
					}
				}
			}
		}

		void WpfTextView_Closed(object sender, EventArgs e) {
			foreach (var info in listenerInfos) {
				if (info.Buffers.Count > 0) {
					info.Lazy.Value.SubjectBuffersDisconnected(wpfTextView, ConnectionReason.TextViewLifetime, info.Buffers);
					info.Buffers.Clear();
				}
			}

			wpfTextView.Closed -= WpfTextView_Closed;
			wpfTextView.TextDataModel.ContentTypeChanged -= TextDataModel_ContentTypeChanged;
		}
	}
}
