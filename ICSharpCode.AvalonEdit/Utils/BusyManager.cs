// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;

namespace ICSharpCode.AvalonEdit.Utils
{
	/// <summary>
	/// This class is used to prevent stack overflows by representing a 'busy' flag
	/// that prevents reentrance when another call is running.
	/// However, using a simple 'bool busy' is not thread-safe, so we use a
	/// thread-static BusyManager.
	/// </summary>
	static class BusyManager
	{
		public struct BusyLock : IDisposable
		{
			public static readonly BusyLock Failed = new BusyLock(null);
			
			readonly List<object> objectList;
			
			public BusyLock(List<object> objectList)
			{
				this.objectList = objectList;
			}
			
			public bool Success {
				get { return objectList != null; }
			}
			
			public void Dispose()
			{
				if (objectList != null) {
					objectList.RemoveAt(objectList.Count - 1);
				}
			}
		}
		
		[ThreadStatic] static List<object> _activeObjects;
		
		public static BusyLock Enter(object obj)
		{
			List<object> activeObjects = _activeObjects;
			if (activeObjects == null)
				activeObjects = _activeObjects = new List<object>();
			for (int i = 0; i < activeObjects.Count; i++) {
				if (activeObjects[i] == obj)
					return BusyLock.Failed;
			}
			activeObjects.Add(obj);
			return new BusyLock(activeObjects);
		}
	}
}
