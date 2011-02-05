// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using System.Xml;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Folding;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using Mono.Cecil;

namespace ICSharpCode.ILSpy.TextView
{
	/// <summary>
	/// Manages the TextEditor showing the decompiled code.
	/// </summary>
	sealed partial class DecompilerTextView : UserControl
	{
		readonly ReferenceElementGenerator referenceElementGenerator;
		readonly FoldingManager foldingManager;
		internal MainWindow mainWindow;
		
		DefinitionLookup definitionLookup;
		CancellationTokenSource currentCancellationTokenSource;
		
		public DecompilerTextView()
		{
			HighlightingManager.Instance.RegisterHighlighting(
				"ILAsm", new string[] { ".il" },
				delegate {
					using (Stream s = typeof(DecompilerTextView).Assembly.GetManifestResourceStream(typeof(DecompilerTextView), "ILAsm-Mode.xshd")) {
						using (XmlTextReader reader = new XmlTextReader(s)) {
							return HighlightingLoader.Load(reader, HighlightingManager.Instance);
						}
					}
				});
			
			InitializeComponent();
			this.referenceElementGenerator = new ReferenceElementGenerator(this);
			textEditor.TextArea.TextView.ElementGenerators.Add(referenceElementGenerator);
			textEditor.Text = "Welcome to ILSpy!";
			foldingManager = FoldingManager.Install(textEditor.TextArea);
		}
		
		public void Decompile(IEnumerable<ILSpyTreeNodeBase> treeNodes)
		{
			if (waitAdorner.Visibility != Visibility.Visible) {
				waitAdorner.Visibility = Visibility.Visible;
				waitAdorner.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, new Duration(TimeSpan.FromSeconds(0.5)), FillBehavior.Stop));
			}
			CancellationTokenSource previousCancellationTokenSource = currentCancellationTokenSource;
			var myCancellationTokenSource = new CancellationTokenSource();
			currentCancellationTokenSource = myCancellationTokenSource;
			// cancel the previous only after current was set to the new one (avoid that the old one still finishes successfully)
			if (previousCancellationTokenSource != null)
				previousCancellationTokenSource.Cancel();
			var task = RunDecompiler(ILSpy.Language.Current, treeNodes.ToArray(), myCancellationTokenSource.Token);
			Action continuation = delegate {
				try {
					if (currentCancellationTokenSource == myCancellationTokenSource) {
						currentCancellationTokenSource = null;
						waitAdorner.Visibility = Visibility.Collapsed;
						foldingManager.Clear();
						try {
							SmartTextOutput textOutput = task.Result;
							referenceElementGenerator.References = textOutput.References;
							definitionLookup = textOutput.DefinitionLookup;
							textEditor.SyntaxHighlighting = ILSpy.Language.Current.SyntaxHighlighting;
							textEditor.Text = textOutput.ToString();
							foldingManager.UpdateFoldings(textOutput.Foldings.OrderBy(f => f.StartOffset), -1);
						} catch (AggregateException ex) {
							textEditor.SyntaxHighlighting = null;
							referenceElementGenerator.References = null;
							definitionLookup = null;
							textEditor.Text = string.Join(Environment.NewLine, ex.InnerExceptions.Select(ie => ie.ToString()));
						}
					} else {
						try {
							task.Wait();
						} catch (AggregateException) {
							// observe the exception (otherwise the task's finalizer will shut down the AppDomain)
						}
					}
				} finally {
					myCancellationTokenSource.Dispose();
				}
			};
			task.ContinueWith(delegate { Dispatcher.BeginInvoke(DispatcherPriority.Normal, continuation); });
		}
		
		static Task<SmartTextOutput> RunDecompiler(ILSpy.Language language, ILSpyTreeNodeBase[] nodes, CancellationToken cancellationToken)
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
					Dispatcher.Invoke(DispatcherPriority.Background, new Action(
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
		
		void cancelButton_Click(object sender, RoutedEventArgs e)
		{
			if (currentCancellationTokenSource != null)
				currentCancellationTokenSource.Cancel();
		}
	}
}
