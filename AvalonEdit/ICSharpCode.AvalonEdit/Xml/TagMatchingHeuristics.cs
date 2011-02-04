// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ICSharpCode.AvalonEdit.Utils;

namespace ICSharpCode.AvalonEdit.Xml
{
	class TagMatchingHeuristics
	{
		const int maxConfigurationCount = 10;
		
		AXmlParser parser;
		TrackedSegmentCollection trackedSegments;
		string input;
		List<AXmlObject> tags;
		
		public TagMatchingHeuristics(AXmlParser parser, string input, List<AXmlObject> tags)
		{
			this.parser = parser;
			this.trackedSegments = parser.TrackedSegments;
			this.input = input;
			this.tags = tags;
		}
		
		public AXmlDocument ReadDocument()
		{
			AXmlDocument doc = new AXmlDocument() { Parser = parser };
			
			// AXmlParser.Log("Flat stream: {0}", PrintObjects(tags));
			List<AXmlObject> valid = MatchTags(tags);
			// AXmlParser.Log("Fixed stream: {0}", PrintObjects(valid));
			IEnumerator<AXmlObject> validStream = valid.GetEnumerator();
			validStream.MoveNext(); // Move to first
			while(true) {
				// End of stream?
				try {
					if (validStream.Current == null) break;
				} catch (InvalidCastException) {
					break;
				}
				doc.AddChild(ReadTextOrElement(validStream));
			}
			
			if (doc.Children.Count > 0) {
				doc.StartOffset = doc.FirstChild.StartOffset;
				doc.EndOffset = doc.LastChild.EndOffset;
			}
			
			// Check well formed
			foreach(AXmlTag xmlDeclaration in doc.Children.OfType<AXmlTag>().Where(t => t.IsProcessingInstruction && string.Equals(t.Name, "xml", StringComparison.OrdinalIgnoreCase))) {
				if (xmlDeclaration.StartOffset != 0)
					TagReader.OnSyntaxError(doc, xmlDeclaration.StartOffset, xmlDeclaration.StartOffset + 5,
					                        "XML declaration must be at the start of document");
			}
			int elemCount = doc.Children.OfType<AXmlElement>().Count();
			if (elemCount == 0)
				TagReader.OnSyntaxError(doc, doc.EndOffset, doc.EndOffset,
				                        "Root element is missing");
			if (elemCount > 1) {
				AXmlElement next = doc.Children.OfType<AXmlElement>().Skip(1).First();
				TagReader.OnSyntaxError(doc, next.StartOffset, next.StartOffset,
				                        "Only one root element is allowed");
			}
			foreach(AXmlTag tag in doc.Children.OfType<AXmlTag>()) {
				if (tag.IsCData)
					TagReader.OnSyntaxError(doc, tag.StartOffset, tag.EndOffset,
					                        "CDATA not allowed in document root");
			}
			foreach(AXmlText text in doc.Children.OfType<AXmlText>()) {
				if (!text.ContainsOnlyWhitespace)
					TagReader.OnSyntaxError(doc, text.StartOffset, text.EndOffset,
					                        "Only whitespace is allowed in document root");
			}
				
			
			AXmlParser.Log("Constructed {0}", doc);
			trackedSegments.AddParsedObject(doc, null);
			return doc;
		}
		
		static AXmlObject ReadSingleObject(IEnumerator<AXmlObject> objStream)
		{
			AXmlObject obj = objStream.Current;
			objStream.MoveNext();
			return obj;
		}
		
		AXmlObject ReadTextOrElement(IEnumerator<AXmlObject> objStream)
		{
			AXmlObject curr = objStream.Current;
			if (curr is AXmlText || curr is AXmlElement) {
				return ReadSingleObject(objStream);
			} else {
				AXmlTag currTag = (AXmlTag)curr;
				if (currTag == StartTagPlaceholder) {
					return ReadElement(objStream);
				} else if (currTag.IsStartOrEmptyTag) {
					return ReadElement(objStream);
				} else {
					return ReadSingleObject(objStream);
				}
			}
		}
		
