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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using ICSharpCode.Decompiler.Ast;
using ICSharpCode.Decompiler.Disassembler;
using ICSharpCode.Decompiler.ILAst;
using ICSharpCode.NRefactory;
using ICSharpCode.NRefactory.CSharp;
using Mono.Cecil;

namespace ICSharpCode.Decompiler
{
	/// <summary>
	/// Maps the source code to IL.
	/// </summary>
	public sealed class SourceCodeMapping
	{
		/// <summary>
		/// Gets or sets the start location of the instruction.
		/// </summary>
		public TextLocation StartLocation { get; set; }
		
		/// <summary>
		/// Gets or sets the end location of the instruction.
		/// </summary>
		public TextLocation EndLocation { get; set; }
		
		/// <summary>
		/// Gets or sets IL Range offset for the source code line. E.g.: 13-19 &lt;-&gt; 135.
		/// </summary>
		public ILRange ILInstructionOffset { get; set; }
		
		/// <summary>
		/// Gets or sets the member mapping this source code mapping belongs to.
		/// </summary>
		public MemberMapping MemberMapping { get; set; }
		
		/// <summary>
		/// Retrieves the array that contains the IL range and the missing gaps between ranges.
		/// </summary>
		/// <returns>The array representation of the step aranges.</returns>
		public int[] ToArray(bool isMatch)
		{
			var currentList = new List<ILRange>();
			
			// add list for the current source code line
			currentList.AddRange(ILRange.OrderAndJoint(MemberMapping.MemberCodeMappings
			                                           .FindAll(m => m.StartLocation.Line == this.StartLocation.Line)
			                                           .ConvertAll<ILRange>(m => m.ILInstructionOffset)));
			
			if (!isMatch) {
				// add inverted
				currentList.AddRange(MemberMapping.InvertedList);
			} else {
				// if the current list contains the last mapping, add also the last gap
				var lastInverted = MemberMapping.InvertedList.LastOrDefault();
				if (lastInverted != null && lastInverted.From == currentList[currentList.Count - 1].To)
					currentList.Add(lastInverted);
			}
			
			// set the output
			var resultList = new List<int>();
			foreach (var element in ILRange.OrderAndJoint(currentList)) {
				resultList.Add(element.From);
				resultList.Add(element.To);
			}
			
			return resultList.ToArray();
		}
	}
	
	/// <summary>
	/// Stores the member information and its source code mappings.
	/// </summary>
	public sealed class MemberMapping
	{
		IEnumerable<ILRange> invertedList;
		
		internal MemberMapping()
		{
		}
		
		public MemberMapping(MethodDefinition method)
		{
			this.MetadataToken = method.MetadataToken.ToInt32();
			this.MemberCodeMappings = new List<SourceCodeMapping>();
			this.MemberReference = method;
			this.CodeSize = method.Body.CodeSize;
		}
		
		/// <summary>
		/// Gets or sets the type of the mapping.
		/// </summary>
		public MemberReference MemberReference { get; internal set; }
		
		/// <summary>
		/// Metadata token of the member.
		/// </summary>
		public int MetadataToken { get; internal set; }
		
		/// <summary>
		/// Gets or sets the code size for the member mapping.
		/// </summary>
		public int CodeSize { get; internal set; }
		
		/// <summary>
		/// Gets or sets the source code mappings.
		/// </summary>
		public List<SourceCodeMapping> MemberCodeMappings { get; internal set; }
		
		/// <summary>
		/// Gets or sets the local variables.
		/// </summary>
		public IEnumerable<ILVariable> LocalVariables { get; internal set; }
		
		/// <summary>
		/// Gets the inverted IL Ranges.<br/>
		/// E.g.: for (0-9, 11-14, 14-18, 21-25) => (9-11,18-21).
		/// </summary>
		/// <returns>IL Range inverted list.</returns>
		public IEnumerable<ILRange> InvertedList
		{
			get {
				if (invertedList == null) {
					var list = MemberCodeMappings.ConvertAll<ILRange>(
						s => new ILRange { From = s.ILInstructionOffset.From, To = s.ILInstructionOffset.To });
					invertedList = ILRange.OrderAndJoint(ILRange.Invert(list, CodeSize));
				}
				return invertedList;
			}
		}
	}
	
