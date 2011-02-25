// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

using Decompiler;
using ICSharpCode.Decompiler.Disassembler;
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
		public int SourceCodeLine { get; set; }
		
		public ILRange ILInstructionOffset { get; set; }
	}
	
	/// <summary>
	/// Stores the method information and its source code mappings.
	/// </summary>
	public sealed class MethodMapping
	{
		public string TypeName { get; set; }
		
		public uint MetadataToken { get; set; }
		
		public List<SourceCodeMapping> MethodCodeMappings { get; set; }
		
		public int[] ToArray()
		{
			int[] result = new int[MethodCodeMappings.Count + 1];
			int i = 0;
			foreach (var element in MethodCodeMappings) {
				result[i] = MethodCodeMappings[i].ILInstructionOffset.From;	
				++i;
			}
			
			result[MethodCodeMappings.Count] = MethodCodeMappings[MethodCodeMappings.Count - 1].ILInstructionOffset.To;
			
			return result;
		}
	}
	
	public static class CodeMappings
	{
		public static ConcurrentDictionary<string, List<MethodMapping>> GetStorage(DecompiledLanguages language)
		{
			ConcurrentDictionary<string, List<MethodMapping>> storage = null;
			
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
		public static MethodMapping CreateCodeMapping(
			this MethodDefinition method,
			ConcurrentDictionary<string, List<MethodMapping>> sourceCodeMappings)
		{
			// create IL code mappings - used in debugger
			MethodMapping currentMethodMapping = null;
			if (sourceCodeMappings.ContainsKey(method.DeclaringType.FullName)) {
				var mapping = sourceCodeMappings[method.DeclaringType.FullName];
				if (mapping.Find(map => (int)map.MetadataToken == method.MetadataToken.ToInt32()) == null) {
					currentMethodMapping = new MethodMapping() {
						MetadataToken = (uint)method.MetadataToken.ToInt32(),
						TypeName = method.DeclaringType.FullName,
						MethodCodeMappings = new List<SourceCodeMapping>()
					};
					mapping.Add(currentMethodMapping);
				}
			}
			
			return currentMethodMapping;
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
			this ConcurrentDictionary<string, List<MethodMapping>> codeMappings,
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
				var map = maping.MethodCodeMappings.Find(m => m.SourceCodeLine == lineNumber);
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
		/// <param name="typeName">Type name.</param>
		/// <param name="line">Line number.</param>
		public static void GetSourceCodeFromMetadataTokenAndOffset(
			this ConcurrentDictionary<string, List<MethodMapping>> codeMappings,
			uint token,
			int ilOffset,
			out string typeName,
			out int line)
		{
			typeName = null;
			line = 0;
			
			foreach (var typename in codeMappings.Keys) {
				var mapping = codeMappings[typename].Find(m => m.MetadataToken == token);
				if (mapping == null)
					continue;
				
				var codeMapping = mapping.MethodCodeMappings.Find(
					cm => (cm.ILInstructionOffset.From <= ilOffset && ilOffset <= cm.ILInstructionOffset.To - 1) || // for CSharp
						  (cm.ILInstructionOffset.From == ilOffset && ilOffset == cm.ILInstructionOffset.To)); // for IL
				if (codeMapping == null)
					continue;
				
				typeName = typename;
				line = codeMapping.SourceCodeLine;
				break;
			}
		}
	}
}