		AXmlElement ReadElement(IEnumerator<AXmlObject> objStream)
		{
			AXmlElement element = new AXmlElement();
			element.IsProperlyNested = true;
			
			// Read start tag
			AXmlTag startTag = ReadSingleObject(objStream) as AXmlTag;
			AXmlParser.DebugAssert(startTag != null, "Start tag expected");
			AXmlParser.DebugAssert(startTag.IsStartOrEmptyTag || startTag == StartTagPlaceholder, "Start tag expected");
			if (startTag == StartTagPlaceholder) {
				element.HasStartOrEmptyTag = false;
				element.IsProperlyNested = false;
				TagReader.OnSyntaxError(element, objStream.Current.StartOffset, objStream.Current.EndOffset,
				                        "Matching openning tag was not found");
			} else {
				element.HasStartOrEmptyTag = true;
				element.AddChild(startTag);
			}
			
			// Read content and end tag
			if (startTag == StartTagPlaceholder || // Check first in case the start tag is null
			    element.StartTag.IsStartTag)
			{
				while(true) {
					AXmlTag currTag = objStream.Current as AXmlTag; // Peek
					if (currTag == EndTagPlaceholder) {
						TagReader.OnSyntaxError(element, element.LastChild.EndOffset, element.LastChild.EndOffset,
						                        "Expected '</{0}>'", element.StartTag.Name);
						ReadSingleObject(objStream);
						element.HasEndTag = false;
						element.IsProperlyNested = false;
						break;
					} else if (currTag != null && currTag.IsEndTag) {
						if (element.HasStartOrEmptyTag && currTag.Name != element.StartTag.Name) {
							TagReader.OnSyntaxError(element, currTag.StartOffset + 2, currTag.StartOffset + 2 + currTag.Name.Length,
							                        "Expected '{0}'.  End tag must have same name as start tag.", element.StartTag.Name);
						}
						element.AddChild(ReadSingleObject(objStream));
						element.HasEndTag = true;
						break;
					}
					AXmlObject nested = ReadTextOrElement(objStream);
					
					AXmlElement nestedAsElement = nested as AXmlElement;
					if (nestedAsElement != null) {
						if (!nestedAsElement.IsProperlyNested)
							element.IsProperlyNested = false;
						element.AddChildren(Split(nestedAsElement).ToList());
					} else {
						element.AddChild(nested);
					}
				}
			} else {
				element.HasEndTag = false;
			}
			
			element.StartOffset = element.FirstChild.StartOffset;
			element.EndOffset = element.LastChild.EndOffset;
			
			AXmlParser.Assert(element.HasStartOrEmptyTag || element.HasEndTag, "Must have at least start or end tag");
			
			AXmlParser.Log("Constructed {0}", element);
			trackedSegments.AddParsedObject(element, null); // Need all elements in cache for offset tracking
			return element;
		}
		
		IEnumerable<AXmlObject> Split(AXmlElement elem)
		{
			int myIndention = GetIndentLevel(elem);
			// Has start tag and no end tag ?  (other then empty-element tag)
			if (elem.HasStartOrEmptyTag && elem.StartTag.IsStartTag && !elem.HasEndTag && myIndention != -1) {
				int lastAccepted = 0; // Accept start tag
				while (lastAccepted + 1 < elem.Children.Count) {
					AXmlObject nextItem = elem.Children[lastAccepted + 1];
					if (nextItem is AXmlText) {
						lastAccepted++; continue;  // Accept
					} else {
						// Include all more indented items
						if (GetIndentLevel(nextItem) > myIndention) {
							lastAccepted++; continue;  // Accept
						} else {
							break;  // Reject
						}
					}
				}
				// Accepted everything?
				if (lastAccepted + 1 == elem.Children.Count) {
					yield return elem;
					yield break;
				}
				AXmlParser.Log("Splitting {0} - take {1} of {2} nested", elem, lastAccepted, elem.Children.Count - 1);
				AXmlElement topHalf = new AXmlElement();
				topHalf.HasStartOrEmptyTag = elem.HasStartOrEmptyTag;
				topHalf.HasEndTag = elem.HasEndTag;
				topHalf.AddChildren(elem.Children.Take(1 + lastAccepted));    // Start tag + nested
				topHalf.StartOffset = topHalf.FirstChild.StartOffset;
				topHalf.EndOffset = topHalf.LastChild.EndOffset;
				TagReader.OnSyntaxError(topHalf, topHalf.LastChild.EndOffset, topHalf.LastChild.EndOffset,
						                 "Expected '</{0}>'", topHalf.StartTag.Name);
				
				AXmlParser.Log("Constructed {0}", topHalf);
				trackedSegments.AddParsedObject(topHalf, null);
				yield return topHalf;
				for(int i = lastAccepted + 1; i < elem.Children.Count; i++) {
					yield return elem.Children[i];
				}
			} else {
				yield return elem;
			}
		}
		
