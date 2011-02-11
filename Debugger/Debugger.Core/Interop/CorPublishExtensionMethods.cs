// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;

namespace Debugger.Interop.CorPublish
{
	public static partial class CorPublishExtensionMethods
	{
		static void ProcessOutParameter(object parameter)
		{
			TrackedComObjects.ProcessOutParameter(parameter);
		}
	}
}
