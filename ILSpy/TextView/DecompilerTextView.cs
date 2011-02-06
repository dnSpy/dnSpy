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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using System.Xml;

using ICSharpCode.AvalonEdit.Folding;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using ICSharpCode.Decompiler;
using ICSharpCode.ILSpy.TreeNodes;
using Microsoft.Win32;
using Mono.Cecil;

namespace ICSharpCode.ILSpy.TextView
{
	/// <summary>
	/// Manages the TextEditor showing the decompiled code.
	/// </summary>
	sealed partial class DecompilerTextView : UserControl
	{
		readonly ReferenceElementGenerator referenceElementGenerator;
		readonly UIElementGenerator uiElementGenerator;
		readonly FoldingManager foldingManager;
		internal MainWindow mainWindow;
		
		DefinitionLookup definitionLookup;
		CancellationTokenSource currentCancellationTokenSource;
		
		#region Constructor
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
			this.uiElementGenerator = new UIElementGenerator();
			textEditor.TextArea.TextView.ElementGenerators.Add(uiElementGenerator);
			textEditor.Text = "Welcome to ILSpy!";
			foldingManager = FoldingManager.Install(textEditor.TextArea);
		}
		#endregion
		
		#region RunWithCancellation
		void RunWithCancellation<T>(Func<CancellationToken, Task<T>> taskCreation, Action<Task<T>> taskCompleted)
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
			