		int GetIndentLevel(AXmlObject obj)
		{
			int offset = obj.StartOffset - 1;
			int level = 0;
			while(true) {
				if (offset < 0) break;
				char c = input[offset];
				if (c == ' ') {
					level++;
				} else if (c == '\t') {
					level += 4;
				} else if (c == '\r' || c == '\n') {
					break;
				} else {
					return -1;
				}
				offset--;
			}
			return level;
		}
		
		/// <summary>
		/// Stack of still unmatched start tags.
		/// It includes the cost and backtack information.
		/// </summary>
		class Configuration
		{
			/// <summary> Unmatched start tags </summary>
			public ImmutableStack<AXmlTag> StartTags { get; set; }
			/// <summary> Properly nested tags </summary>
			public ImmutableStack<AXmlObject> Document { get; set; }
			/// <summary> Number of needed modificaitons to the document </summary>
			public int Cost { get; set; }
		}
		
		/// <summary>
		/// Dictionary which stores the cheapest configuration
		/// </summary>
		class Configurations: Dictionary<ImmutableStack<AXmlTag>, Configuration>
		{
			public Configurations()
			{
			}
			
			public Configurations(IEnumerable<Configuration> configs)
			{
				foreach(Configuration config in configs) {
					this.Add(config);
				}
			}
			
			/// <summary> Overwrite only if cheaper </summary>
			public void Add(Configuration newConfig)
			{
				Configuration oldConfig;
				if (this.TryGetValue(newConfig.StartTags, out oldConfig)) {
					if (newConfig.Cost < oldConfig.Cost) {
						this[newConfig.StartTags] = newConfig;
					}
				} else {
					base.Add(newConfig.StartTags, newConfig);
				}
			}
			
			public override string ToString()
			{
				StringBuilder sb = new StringBuilder();
				foreach(var kvp in this) {
					sb.Append("\n - '");
					foreach(AXmlTag startTag in kvp.Value.StartTags.Reverse()) {
						sb.Append('<');
						sb.Append(startTag.Name);
						sb.Append('>');
					}
					sb.AppendFormat("' = {0}", kvp.Value.Cost);
				}
				return sb.ToString();
			}
		}
		
		// Tags used to guide the element creation
		readonly AXmlTag StartTagPlaceholder = new AXmlTag();
		readonly AXmlTag EndTagPlaceholder = new AXmlTag();
		
		/// <summary>
		/// Add start or end tag placeholders so that the documment is properly nested
		/// </summary>
		List<AXmlObject> MatchTags(IEnumerable<AXmlObject> objs)
		{
			Configurations configurations = new Configurations();
			configurations.Add(new Configuration {
				StartTags = ImmutableStack<AXmlTag>.Empty,
				Document = ImmutableStack<AXmlObject>.Empty,
				Cost = 0,
			});
			foreach(AXmlObject obj in objs) {
				configurations = ProcessObject(configurations, obj);
			}
			// Close any remaining start tags
			foreach(Configuration conifg in configurations.Values) {
				while(!conifg.StartTags.IsEmpty) {
					conifg.StartTags = conifg.StartTags.Pop();
					conifg.Document = conifg.Document.Push(EndTagPlaceholder);
					conifg.Cost += 1;
				}
			}
			// AXmlParser.Log("Configurations after closing all remaining tags:" + configurations.ToString());
			Configuration bestConfig = configurations.Values.OrderBy(v => v.Cost).First();
			AXmlParser.Log("Best configuration has cost {0}", bestConfig.Cost);
			
			return bestConfig.Document.Reverse().ToList();
		}
		
