// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows.Controls;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.Ast;
using ICSharpCode.ILSpy;
using ICSharpCode.NRefactory.CSharp;
using Mono.Cecil;

namespace TestPlugin
{
	/// <summary>
	/// Adds a new language to the decompiler.
	/// </summary>
	[Export(typeof(Language))]
	public class CustomLanguage : Language
	{
		public override string Name {
			get {
				return "Custom";
			}
		}
		
		public override string FileExtension {
			get {
				// used in 'Save As' dialog
				return ".txt";
			}
		}
		
		// There are several methods available to override; in this sample, we deal with methods only
		
		public override void DecompileMethod(MethodDefinition method, ITextOutput output, DecompilationOptions options)
		{
			if (method.Body != null) {
				output.WriteLine("Size of method: {0} bytes", method.Body.CodeSize);
				
				ISmartTextOutput smartOutput = output as ISmartTextOutput;
				if (smartOutput != null) {
					// when writing to the text view (but not when writing to a file), we can even add UI elements such as buttons:
					smartOutput.AddButton(null, "Click me!", (sender, e) => (sender as Button).Content = "I was clicked!");
					smartOutput.WriteLine();
				}
				
				// ICSharpCode.Decompiler.Ast.AstBuilder can be used to decompile to C#
				AstBuilder b = new AstBuilder(new DecompilerContext(method.Module) {
				                              	Settings = options.DecompilerSettings,
				                              	CurrentType = method.DeclaringType
				                              });
				b.AddMethod(method);
				b.RunTransformations();
				output.WriteLine("Decompiled AST has {0} nodes", b.SyntaxTree.DescendantsAndSelf.Count());
			}
		}
	}
}
