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
using System.Collections.Generic;
using System.Linq;
using dnlib.DotNet;

namespace dnSpy.Decompiler.Shared {
	/// <summary>
	/// Maps the source code to IL.
	/// </summary>
	public sealed class SourceCodeMapping : IEquatable<SourceCodeMapping> {
		public SourceCodeMapping(ILRange ilRange, TextPosition start, TextPosition end, MemberMapping mapping) {
			this.ilRange = ilRange;
			this.startPos = start;
			this.endPos = end;
			this.memberMapping = mapping;
		}

		/// <summary>
		/// Gets or sets the start location of the instruction.
		/// </summary>
		public TextPosition StartPosition {
			get { return startPos; }
		}
		readonly TextPosition startPos;

		/// <summary>
		/// Gets or sets the end location of the instruction.
		/// </summary>
		public TextPosition EndPosition {
			get { return endPos; }
		}
		readonly TextPosition endPos;

		/// <summary>
		/// Gets or sets IL Range offset for the source code line. E.g.: 13-19 &lt;-&gt; 135.
		/// </summary>
		public ILRange ILRange {
			get { return ilRange; }
		}
		readonly ILRange ilRange;

		/// <summary>
		/// Gets or sets the member mapping this source code mapping belongs to.
		/// </summary>
		public MemberMapping Mapping {
			get { return memberMapping; }
		}
		readonly MemberMapping memberMapping;

		/// <summary>
		/// Retrieves the array that contains the IL range and the missing gaps between ranges.
		/// </summary>
		/// <returns>The array representation of the step aranges.</returns>
		public uint[] ToArray(bool isMatch) {
			var currentList = new List<ILRange>();

			// add list for the current source code line
			currentList.Add(ILRange);

			return Mapping.ToArray(currentList, isMatch);
		}

		public bool Equals(SourceCodeMapping other) {
			return other != null &&
				ReferenceEquals(Mapping, other.Mapping) &&
				ILRange.Equals(other.ILRange) &&
				StartPosition.Equals(other.StartPosition) &&
				EndPosition.Equals(other.EndPosition);
		}

		public override bool Equals(object obj) {
			return Equals(obj as SourceCodeMapping);
		}

		public override int GetHashCode() {
			return Mapping.GetHashCode() ^ ILRange.GetHashCode() ^ StartPosition.GetHashCode() ^ EndPosition.GetHashCode();
		}

		public override string ToString() {
			return string.Format("{0} {1},{2} - {3},{4}",
				ILRange,
				StartPosition.Line, StartPosition.Column,
				EndPosition.Line, EndPosition.Column
				);
		}
	}

	/// <summary>
	/// Stores the member information and its source code mappings.
	/// </summary>
	public sealed class MemberMapping {
		public MemberMapping(MethodDef method, IEnumerable<IILVariable> localVariables = null) {
			this.memberCodeMappings = new List<SourceCodeMapping>();
			this.method = method;
			this.codeSize = method.Body.GetCodeSize();
			this.localVariables = localVariables;
		}

		/// <summary>
		/// Gets or sets the type of the mapping.
		/// </summary>
		public MethodDef Method {
			get { return method; }
		}
		readonly MethodDef method;

		/// <summary>
		/// Gets or sets the code size for the member mapping.
		/// </summary>
		public int CodeSize {
			get { return codeSize; }
		}
		readonly int codeSize;

		/// <summary>
		/// Gets or sets the source code mappings.
		/// </summary>
		public List<SourceCodeMapping> MemberCodeMappings {
			get { return memberCodeMappings; }
		}
		readonly List<SourceCodeMapping> memberCodeMappings;

		/// <summary>
		/// Gets the local variables or null if none
		/// </summary>
		public IEnumerable<IILVariable> LocalVariables {
			get { return localVariables; }
		}
		readonly IEnumerable<IILVariable> localVariables;

		/// <summary>
		/// Gets the inverted IL Ranges.<br/>
		/// E.g.: for (0-9, 11-14, 14-18, 21-25) => (9-11,18-21).
		/// </summary>
		/// <returns>IL Range inverted list.</returns>
		IEnumerable<ILRange> InvertedList {
			get {
				if (invertedList == null) {
					var list = MemberCodeMappings.ConvertAll<ILRange>(s => s.ILRange);
					invertedList = ILRange.OrderAndJoin(ILRange.Invert(list, CodeSize));
				}
				return invertedList;
			}
		}
		IEnumerable<ILRange> invertedList;

		public uint[] ToArray(List<ILRange> currentList, bool isMatch) {
			if (currentList == null)
				currentList = new List<ILRange>();

			// add inverted
			currentList.AddRange(InvertedList);

			if (isMatch) {
				// if the current list contains the last mapping, add also the last gap
				var lastInverted = InvertedList.LastOrDefault();
				if (!lastInverted.IsDefault && lastInverted.From == currentList[currentList.Count - 1].To)
					currentList.Add(lastInverted);
			}

			// set the output
			var resultList = new List<uint>();
			foreach (var element in ILRange.OrderAndJoinList(currentList)) {
				resultList.Add(element.From);
				resultList.Add(element.To);
			}

			return resultList.ToArray();
		}
	}