		/// <summary> Get posible configurations after considering given object </summary>
		Configurations ProcessObject(Configurations oldConfigs, AXmlObject obj)
		{
			AXmlParser.Log("Processing {0}", obj);
			
			AXmlTag objAsTag = obj as AXmlTag;
			AXmlElement objAsElement = obj as AXmlElement;
			AXmlParser.DebugAssert(objAsTag != null || objAsElement != null || obj is AXmlText, obj.GetType().Name + " not expected");
			if (objAsElement != null)
				AXmlParser.Assert(objAsElement.IsProperlyNested, "Element not properly nested");
			
			Configurations newConfigs = new Configurations();
			
			foreach(var kvp in oldConfigs) {
				Configuration oldConfig = kvp.Value;
				var oldStartTags = oldConfig.StartTags;
				var oldDocument = oldConfig.Document;
				int oldCost = oldConfig.Cost;
				
				if (objAsTag != null && objAsTag.IsStartTag) {
					newConfigs.Add(new Configuration {                    // Push start-tag (cost 0)
						StartTags = oldStartTags.Push(objAsTag),
						Document = oldDocument.Push(objAsTag),
						Cost = oldCost,
					});
				} else if (objAsTag != null && objAsTag.IsEndTag) {
					newConfigs.Add(new Configuration {                    // Ignore (cost 1)
						StartTags = oldStartTags,
						Document = oldDocument.Push(StartTagPlaceholder).Push(objAsTag),
						Cost = oldCost + 1,
	               });
					if (!oldStartTags.IsEmpty && oldStartTags.Peek().Name != objAsTag.Name) {
						newConfigs.Add(new Configuration {                // Pop 1 item (cost 1) - not mathcing
							StartTags = oldStartTags.Pop(),
							Document = oldDocument.Push(objAsTag),
							Cost = oldCost + 1,
		               });
					}
					int popedCount = 0;
					var startTags = oldStartTags;
					var doc = oldDocument;
					foreach(AXmlTag poped in oldStartTags) {
						popedCount++;
						if (poped.Name == objAsTag.Name) {
							newConfigs.Add(new Configuration {             // Pop 'x' items (cost x-1) - last one is matching
								StartTags = startTags.Pop(),
								Document = doc.Push(objAsTag),
								Cost = oldCost + popedCount - 1,
							});
						}
						startTags = startTags.Pop();
						doc = doc.Push(EndTagPlaceholder);
					}
				} else {
					// Empty tag  or  other tag type  or  text  or  properly nested element
					newConfigs.Add(new Configuration {                    // Ignore (cost 0)
						StartTags = oldStartTags,
						Document = oldDocument.Push(obj),
						Cost = oldCost,
	               });
				}
			}
			
			// Log("New configurations:" + newConfigs.ToString());
			
			Configurations bestNewConfigurations = new Configurations(
				newConfigs.Values.OrderBy(v => v.Cost).Take(maxConfigurationCount)
			);
			
			// AXmlParser.Log("Best new configurations:" + bestNewConfigurations.ToString());
			
			return bestNewConfigurations;
		}
		
		#region Helper methods
		/*
		string PrintObjects(IEnumerable<AXmlObject> objs)
		{
			StringBuilder sb = new StringBuilder();
			foreach(AXmlObject obj in objs) {
				if (obj is AXmlTag) {
					if (obj == StartTagPlaceholder) {
						sb.Append("#StartTag#");
					} else if (obj == EndTagPlaceholder) {
						sb.Append("#EndTag#");
					} else {
						sb.Append(((AXmlTag)obj).OpeningBracket);
						sb.Append(((AXmlTag)obj).Name);
						sb.Append(((AXmlTag)obj).ClosingBracket);
					}
				} else if (obj is AXmlElement) {
					sb.Append('[');
					sb.Append(PrintObjects(((AXmlElement)obj).Children));
					sb.Append(']');
				} else if (obj is AXmlText) {
					sb.Append('~');
				} else {
					throw new InternalException("Should not be here: " + obj);
				}
			}
			return sb.ToString();
		}
		*/
		#endregion
	}
}
