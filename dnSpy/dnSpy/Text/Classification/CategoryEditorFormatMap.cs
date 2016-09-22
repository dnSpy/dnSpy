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
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using Microsoft.VisualStudio.Text.Classification;

namespace dnSpy.Text.Classification {
	sealed class CategoryEditorFormatMap : IEditorFormatMap {
		public bool IsInBatchUpdate { get; private set; }
		public event EventHandler<FormatItemsEventArgs> FormatMappingChanged;

		readonly Dispatcher dispatcher;
		readonly IEditorFormatDefinitionService editorFormatDefinitionService;
		readonly HashSet<string> batchChanges;
		readonly Dictionary<string, ResourceDictionary> resourceDicts;

		public CategoryEditorFormatMap(Dispatcher dispatcher, IEditorFormatDefinitionService editorFormatDefinitionService) {
			if (dispatcher == null)
				throw new ArgumentNullException(nameof(dispatcher));
			if (editorFormatDefinitionService == null)
				throw new ArgumentNullException(nameof(editorFormatDefinitionService));
			this.dispatcher = dispatcher;
			this.editorFormatDefinitionService = editorFormatDefinitionService;
			this.batchChanges = new HashSet<string>();
			this.resourceDicts = new Dictionary<string, ResourceDictionary>(StringComparer.Ordinal);
		}

		public void BeginBatchUpdate() {
			dispatcher.VerifyAccess();
			if (IsInBatchUpdate)
				throw new InvalidOperationException();
			IsInBatchUpdate = true;
		}

		public void EndBatchUpdate() {
			dispatcher.VerifyAccess();
			if (!IsInBatchUpdate)
				throw new InvalidOperationException();
			IsInBatchUpdate = false;
			if (!startedEndBatchUpdateCore && batchChanges.Count > 0) {
				startedEndBatchUpdateCore = true;
				// Use Send so we get called as soon as the theme-changed handlers have gotten
				// a chance to update all colors. Without this delay, there could be 15 events
				// with the same data!
				dispatcher.BeginInvoke(DispatcherPriority.Send, new Action(EndBatchUpdateCore));
			}
		}
		bool startedEndBatchUpdateCore;

		void EndBatchUpdateCore() {
			Debug.Assert(!IsInBatchUpdate);
			Debug.Assert(startedEndBatchUpdateCore);
			Debug.Assert(batchChanges.Count != 0);

			startedEndBatchUpdateCore = false;
			if (batchChanges.Count == 0)
				return;
			var array = batchChanges.ToArray();
			batchChanges.Clear();
			FormatMappingChanged?.Invoke(this, new FormatItemsEventArgs(new ReadOnlyCollection<string>(array)));
		}

		public ResourceDictionary GetProperties(string key) {
			if (key == null)
				throw new ArgumentNullException(nameof(key));
			ResourceDictionary resDict;
			if (resourceDicts.TryGetValue(key, out resDict))
				return resDict;
			resDict = editorFormatDefinitionService.GetDefinition(key)?.CreateResourceDictionary() ?? new ResourceDictionary();
			resourceDicts.Add(key, resDict);
			return resDict;
		}

		public void AddProperties(string key, ResourceDictionary properties) =>
			SetProperties(key, properties);
		public void SetProperties(string key, ResourceDictionary properties) {
			if (key == null)
				throw new ArgumentNullException(nameof(key));
			resourceDicts[key] = properties;
			if (IsInBatchUpdate)
				batchChanges.Add(key);
			else
				FormatMappingChanged?.Invoke(this, new FormatItemsEventArgs(new ReadOnlyCollection<string>(new[] { key })));
		}
	}
}
