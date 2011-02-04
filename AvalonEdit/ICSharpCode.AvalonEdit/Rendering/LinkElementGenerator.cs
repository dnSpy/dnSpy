// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;

using ICSharpCode.AvalonEdit.Document;
using System.Windows.Navigation;

namespace ICSharpCode.AvalonEdit.Rendering
{
	// This class is public because it can be used as a base class for custom links.
	
	/// <summary>
	/// Detects hyperlinks and makes them clickable.
	/// </summary>
	/// <remarks>
	/// This element generator can be easily enabled and configured using the
	/// <see cref="TextEditorOptions"/>.
	/// </remarks>
	public class LinkElementGenerator : VisualLineElementGenerator, IBuiltinElementGenerator
	{
		// a link starts with a protocol (or just with www), followed by 0 or more 'link characters', followed by a link end character
		// (this allows accepting punctuation inside links but not at the end)
		internal readonly static Regex defaultLinkRegex = new Regex(@"\b(https?://|ftp://|www\.)[\w\d\._/\-~%@()+:?&=#!]*[\w\d/]");
		
		// try to detect email addresses
		internal readonly static Regex defaultMailRegex = new Regex(@"\b[\w\d\.\-]+\@[\w\d\.\-]+\.[a-z]{2,6}\b");
		
		readonly Regex linkRegex;
		
		/// <summary>
		/// Gets/Sets whether the user needs to press Control to click the link.
		/// The default value is true.
		/// </summary>
		public bool RequireControlModifierForClick { get; set; }
		
		/// <summary>
		/// Creates a new LinkElementGenerator.
		/// </summary>
		public LinkElementGenerator()
		{
			this.linkRegex = defaultLinkRegex;
			this.RequireControlModifierForClick = true;
		}
		
		/// <summary>
		/// Creates a new LinkElementGenerator using the specified regex.
		/// </summary>
		protected LinkElementGenerator(Regex regex) : this()
		{
			if (regex == null)
				throw new ArgumentNullException("regex");
			this.linkRegex = regex;
		}
		
		void IBuiltinElementGenerator.FetchOptions(TextEditorOptions options)
		{
			this.RequireControlModifierForClick = options.RequireControlModifierForHyperlinkClick;
		}
		
		Match GetMatch(int startOffset)
		{
			int endOffset = CurrentContext.VisualLine.LastDocumentLine.EndOffset;
			string relevantText = CurrentContext.Document.GetText(startOffset, endOffset - startOffset);
			return linkRegex.Match(relevantText);
		}
		
		/// <inheritdoc/>
		public override int GetFirstInterestedOffset(int startOffset)
		{
			Match m = GetMatch(startOffset);
			return m.Success ? startOffset + m.Index : -1;
		}
		
		/// <inheritdoc/>
		public override VisualLineElement ConstructElement(int offset)
		{
			Match m = GetMatch(offset);
			if (m.Success && m.Index == 0) {
				Uri uri = GetUriFromMatch(m);
				if (uri == null)
					return null;
				VisualLineLinkText linkText = new VisualLineLinkText(CurrentContext.VisualLine, m.Length);
				linkText.NavigateUri = uri;
				linkText.RequireControlModifierForClick = this.RequireControlModifierForClick;
				return linkText;
			} else {
				return null;
			}
		}
		
		/// <summary>
		/// Fetches the URI from the regex match. Returns null if the URI format is invalid.
		/// </summary>
		protected virtual Uri GetUriFromMatch(Match match)
		{
			string targetUrl = match.Value;
			if (targetUrl.StartsWith("www.", StringComparison.Ordinal))
				targetUrl = "http://" + targetUrl;
			if (Uri.IsWellFormedUriString(targetUrl, UriKind.Absolute))
				return new Uri(targetUrl);
			
			return null;
		}
	}
	
	// This class is internal because it does not need to be accessed by the user - it can be configured using TextEditorOptions.
	
	/// <summary>
	/// Detects e-mail addresses and makes them clickable.
	/// </summary>
	/// <remarks>
	/// This element generator can be easily enabled and configured using the
	/// <see cref="TextEditorOptions"/>.
	/// </remarks>
	sealed class MailLinkElementGenerator : LinkElementGenerator
	{
		/// <summary>
		/// Creates a new MailLinkElementGenerator.
		/// </summary>
		public MailLinkElementGenerator()
			: base(defaultMailRegex)
		{
		}
		
		/// <inheritdoc/>
		protected override Uri GetUriFromMatch(Match match)
		{
			return new Uri("mailto:" + match.Value);
		}
	}
}