	/// <summary>
	/// Code mappings helper class.
	/// </summary>
	public static class CodeMappings {
		/// <summary>
		/// Gets source code mapping and metadata token based on type name and line number.
		/// </summary>
		/// <param name="codeMappings">Code mappings storage.</param>
		/// <param name="typeName">Member reference name.</param>
		/// <param name="lineNumber">Line number.</param>
		/// <param name="columnNumber">Column number or 0 for any column.</param>
		/// <returns></returns>
		public static SourceCodeMapping GetInstructionByLineNumber(
			this MemberMapping codeMapping,
			int lineNumber,
			int columnNumber) {
			if (codeMapping == null)
				throw new ArgumentException("CodeMappings storage must be valid!");

			if (columnNumber != 0) {
				var loc = new TextPosition(lineNumber, columnNumber);
				foreach (var m in codeMapping.MemberCodeMappings.OrderBy(a => a.ILRange.From)) {
					if (m.StartPosition <= loc && loc <= m.EndPosition)
						return m;
				}
				var list = new List<SourceCodeMapping>(codeMapping.MemberCodeMappings.FindAll(a => a.StartPosition.Line <= lineNumber && lineNumber <= a.EndPosition.Line));
				list.Sort((a, b) => {
					var d = GetDist(a.StartPosition, lineNumber, columnNumber).CompareTo(GetDist(b.StartPosition, lineNumber, columnNumber));
					if (d != 0)
						return d;
					return a.ILRange.From.CompareTo(b.ILRange.From);
				});
				if (list.Count > 0)
					return list[0];
				return null;
			}
			else {
				SourceCodeMapping map = null;
				foreach (var m in codeMapping.MemberCodeMappings) {
					if (lineNumber < m.StartPosition.Line || lineNumber > m.EndPosition.Line)
						continue;
					if (map == null || m.ILRange.From < map.ILRange.From)
						map = m;
				}
				return map;
			}
		}

		static int GetDist(TextPosition loc, int line, int column) {
			int hi = Math.Min(Math.Abs(loc.Line - line), short.MaxValue);
			int lo = Math.Min(Math.Abs(loc.Column - column), short.MaxValue);
			return (hi << 16) | lo;
		}

		/// <summary>
		/// Gets a mapping given a type, a token and an IL offset.
		/// </summary>
		/// <param name="codeMappings">Code mappings storage.</param>
		/// <param name="ilOffset">IL offset.</param>
		/// <param name="isMatch">True, if perfect match.</param>
		/// <returns>A code mapping.</returns>
		public static SourceCodeMapping GetInstructionByOffset(
			this MemberMapping codeMapping,
			uint ilOffset,
			out bool isMatch) {
			isMatch = false;

			if (codeMapping == null)
				throw new ArgumentNullException("CodeMappings storage must be valid!");

			// try find an exact match
			var map = codeMapping.MemberCodeMappings.Find(m => m.ILRange.From <= ilOffset && ilOffset < m.ILRange.To);
			isMatch = map != null;
			if (map == null) {
				// get the immediate next one
				map = codeMapping.MemberCodeMappings.Find(m => m.ILRange.From > ilOffset);
			}

			return map;
		}

		/// <summary>
		/// Gets the source code and type name from metadata token and offset.
		/// </summary>
		/// <param name="mapping">Code mapping storage.</param>
		/// <param name="ilOffset">IL offset.</param>
		/// <param name="location">Start location</param>
		/// <param name="endLocation">End location</param>
		public static bool GetInstructionByTokenAndOffset(
			this MemberMapping mapping,
			uint ilOffset,
			out TextPosition location,
			out TextPosition endLocation) {
			MethodDef methodDef;
			return mapping.GetInstructionByTokenAndOffset(ilOffset, out methodDef, out location, out endLocation);
		}

		/// <summary>
		/// Gets the source code and type name from metadata token and offset.
		/// </summary>
		/// <param name="mapping">Code mapping storage.</param>
		/// <param name="ilOffset">IL offset.</param>
		/// <param name="methodDef">Method definition.</param>
		/// <param name="location">Start location</param>
		/// <param name="endLocation">End location</param>
		public static bool GetInstructionByTokenAndOffset(
			this MemberMapping mapping,
			uint ilOffset,
			out MethodDef methodDef,
			out TextPosition location,
			out TextPosition endLocation) {
			methodDef = null;

			if (mapping == null)
				throw new ArgumentException("CodeMappings storage must be valid!");

			var codeMapping = mapping.MemberCodeMappings.Find(
				cm => cm.ILRange.From <= ilOffset && ilOffset <= cm.ILRange.To - 1);
			if (codeMapping == null) {
				codeMapping = mapping.MemberCodeMappings.Find(cm => cm.ILRange.From > ilOffset);
				if (codeMapping == null) {
					location = new TextPosition();
					endLocation = new TextPosition();
					return false;
				}
			}

			methodDef = mapping.Method;
			location = codeMapping.StartPosition;
			endLocation = codeMapping.EndPosition;
			return true;
		}
	}
}
