// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

/*
Original Roslyn files:
	Controller.Session_ComputeModel.cs
	Controller.Session_UpdateModel.cs
*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.SignatureHelp;
using Microsoft.CodeAnalysis.Text;
using SIGHLP = Microsoft.CodeAnalysis.SignatureHelp;

namespace dnSpy.Roslyn.Internal.SignatureHelp {
	partial class SignatureHelpService {
		private async Task<Tuple<ISignatureHelpProvider, SignatureHelpItems>> ComputeItemsAsync(
			IList<ISignatureHelpProvider> providers,
			int caretPosition,
			SIGHLP.SignatureHelpTriggerInfo triggerInfo,
			Document document,
			CancellationToken cancellationToken) {
			ISignatureHelpProvider bestProvider = null;
			SignatureHelpItems bestItems = null;

			// TODO(cyrusn): We're calling into extensions, we need to make ourselves resilient
			// to the extension crashing.
			foreach (var provider in providers) {
				cancellationToken.ThrowIfCancellationRequested();

				var currentItems = await provider.GetItemsAsync(document, caretPosition, triggerInfo, cancellationToken).ConfigureAwait(false);
				if (currentItems != null && currentItems.ApplicableSpan.IntersectsWith(caretPosition)) {
					// If another provider provides sig help items, then only take them if they
					// start after the last batch of items.  i.e. we want the set of items that
					// conceptually are closer to where the caret position is.  This way if you have:
					//
					//  Foo(new Bar($$
					//
					// Then invoking sig help will only show the items for "new Bar(" and not also
					// the items for "Foo(..."
					if (IsBetter(bestItems, currentItems.ApplicableSpan)) {
						bestItems = new SignatureHelpItems(currentItems);
						bestProvider = provider;
					}
				}
			}

			return Tuple.Create(bestProvider, bestItems);
		}

		private bool IsBetter(SignatureHelpItems bestItems, TextSpan currentTextSpan) {
			// If we have no best text span, then this span is definitely better.
			if (bestItems == null) {
				return true;
			}

			// Otherwise we want the one that is conceptually the innermost signature.  So it's
			// only better if the distance from it to the caret position is less than the best
			// one so far.
			return currentTextSpan.Start > bestItems.ApplicableSpan.Start;
		}

		SignatureHelpResult GetSignatureHelpResult(Tuple<ISignatureHelpProvider, SignatureHelpItems> res, Document document) {
			// Code is from the end of ComputeModelInBackgroundAsync()
			var items = res.Item2;
			if (items == null)
				return null;
			var selectedItem = GetSelectedItem(items, res.Item1);
			var syntaxFactsService = document?.Project?.LanguageServices?.GetService<Microsoft.CodeAnalysis.LanguageServices.ISyntaxFactsService>();
			var isCaseSensitive = syntaxFactsService == null || syntaxFactsService.IsCaseSensitive;
			var selection = DefaultSignatureHelpSelector.GetSelection(items.Items,
				selectedItem, items.ArgumentIndex, items.ArgumentCount, items.ArgumentName, isCaseSensitive);
			return new SignatureHelpResult(items, selection.SelectedItem, selection.SelectedParameter);
		}

		private static SignatureHelpItem GetSelectedItem(SignatureHelpItems items, ISignatureHelpProvider provider) {
			// Try to find the most appropriate item in the list to select by default.

			// If the provider specified one a selected item, then always stick with that one. 
			if (items.SelectedItemIndex.HasValue) {
				return items.Items[items.SelectedItemIndex.Value];
			}

			// If the provider did not pick a default, and it's the same provider as the previous
			// model we have, then try to return the same item that we had before. 
			//if (currentModel != null && currentModel.Provider == provider) {
			//	return items.Items.FirstOrDefault(i => DisplayPartsMatch(i, currentModel.SelectedItem)) ?? items.Items.First();
			//}

			// Otherwise, just pick the first item we have.
			return items.Items.First();
		}

		private static bool DisplayPartsMatch(SignatureHelpItem i1, SignatureHelpItem i2) {
			return i1.GetAllParts().SequenceEqual(i2.GetAllParts(), CompareParts);
		}

		private static bool CompareParts(TaggedText p1, TaggedText p2) {
			return p1.ToString() == p2.ToString();
		}

		internal struct SignatureHelpSelection {
			private readonly SignatureHelpItem _selectedItem;
			private readonly int? _selectedParameter;

			public SignatureHelpSelection(SignatureHelpItem selectedItem, int? selectedParameter) : this() {
				_selectedItem = selectedItem;
				_selectedParameter = selectedParameter;
			}

			public int? SelectedParameter { get { return _selectedParameter; } }
			public SignatureHelpItem SelectedItem { get { return _selectedItem; } }
		}

		internal static class DefaultSignatureHelpSelector {
			public static SignatureHelpSelection GetSelection(
				IList<SignatureHelpItem> items,
				SignatureHelpItem selectedItem,
				int argumentIndex,
				int argumentCount,
				string argumentName,
				bool isCaseSensitive) {
				var bestItem = GetBestItem(selectedItem, items, argumentIndex, argumentCount, argumentName, isCaseSensitive);
				var selectedParameter = GetSelectedParameter(bestItem, argumentIndex, argumentName, isCaseSensitive);
				return new SignatureHelpSelection(bestItem, selectedParameter);
			}

			private static int GetSelectedParameter(SignatureHelpItem bestItem, int parameterIndex, string parameterName, bool isCaseSensitive) {
				if (parameterName != null) {
					var comparer = isCaseSensitive ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase;
					var index = bestItem.Parameters.IndexOf(p => comparer.Equals(p.Name, parameterName));
					if (index >= 0) {
						return index;
					}
				}

				return parameterIndex;
			}

			private static SignatureHelpItem GetBestItem(
				SignatureHelpItem currentItem, IList<SignatureHelpItem> filteredItems, int selectedParameter, int argumentCount, string name, bool isCaseSensitive) {
				// If the current item is still applicable, then just keep it.
				if (filteredItems.Contains(currentItem) &&
					IsApplicable(currentItem, argumentCount, name, isCaseSensitive)) {
					return currentItem;
				}

				// Try to find the first applicable item.  If there is none, then that means the
				// selected parameter was outside the bounds of all methods.  i.e. all methods only
				// went up to 3 parameters, and selected parameter is 3 or higher.  In that case,
				// just pick the very last item as it is closest in parameter count.
				var result = filteredItems.FirstOrDefault(i => IsApplicable(i, argumentCount, name, isCaseSensitive));
				if (result != null) {
					return result;
				}

				// if we couldn't find a best item, and they provided a name, then try again without
				// a name.
				if (name != null) {
					return GetBestItem(currentItem, filteredItems, selectedParameter, argumentCount, null, isCaseSensitive);
				}

				// If we don't have an item that can take that number of parameters, then just pick
				// the last item.  Or stick with the current item if the last item isn't any better.
				var lastItem = filteredItems.Last();
				if (currentItem.IsVariadic || currentItem.Parameters.Length == lastItem.Parameters.Length) {
					return currentItem;
				}

				return lastItem;
			}

			private static bool IsApplicable(SignatureHelpItem item, int argumentCount, string name, bool isCaseSensitive) {
				// If they provided a name, then the item is only valid if it has a parameter that
				// matches that name.
				if (name != null) {
					var comparer = isCaseSensitive ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase;
					return item.Parameters.Any(p => comparer.Equals(p.Name, name));
				}

				// An item is applicable if it has at least as many parameters as the selected
				// parameter index.  i.e. if it has 2 parameters and we're at index 0 or 1 then it's
				// applicable.  However, if it has 2 parameters and we're at index 2, then it's not
				// applicable.  
				if (item.Parameters.Length >= argumentCount) {
					return true;
				}

				// However, if it is variadic then it is applicable as it can take any number of
				// items.
				if (item.IsVariadic) {
					return true;
				}

				// Also, we special case 0.  that's because if the user has "Foo(" and foo takes no
				// arguments, then we'll see that it's arg count is 0.  We still want to consider
				// any item applicable here though.
				return argumentCount == 0;
			}
		}
	}

	static class IListExtensions {
		public static int IndexOf<T>(this IList<T> list, Func<T, bool> predicate) {
			for (var i = 0; i < list.Count; i++) {
				if (predicate(list[i])) {
					return i;
				}
			}

			return -1;
		}

		public static bool SequenceEqual<T>(this IEnumerable<T> first, IEnumerable<T> second, Func<T, T, bool> comparer) {
			Debug.Assert(comparer != null);

			if (first == second) {
				return true;
			}

			if (first == null || second == null) {
				return false;
			}

			using (var enumerator = first.GetEnumerator())
			using (var enumerator2 = second.GetEnumerator()) {
				while (enumerator.MoveNext()) {
					if (!enumerator2.MoveNext() || !comparer(enumerator.Current, enumerator2.Current)) {
						return false;
					}
				}

				if (enumerator2.MoveNext()) {
					return false;
				}
			}

			return true;
		}
	}
}
