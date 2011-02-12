// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;

namespace ICSharpCode.SharpDevelop.Dom
{
	static class DomCache
	{
		/// <summary>
		/// Clear the static searchclass cache. You should call this method
		/// whenever the DOM changes.
		/// </summary>
		/// <remarks>
		/// automatically called by DefaultProjectContent.UpdateCompilationUnit
		/// and DefaultProjectContent.OnReferencedContentsChanged.
		/// </remarks>
		public static void Clear()
		{
			List<Action> oldActions;
			lock (lockObject) {
				oldActions = actions;
				actions = new List<Action>();
			}
			foreach (Action a in oldActions) {
				a();
			}
		}
		
		static readonly object lockObject = new Object();
		static List<Action> actions = new List<Action>();
		
		public static void RegisterForClear(Action action)
		{
			lock (lockObject) {
				actions.Add(action);
			}
		}
	}
}
