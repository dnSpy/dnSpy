// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

#pragma warning disable 1591

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Debugger.Interop
{
	public static class TrackedComObjects
	{
		static List<WeakReference> objects = new List<WeakReference>();
		
		public static void ProcessOutParameter(object parameter)
		{
			if (parameter != null) {
				if (Marshal.IsComObject(parameter)) {
					Track(parameter);
				} else if (parameter is Array) {
					foreach(object elem in (Array)parameter) {
						ProcessOutParameter(elem);
					}
				}
			}
		}
		
		public static void Track(object obj)
		{
			if (Marshal.IsComObject(obj)) {
				lock(objects) {
					objects.Add(new WeakReference(obj));
				}
			}
		}
		
		public static int ReleaseAll()
		{
			lock(objects) {
				int count = 0;
				foreach(WeakReference weakRef in objects) {
					object obj = weakRef.Target;
					if (obj != null) {
						Marshal.FinalReleaseComObject(obj);
						count++;
					}
				}
				objects.Clear();
				objects.TrimExcess();
				return count;
			}
		}
	}
}

#pragma warning restore 1591
