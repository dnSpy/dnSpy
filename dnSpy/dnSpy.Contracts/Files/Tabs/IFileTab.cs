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
using System.Threading;
using dnSpy.Contracts.Files.Tabs.TextEditor;
using dnSpy.Contracts.Settings;

namespace dnSpy.Contracts.Files.Tabs {
	/// <summary>
	/// A tab
	/// </summary>
	public interface IFileTab {
		/// <summary>
		/// Current <see cref="IFileTabContent"/> instance
		/// </summary>
		IFileTabContent Content { get; }

		/// <summary>
		/// Current <see cref="IFileTabUIContext"/>
		/// </summary>
		IFileTabUIContext UIContext { get; }

		/// <summary>
		/// Gets the <see cref="IFileTabManager"/> owner
		/// </summary>
		IFileTabManager FileTabManager { get; }

		/// <summary>
		/// Deserializes UI settings serialized by <see cref="SerializeUI(ISettingsSection)"/>
		/// </summary>
		/// <param name="tabContentUI">Serialized data</param>
		void DeserializeUI(ISettingsSection tabContentUI);

		/// <summary>
		/// Serializes UI settings
		/// </summary>
		/// <param name="tabContentUI">Target section</param>
		void SerializeUI(ISettingsSection tabContentUI);

		/// <summary>
		/// true if this is the active tab
		/// </summary>
		bool IsActiveTab { get; }

		/// <summary>
		/// true if <see cref="NavigateBackward()"/> can execute
		/// </summary>
		bool CanNavigateBackward { get; }

		/// <summary>
		/// Navigates backward in history
		/// </summary>
		void NavigateBackward();

		/// <summary>
		/// true if <see cref="NavigateForward()"/> can execute
		/// </summary>
		bool CanNavigateForward { get; }

		/// <summary>
		/// Navigates forward in history
		/// </summary>
		void NavigateForward();

		/// <summary>
		/// Follows a reference
		/// </summary>
		/// <param name="ref">Reference</param>
		/// <param name="sourceContent">Source content or null</param>
		/// <param name="onShown">Called after the content has been shown. Can be null.</param>
		void FollowReference(object @ref, IFileTabContent sourceContent = null, Action<ShowTabContentEventArgs> onShown = null);

		/// <summary>
		/// Follows a reference in a new tab
		/// </summary>
		/// <param name="ref">Reference</param>
		/// <param name="onShown">Called after the content has been shown. Can be null.</param>
		void FollowReferenceNewTab(object @ref, Action<ShowTabContentEventArgs> onShown = null);

		/// <summary>
		/// Follows a reference
		/// </summary>
		/// <param name="ref">Reference</param>
		/// <param name="newTab">true to open a new tab</param>
		/// <param name="onShown">Called after the content has been shown. Can be null.</param>
		void FollowReference(object @ref, bool newTab, Action<ShowTabContentEventArgs> onShown = null);

		/// <summary>
		/// Shows the tab content
		/// </summary>
		/// <param name="tabContent">Tab content</param>
		/// <param name="serializedUI">Serialized UI data or null</param>
		/// <param name="onShown">Called after the output has been shown on the screen</param>
		void Show(IFileTabContent tabContent, object serializedUI, Action<ShowTabContentEventArgs> onShown);

		/// <summary>
		/// Sets focus to the focused element if this is the active tab
		/// </summary>
		void TrySetFocus();

		/// <summary>
		/// Closes this tab
		/// </summary>
		void Close();

		/// <summary>
		/// true if <see cref="AsyncExec(Action{CancellationTokenSource}, Action, Action{IAsyncShowResult})"/> hasn't finished executing
		/// </summary>
		bool IsAsyncExecInProgress { get; }

		/// <summary>
		/// Executes new code, cancelling any other started <see cref="AsyncExec(Action{CancellationTokenSource}, Action, Action{IAsyncShowResult})"/> call
		/// </summary>
		/// <param name="preExec">Executed in the current thread before the async code has started</param>
		/// <param name="asyncAction">Executed in a new thread</param>
		/// <param name="postExec">Executed in the current thread after <paramref name="asyncAction"/>
		/// has finished executing</param>
		void AsyncExec(Action<CancellationTokenSource> preExec, Action asyncAction, Action<IAsyncShowResult> postExec);
	}

	/// <summary>
	/// Extension methods
	/// </summary>
	public static class FileTabExtensions {
		/// <summary>
		/// Returns the tab's <see cref="ITextEditorUIContext"/> or null if it's not visible
		/// </summary>
		/// <param name="tab">Tab</param>
		/// <returns></returns>
		public static ITextEditorUIContext TryGetTextEditorUIContext(this IFileTab tab) {
			return tab == null ? null : tab.UIContext as ITextEditorUIContext;
		}
	}
}
