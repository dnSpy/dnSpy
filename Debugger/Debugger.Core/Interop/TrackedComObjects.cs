// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

#pragma warning disable 1591
#define DISABLE_COM_TRACKING

#if !DISABLE_COM_TRACKING
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
#endif

namespace Debugger.Interop
{
	public static class TrackedComObjects
	{
#if !DISABLE_COM_TRACKING
		static List<WeakReference> objects = new List<WeakReference>();
#endif
		
		public static void ProcessOutParameter(object parameter)
		{
#if !DISABLE_COM_TRACKING
			if (parameter != null) {
				if (Marshal.IsComObject(parameter)) {
					Track(parameter);
				} else if (parameter is Array) {
					foreach(object elem in (Array)parameter) {
						ProcessOutParameter(elem);
					}
				}
			}
#endif
		}
		
		public static void Track(object obj)
		{
#if !DISABLE_COM_TRACKING
			if (Marshal.IsComObject(obj)) {
				lock(objects) {
					objects.Add(new WeakReference(obj));
				}
			}
#endif
		}
		
		public static int ReleaseAll()
		{
#if !DISABLE_COM_TRACKING
			lock(objects) {
				int count = 0;
				foreach(WeakReference weakRef in objects) {
					object obj = weakRef.Target;
					if (obj != null) {
						// TODO: This sometimes (often) causes a hang! Disable it.
						//Marshal.FinalReleaseComObject(obj);
						count++;
					}
				}
				objects.Clear();
				objects.TrimExcess();
				return count;
			}
#else
			return 0;
#endif
		}
	}
}

#pragma warning restore 1591