			var task = taskCreation(myCancellationTokenSource.Token);
			Action continuation = delegate {
				try {
					if (currentCancellationTokenSource == myCancellationTokenSource) {
						currentCancellationTokenSource = null;
						waitAdorner.Visibility = Visibility.Collapsed;
						taskCompleted(task);
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
		
		void cancelButton_Click(object sender, RoutedEventArgs e)
		{
			if (currentCancellationTokenSource != null)
				currentCancellationTokenSource.Cancel();
		}
		#endregion
		
		#region ShowOutput
		void ShowOutput(SmartTextOutput textOutput, ILSpy.Language language = null)
		{
			textEditor.ScrollToHome();
			foldingManager.Clear();
			uiElementGenerator.UIElements = textOutput.UIElements;
			referenceElementGenerator.References = textOutput.References;
			definitionLookup = textOutput.DefinitionLookup;
			textEditor.SyntaxHighlighting = language != null ? language.SyntaxHighlighting : null;
			textEditor.Text = textOutput.ToString();
			foldingManager.UpdateFoldings(textOutput.Foldings.OrderBy(f => f.StartOffset), -1);
		}
		#endregion
		
		#region Decompile (for display)
		const int defaultOutputLengthLimit  =  5000000; // more than 5M characters is too slow to output (when user browses treeview)
		const int extendedOutputLengthLimit = 75000000; // more than 75M characters can get us into trouble with memory usage
		
		public void Decompile(ILSpy.Language language, IEnumerable<ILSpyTreeNodeBase> treeNodes, DecompilationOptions options)
		{
			Decompile(language, treeNodes.ToArray(), defaultOutputLengthLimit, options);
		}
		
		void Decompile(ILSpy.Language language, ILSpyTreeNodeBase[] treeNodes, int outputLengthLimit, DecompilationOptions options)
		{
			RunWithCancellation(
				delegate (CancellationToken ct) { // creation of the background task
					options.CancellationToken = ct;
					return RunDecompiler(language, treeNodes, options, outputLengthLimit);
				},
				delegate (Task<SmartTextOutput> task) { // handling the result
					try {
						SmartTextOutput textOutput = task.Result;
						Debug.WriteLine("Decompiler finished; output size = {0} characters", textOutput.TextLength);
						ShowOutput(textOutput, language);
					} catch (AggregateException aggregateException) {
						textEditor.SyntaxHighlighting = null;
						Debug.WriteLine("Decompiler crashed: " + aggregateException.ToString());
						// Unpack aggregate exceptions as long as there's only a single exception:
						// (assembly load errors might produce nested aggregate exceptions)
						Exception ex = aggregateException;
						while (ex is AggregateException && (ex as AggregateException).InnerExceptions.Count == 1)
							ex = ex.InnerException;
						if (ex is OutputLengthExceededException) {
							ShowOutputLengthExceededMessage(language, treeNodes, options, outputLengthLimit == defaultOutputLengthLimit);
						} else {
							SmartTextOutput output = new SmartTextOutput();
							output.WriteLine(ex.ToString());
							ShowOutput(output);
						}
					}
				});
		}
		
		static Task<SmartTextOutput> RunDecompiler(ILSpy.Language language, ILSpyTreeNodeBase[] nodes, DecompilationOptions options, int outputLengthLimit)
		{
			Debug.WriteLine("Start decompilation of {0} nodes", nodes.Length);
			
			if (nodes.Length == 0) {
				// If there's nothing to be decompiled, don't bother starting up a thread.
				// (Improves perf in some cases since we don't have to wait for the thread-pool to accept our task)
				TaskCompletionSource<SmartTextOutput> tcs = new TaskCompletionSource<SmartTextOutput>();
				tcs.SetResult(new SmartTextOutput());
				return tcs.Task;
			}
			
			return Task.Factory.StartNew(
				delegate {
					SmartTextOutput textOutput = new SmartTextOutput();
					textOutput.LengthLimit = outputLengthLimit;
					DecompileNodes(language, nodes, options, textOutput);
					return textOutput;
				});
		}
		
		static void DecompileNodes(ILSpy.Language language, ILSpyTreeNodeBase[] nodes, DecompilationOptions options, ITextOutput textOutput)
		{
			bool first = true;
			foreach (var node in nodes) {
				if (first) first = false; else textOutput.WriteLine();
				options.CancellationToken.ThrowIfCancellationRequested();
				node.Decompile(language, textOutput, options);
			}
		}
		#endregion
		
		#region ShowOutputLengthExceededMessage
		void ShowOutputLengthExceededMessage(ILSpy.Language language, ILSpyTreeNodeBase[] treeNodes, DecompilationOptions options, bool wasNormalLimit)
		{
			SmartTextOutput output = new SmartTextOutput();
			if (wasNormalLimit) {
				output.WriteLine("You have selected too much code for it to be displayed automatically.");
			} else {
				output.WriteLine("You have selected too much code; it cannot be displayed here.");
			}
			output.WriteLine();
			Button button;
			if (wasNormalLimit) {
				output.AddUIElement(MakeButton(
					Images.ViewCode, "Display Code",
					delegate {
						Decompile(language, treeNodes, extendedOutputLengthLimit, options);
					}));
				output.WriteLine();
			}
			
			output.AddUIElement(MakeButton(
				Images.Save, "Save Code",
				delegate {
					SaveToDisk(language, treeNodes, options);
				}));
			output.WriteLine();
			
			ShowOutput(output);
		}
		
		Func<Button> MakeButton(ImageSource icon, string text, RoutedEventHandler click)
		{
			return () => {
				Button button = new Button();
				button.Cursor = Cursors.Arrow;
				button.Margin = new Thickness(2);
				if (icon != null) {
					button.Content = new StackPanel {
						Orientation = Orientation.Horizontal,
						Children = {
							new Image { Width = 16, Height = 16, Source = icon, Margin = new Thickness(0, 0, 4, 0) },
							new TextBlock { Text = text }
						}
					};
				} else {
					button.Content = text;
				}
				button.Click += click;
				return button;
			};
		}
		#endregion
		
		#region JumpToReference
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
		#endregion
		
		#region SaveToDisk
		public void SaveToDisk(ILSpy.Language language, IEnumerable<ILSpyTreeNodeBase> treeNodes, DecompilationOptions options)
		{
			if (!treeNodes.Any())
				return;
			
			SaveFileDialog dlg = new SaveFileDialog();
			dlg.DefaultExt = language.FileExtension;
			dlg.Filter = language.Name + "|*" + language.FileExtension + "|All Files|*.*";
			dlg.FileName = CleanUpName(treeNodes.First().ToString()) + language.FileExtension;
			if (dlg.ShowDialog() == true) {
				SaveToDisk(language, treeNodes.ToArray(), options, dlg.FileName);
			}
		}
		
		void SaveToDisk(ILSpy.Language language, ILSpyTreeNodeBase[] nodes, DecompilationOptions options, string fileName)
		{
			RunWithCancellation(
				delegate (CancellationToken ct) {
					options.CancellationToken = ct;
					return Task.Factory.StartNew(
						delegate {
							using (StreamWriter w = new StreamWriter(fileName)) {
								try {
									DecompileNodes(language, nodes, options, new PlainTextOutput(w));
								} catch (OperationCanceledException) {
									w.WriteLine();
									w.WriteLine("Decompiled was cancelled.");
									throw;
								}
							}
							SmartTextOutput output = new SmartTextOutput();
							output.WriteLine("Decompilation complete.");
							output.WriteLine();
							output.AddUIElement(MakeButton(
								null, "Open Explorer",
								delegate {
									Process.Start("explorer", "/select,\"" + fileName + "\"");
								}
							));
							output.WriteLine();
							return output;
						});
				},
				delegate (Task<SmartTextOutput> task) {
					try {
						ShowOutput(task.Result);
					} catch (AggregateException aggregateException) {
						textEditor.SyntaxHighlighting = null;
						Debug.WriteLine("Decompiler crashed: " + aggregateException.ToString());
						// Unpack aggregate exceptions as long as there's only a single exception:
						// (assembly load errors might produce nested aggregate exceptions)
						Exception ex = aggregateException;
						while (ex is AggregateException && (ex as AggregateException).InnerExceptions.Count == 1)
							ex = ex.InnerException;
						SmartTextOutput output = new SmartTextOutput();
						output.WriteLine(ex.ToString());
						ShowOutput(output);
					}
				});
		}
		
		string CleanUpName(string text)
		{
			int pos = text.IndexOf(':');
			if (pos > 0)
				text = text.Substring(0, pos);
			text = text.Trim();
			foreach (char c in Path.GetInvalidFileNameChars())
				text = text.Replace(c, '-');
			return text;
		}
		#endregion
	}
}
