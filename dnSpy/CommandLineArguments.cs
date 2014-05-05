// Copyright (c) 2011 AlphaSierraPapa for the SharpDevelop Team
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
