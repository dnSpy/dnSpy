// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.ILAst;
using ICSharpCode.ILSpy.Bookmarks;
using ICSharpCode.ILSpy.Debugger.Bookmarks;
using ICSharpCode.ILSpy.Debugger.Tooltips;
using ICSharpCode.NRefactory.CSharp.Resolver;
using Mono.Cecil;

namespace ICSharpCode.ILSpy.Debugger.Services
{
	public static class DebuggerService
	{
		static IDebugger   currentDebugger;
		
		static DebuggerService()
		{
			BookmarkManager.Added   += BookmarkAdded;
			BookmarkManager.Removed += BookmarkRemoved;
		}
		
		static IDebugger GetCompatibleDebugger()
		{
			DebugData.IsDebuggerLoaded = true;
			return currentDebugger = new WindowsDebugger();
		}
		
		/// <summary>
		/// Gets the current debugger. The debugger addin is loaded on demand; so if you
		/// just want to check a property like IsDebugging, check <see cref="IsDebuggerLoaded"/>
		/// before using this property.
		/// </summary>
		public static IDebugger CurrentDebugger {
			get {
				if (currentDebugger == null) {
					currentDebugger = GetCompatibleDebugger();
					currentDebugger.DebugStarting += new EventHandler(OnDebugStarting);
					currentDebugger.DebugStarted += new EventHandler(OnDebugStarted);
					currentDebugger.DebugStopped += new EventHandler(OnDebugStopped);
				}
				return currentDebugger;
			}
		}
		
		/// <summary>
		/// Returns true if debugger is already loaded.
		/// </summary>
		public static bool IsDebuggerLoaded {
			get {
				return currentDebugger != null;
			}
		}
		
		static bool debuggerStarted;
		
		/// <summary>
		/// Gets whether the debugger is currently active.
		/// </summary>
		public static bool IsDebuggerStarted {
			get { return debuggerStarted; }
		}
		
		public static event EventHandler DebugStarting;
		public static event EventHandler DebugStarted;
		public static event EventHandler DebugStopped;
		
		static void OnDebugStarting(object sender, EventArgs e)
		{
			ClearDebugMessages();
			
			if (DebugStarting != null)
				DebugStarting(null, e);
		}
		
		static void OnDebugStarted(object sender, EventArgs e)
		{
			debuggerStarted = true;
			if (DebugStarted != null)
				DebugStarted(null, e);
		}
		
		static void OnDebugStopped(object sender, EventArgs e)
		{
			debuggerStarted = false;
			
			RemoveCurrentLineMarker();
			
			if (DebugStopped != null)
				DebugStopped(null, e);
		}
		
		public static void ClearDebugMessages()
		{
			
		}

		public static void PrintDebugMessage(string msg)
		{
			
		}

		public static event EventHandler<BreakpointBookmarkEventArgs> BreakPointChanged;
		public static event EventHandler<BreakpointBookmarkEventArgs> BreakPointAdded;
		public static event EventHandler<BreakpointBookmarkEventArgs> BreakPointRemoved;
		
		static void OnBreakPointChanged(BreakpointBookmarkEventArgs e)
		{
			if (BreakPointChanged != null) {
				BreakPointChanged(null, e);
			}
		}
		
		static void OnBreakPointAdded(BreakpointBookmarkEventArgs e)
		{
			if (BreakPointAdded != null) {
				BreakPointAdded(null, e);
			}
		}
		
		static void OnBreakPointRemoved(BreakpointBookmarkEventArgs e)
		{
			if (BreakPointRemoved != null) {
				BreakPointRemoved(null, e);
			}
		}
		
		public static IList<BreakpointBookmark> Breakpoints {
			get {
				List<BreakpointBookmark> breakpoints = new List<BreakpointBookmark>();
				foreach (var bookmark in BookmarkManager.Bookmarks) {
					BreakpointBookmark breakpoint = bookmark as BreakpointBookmark;
					if (breakpoint != null) {
						breakpoints.Add(breakpoint);
					}
				}
				return breakpoints.AsReadOnly();
			}
		}
		
		static void BookmarkAdded(object sender, BookmarkEventArgs e)
		{
			BreakpointBookmark bb = e.Bookmark as BreakpointBookmark;
			if (bb != null) {
				OnBreakPointAdded(new BreakpointBookmarkEventArgs(bb));
			}
		}
		
		static void BookmarkRemoved(object sender, BookmarkEventArgs e)
		{
			BreakpointBookmark bb = e.Bookmark as BreakpointBookmark;
			if (bb != null) {
				OnBreakPointRemoved(new BreakpointBookmarkEventArgs(bb));
			}
		}
		
