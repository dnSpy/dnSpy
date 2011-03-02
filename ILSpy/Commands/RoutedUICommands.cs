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
using System.Windows.Input;

namespace ICSharpCode.ILSpy.Commands
{
	public static class RoutedUICommands
	{
		static RoutedUICommands()
		{
			AttachToProcess = new RoutedUICommand("Attach to running process...", "AttachToProcess", typeof(RoutedUICommands));
			DetachFromProcess = new RoutedUICommand("Detach from process...", "DetachFromProcess", typeof(RoutedUICommands));
			ContinueDebugging = new RoutedUICommand("Continue debugging", "ContinueDebugging", typeof(RoutedUICommands));
			StepInto = new RoutedUICommand("Step into", "StepInto", typeof(RoutedUICommands));
			StepOver = new RoutedUICommand("Step over", "StepOver", typeof(RoutedUICommands));
			StepOut = new RoutedUICommand("Step out", "StepOut", typeof(RoutedUICommands));
			
			RemoveAllBreakpoint = new RoutedUICommand("Remove all breakpoints", "RemoveAllBreakpoint", typeof(RoutedUICommands));
			
			DebugExecutable = new RoutedUICommand("Debug an executable", "DebugExecutable", typeof(RoutedUICommands));
		}
		
		public static RoutedUICommand AttachToProcess { get; private set; }
		
		public static RoutedUICommand DetachFromProcess { get; private set; }
		
		public static RoutedUICommand ContinueDebugging { get; private set; }
		
		public static RoutedUICommand StepInto { get; private set; }
		
		public static RoutedUICommand StepOver { get; private set; }
		
		public static RoutedUICommand StepOut { get; private set; }
		
		public static RoutedUICommand RemoveAllBreakpoint { get; private set; }
		
		public static RoutedUICommand DebugExecutable { get; private set; }
	}
}
