// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

using ICSharpCode.Decompiler.Ast;
using ICSharpCode.Decompiler.Disassembler;
using ICSharpCode.Decompiler.ILAst;
using Mono.Cecil;

namespace ICSharpCode.Decompiler
{
	public enum DecompiledLanguages
	{
		IL,
		CSharp
	}
	
	/// <summary>
	/// Maps the source code to IL.
	/// </summary>
	public class SourceCodeMapping
	{
		/// <summary>
		/// Gets or sets the source code line number in the output.
		/// </summary>
		public int SourceCodeLine { get; set; }
		
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
		/// <returns></returns>
		public int[] ToArray()
		{
			var resultList = new List<int>();
			
			// add list for the current source code line
			var currentList = MemberMapping.MemberCodeMappings.FindAll(m => m.SourceCodeLine == this.SourceCodeLine);
			foreach (var element in currentList.Distinct(new SourceCodeMappingComparer())) {
				resultList.Add(element.ILInstructionOffset.From);
				resultList.Add(element.ILInstructionOffset.To);
			}
			
			// add inverted
			var invertedList = MemberMapping.GetInvertedList();
			if (invertedList != null && invertedList.Count() > 0) {
				foreach (var range in invertedList) {
					resultList.Add(range.From);
					resultList.Add(range.To);
				}
			}
			
			return resultList.ToArray();
		}
		
		sealed class SourceCodeMappingComparer : IEqualityComparer<SourceCodeMapping>
		{
			public bool Equals(SourceCodeMapping x, SourceCodeMapping y)
			{
				//Check whether the compared objects reference the same data.
				if (Object.ReferenceEquals(x, y)) return true;
				
				//Check whether any of the compared objects is null.
				if (Object.ReferenceEquals(x, null) || Object.ReferenceEquals(y, null))
					return false;
				
				return x.ILInstructionOffset.From == y.ILInstructionOffset.From &&
					x.ILInstructionOffset.To == y.ILInstructionOffset.To &&
					x.SourceCodeLine == y.SourceCodeLine;
			}
			
			public int GetHashCode(SourceCodeMapping map)
			{
				//Check whether the object is null
				if (Object.ReferenceEquals(map, null)) return 0;

				//Get hash code for the ILInstructionOffset field if it is not null.
				int hashRange = map.ILInstructionOffset == null ? 0 : map.ILInstructionOffset.GetHashCode();

				//Get hash code for the SourceCodeLine field.
				int hashLine = map.SourceCodeLine.GetHashCode();

				//Calculate the hash code.
				return hashRange ^ hashLine;
			}
		}
	}
	
	/// <summary>
	/// Stores the method information and its source code mappings.
	/// </summary>
	public sealed class MemberMapping
	{
		/// <summary>
		/// Gets or sets the type of the mapping.
		/// </summary>
		public TypeDefinition Type { get; set; }
		
		/// <summary>
		/// Metadata token of the method.
		/// </summary>
		public uint MetadataToken { get; set; }
		
		/// <summary>
		/// Gets or sets the code size for the member mapping.
		/// </summary>
		public int CodeSize { get; set; }
		
		/// <summary>
		/// Gets or sets the source code mappings.
		/// </summary>
		public List<SourceCodeMapping> MemberCodeMappings { get; set; }
		
		/// <summary>
		/// Gets the inverted IL Ranges.<br/>
		/// E.g.: for (0-9, 11-14, 14-18, 21-25) => (9-11,18-21).
		/// </summary>
		/// <returns>IL Range inverted list.</returns>
		public IEnumerable<ILRange> GetInvertedList()
		{
			var list = MemberCodeMappings.ConvertAll<ILRange>(
				s => new ILRange { From = s.ILInstructionOffset.From, To = s.ILInstructionOffset.To });
			return ILRange.Invert(list, CodeSize);
		}
	}
	
