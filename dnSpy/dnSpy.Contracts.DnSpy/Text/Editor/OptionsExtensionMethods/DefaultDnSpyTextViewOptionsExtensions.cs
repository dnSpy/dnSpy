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
	/// <see cref="DefaultDnSpyTextViewOptions"/> extension methods
	/// </summary>
	public static class DefaultDnSpyTextViewOptionsExtensions {
		/// <summary>
		/// Returns true if the user can change overwrite mode
		/// </summary>
		/// <param name="options">Options</param>
		/// <returns></returns>
		public static bool IsCanChangeOverwriteModeEnabled(this IEditorOptions options) {
			if (options == null)
				throw new ArgumentNullException(nameof(options));
			return options.GetOptionValue(DefaultDnSpyTextViewOptions.CanChangeOverwriteModeId);
		}

		/// <summary>
		/// Returns true if the user can enable or disable use-visible-whitespace option
		/// </summary>
		/// <param name="options">Options</param>
		/// <returns></returns>
		public static bool IsCanChangeUseVisibleWhitespaceEnabled(this IEditorOptions options) {
			if (options == null)
				throw new ArgumentNullException(nameof(options));
			return options.GetOptionValue(DefaultDnSpyTextViewOptions.CanChangeUseVisibleWhitespaceId);
		}

		/// <summary>
		/// Returns true if the user can change word wrap style
		/// </summary>
		/// <param name="options">Options</param>
		/// <returns></returns>
		public static bool IsCanChangeWordWrapStyleEnabled(this IEditorOptions options) {
			if (options == null)
				throw new ArgumentNullException(nameof(options));
			return options.GetOptionValue(DefaultDnSpyTextViewOptions.CanChangeWordWrapStyleId);
		}

		/// <summary>
		/// Returns true if box selection is enabled
		/// </summary>
		/// <param name="options">Options</param>
		/// <returns></returns>
		public static bool IsAllowBoxSelectionEnabled(this IEditorOptions options) {
			if (options == null)
				throw new ArgumentNullException(nameof(options));
			return options.GetOptionValue(DefaultDnSpyTextViewOptions.AllowBoxSelectionId);
		}

		/// <summary>
		/// Returns true if refresh-screen-on-change is enabled
		/// </summary>
		/// <param name="options">Options</param>
		/// <returns></returns>
		public static bool IsRefreshScreenOnChangeEnabled(this IEditorOptions options) {
			if (options == null)
				throw new ArgumentNullException(nameof(options));
			return options.GetOptionValue(DefaultDnSpyTextViewOptions.RefreshScreenOnChangeId);
		}

		/// <summary>
		/// Returns the number of milliseconds to wait before refreshing the screen after the document gets changed
		/// </summary>
		/// <param name="options">Options</param>
		/// <returns></returns>
		public static int GetRefreshScreenOnChangeWaitMilliSeconds(this IEditorOptions options) {
			if (options == null)
				throw new ArgumentNullException(nameof(options));
			return options.GetOptionValue(DefaultDnSpyTextViewOptions.RefreshScreenOnChangeWaitMilliSecondsId);
		}

		/// <summary>
		/// Returns true if text should be colorized
		/// </summary>
		/// <param name="options">Options</param>
		/// <returns></returns>
		public static bool IsColorizationEnabled(this IEditorOptions options) {
			if (options == null)
				throw new ArgumentNullException(nameof(options));
			return options.GetOptionValue(DefaultDnSpyTextViewOptions.EnableColorizationId);
		}

		/// <summary>
		/// Returns true if references should be highlighted
		/// </summary>
		/// <param name="options">Options</param>
		/// <returns></returns>
		public static bool IsReferenceHighlightingEnabled(this IEditorOptions options) {
			if (options == null)
				throw new ArgumentNullException(nameof(options));
			return options.GetOptionValue(DefaultDnSpyTextViewOptions.ReferenceHighlightingId);
		}

		/// <summary>
		/// Returns true if braces should be highlighted
		/// </summary>
		/// <param name="options">Options</param>
		/// <returns></returns>
		public static bool IsBraceMatchingEnabled(this IEditorOptions options) {
			if (options == null)
				throw new ArgumentNullException(nameof(options));
			return options.GetOptionValue(DefaultDnSpyTextViewOptions.BraceMatchingId);
		}

		/// <summary>
		/// Returns true if line separators should be shown
		/// </summary>
		/// <param name="options">Options</param>
		/// <returns></returns>
		public static bool IsLineSeparatorEnabled(this IEditorOptions options) {
			if (options == null)
				throw new ArgumentNullException(nameof(options));
			return options.GetOptionValue(DefaultDnSpyTextViewOptions.LineSeparatorId);
		}

		/// <summary>
		/// Returns true if related keywords should be highlighted
		/// </summary>
		/// <param name="options">Options</param>
		/// <returns></returns>
		public static bool IsHighlightRelatedKeywordsEnabled(this IEditorOptions options) {
			if (options == null)
				throw new ArgumentNullException(nameof(options));
			return options.GetOptionValue(DefaultDnSpyTextViewOptions.HighlightRelatedKeywordsId);
		}
	}
}
