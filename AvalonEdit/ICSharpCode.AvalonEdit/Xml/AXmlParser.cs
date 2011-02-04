// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;

using ICSharpCode.AvalonEdit.Document;

namespace ICSharpCode.AvalonEdit.Xml
{
	/// <summary>
	/// Creates object tree from XML document.
	/// </summary>
	/// <remarks>
	/// The created tree fully describes the document and thus the orginal XML file can be
	/// exactly reproduced.
	/// 
	/// Any further parses will reparse only the changed parts and the existing tree will
	/// be updated with the changes.  The user can add event handlers to be notified of
	/// the changes.  The parser tries to minimize the number of changes to the tree.
	/// (for example, it will add a single child at the start of collection rather than
	/// clearing the collection and adding new children)
	/// 
	/// The object tree consists of following types:
	///   RawObject - Abstact base class for all types
	///     RawContainer - Abstact base class for all types that can contain child nodes
	///       RawDocument - The root object of the XML document
	///       RawElement - Logical grouping of other nodes together.  The first child is always the start tag.
	///       RawTag - Represents any markup starting with "&lt;" and (hopefully) ending with ">"
	///     RawAttribute - Name-value pair in a tag
	///     RawText - Whitespace or character data
	/// 
	/// For example, see the following XML and the produced object tree:
	/// <![CDATA[
	///   <!-- My favourite quote -->
	///   <quote author="Albert Einstein">
	///     Make everything as simple as possible, but not simpler.
	///   </quote>
	/// 
	///   RawDocument
	///     RawTag "<!--" "-->"
	///       RawText " My favourite quote "
	///     RawElement
	///       RawTag "<" "quote" ">"
	///         RawText " "
	///         RawAttribute 'author="Albert Einstein"'
	///       RawText "\n  Make everything as simple as possible, but not simpler.\n"
	///       RawTag "</" "quote" ">"
	/// ]]>
	/// 
	/// The precise content of RawTag depends on what it represents:
	/// <![CDATA[
	///   Start tag:  "<"  Name?  (RawText+ RawAttribute)* RawText* (">" | "/>")
	///   End tag:    "</" Name?  (RawText+ RawAttribute)* RawText* ">"
	///   P.instr.:   "<?" Name?  (RawText)* "?>"
	///   Comment:    "<!--"      (RawText)* "-->"
	///   CData:      "<![CDATA[" (RawText)* "]]" ">"
	///   DTD:        "<!DOCTYPE" (RawText+ RawTag)* RawText* ">"    (DOCTYPE or other DTD names)
	///   UknownBang: "<!"        (RawText)* ">"
	/// ]]>
	/// 
	/// The type of tag can be identified by the opening backet.
	/// There are helpper properties in the RawTag class to identify the type, exactly
	/// one of the properties will be true.
	/// 
	/// The closing bracket may be missing or may be different for mallformed XML.
	/// 
	/// Note that there can always be multiple consequtive RawText nodes.
	/// This is to ensure that idividual texts are not too long.
	/// 
	/// XML Spec:  http://www.w3.org/TR/xml/
	/// XML EBNF:  http://www.jelks.nu/XML/xmlebnf.html
	/// 
	/// Internals:
	/// 
	/// "Try" methods can silently fail by returning false.
	/// MoveTo methods do not move if they are already at the given target
	/// If methods return some object, it must be no-empty.  It is up to the caller to ensure
	/// the context is appropriate for reading.
	/// 
	/// </remarks>
	public class AXmlParser
	{
		AXmlDocument userDocument;
		
		internal TrackedSegmentCollection TrackedSegments { get; private set; }
		
		/// <summary>
		/// Generate syntax error when seeing enity reference other then the build-in ones
		/// </summary>
		public bool UnknownEntityReferenceIsError { get; set; }
		
		/// <summary> Create new parser </summary>
		public AXmlParser()
		{
			this.Lock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
			ClearInternal();
		}
		
		/// <summary> Throws exception if condition is false </summary>
		internal static void Assert(bool condition, string message)
		{
			if (!condition) {
				throw new InternalException("Assertion failed: " + message);
			}
		}

		/// <summary> Throws exception if condition is false </summary>
		[Conditional("DEBUG")]
		internal static void DebugAssert(bool condition, string message)
		{
			if (!condition) {
				throw new InternalException("Assertion failed: " + message);
			}
		}
		
		[Conditional("DEBUG")]
		internal static void Log(string text, params object[] pars)
		{
			//System.Diagnostics.Debug.WriteLine(string.Format(CultureInfo.InvariantCulture, "XML: " + text, pars));
		}
		
		/// <summary>
		/// Incrementaly parse the given text.
		/// You have to hold the write lock.
		/// </summary>
		/// <param name="input">
		/// The full XML text of the new document.
		/// </param>
		/// <param name="changesSinceLastParse">
		/// Changes since last parse.  Null will cause full reparse.
		/// </param>
		public AXmlDocument Parse(string input, IEnumerable<DocumentChangeEventArgs> changesSinceLastParse)
		{
			if (!Lock.IsWriteLockHeld)
				throw new InvalidOperationException("Lock needed!");
			
			// Use changes to invalidate cache
			if (changesSinceLastParse != null) {
				this.TrackedSegments.UpdateOffsetsAndInvalidate(changesSinceLastParse);
			} else {
				this.TrackedSegments.InvalidateAll();
			}
			
			TagReader tagReader = new TagReader(this, input);
			List<AXmlObject> tags = tagReader.ReadAllTags();
			AXmlDocument parsedDocument = new TagMatchingHeuristics(this, input, tags).ReadDocument();
			tagReader.PrintStringCacheStats();
			AXmlParser.Log("Updating main DOM tree...");
			userDocument.UpdateTreeFrom(parsedDocument);
			userDocument.DebugCheckConsistency(true);
			Assert(userDocument.GetSelfAndAllChildren().Count() == parsedDocument.GetSelfAndAllChildren().Count(), "Parsed document and updated document have different number of children");
			return userDocument;
		}
		
		/// <summary>
		/// Makes calls to Parse() thread-safe. Use Lock everywhere Parse() is called.
		/// </summary>
		public ReaderWriterLockSlim Lock { get; private set; }
		
		/// <summary>
		/// Returns the last cached version of the document.
		/// </summary>
		/// <exception cref="InvalidOperationException">No read lock is held by the current thread.</exception>
		public AXmlDocument LastDocument {
			get {
				if (!Lock.IsReadLockHeld)
					throw new InvalidOperationException("Read lock needed!");
				
				return userDocument;
			}
		}
		
		/// <summary>
		/// Clears the parser data.
		/// </summary>
		/// <exception cref="InvalidOperationException">No write lock is held by the current thread.</exception>
		public void Clear()
		{
			if (!Lock.IsWriteLockHeld)
				throw new InvalidOperationException("Write lock needed!");
			
			ClearInternal();
		}
		
		void ClearInternal()
		{
			this.UnknownEntityReferenceIsError = true;
			this.TrackedSegments = new TrackedSegmentCollection();
			this.userDocument = new AXmlDocument() { Parser = this };
			this.userDocument.Document = this.userDocument;
			// Track the document
			this.TrackedSegments.AddParsedObject(this.userDocument, null);
			this.userDocument.IsCached = false;
		}
	}
}