	public static class CodeMappings
	{
		public static ConcurrentDictionary<string, List<MemberMapping>> GetStorage(DecompiledLanguages language)
		{
			ConcurrentDictionary<string, List<MemberMapping>> storage = null;
			
			switch (language) {
				case DecompiledLanguages.IL:
					storage = ILCodeMapping.SourceCodeMappings;
					break;
				case DecompiledLanguages.CSharp:
					storage = CSharpCodeMapping.SourceCodeMappings;
					break;
				default:
					throw new System.Exception("Invalid value for DecompiledLanguages");
			}
			
			return storage;
		}
		
		/// <summary>
		/// Create code mapping for a method.
		/// </summary>
		/// <param name="method">Method to create the mapping for.</param>
		/// <param name="sourceCodeMappings">Source code mapping storage.</param>
		public static MemberMapping CreateCodeMapping(
			this MethodDefinition member,
			ConcurrentDictionary<string, List<MemberMapping>> codeMappings)
		{
			// create IL/CSharp code mappings - used in debugger
			MemberMapping currentMemberMapping = null;
			if (codeMappings.ContainsKey(member.DeclaringType.FullName)) {
				var mapping = codeMappings[member.DeclaringType.FullName];
				if (mapping.Find(map => (int)map.MetadataToken == member.MetadataToken.ToInt32()) == null) {
					currentMemberMapping = new MemberMapping() {
						MetadataToken = (uint)member.MetadataToken.ToInt32(),
						Type = member.DeclaringType.Resolve(),
						MemberCodeMappings = new List<SourceCodeMapping>(),
						CodeSize = member.Body.CodeSize
					};
					mapping.Add(currentMemberMapping);
				}
			}
			
			return currentMemberMapping;
		}
		
		/// <summary>
		/// Gets source code mapping and metadata token based on type name and line number.
		/// </summary>
		/// <param name="codeMappings">Code mappings storage.</param>
		/// <param name="typeName">Type name.</param>
		/// <param name="lineNumber">Line number.</param>
		/// <param name="metadataToken">Metadata token.</param>
		/// <returns></returns>
		public static SourceCodeMapping GetInstructionByTypeAndLine(
			this ConcurrentDictionary<string, List<MemberMapping>> codeMappings,
			string typeName,
			int lineNumber,
			out uint metadataToken)
		{
			if (!codeMappings.ContainsKey(typeName)) {
				metadataToken = 0;
				return null;
			}
			
			if (lineNumber <= 0) {
				metadataToken = 0;
				return null;
			}
			
			var methodMappings = codeMappings[typeName];
			foreach (var maping in methodMappings) {
				var map = maping.MemberCodeMappings.Find(m => m.SourceCodeLine == lineNumber);
				if (map != null) {
					metadataToken = maping.MetadataToken;
					return map;
				}
			}
			
			metadataToken = 0;
			return null;
		}
		
		/// <summary>
		/// Gets the source code and type name from metadata token and offset.
		/// </summary>
		/// <param name="codeMappings">Code mappings storage.</param>
		/// <param name="token">Metadata token.</param>
		/// <param name="ilOffset">IL offset.</param>
		/// <param name="typeName">Type definition.</param>
		/// <param name="line">Line number.</param>
		public static bool GetSourceCodeFromMetadataTokenAndOffset(
			this ConcurrentDictionary<string, List<MemberMapping>> codeMappings,
			uint token,
			int ilOffset,
			out TypeDefinition type,
			out int line)
		{
			type = null;
			line = 0;
			
			foreach (var typename in codeMappings.Keys) {
				var mapping = codeMappings[typename].Find(m => m.MetadataToken == token);
				if (mapping == null)
					continue;
				var codeMapping = mapping.MemberCodeMappings.Find(
					cm => cm.ILInstructionOffset.From <= ilOffset && ilOffset <= cm.ILInstructionOffset.To - 1);
				if (codeMapping == null) {
					codeMapping = mapping.MemberCodeMappings.Find(cm => (cm.ILInstructionOffset.From >= ilOffset));
					if (codeMapping == null) {
						codeMapping = mapping.MemberCodeMappings.LastOrDefault();
						if (codeMapping == null)
							continue;
					}
				}
				
				type = mapping.Type;
				line = codeMapping.SourceCodeLine;
				return true;
			}
			
			return false;
		}
	}
}
