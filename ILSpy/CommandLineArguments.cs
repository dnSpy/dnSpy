// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;

namespace ICSharpCode.ILSpy
{
	sealed class CommandLineArguments
	{
		public List<string> AssembliesToLoad = new List<string>();
		public string NavigateTo;
		public bool SharedInstance = true;
		
		public CommandLineArguments(IEnumerable<string> arguments)
		{
			foreach (string arg in arguments) {
				if (arg.Length == 0)
					continue;
				if (arg[0] == '/') {
					if (arg.Equals("/sharedInstance", StringComparison.OrdinalIgnoreCase))
						this.SharedInstance = true;
					else if (arg.Equals("/separate", StringComparison.OrdinalIgnoreCase))
						this.SharedInstance = false;
					else if (arg.StartsWith("/navigateTo:", StringComparison.OrdinalIgnoreCase))
						this.NavigateTo = arg.Substring("/navigateTo:".Length);
				} else {
					this.AssembliesToLoad.Add(arg);
				}
			}
		}
	}
}
