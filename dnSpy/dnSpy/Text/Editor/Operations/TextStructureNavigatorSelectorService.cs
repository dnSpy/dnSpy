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
using System.Linq;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Editor.Operations;

namespace dnSpy.Text.Editor.Operations {
	[Export(typeof(ITextStructureNavigatorSelectorService))]
	sealed class TextStructureNavigatorSelectorService : ITextStructureNavigatorSelectorService {
		readonly IContentTypeRegistryService contentTypeRegistryService;
		readonly ITextStructureNavigatorProvider[] textStructureNavigatorProviders;
		Dictionary<IContentType, List<ITextStructureNavigatorProvider>> providerDict;

		[ImportingConstructor]
		TextStructureNavigatorSelectorService(IContentTypeRegistryService contentTypeRegistryService, [ImportMany] IEnumerable<ITextStructureNavigatorProvider> textStructureNavigatorProviders) {
			this.contentTypeRegistryService = contentTypeRegistryService;
			this.textStructureNavigatorProviders = textStructureNavigatorProviders.ToArray();
		}

		public ITextStructureNavigator GetTextStructureNavigator(ITextBuffer textBuffer) {
			if (textBuffer == null)
				throw new ArgumentNullException(nameof(textBuffer));
			return textBuffer.Properties.GetOrCreateSingletonProperty(typeof(ITextStructureNavigator), () => {
				var nav = CreateTextStructureNavigator(textBuffer, textBuffer.ContentType);
				textBuffer.ContentTypeChanged += TextBuffer_ContentTypeChanged;
				return nav;
			});
		}

		static void TextBuffer_ContentTypeChanged(object sender, ContentTypeChangedEventArgs e) {
			var textBuffer = (ITextBuffer)sender;
			textBuffer.ContentTypeChanged -= TextBuffer_ContentTypeChanged;
			bool b = textBuffer.Properties.RemoveProperty(typeof(ITextStructureNavigator));
			Debug.Assert(b);
		}

		public ITextStructureNavigator CreateTextStructureNavigator(ITextBuffer textBuffer, Guid contentType) =>
			CreateTextStructureNavigator(textBuffer, contentTypeRegistryService.GetContentType(contentType) ?? contentTypeRegistryService.UnknownContentType);
		public ITextStructureNavigator CreateTextStructureNavigator(ITextBuffer textBuffer, IContentType contentType) {
			if (textBuffer == null)
				throw new ArgumentNullException(nameof(textBuffer));
			if (contentType == null)
				throw new ArgumentNullException(nameof(contentType));

			foreach (var p in GetProviders(contentType)) {
				var nav = p.CreateTextStructureNavigator(textBuffer);
				if (nav != null)
					return nav;
			}
			Debug.Fail($"Couldn't find a {nameof(ITextStructureNavigatorProvider)}");
			return new TextStructureNavigator(textBuffer, contentTypeRegistryService.UnknownContentType);
		}

		IEnumerable<ITextStructureNavigatorProvider> GetProviders(IContentType contentType) {
			if (contentType == null)
				throw new ArgumentNullException(nameof(contentType));

			var ctDict = new Dictionary<IContentType, int>();
			GetContentTypes(ctDict, contentType, 0);

			var dict = GetProviderDictionary();
			List<ITextStructureNavigatorProvider> list;
			foreach (var c in ctDict.OrderBy(a => a.Value)) {
				if (dict.TryGetValue(c.Key, out list))
					return list;
			}
			return Array.Empty<ITextStructureNavigatorProvider>();
		}

		void GetContentTypes(Dictionary<IContentType, int> dict, IContentType contentType, int depth) {
			if (dict.ContainsKey(contentType))
				return;
			dict.Add(contentType, depth);
			foreach (var c in contentType.BaseTypes)
				GetContentTypes(dict, c, depth + 1);
		}

		Dictionary<IContentType, List<ITextStructureNavigatorProvider>> GetProviderDictionary() =>
			providerDict ?? (providerDict = GetProviderDictionary(textStructureNavigatorProviders));

		static Dictionary<IContentType, List<ITextStructureNavigatorProvider>> GetProviderDictionary(ITextStructureNavigatorProvider[] textStructureNavigatorProviders) {
			var dict = new Dictionary<IContentType, List<ITextStructureNavigatorProvider>>();
			var hash = new HashSet<IContentType>();
			var stack = new Stack<IContentType>();
			foreach (var p in textStructureNavigatorProviders) {
				foreach (var c in p.ContentTypes) {
					Debug.Assert(c != null);
					if (c == null)
						break;
					Add(dict, p, c, true);
					foreach (var bc in GetAllBaseTypes(c, hash, stack)) {
						Debug.Assert(bc != c);
						if (bc == c)
							continue;
						Add(dict, p, bc, false);
					}
				}
			}
			return dict;
		}

		static IEnumerable<IContentType> GetAllBaseTypes(IContentType contentType, HashSet<IContentType> hash, Stack<IContentType> stack) {
			hash.Clear();
			stack.Clear();
			hash.Add(contentType);
			foreach (var c in contentType.BaseTypes)
				stack.Push(c);
			while (stack.Count > 0) {
				contentType = stack.Pop();
				if (hash.Contains(contentType))
					continue;
				yield return contentType;
				hash.Add(contentType);
				foreach (var c in contentType.BaseTypes)
					stack.Push(c);
			}
		}

		static void Add(Dictionary<IContentType, List<ITextStructureNavigatorProvider>> dict, ITextStructureNavigatorProvider textStructureNavigatorProvider, IContentType contentType, bool isDef) {
			List<ITextStructureNavigatorProvider> list;
			if (!dict.TryGetValue(contentType, out list))
				dict.Add(contentType, list = new List<ITextStructureNavigatorProvider>());
			if (isDef)
				list.Insert(0, textStructureNavigatorProvider);
			else
				list.Add(textStructureNavigatorProvider);
		}
	}
}
