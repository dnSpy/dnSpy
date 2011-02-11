// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

namespace Debugger
{
	public class Options
	{
		public bool EnableJustMyCode = true;
		public bool StepOverNoSymbols = true;
		public bool StepOverDebuggerAttributes = true;
		public bool StepOverAllProperties = false;
		public bool StepOverSingleLineProperties = false;
		public bool StepOverFieldAccessProperties = true;
		public bool Verbose = false;
		public string[] SymbolsSearchPaths = new string[0];
		public bool SuspendOtherThreads = true;
		public bool EnableEditAndContinue = false;
	}
}
