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
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using ICSharpCode.NRefactory.Editor;
using ICSharpCode.NRefactory.Utils;

namespace ICSharpCode.NRefactory.Xml
{
	class TagMatchingHeuristics
	{
		readonly ITextSource textSource;
		
		const int MaxConfigurationCount = 30;
		
		public TagMatchingHeuristics(ITextSource textSource)
		{
			this.textSource = textSource;
		}
		
		public InternalDocument CreateDocument(List<InternalObject> tagSoup, CancellationToken cancellationToken)
		{
			var stack = InsertPlaceholderTags(tagSoup, cancellationToken);
			InternalDocument doc = new InternalDocument();
			var docElements = CreateElements(ref stack);
			docElements.Reverse(); // reverse due to stack
			doc.NestedObjects = new InternalObject[docElements.Count];
			int pos = 0;
			for (int i = 0; i < docElements.Count; i++) {
				doc.NestedObjects[i] = docElements[i].SetStartRelativeToParent(pos);
				pos += doc.NestedObjects[i].Length;
			}
			doc.Length = pos;
			return doc;
		}
		
		#region Heuristic implementation - Inserting place holders into object stream
		// Tags used to guide the element creation
		static readonly InternalTag StartTagPlaceholder = new InternalTag { OpeningBracket = "<", ClosingBracket = ">" };
		static readonly InternalTag EndTagPlaceholder = new InternalTag { OpeningBracket = "</", ClosingBracket = ">" };
		
		class OpenTagStack
		{
			readonly OpenTagStack prev;
			public readonly string Name;
			public readonly int IndentationLevel;
			readonly int hashCode;
			
			public OpenTagStack()
			{
			}
			
			private OpenTagStack(OpenTagStack prev, string name, int indentationLevel)
			{
				this.prev = prev;
				this.Name = name;
				this.IndentationLevel = indentationLevel;
				unchecked {
					this.hashCode = prev.hashCode * 27 + (name.GetHashCode() ^ indentationLevel);
				}
			}
			
			public bool IsEmpty {
				get { return prev == null; }
			}
			
			public OpenTagStack Push(string name, int indentationLevel)
			{
				return new OpenTagStack(this, name, indentationLevel);
			}
			
			public OpenTagStack Pop()
			{
				return prev;
			}
			
			public override int GetHashCode()
			{
				return hashCode;
			}
			
			public override bool Equals(object obj)
			{
				OpenTagStack o = obj as OpenTagStack;
				if (o != null && hashCode == o.hashCode && IndentationLevel == o.IndentationLevel && Name == o.Name) {
					if (prev == o.prev)
						return true;
					if (prev == null || o.prev == null)
						return false;
					return prev.Equals(o.prev);
				}
				return false;
			}
		}
		
		struct Configuration
		{
			public readonly OpenTagStack OpenTags;
			public readonly ImmutableStack<InternalObject> Document;
			public readonly uint Cost;
			
			public Configuration(OpenTagStack openTags, ImmutableStack<InternalObject> document, uint cost)
			{
				this.OpenTags = openTags;
				this.Document = document;
				this.Cost = cost;
			}
		}
		
		struct ConfigurationList
		{
			internal Configuration[] configurations;
			internal int count;
			
			public static ConfigurationList Create()
			{
				return new ConfigurationList {
					configurations = new Configuration[MaxConfigurationCount],
					count = 0
				};
			}
			
			public void Clear()
			{
				this.count = 0;
			}
			
			public void Add(OpenTagStack openTags, ImmutableStack<InternalObject> document, uint cost)
			{
				Add(new Configuration(openTags, document, cost));
			}
			
			public void Add(Configuration configuration)
			{
				for (int i = 0; i < count; i++) {
					if (configuration.OpenTags.Equals(configurations[i].OpenTags)) {
						// We found an existing configuration with the same state.
						// Either replace it, or drop this configurations --
						// we don't want to add multiple configurations with the same state.
						if (configuration.Cost < configurations[i].Cost)
							configurations[i] = configuration;
						return;
					}
				}
				if (count < configurations.Length) {
					configurations[count++] = configuration;
				} else {
					int index = 0;
					uint maxCost = configurations[0].Cost;
					for (int i = 1; i < configurations.Length; i++) {
						if (configurations[i].Cost < maxCost) {
							maxCost = configurations[i].Cost;
							index = i;
						}
					}
					configurations[index] = configuration;
				}
			}
		}
		
		const uint InfiniteCost = uint.MaxValue;
		const uint MissingEndTagCost = 10;
		const uint IgnoreEndTagCost = 10;
		const uint MismatchedNameCost = 6;
		
		int GetIndentationBefore(int position)
		{
			int indentation = 0;
			while (--position >= 0) {
				char c = textSource.GetCharAt(position);
				switch (c) {
					case ' ':
						indentation++;
						break;
					case '\t':
						indentation += 4;
						break;
					case '\n':
						return indentation;
					default:
						return -1;
				}
			}
			return indentation;
		}
		
