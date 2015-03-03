// Copyright (c) 2014 AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using System.Diagnostics;
using System.Windows;

namespace ICSharpCode.AvalonEdit.Utils
{
	/// <summary>
	/// WeakEventManager with AddListener/RemoveListener and CurrentManager implementation.
	/// Helps implementing the WeakEventManager pattern with less code.
	/// </summary>
	public abstract class WeakEventManagerBase<TManager, TEventSource> : WeakEventManager
		where TManager : WeakEventManagerBase<TManager, TEventSource>, new()
		where TEventSource : class
	{
		/// <summary>
		/// Creates a new WeakEventManagerBase instance.
		/// </summary>
		protected WeakEventManagerBase()
		{
			Debug.Assert(GetType() == typeof(TManager));
		}
		
		/// <summary>
		/// Adds a weak event listener.
		/// </summary>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1000:DoNotDeclareStaticMembersOnGenericTypes")]
		public static void AddListener(TEventSource source, IWeakEventListener listener)
		{
			CurrentManager.ProtectedAddListener(source, listener);
		}
		
		/// <summary>
		/// Removes a weak event listener.
		/// </summary>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1000:DoNotDeclareStaticMembersOnGenericTypes")]
		public static void RemoveListener(TEventSource source, IWeakEventListener listener)
		{
			CurrentManager.ProtectedRemoveListener(source, listener);
		}
		
		/// <inheritdoc/>
		protected sealed override void StartListening(object source)
		{
			if (source == null)
				throw new ArgumentNullException("source");
			StartListening((TEventSource)source);
		}
		
		/// <inheritdoc/>
		protected sealed override void StopListening(object source)
		{
			if (source == null)
				throw new ArgumentNullException("source");
			StopListening((TEventSource)source);
		}
		
		/// <summary>
		/// Attaches the event handler.
		/// </summary>
		protected abstract void StartListening(TEventSource source);
		
		/// <summary>
		/// Detaches the event handler.
		/// </summary>
		protected abstract void StopListening(TEventSource source);
		
		/// <summary>
		/// Gets the current manager.
		/// </summary>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1721:PropertyNamesShouldNotMatchGetMethods")]
		protected static TManager CurrentManager {
			get {
				Type managerType = typeof(TManager);
				TManager manager = (TManager)GetCurrentManager(managerType);
				if (manager == null) {
					manager = new TManager();
					SetCurrentManager(managerType, manager);
				}
				return manager;
			}
		}
	}
}