	/// <summary>
	/// Code mappings helper class.
	/// </summary>
	public static class CodeMappings
	{
		/// <summary>
		/// Gets source code mapping and metadata token based on type name and line number.
		/// </summary>
		/// <param name="codeMappings">Code mappings storage.</param>
		/// <param name="typeName">Member reference name.</param>
		/// <param name="lineNumber">Line number.</param>
		/// <param name="metadataToken">Metadata token.</param>
		/// <returns></returns>
		public static SourceCodeMapping GetInstructionByLineNumber(
			this MemberMapping codeMapping,
			int lineNumber,
			out int metadataToken)
		{
			if (codeMapping == null)
				throw new ArgumentException("CodeMappings storage must be valid!");
			
			var map = codeMapping.MemberCodeMappings.Find(m => m.StartLocation.Line == lineNumber);
			if (map != null) {
				metadataToken = codeMapping.MetadataToken;
				return map;
			}
			
			metadataToken = 0;
			return null;
		}
		
		/// <summary>
		/// Gets a mapping given a type, a token and an IL offset.
		/// </summary>
		/// <param name="codeMappings">Code mappings storage.</param>
		/// <param name="token">Token.</param>
		/// <param name="ilOffset">IL offset.</param>
		/// <param name="isMatch">True, if perfect match.</param>
		/// <returns>A code mapping.</returns>
		public static SourceCodeMapping GetInstructionByTokenAndOffset(
			this MemberMapping codeMapping,
			int ilOffset,
			out bool isMatch)
		{
			isMatch = false;
			
			if (codeMapping == null)
				throw new ArgumentNullException("CodeMappings storage must be valid!");
			
			// try find an exact match
			var map = codeMapping.MemberCodeMappings.Find(m => m.ILInstructionOffset.From <= ilOffset && ilOffset < m.ILInstructionOffset.To);
			
			if (map == null) {
				// get the immediate next one
				map = codeMapping.MemberCodeMappings.Find(m => m.ILInstructionOffset.From > ilOffset);
				isMatch = false;
				if (map == null)
					map = codeMapping.MemberCodeMappings.LastOrDefault(); // get the last
				
				return map;
			}
			
			isMatch = true;
			return map;
		}
		
		/// <summary>
		/// Gets the source code and type name from metadata token and offset.
		/// </summary>
		/// <param name="codeMappings">Code mapping storage.</param>
		/// <param name="token">Metadata token.</param>
		/// <param name="ilOffset">IL offset.</param>
		/// <param name="typeName">Type definition.</param>
		/// <param name="line">Line number.</param>
		/// <remarks>It is possible to exist to different types from different assemblies with the same metadata token.</remarks>
		public static bool GetInstructionByTokenAndOffset(
			this MemberMapping mapping,
			int ilOffset,
			out MemberReference member,
			out int line)
		{
			member = null;
			line = 0;
			
			if (mapping == null)
				throw new ArgumentException("CodeMappings storage must be valid!");

			var codeMapping = mapping.MemberCodeMappings.Find(
				cm => cm.ILInstructionOffset.From <= ilOffset && ilOffset <= cm.ILInstructionOffset.To - 1);
			if (codeMapping == null) {
				codeMapping = mapping.MemberCodeMappings.Find(cm => cm.ILInstructionOffset.From > ilOffset);
				if (codeMapping == null) {
					codeMapping = mapping.MemberCodeMappings.LastOrDefault();
					if (codeMapping == null)
						return false;
				}
			}
			
			member = mapping.MemberReference;
			line = codeMapping.StartLocation.Line;
			return true;
		}
	}
}
