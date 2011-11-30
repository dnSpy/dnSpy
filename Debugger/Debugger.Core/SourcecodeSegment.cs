// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

using Debugger.Interop.CorDebug;
using Debugger.Interop.CorSym;

namespace Debugger
{
	public class SourcecodeSegment: DebuggerObject
	{
		Module module;
		
		string filename;
		string typename;
		byte[] checkSum;
		int startLine;
		int startColumn;
		int endLine;
		int endColumn;
		
		ICorDebugFunction corFunction;
		int ilStart;
		int ilEnd;
		int[] stepRanges;
		
		public Module Module {
			get { return module; }
		}
		
		public string Filename {
			get { return filename; }
		}
		
		public string Typename {
			get { return typename; }
		}
		
		public byte[] CheckSum {
			get { return checkSum; }
		}
		
		public int StartLine {
			get { return startLine; }
		}
		
		public int StartColumn {
			get { return startColumn; }
		}
		
		public int EndLine {
			get { return endLine; }
		}
		
		public int EndColumn {
			get { return endColumn; }
		}
		
		internal ICorDebugFunction CorFunction {
			get { return corFunction; }
		}
		
		public int ILStart {
			get { return ilStart; }
		}
		
		public int ILEnd {
			get { return ilEnd; }
		}
		
		public int[] StepRanges {
			get { return stepRanges; }
		}
		
		private SourcecodeSegment()
		{
		}
		
