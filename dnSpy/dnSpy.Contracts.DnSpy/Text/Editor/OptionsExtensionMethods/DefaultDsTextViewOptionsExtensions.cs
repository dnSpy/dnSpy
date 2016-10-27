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
using Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Contracts.Text.Editor.OptionsExtensionMethods {
	/// <summary>
	/// <see cref="DefaultDsTextViewOptions"/> extension methods
	/// </summary>
	public static class DefaultDsTextViewOptionsExtensions {
		/// <summary>
		/// Returns true if the user can change overwrite mode
		/// </summary>
		/// <param name="options">Options</param>
		/// <returns></returns>
		public static bool IsCanChangeOverwriteModeEnabled(this IEditorOptions options) {
			if (options == null)
				throw new ArgumentNullException(nameof(options));
			return options.GetOptionValue(DefaultDsTextViewOptions.CanChangeOverwriteModeId);
		}

		/// <summary>
		/// Returns true if the user can enable or disable use-visible-whitespace option
		/// </summary>
		/// <param name="options">Options</param>
		/// <returns></returns>
		public static bool IsCanChangeUseVisibleWhitespaceEnabled(this IEditorOptions options) {
			if (options == null)
				throw new ArgumentNullException(nameof(options));
			return options.GetOptionValue(DefaultDsTextViewOptions.CanChangeUseVisibleWhitespaceId);
		}

		/// <summary>
		/// Returns true if the user can change word wrap style
		/// </summary>
		/// <param name="options">Options</param>
		/// <returns></returns>
		public static bool IsCanChangeWordWrapStyleEnabled(this IEditorOptions options) {
			if (options == null)
				throw new ArgumentNullException(nameof(options));
			return options.GetOptionValue(DefaultDsTextViewOptions.CanChangeWordWrapStyleId);
		}

		/// <summary>
		/// Returns true if box selection is enabled
		/// </summary>
		/// <param name="options">Options</param>
		/// <returns></returns>
		public static bool IsAllowBoxSelectionEnabled(this IEditorOptions options) {
			if (options == null)
				throw new ArgumentNullException(nameof(options));
			return options.GetOptionValue(DefaultDsTextViewOptions.AllowBoxSelectionId);
		}

		/// <summary>
		/// Returns true if refresh-screen-on-change is enabled
		/// </summary>
		/// <param name="options">Options</param>
		/// <returns></returns>
		public static bool IsRefreshScreenOnChangeEnabled(this IEditorOptions options) {
			if (options == null)
				throw new ArgumentNullException(nameof(options));
			return options.GetOptionValue(DefaultDsTextViewOptions.RefreshScreenOnChangeId);
		}

		/// <summary>
		/// Returns the number of milliseconds to wait before refreshing the screen after the document gets changed
		/// </summary>
		/// <param name="options">Options</param>
		/// <returns></returns>
		public static int GetRefreshScreenOnChangeWaitMilliSeconds(this IEditorOptions options) {
			if (options == null)
				throw new ArgumentNullException(nameof(options));
			return options.GetOptionValue(DefaultDsTextViewOptions.RefreshScreenOnChangeWaitMilliSecondsId);
		}

		/// <summary>
		/// Returns true if text should be colorized
		/// </summary>
		/// <param name="options">Options</param>
		/// <returns></returns>
		public static bool IsColorizationEnabled(this IEditorOptions options) {
			if (options == null)
				throw new ArgumentNullException(nameof(options));
			return options.GetOptionValue(DefaultDsTextViewOptions.EnableColorizationId);
		}

		/// <summary>
		/// Returns true if references should be highlighted
		/// </summary>
		/// <param name="options">Options</param>
		/// <returns></returns>
		public static bool IsReferenceHighlightingEnabled(this IEditorOptions options) {
			if (options == null)
				throw new ArgumentNullException(nameof(options));
			return options.GetOptionValue(DefaultDsTextViewOptions.ReferenceHighlightingId);
		}

		/// <summary>
		/// Returns true if braces should be highlighted
		/// </summary>
		/// <param name="options">Options</param>
		/// <returns></returns>
		public static bool IsBraceMatchingEnabled(this IEditorOptions options) {
			if (options == null)
				throw new ArgumentNullException(nameof(options));
			return options.GetOptionValue(DefaultDsTextViewOptions.BraceMatchingId);
		}

		/// <summary>
		/// Returns true if line separators should be shown
		/// </summary>
		/// <param name="options">Options</param>
		/// <returns></returns>
		public static bool IsLineSeparatorEnabled(this IEditorOptions options) {
			if (options == null)
				throw new ArgumentNullException(nameof(options));
			return options.GetOptionValue(DefaultDsTextViewOptions.LineSeparatorsId);
		}

		/// <summary>
		/// Returns true if related keywords should be highlighted
		/// </summary>
		/// <param name="options">Options</param>
		/// <returns></returns>
		public static bool IsHighlightRelatedKeywordsEnabled(this IEditorOptions options) {
			if (options == null)
				throw new ArgumentNullException(nameof(options));
			return options.GetOptionValue(DefaultDsTextViewOptions.HighlightRelatedKeywordsId);
		}

		/// <summary>
		/// Returns true if empty or whitespace-only lines should be compressed
		/// </summary>
		/// <param name="options">Options</param>
		/// <returns></returns>
		public static bool IsCompressEmptyOrWhitespaceLinesEnabled(this IEditorOptions options) {
			if (options == null)
				throw new ArgumentNullException(nameof(options));
			return options.GetOptionValue(DefaultDsTextViewOptions.CompressEmptyOrWhitespaceLinesId);
		}

		/// <summary>
		/// Returns true if non-empty lines that don't contain letters or digits should be compressed
		/// </summary>
		/// <param name="options">Options</param>
		/// <returns></returns>
		public static bool IsCompressNonLetterLinesEnabled(this IEditorOptions options) {
			if (options == null)
				throw new ArgumentNullException(nameof(options));
			return options.GetOptionValue(DefaultDsTextViewOptions.CompressNonLetterLinesId);
		}

		/// <summary>
		/// Returns true if extra vertical pixels should be removed from text lines
		/// </summary>
		/// <param name="options">Options</param>
		/// <returns></returns>
		public static bool IsRemoveExtraTextLineVerticalPixelsEnabled(this IEditorOptions options) {
			if (options == null)
				throw new ArgumentNullException(nameof(options));
			return options.GetOptionValue(DefaultDsTextViewOptions.RemoveExtraTextLineVerticalPixelsId);
		}

		/// <summary>
		/// Gets the <see cref="BlockStructureLineKind"/> value
		/// </summary>
		/// <param name="options">Options</param>
		/// <returns></returns>
		public static BlockStructureLineKind GetBlockStructureLineKind(this IEditorOptions options) {
			if (options == null)
				throw new ArgumentNullException(nameof(options));
			return options.GetOptionValue(DefaultDsTextViewOptions.BlockStructureLineKindId);
		}
	}
}