		static void BookmarkChanged(object sender, EventArgs e)
		{
			BreakpointBookmark bb = sender as BreakpointBookmark;
			if (bb != null) {
				OnBreakPointChanged(new BreakpointBookmarkEventArgs(bb));
			}
		}
		
		public static void ToggleBreakpointAt(MemberReference member, int lineNumber, ILRange range, DecompiledLanguages language)
		{
			BookmarkManager.ToggleBookmark(
				member.FullName, lineNumber,
				b => b.CanToggle && b is BreakpointBookmark,
				location => new BreakpointBookmark(member, location, range, BreakpointAction.Break, language));
		}
		
		/* TODO: reimplement this stuff
		static void ViewContentOpened(object sender, ViewContentEventArgs e)
		{
				textArea.IconBarMargin.MouseDown += IconBarMouseDown;
				textArea.ToolTipRequest          += TextAreaToolTipRequest;
				textArea.MouseLeave              += TextAreaMouseLeave;
		}*/
		
		public static void RemoveCurrentLineMarker()
		{
			CurrentLineBookmark.Remove();
		}
		
		public static void JumpToCurrentLine(MemberReference memberReference, int startLine, int startColumn, int endLine, int endColumn)
		{
			CurrentLineBookmark.SetPosition(memberReference, startLine, startColumn, endLine, endColumn);
		}
		
		#region Tool tips
		/// <summary>
		/// Gets debugger tooltip information for the specified position.
		/// A descriptive string for the element or a DebuggerTooltipControl
		/// showing its current value (when in debugging mode) can be returned
		/// through the ToolTipRequestEventArgs.SetTooltip() method.
		/// </summary>
		internal static void HandleToolTipRequest(ToolTipRequestEventArgs e)
		{
			if (!e.InDocument)
				return;
			
			var logicPos = e.LogicalPosition;
			var doc = (TextDocument)e.Editor.Document;
			var line = doc.GetLineByNumber(logicPos.Line);
			
			if (line.Offset + logicPos.Column >= doc.TextLength)
				return;
			
			var c = doc.GetText(line.Offset + logicPos.Column, 1);			
			if (string.IsNullOrEmpty(c) || c == "\n" || c == "\t")
				return;
			
			string variable =
				ParserService.SimpleParseAt(doc.Text, doc.GetOffset(new TextLocation(logicPos.Line, logicPos.Column)));
			
			if (currentDebugger == null || !currentDebugger.IsDebugging || !currentDebugger.CanEvaluate) {
				e.ContentToShow = null;
			}
			else {
				try {
					e.ContentToShow = currentDebugger.GetTooltipControl(e.LogicalPosition, variable);
				} catch {
					return;
				}
			}
			
			// FIXME Do proper parsing
//
//			using (var sr = new StringReader(doc.Text))
//			{
//				var parser = new CSharpParser();
//				parser.Parse(sr);
//
//				IExpressionFinder expressionFinder = ParserService.GetExpressionFinder();
//				if (expressionFinder == null)
//					return;
//				var currentLine = doc.GetLine(logicPos.Y);
//				if (logicPos.X > currentLine.Length)
//					return;
//				string textContent = doc.Text;
//				ExpressionResult expressionResult = expressionFinder.FindFullExpression(textContent, doc.GetOffset(new TextLocation(logicPos.Line, logicPos.Column)));
//				string expression = (expressionResult.Expression ?? "").Trim();
//				if (expression.Length > 0) {
//					// Look if it is variable
//					ResolveResult result = ParserService.Resolve(expressionResult, logicPos.Y, logicPos.X, e.Editor.FileName, textContent);
//					bool debuggerCanShowValue;
//					string toolTipText = GetText(result, expression, out debuggerCanShowValue);
//					if (Control.ModifierKeys == Keys.Control) {
//						toolTipText = "expr: " + expressionResult.ToString() + "\n" + toolTipText;
//						debuggerCanShowValue = false;
//					}
//					if (toolTipText != null) {
//						if (debuggerCanShowValue && currentDebugger != null) {
//							object toolTip = currentDebugger.GetTooltipControl(e.LogicalPosition, expressionResult.Expression);
//							if (toolTip != null)
//								e.SetToolTip(toolTip);
//							else
//								e.SetToolTip(toolTipText);
//						} else {
//							e.SetToolTip(toolTipText);
//						}
//					}
//				}
//				else {
//					#if DEBUG
//					if (Control.ModifierKeys == Keys.Control) {
//						e.SetToolTip("no expr: " + expressionResult.ToString());
//					}
//					#endif
//				}
//			}
		}