		/// <summary>
		/// Use the module path to figure out where to look for the source file
		/// </summary>
		static IEnumerable<string> RelocateSymURL(Module module, string symUrl)
		{
			string modulePath = module.Process.WorkingDirectory;
			if (module.IsInMemory || module.IsDynamic) {
				// Just use any module with symboles
				foreach(Module m in module.Process.Modules) {
					if (m.HasSymbols) {
						if (!string.IsNullOrEmpty(m.FullPath)) {
							modulePath = Path.GetDirectoryName(m.FullPath);
							break;
						}
					}
				}
			} else {
				if (!string.IsNullOrEmpty(module.FullPath))
					modulePath = Path.GetDirectoryName(module.FullPath);
			}
			if (string.IsNullOrEmpty(modulePath)) {
				yield return symUrl;
				yield break;
			}
			
			if (Path.IsPathRooted(symUrl)) {
				Dictionary<string, object> returned = new Dictionary<string, object>();
				
				// Try without relocating
				returned.Add(symUrl, null);
				yield return symUrl;
				
				// The two paths to combine
				string[] moduleDirs = modulePath.Split('\\');
				string[] urlDirs = symUrl.Split('\\');
				
				// Join the paths at some point (joining directry must match)
				for (int i = 0; i < moduleDirs.Length; i++) {
					for (int j = 0; j < urlDirs.Length; j++) {
						if (!string.IsNullOrEmpty(moduleDirs[i]) &&
						    !string.IsNullOrEmpty(urlDirs[j])    &&
						    string.Equals(moduleDirs[i], urlDirs[j], StringComparison.OrdinalIgnoreCase))
						{
							// Join the paths
							string[] joinedDirs = new string[i + (urlDirs.Length - j)];
							Array.Copy(moduleDirs, joinedDirs, i);
							Array.Copy(urlDirs, j, joinedDirs, i, urlDirs.Length - j);
							string joined = string.Join(@"\", joinedDirs);
							
							// Return joined path
							if (!returned.ContainsKey(joined)) {
								returned.Add(joined, null);
								yield return joined;
							}
						}
					}
				}
			} else {
				if (symUrl.StartsWith(@".\")) symUrl = symUrl.Substring(2);
				if (symUrl.StartsWith(@"\"))  symUrl = symUrl.Substring(1);
				// Try 0, 1 and 2 levels above the module directory
				string dir = modulePath;
				if (!string.IsNullOrEmpty(dir)) yield return Path.Combine(dir, symUrl);
				dir = Path.GetDirectoryName(dir);
				if (!string.IsNullOrEmpty(dir)) yield return Path.Combine(dir, symUrl);
				dir = Path.GetDirectoryName(dir);
				if (!string.IsNullOrEmpty(dir)) yield return Path.Combine(dir, symUrl);
			}
		}
		
		static ISymUnmanagedDocument GetSymDocumentFromFilename(Module module, string filename, byte[] checksum)
		{
			if (filename == null) throw new ArgumentNullException("filename");
			
			if (Path.IsPathRooted(filename)) {
				foreach(ISymUnmanagedDocument symDoc in module.SymDocuments) {
					foreach (string url in RelocateSymURL(module, symDoc.GetURL())) {
						if (string.Equals(url, filename, StringComparison.OrdinalIgnoreCase))
							return symDoc;
					}
				}
			} else {
				foreach(ISymUnmanagedDocument symDoc in module.SymDocuments) {
					if (filename.StartsWith(@".\")) filename = filename.Substring(2);
					if (filename.StartsWith(@"\"))  filename = filename.Substring(1);
					if (symDoc.GetURL().ToLowerInvariant().EndsWith(@"\" + filename.ToLowerInvariant())) {
						return symDoc;
					}
				}
			}
			
			return null;
		}
		
		public static SourcecodeSegment Resolve(Module module, string fileName, byte[] checkSum, int line, int column)
		{
			// Do not use ISymUnmanagedReader.GetDocument!  It is broken if two files have the same name
			// Do not use ISymUnmanagedMethod.GetOffset!  It sometimes returns negative offset
			
			ISymUnmanagedReader symReader = module.SymReader;
			if (symReader == null) return null; // No symbols
			
			ISymUnmanagedDocument symDoc = GetSymDocumentFromFilename(module, fileName, checkSum);
			if (symDoc == null) return null; // Document not found
			
			ISymUnmanagedMethod symMethod;
			try {
				uint validLine = symDoc.FindClosestLine((uint)line);
				symMethod = symReader.GetMethodFromDocumentPosition(symDoc, (uint)validLine, (uint)column);
			} catch {
				return null; //Not found
			}
			
			SequencePoint[] seqPoints = symMethod.GetSequencePoints();
			Array.Sort(seqPoints);
			if (seqPoints.Length == 0) return null;
			if (line < seqPoints[0].Line) return null;
			foreach(SequencePoint sqPoint in seqPoints) {
				if (sqPoint.Line == 0xFEEFEE) continue;
				// If the desired breakpoint position is before the end of the sequence point
				if (line < sqPoint.EndLine || (line == sqPoint.EndLine && column < sqPoint.EndColumn)) {
					SourcecodeSegment segment = new SourcecodeSegment();
					segment.module        = module;
					segment.filename      = symDoc.GetURL();
					segment.checkSum      = symDoc.GetCheckSum();
					segment.startLine     = (int)sqPoint.Line;
					segment.startColumn   = (int)sqPoint.Column;
					segment.endLine       = (int)sqPoint.EndLine;
					segment.endColumn     = (int)sqPoint.EndColumn;
					segment.corFunction   = module.CorModule.GetFunctionFromToken(symMethod.GetToken());
					segment.ilStart = (int)sqPoint.Offset;
					segment.ilEnd   = (int)sqPoint.Offset;
					segment.stepRanges    = null;
					return segment;
				}
			}
			return null;
		}
		
		static string GetFilenameFromSymDocument(Module module, ISymUnmanagedDocument symDoc)
		{
			foreach (string filename in RelocateSymURL(module, symDoc.GetURL())) {
				if (File.Exists(filename))
					return filename;
			}
			return symDoc.GetURL();
		}
		
		/// <summary>
		/// 'ILStart &lt;= ILOffset &lt;= ILEnd' and this range includes at least
		/// the returned area of source code. (May incude some extra compiler generated IL too)
		/// </summary>
		internal static SourcecodeSegment Resolve(Module module, ICorDebugFunction corFunction, int offset)
		{
			ISymUnmanagedReader symReader = module.SymReader;
			if (symReader == null) return null; // No symbols
			
			ISymUnmanagedMethod symMethod;
			try {
				symMethod = symReader.GetMethod(corFunction.GetToken());
			} catch (COMException) {
				// Can not find the method
				// eg. Compiler generated constructors are not in symbol store
				return null;
			}
			if (symMethod == null) return null;
			
			uint sequencePointCount = symMethod.GetSequencePointCount();
			SequencePoint[] sequencePoints = symMethod.GetSequencePoints();
			
			// Get i for which: offsets[i] <= offset < offsets[i + 1]
			// or fallback to first element if  offset < offsets[0]
			for (int i = (int)sequencePointCount - 1; i >= 0; i--) { // backwards
				if ((int)sequencePoints[i].Offset <= offset || i == 0) {
					// Set inforamtion about current IL range
					int codeSize = (int)corFunction.GetILCode().GetSize();
					
					int ilStart = (int)sequencePoints[i].Offset;
					int ilEnd = (i + 1 < sequencePointCount) ? (int)sequencePoints[i+1].Offset : codeSize;
					
					// 0xFeeFee means "code generated by compiler"
					// If we are in generated sequence use to closest real one instead,
					// extend the ILStart and ILEnd to include the 'real' sequence
					
					// Look ahead for 'real' sequence
					while (i + 1 < sequencePointCount && sequencePoints[i].Line == 0xFeeFee) {
						i++;
						ilEnd = (i + 1 < sequencePointCount) ? (int)sequencePoints[i+1].Offset : codeSize;
					}
					// Look back for 'real' sequence
					while (i - 1 >= 0 && sequencePoints[i].Line == 0xFeeFee) {
						i--;
						ilStart = (int)sequencePoints[i].Offset;
					}
					// Wow, there are no 'real' sequences
					if (sequencePoints[i].Line == 0xFeeFee) {
						return null;
					}
					
					List<int> stepRanges = new List<int>();
					for (int j = 0; j < sequencePointCount; j++) {
						// Step over compiler generated sequences and current statement
						// 0xFeeFee means "code generated by compiler"
						if (sequencePoints[j].Line == 0xFeeFee || j == i) {
							// Add start offset or remove last end (to connect two ranges into one)
							if (stepRanges.Count > 0 && stepRanges[stepRanges.Count - 1] == sequencePoints[j].Offset) {
								stepRanges.RemoveAt(stepRanges.Count - 1);
							} else {
								stepRanges.Add((int)sequencePoints[j].Offset);
							}
							// Add end offset | handle last sequence point
							if (j + 1 < sequencePointCount) {
								stepRanges.Add((int)sequencePoints[j+1].Offset);
							} else {
								stepRanges.Add(codeSize);
							}
						}
					}
					
					SourcecodeSegment segment = new SourcecodeSegment();
					segment.module        = module;
					segment.filename      = GetFilenameFromSymDocument(module, sequencePoints[i].Document);
					segment.checkSum      = sequencePoints[i].Document.GetCheckSum();
					segment.startLine     = (int)sequencePoints[i].Line;
					segment.startColumn   = (int)sequencePoints[i].Column;
					segment.endLine       = (int)sequencePoints[i].EndLine;
					segment.endColumn     = (int)sequencePoints[i].EndColumn;
					segment.corFunction   = corFunction;
					segment.ilStart       = ilStart;
					segment.ilEnd         = ilEnd;
					segment.stepRanges    = stepRanges.ToArray();
					
					// VB.NET sometimes produces temporary files which it then deletes
					// (eg 17d14f5c-a337-4978-8281-53493378c1071.vb)
					string filename = Path.GetFileName(segment.filename);
					if (filename.Length == 40 && filename.EndsWith(".vb")) {
						bool guidName = true;
						foreach(char c in filename.Substring(0, filename.Length - 3)) {
							if (('0' <= c && c <= '9') ||
							    ('a' <= c && c <= 'f') ||
							    ('A' <= c && c <= 'F') ||
							    (c == '-'))
							{
								guidName = true;
							} else {
								guidName = false;
								break;
							}
						}
						if (guidName)
							return null;
					}
					
					return segment;
				}
			}
			return null;
		}
		
		public override string ToString()
		{
			return string.Format("{0}:{1},{2}-{3},{4}",
			                     Path.GetFileName(this.Filename ?? string.Empty),
			                     this.startLine, this.startColumn, this.endLine, this.endColumn);
		}
		
		#region ILSpy
		
		public static SourcecodeSegment CreateForIL(Module module, int line, int metadataToken, int iLOffset)
		{
			try {
				SourcecodeSegment segment = new SourcecodeSegment();
				segment.module        = module;
				segment.typename      = null;
				segment.checkSum      = null;
				segment.startLine     = line;
				segment.startColumn   = 0;
				segment.endLine       = line;
				segment.endColumn     = 0;
				segment.corFunction   = module.CorModule.GetFunctionFromToken((uint)metadataToken);
				segment.ilStart 	  = iLOffset;
				segment.ilEnd   	  = iLOffset;
				segment.stepRanges    = null;
				
				return segment;
			} catch {
				return null;
			}
		}
		
		public static SourcecodeSegment ResolveForIL(Module module, ICorDebugFunction corFunction, int line, int offset, int[] ranges)
		{
			if (ranges == null)
				return null; // this would lead to a catched exception and the same result

			try {				
				SourcecodeSegment segment = new SourcecodeSegment();
				segment.module        = module;
				segment.typename      = null;
				segment.checkSum      = null;
				segment.startLine     = line;
				segment.startColumn   = 0;
				segment.endLine       = line;
				segment.endColumn     = 0;
				segment.corFunction   = corFunction;
				segment.ilStart 	  = offset;
				segment.ilEnd   	  = ranges[1];
				segment.stepRanges    = ranges;
				
				return segment;
			} catch {
				return null;
			}
		}
		
		#endregion
	}
}
