// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;

namespace ICSharpCode.ILSpy
{
	sealed class CommandLineArguments
	{
		// see /doc/Command Line.txt for details
		public List<string> AssembliesToLoad = new List<string>();
		public bool? SingleInstance;
		public string NavigateTo;
		public string Language;
		public bool NoActivate;
		
		public CommandLineArguments(IEnumerable<string> arguments)
		{
			foreach (string arg in arguments) {
				if (arg.Length == 0)
					continue;
				if (arg[0] == '/') {
					if (arg.Equals("/singleInstance", StringComparison.OrdinalIgnoreCase))
						this.SingleInstance = true;
					else if (arg.Equals("/separate", StringComparison.OrdinalIgnoreCase))
						this.SingleInstance = false;
					else if (arg.StartsWith("/navigateTo:", StringComparison.OrdinalIgnoreCase))
						this.NavigateTo = arg.Substring("/navigateTo:".Length);
					else if (arg.StartsWith("/language:", StringComparison.OrdinalIgnoreCase))
						this.Language = arg.Substring("/language:".Length);
					else if (arg.Equals("/noActivate", StringComparison.OrdinalIgnoreCase))
						this.NoActivate = true;
				} else {
					this.AssembliesToLoad.Add(arg);
				}
			}
		}
	}
}
