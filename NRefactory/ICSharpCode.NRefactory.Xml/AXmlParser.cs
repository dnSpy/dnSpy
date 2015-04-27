// Copyright (c) 2009-2013 AlphaSierraPapa for the SharpDevelop Team
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
using System.Linq;
using System.Threading;
using System.Xml.Linq;
using ICSharpCode.NRefactory.Editor;
using ICSharpCode.NRefactory.Utils;

namespace ICSharpCode.NRefactory.Xml
{
	/// <summary>
	/// XML parser that is error tolerant.
	/// </summary>
	public class AXmlParser
	{
		/// <summary>
		/// Generate syntax error when seeing entity reference other then the built-in ones
		/// </summary>
		public bool UnknownEntityReferenceIsError { get; set; }
		
		IList<AXmlObject> CreatePublic(IList<InternalObject> internalObjects)
		{
			var publicObjects = new AXmlObject[internalObjects.Count];
			int pos = 0;
			for (int i = 0; i < internalObjects.Count; i++) {
				publicObjects[i] = internalObjects[i].CreatePublicObject(null, pos);
				pos += internalObjects[i].Length;
			}
			return Array.AsReadOnly(publicObjects);
		}
		
		/// <summary>
		/// Parses a document into a flat list of tags.
		/// </summary>
		/// <returns>Parsed tag soup.</returns>
		public IList<AXmlObject> ParseTagSoup(ITextSource textSource,
		                                      CancellationToken cancellationToken = default(CancellationToken))
		{
			if (textSource == null)
				throw new ArgumentNullException("textSource");
			var reader = new TagReader(this, textSource, false);
			var internalObjects = reader.ReadAllObjects(cancellationToken);
			return CreatePublic(internalObjects);
		}
		
		/// <summary>
		/// Parses a document incrementally into a flat list of tags.
		/// </summary>
		/// <param name="oldParserState">The parser state from a previous call to ParseIncremental(). Use null for the first call.</param>
		/// <param name="newTextSource">The text source for the new document version.</param>
		/// <param name="newParserState">Out: the new parser state, pass this to the next ParseIncremental() call.</param>
		/// <param name="cancellationToken">Optional: cancellation token.</param>
		/// <returns>Parsed tag soup.</returns>
		public IList<AXmlObject> ParseTagSoupIncremental(
			IncrementalParserState oldParserState, ITextSource newTextSource, out IncrementalParserState newParserState,
			CancellationToken cancellationToken = default(CancellationToken))
		{
			if (newTextSource == null)
				throw new ArgumentNullException("newTextSource");
			var internalObjects = InternalParseIncremental(oldParserState, newTextSource, out newParserState, false, cancellationToken);
			return CreatePublic(internalObjects);
		}
		
		List<InternalObject> InternalParseIncremental(
			IncrementalParserState oldParserState, ITextSource newTextSource, out IncrementalParserState newParserState,
			bool collapseProperlyNestedElements, CancellationToken cancellationToken)
		{
			var reader = new TagReader(this, newTextSource, collapseProperlyNestedElements);
			ITextSourceVersion newVersion = newTextSource.Version;
			var reuseMap = oldParserState != null ? oldParserState.GetReuseMapTo(newVersion) : null;
			
			List<InternalObject> internalObjects;
			if (reuseMap != null)
				internalObjects = reader.ReadAllObjectsIncremental(oldParserState.Objects, reuseMap, cancellationToken);
			else
				internalObjects = reader.ReadAllObjects(cancellationToken);
			
			if (newVersion != null)
				newParserState = new IncrementalParserState(newTextSource.TextLength, newVersion, internalObjects.ToArray());
			else
				newParserState = null;
			
			return internalObjects;
		}
		
		/// <summary>
		/// Parses a document.
		/// </summary>
		public AXmlDocument Parse(ITextSource textSource, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (textSource == null)
				throw new ArgumentNullException("textSource");
			var reader = new TagReader(this, textSource, true);
			var internalObjects = reader.ReadAllObjects(cancellationToken);
			var heuristic = new TagMatchingHeuristics(textSource);
			return new AXmlDocument(null, 0, heuristic.CreateDocument(internalObjects, cancellationToken));
		}
		
		/// <summary>
		/// Parses a document incrementally into a flat list of tags.
		/// </summary>
		/// <param name="oldParserState">The parser state from a previous call to ParseIncremental(). Use null for the first call.</param>
		/// <param name="newTextSource">The text source for the new document version.</param>
		/// <param name="newParserState">Out: the new parser state, pass this to the next ParseIncremental() call.</param>
		/// <param name="cancellationToken">Optional: cancellation token.</param>
		/// <returns>Parsed tag soup.</returns>
		public AXmlDocument ParseIncremental(
			IncrementalParserState oldParserState, ITextSource newTextSource, out IncrementalParserState newParserState,
			CancellationToken cancellationToken = default(CancellationToken))
		{
			if (newTextSource == null)
				throw new ArgumentNullException("newTextSource");
			var internalObjects = InternalParseIncremental(oldParserState, newTextSource, out newParserState, true, cancellationToken);
			var heuristic = new TagMatchingHeuristics(newTextSource);
			return new AXmlDocument(null, 0, heuristic.CreateDocument(internalObjects, cancellationToken));
		}
		
		/// <summary>
		/// Checks whether the given name is a valid XML name.
		/// </summary>
		public static bool IsValidXmlName(string name)
		{
			if (string.IsNullOrWhiteSpace(name))
				throw new ArgumentException("The XML name cannot be null, empty or consist solely of white space", "name");
			return TagReader.IsValidName(name);
		}
	}
}
