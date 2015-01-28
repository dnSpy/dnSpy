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
using System.Threading;
using ICSharpCode.Decompiler;
using ICSharpCode.ILSpy.Options;

namespace ICSharpCode.ILSpy
{
	public class ProjectInfo
	{
		public string AssemblyFileName { get; set; }
		public string AssemblySimpleName { get; set; }
		public string ProjectFileName { get; set; }
		public Guid ProjectGuid { get; set; }
	}

	/// <summary>
	/// Options passed to the decompiler.
	/// </summary>
	public class DecompilationOptions
	{
		/// <summary>
		/// Gets whether a full decompilation (all members recursively) is desired.
		/// If this option is false, language bindings are allowed to show the only headers of the decompiled element's children.
		/// </summary>
		public bool FullDecompilation { get; set; }
		
		/// <summary>
		/// Gets/Sets the directory into which the project is saved.
		/// </summary>
		public string SaveAsProjectDirectory { get; set; }

		/// <summary>
		/// Gets/sets whether project files can reference the standard library (eg. mscorlib). Only
		/// used in C# projects.
		/// </summary>
		public bool DontReferenceStdLib { get; set; }

		/// <summary>
		/// Gets/sets the project files. First string is path to assembly, second string is assembly
		/// name, and the GUID is the project GUID.
		/// </summary>
		public List<ProjectInfo> ProjectFiles { get; set; }

		/// <summary>
		/// Gets/sets the project guid
		/// </summary>
		public Guid? ProjectGuid { get; set; }

		/// <summary>
		/// Don't print an error message if an exception occurs when decompiling a method, instead
		/// let the exception pass through (the decompile operation will fail of course).
		/// </summary>
		public bool DontShowCreateMethodBodyExceptions { get; set; }
		
		/// <summary>
		/// Gets the cancellation token that is used to abort the decompiler.
		/// </summary>
		/// <remarks>
		/// Decompilers should regularly call <c>options.CancellationToken.ThrowIfCancellationRequested();</c>
		/// to allow for cooperative cancellation of the decompilation task.
		/// </remarks>
		public CancellationToken CancellationToken { get; set; }
		
		/// <summary>
		/// Gets the settings for the decompiler.
		/// </summary>
		public DecompilerSettings DecompilerSettings { get; set; }

		/// <summary>
		/// Gets/sets an optional state of a decompiler text view.
		/// </summary>
		/// <remarks>
		/// This state is used to restore test view's state when decompilation is started by Go Back/Forward action.
		/// </remarks>
		public TextView.DecompilerTextViewState TextViewState { get; set; }

		public DecompilationOptions()
		{
			this.DecompilerSettings = DecompilerSettingsPanel.CurrentDecompilerSettings;
		}
	}
}
