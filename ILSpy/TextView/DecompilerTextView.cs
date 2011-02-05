// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

using ICSharpCode.AvalonEdit;
using Mono.Cecil;

namespace ICSharpCode.ILSpy.TextView
{
	/// <summary>
	/// Manages the TextEditor showing the decompiled code.
	/// </summary>
	sealed class DecompilerTextView
	{
		readonly MainWindow mainWindow;
		readonly TextEditor textEditor;
		readonly ReferenceElementGenerator referenceElementGenerator;
		
		DefinitionLookup definitionLookup;
		CancellationTokenSource currentCancellationTokenSource;
		
		public DecompilerTextView(MainWindow mainWindow, TextEditor textEditor)
		{
			if (mainWindow == null)
				throw new ArgumentNullException("mainWindow");
			if (textEditor == null)
				throw new ArgumentNullException("textEditor");
			this.mainWindow = mainWindow;
			this.textEditor = textEditor;
			this.referenceElementGenerator = new ReferenceElementGenerator(this);
			textEditor.TextArea.TextView.ElementGenerators.Add(referenceElementGenerator);
		}
		
		public void Decompile(IEnumerable<ILSpyTreeNodeBase> treeNodes)
		{
			if (currentCancellationTokenSource != null)
				currentCancellationTokenSource.Cancel();
			var myCancellationTokenSource = new CancellationTokenSource();
			currentCancellationTokenSource = myCancellationTokenSource;
			var task = RunDecompiler(Language.Current, treeNodes.ToArray(), myCancellationTokenSource.Token);
			task.ContinueWith(
				delegate {
					try {
						if (currentCancellationTokenSource == myCancellationTokenSource) {
							currentCancellationTokenSource = null;
							try {
								SmartTextOutput textOutput = task.Result;
								referenceElementGenerator.References = textOutput.References;
								definitionLookup = textOutput.DefinitionLookup;
								textEditor.SyntaxHighlighting = ILSpy.Language.Current.SyntaxHighlighting;
								textEditor.Text = textOutput.ToString();
							} catch (AggregateException ex) {
								textEditor.SyntaxHighlighting = null;
								referenceElementGenerator.References = null;
								definitionLookup = null;
								textEditor.Text = string.Join(Environment.NewLine, ex.InnerExceptions.Select(ie => ie.ToString()));
							}
						}
					} finally {
						myCancellationTokenSource.Dispose();
					}
				},
				TaskScheduler.FromCurrentSynchronizationContext());
		}
		
		static Task<SmartTextOutput> RunDecompiler(Language language, ILSpyTreeNodeBase[] nodes, CancellationToken cancellationToken)
		{
			return Task.Factory.StartNew(
				delegate {
					SmartTextOutput textOutput = new SmartTextOutput();
					foreach (var node in nodes) {
						cancellationToken.ThrowIfCancellationRequested();
						node.Decompile(language, textOutput, cancellationToken);
					}
					return textOutput;
				});
		}
		
		internal void JumpToReference(ReferenceSegment referenceSegment)
		{
			object reference = referenceSegment.Reference;
			if (definitionLookup != null) {
				int pos = definitionLookup.GetDefinitionPosition(reference);
				if (pos >= 0) {
					textEditor.TextArea.Focus();
					textEditor.Select(pos, 0);
					textEditor.ScrollTo(textEditor.TextArea.Caret.Line, textEditor.TextArea.Caret.Column);
					mainWindow.Dispatcher.Invoke(DispatcherPriority.Background, new Action(
						delegate {
							CaretHighlightAdorner.DisplayCaretHighlightAnimation(textEditor.TextArea);
						}));
					return;
				}
			}
			var assemblyList = mainWindow.AssemblyList;
			if (reference is TypeReference) {
				mainWindow.SelectNode(assemblyList.FindTypeNode(((TypeReference)reference).Resolve()));
			} else if (reference is MethodReference) {
				mainWindow.SelectNode(assemblyList.FindMethodNode(((MethodReference)reference).Resolve()));
			} else if (reference is FieldReference) {
				mainWindow.SelectNode(assemblyList.FindFieldNode(((FieldReference)reference).Resolve()));
			} else if (reference is PropertyReference) {
				mainWindow.SelectNode(assemblyList.FindPropertyNode(((PropertyReference)reference).Resolve()));
			} else if (reference is EventReference) {
				mainWindow.SelectNode(assemblyList.FindEventNode(((EventReference)reference).Resolve()));
			} else if (reference is AssemblyDefinition) {
				mainWindow.SelectNode(assemblyList.Assemblies.FirstOrDefault(node => node.AssemblyDefinition == reference));
			}
		}
	}
}
