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

using System.Collections.ObjectModel;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Contracts.Language.Intellisense {
	/// <summary>
	/// Completion service
	/// </summary>
	public interface ICompletionBroker {
		/// <summary>
		/// Triggers a completion at the current caret position
		/// </summary>
		/// <param name="textView">Text view</param>
		/// <returns></returns>
		ICompletionSession TriggerCompletion(ITextView textView);

		/// <summary>
		/// Triggers a completion
		/// </summary>
		/// <param name="textView">Text view</param>
		/// <param name="triggerPoint">Trigger point</param>
		/// <param name="trackCaret">true to track caret</param>
		/// <returns></returns>
		ICompletionSession TriggerCompletion(ITextView textView, ITrackingPoint triggerPoint, bool trackCaret);

		/// <summary>
		/// Creates a completion session without starting it
		/// </summary>
		/// <param name="textView">Text view</param>
		/// <param name="triggerPoint">Trigger point</param>
		/// <param name="trackCaret">true to track caret</param>
		/// <returns></returns>
		ICompletionSession CreateCompletionSession(ITextView textView, ITrackingPoint triggerPoint, bool trackCaret);

		/// <summary>
		/// Dismisses all sessions
		/// </summary>
		/// <param name="textView">Text view</param>
		void DismissAllSessions(ITextView textView);

		/// <summary>
		/// Returns true if completion is active
		/// </summary>
		/// <param name="textView">Text view</param>
		/// <returns></returns>
		bool IsCompletionActive(ITextView textView);

		/// <summary>
		/// Gets all completion sessions
		/// </summary>
		/// <param name="textView">Text view</param>
		/// <returns></returns>
		ReadOnlyCollection<ICompletionSession> GetSessions(ITextView textView);
	}
}