		ImmutableStack<InternalObject> InsertPlaceholderTags(List<InternalObject> objects, CancellationToken cancellationToken)
		{
			// Calculate indentation levels in front of the tags:
			int[] indentationBeforeTags = new int[objects.Count];
			int pos = 0;
			for (int i = 0; i < objects.Count; i++) {
				if (objects[i] is InternalTag)
					indentationBeforeTags[i] = GetIndentationBefore(pos);
				pos += objects[i].Length;
			}
			
			// Create initial configuration:
			ConfigurationList listA = ConfigurationList.Create();
			ConfigurationList listB = ConfigurationList.Create();
			listA.Add(new Configuration(new OpenTagStack(), ImmutableStack<InternalObject>.Empty, 0));
			
			for (int i = 0; i < indentationBeforeTags.Length; i++) {
				cancellationToken.ThrowIfCancellationRequested();
				ProcessObject(objects[i], indentationBeforeTags[i], listA, ref listB);
				Swap(ref listA, ref listB);
			}
			
			Configuration cheapestConfiguration = new Configuration(null, null, InfiniteCost);
			for (int i = 0; i < listA.count; i++) {
				Configuration c = listA.configurations[i];
				if (c.Cost < cheapestConfiguration.Cost) {
					while (!c.OpenTags.IsEmpty) {
						c = new Configuration(c.OpenTags.Pop(), c.Document.Push(EndTagPlaceholder), c.Cost + MissingEndTagCost);
					}
					if (c.Cost < cheapestConfiguration.Cost)
						cheapestConfiguration = c;
				}
			}
			Log.WriteLine("Best configuration has cost {0}", cheapestConfiguration.Cost);
			return cheapestConfiguration.Document;
		}
		
		static void Swap(ref ConfigurationList a, ref ConfigurationList b)
		{
			ConfigurationList tmp = a;
			a = b;
			b = tmp;
		}
		
		void ProcessObject(InternalObject obj, int indentationLevel, ConfigurationList oldConfigurations, ref ConfigurationList newConfigurations)
		{
			newConfigurations.Clear();
			InternalTag tag = obj as InternalTag;
			for (int i = 0; i < oldConfigurations.count; i++) {
				Configuration c = oldConfigurations.configurations[i];
				if (c.Cost == InfiniteCost)
					continue;
				if (tag != null && tag.IsStartTag) {
					// Push start tag
					newConfigurations.Add(
						c.OpenTags.Push(tag.Name, indentationLevel),
						c.Document.Push(obj),
						c.Cost
					);
				} else if (tag != null && tag.IsEndTag) {
					// We can ignore this end tag
					newConfigurations.Add(
						c.OpenTags,
						c.Document.Push(StartTagPlaceholder).Push(obj),
						c.Cost + IgnoreEndTagCost
					);
					// We can match this end tag with one of the currently open tags
					var openTags = c.OpenTags;
					var documentWithInsertedEndTags = c.Document;
					uint newCost = c.Cost;
					while (!openTags.IsEmpty) {
						uint matchCost = 0;
						if (openTags.IndentationLevel >= 0 && indentationLevel >= 0)
							matchCost += (uint)Math.Abs(openTags.IndentationLevel - indentationLevel);
						if (openTags.Name != tag.Name)
							matchCost += MismatchedNameCost;
						newConfigurations.Add(
							openTags.Pop(),
							documentWithInsertedEndTags.Push(obj),
							newCost + matchCost
						);
						newCost += MissingEndTagCost;
						openTags = openTags.Pop();
						documentWithInsertedEndTags = documentWithInsertedEndTags.Push(EndTagPlaceholder);
					}
				} else {
					newConfigurations.Add(
						c.OpenTags,
						c.Document.Push(obj),
						c.Cost
					);
				}
			}
		}
		#endregion
		
		#region Create Elements from stack with place holders
		List<InternalObject> CreateElements(ref ImmutableStack<InternalObject> inputObjects)
		{
			List<InternalObject> objects = new List<InternalObject>();
			while (!inputObjects.IsEmpty) {
				var obj = inputObjects.Peek();
				var tag = obj as InternalTag;
				if (tag != null && tag.IsStartTag)
					break;
				inputObjects = inputObjects.Pop();
				if (tag != null && tag.IsEndTag) {
					if (inputObjects.Peek() == StartTagPlaceholder) {
						objects.Add(tag.AddSyntaxError("Matching opening tag was not found"));
						inputObjects = inputObjects.Pop();
					} else {
						var childElements = CreateElements(ref inputObjects);
						var startTag = (InternalTag)inputObjects.Peek();
						inputObjects = inputObjects.Pop();
						childElements.Add(startTag);
						childElements.Reverse();
						if (tag != EndTagPlaceholder) {
							// add end tag
							if (startTag.Name != tag.Name) {
								childElements.Add(tag.AddSyntaxError("Expected '</" + startTag.Name + ">'. End tag must have same name as start tag."));
							} else {
								childElements.Add(tag);
							}
						}
						InternalElement e = new InternalElement(startTag);
						e.HasEndTag = (tag != EndTagPlaceholder);
						e.NestedObjects = new InternalObject[childElements.Count];
						int pos = 0;
						for (int i = 0; i < childElements.Count; i++) {
							e.NestedObjects[i] = childElements[i].SetStartRelativeToParent(pos);
							pos += e.NestedObjects[i].Length;
						}
						e.Length = pos;
						if (tag == EndTagPlaceholder) {
							e.SyntaxErrors = new [] { new InternalSyntaxError(pos, pos, "Missing '</" + startTag.Name + ">'") };
						}
						objects.Add(e);
					}
				} else {
					objects.Add(obj);
				}
			}
			return objects;
		}
		#endregion
	}
}
