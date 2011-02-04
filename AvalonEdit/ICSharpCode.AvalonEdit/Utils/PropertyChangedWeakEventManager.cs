// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.ComponentModel;

namespace ICSharpCode.AvalonEdit.Utils
{
	/// <summary>
	/// WeakEventManager for INotifyPropertyChanged.PropertyChanged.
	/// </summary>
	public sealed class PropertyChangedWeakEventManager : WeakEventManagerBase<PropertyChangedWeakEventManager, INotifyPropertyChanged>
	{
		/// <inheritdoc/>
		protected override void StartListening(INotifyPropertyChanged source)
		{
			source.PropertyChanged += DeliverEvent;
		}
		
		/// <inheritdoc/>
		protected override void StopListening(INotifyPropertyChanged source)
		{
			source.PropertyChanged -= DeliverEvent;
		}
	}
}