		static string GetText(ResolveResult result, string expression, out bool debuggerCanShowValue)
		{
			debuggerCanShowValue = false;
			return "FIXME";
			
			// FIXME
//			debuggerCanShowValue = false;
//			if (result == null) {
//				// when pressing control, show the expression even when it could not be resolved
//				return (Control.ModifierKeys == Keys.Control) ? "" : null;
//			}
//			if (result is MixedResolveResult)
//				return GetText(((MixedResolveResult)result).PrimaryResult, expression, out debuggerCanShowValue);
//			else if (result is DelegateCallResolveResult)
//				return GetText(((DelegateCallResolveResult)result).Target, expression, out debuggerCanShowValue);
//
//			IAmbience ambience = AmbienceService.GetCurrentAmbience();
//			ambience.ConversionFlags = ConversionFlags.StandardConversionFlags | ConversionFlags.UseFullyQualifiedMemberNames;
//			if (result is MemberResolveResult) {
//				return GetMemberText(ambience, ((MemberResolveResult)result).ResolvedMember, expression, out debuggerCanShowValue);
//			} else if (result is LocalResolveResult) {
//				LocalResolveResult rr = (LocalResolveResult)result;
//				ambience.ConversionFlags = ConversionFlags.UseFullyQualifiedTypeNames
//					| ConversionFlags.ShowReturnType | ConversionFlags.ShowDefinitionKeyWord;
//				StringBuilder b = new StringBuilder();
//				if (rr.IsParameter)
//					b.Append("parameter ");
//				else
//					b.Append("local variable ");
//				b.Append(ambience.Convert(rr.Field));
//				if (currentDebugger != null) {
//					string currentValue = currentDebugger.GetValueAsString(rr.VariableName);
//					if (currentValue != null) {
//						debuggerCanShowValue = true;
//						b.Append(" = ");
//						if (currentValue.Length > 256)
//							currentValue = currentValue.Substring(0, 256) + "...";
//						b.Append(currentValue);
//					}
//				}
//				return b.ToString();
//			} else if (result is NamespaceResolveResult) {
//				return "namespace " + ((NamespaceResolveResult)result).Name;
//			} else if (result is TypeResolveResult) {
//				IClass c = ((TypeResolveResult)result).ResolvedClass;
//				if (c != null)
//					return GetMemberText(ambience, c, expression, out debuggerCanShowValue);
//				else
//					return ambience.Convert(result.ResolvedType);
//			} else if (result is MethodGroupResolveResult) {
//				MethodGroupResolveResult mrr = result as MethodGroupResolveResult;
//				IMethod m = mrr.GetMethodIfSingleOverload();
//				IMethod m2 = mrr.GetMethodWithEmptyParameterList();
//				if (m != null)
//					return GetMemberText(ambience, m, expression, out debuggerCanShowValue);
//				else if (ambience is VBNetAmbience && m2 != null)
//					return GetMemberText(ambience, m2, expression, out debuggerCanShowValue);
//				else
//					return "Overload of " + ambience.Convert(mrr.ContainingType) + "." + mrr.Name;
//			} else {
//				if (Control.ModifierKeys == Keys.Control) {
//					if (result.ResolvedType != null)
//						return "expression of type " + ambience.Convert(result.ResolvedType);
//					else
//						return "ResolveResult without ResolvedType";
//				} else {
//					return null;
//				}
//			}
		}

//		static string GetMemberText(IAmbience ambience, IEntity member, string expression, out bool debuggerCanShowValue)
//		{
//			bool tryDisplayValue = false;
//			debuggerCanShowValue = false;
//			StringBuilder text = new StringBuilder();
//			if (member is IField) {
//				text.Append(ambience.Convert(member as IField));
//				tryDisplayValue = true;
//			} else if (member is IProperty) {
//				text.Append(ambience.Convert(member as IProperty));
//				tryDisplayValue = true;
//			} else if (member is IEvent) {
//				text.Append(ambience.Convert(member as IEvent));
//			} else if (member is IMethod) {
//				text.Append(ambience.Convert(member as IMethod));
//			} else if (member is IClass) {
//				text.Append(ambience.Convert(member as IClass));
//			} else {
//				text.Append("unknown member ");
//				text.Append(member.ToString());
//			}
//			if (tryDisplayValue && currentDebugger != null) {
//				LoggingService.Info("asking debugger for value of '" + expression + "'");
//				string currentValue = currentDebugger.GetValueAsString(expression);
//				if (currentValue != null) {
//					debuggerCanShowValue = true;
//					text.Append(" = ");
//					text.Append(currentValue);
//				}
//			}
//			string documentation = member.Documentation;
//			if (documentation != null && documentation.Length > 0) {
//				text.Append('\n');
//				text.Append(ICSharpCode.SharpDevelop.Editor.CodeCompletion.CodeCompletionItem.ConvertDocumentation(documentation));
//			}
//			return text.ToString();
//		}
		#endregion
	}
}
