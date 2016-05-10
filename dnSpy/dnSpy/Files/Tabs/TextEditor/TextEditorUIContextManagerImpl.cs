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
using dnSpy.Contracts.Files.Tabs.TextEditor;
using dnSpy.Decompiler.Shared;

namespace dnSpy.Files.Tabs.TextEditor {
	interface ITextEditorUIContextManagerImpl : ITextEditorUIContextManager {
		void RaiseAddedEvent(ITextEditorUIContext uiContext);
		void RaiseRemovedEvent(ITextEditorUIContext uiContext);
		void RaiseNewContentEvent(ITextEditorUIContext uiContext, ITextOutput output, TextEditorUIContextListener listener, double order);
	}

	[Export(typeof(ITextEditorUIContextManager)), Export(typeof(ITextEditorUIContextManagerImpl))]
	sealed class TextEditorUIContextManagerImpl : ITextEditorUIContextManagerImpl {
		sealed class ListenerInfo {
			public readonly TextEditorUIContextListener Listener;

			public double Order { get; }

			public void Execute(TextEditorUIContextListenerEvent @event, ITextEditorUIContext uiContext, object data) =>
				Listener(@event, uiContext, data);

			public ListenerInfo(TextEditorUIContextListener listener, double order) {
				this.Listener = listener;
				this.Order = order;
			}
		}

		readonly List<ListenerInfo> listeners;

		TextEditorUIContextManagerImpl() {
			this.listeners = new List<ListenerInfo>();
		}

		static void Sort(List<ListenerInfo> listeners) =>
			listeners.Sort((a, b) => a.Order.CompareTo(b.Order));

		public void Add(TextEditorUIContextListener listener, double order = double.MaxValue) {
			if (listener == null)
				throw new ArgumentNullException();
			listeners.Add(new ListenerInfo(listener, order));
			Sort(listeners);
		}

		public void Remove(TextEditorUIContextListener listener) {
			if (listener == null)
				throw new ArgumentNullException();
			for (int i = 0; i < listeners.Count; i++) {
				var info = listeners[i];
				if (info.Listener.Method == listener.Method && info.Listener.Target == listener.Target) {
					listeners.RemoveAt(i);
					return;
				}
			}
			Debug.Fail(string.Format("Couldn't remove a listener: {0}", listener));
		}

		public void RaiseAddedEvent(ITextEditorUIContext uiContext) {
			if (uiContext == null)
				throw new ArgumentNullException();
			foreach (var info in listeners.ToArray())
				info.Execute(TextEditorUIContextListenerEvent.Added, uiContext, null);
		}

		public void RaiseRemovedEvent(ITextEditorUIContext uiContext) {
			if (uiContext == null)
				throw new ArgumentNullException();
			foreach (var info in listeners.ToArray())
				info.Execute(TextEditorUIContextListenerEvent.Removed, uiContext, null);
		}

		public void RaiseNewContentEvent(ITextEditorUIContext uiContext, ITextOutput output, TextEditorUIContextListener listener, double order) {
			if (uiContext == null)
				throw new ArgumentNullException();
			if (output == null)
				throw new ArgumentNullException();
			var infos = new List<ListenerInfo>(listeners);
			infos.Add(new ListenerInfo(listener, order));
			Sort(infos);
			foreach (var info in infos)
				info.Execute(TextEditorUIContextListenerEvent.NewContent, uiContext, output);
		}
	}
}
