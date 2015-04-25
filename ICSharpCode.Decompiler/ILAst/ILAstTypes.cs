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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.Disassembler;
using ICSharpCode.NRefactory.Utils;
using ICSharpCode.NRefactory;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using Mono.CSharp;

namespace ICSharpCode.Decompiler.ILAst
{
	public abstract class ILNode
	{
		public readonly List<ILRange> ILRanges = new List<ILRange>(1);

		public virtual List<ILRange> EndILRanges {
			get { return ILRanges; }
		}
		public virtual IEnumerable<ILRange> AllILRanges {
			get { return ILRanges; }
		}

		public bool HasEndILRanges {
			get { return ILRanges != EndILRanges; }
		}

		public bool WritesNewLine {
			get { return !(this is ILLabel || this is ILExpression || this is ILSwitch.CaseBlock); }
		}

		public virtual bool SafeToAddToEndILRanges {
			get { return false; }
		}

		public IEnumerable<ILRange> GetSelfAndChildrenRecursiveILRanges()
		{
			return GetSelfAndChildrenRecursive<ILNode>().SelectMany(e => e.AllILRanges);
		}

		public List<T> GetSelfAndChildrenRecursive<T>(Func<T, bool> predicate = null) where T: ILNode
		{
			List<T> result = new List<T>(16);
			AccumulateSelfAndChildrenRecursive(result, predicate);
			return result;
		}
		
		void AccumulateSelfAndChildrenRecursive<T>(List<T> list, Func<T, bool> predicate) where T:ILNode
		{
			// Note: RemoveEndFinally depends on self coming before children
			T thisAsT = this as T;
			if (thisAsT != null && (predicate == null || predicate(thisAsT)))
				list.Add(thisAsT);
			foreach (ILNode node in this.GetChildren()) {
				if (node != null)
					node.AccumulateSelfAndChildrenRecursive(list, predicate);
			}
		}
		
		public virtual IEnumerable<ILNode> GetChildren()
		{
			yield break;
		}
		
		public override string ToString()
		{
			StringWriter w = new StringWriter();
			WriteTo(new PlainTextOutput(w), null);
			return w.ToString().Replace("\r\n", "; ");
		}
		
		public abstract void WriteTo(ITextOutput output, MemberMapping memberMapping);

		protected void UpdateMemberMapping(MemberMapping memberMapping, TextLocation startLoc, TextLocation endLoc, IEnumerable<ILRange> ranges)
		{
			if (memberMapping == null)
				return;
			foreach (var range in ILRange.OrderAndJoint(ranges)) {
				memberMapping.MemberCodeMappings.Add(new SourceCodeMapping {
					StartLocation = startLoc,
					EndLocation = endLoc,
					ILInstructionOffset = range,
					MemberMapping = memberMapping
				});
			}
		}

		protected void WriteHiddenStart(ITextOutput output, MemberMapping memberMapping, IEnumerable<ILRange> extraIlRanges = null)
		{
			var location = output.Location;
			output.WriteLeftBrace();
			var ilr = new List<ILRange>(ILRanges);
			if (extraIlRanges != null)
				ilr.AddRange(extraIlRanges);
			UpdateMemberMapping(memberMapping, location, output.Location, ilr);
			output.WriteLine();
			output.Indent();
		}

		protected void WriteHiddenEnd(ITextOutput output, MemberMapping memberMapping)
		{
			output.Unindent();
			var location = output.Location;
			output.WriteRightBrace();
			UpdateMemberMapping(memberMapping, location, output.Location, EndILRanges);
			output.WriteLine();
		}
	}
	
	public abstract class ILBlockBase: ILNode
	{
		public List<ILNode> Body;
		public List<ILRange> endILRanges = new List<ILRange>(1);

		public override List<ILRange> EndILRanges {
			get { return endILRanges; }
		}
		public override IEnumerable<ILRange> AllILRanges {
			get {
				foreach (var ilr in ILRanges)
					yield return ilr;
				foreach (var ilr in endILRanges)
					yield return ilr;
			}
		}

		public override bool SafeToAddToEndILRanges {
			get { return true; }
		}

		public ILBlockBase()
		{
			this.Body = new List<ILNode>();
		}

		public ILBlockBase(params ILNode[] body)
		{
			this.Body = new List<ILNode>(body);
		}

		public ILBlockBase(List<ILNode> body)
		{
			this.Body = body;
		}

		public override IEnumerable<ILNode> GetChildren()
		{
			return this.Body;
		}
		
		public override void WriteTo(ITextOutput output, MemberMapping memberMapping)
		{
			WriteTo(output, memberMapping, null);
		}

		internal void WriteTo(ITextOutput output, MemberMapping memberMapping, IEnumerable<ILRange> ilRanges)
		{
			WriteHiddenStart(output, memberMapping, ilRanges);
			foreach(ILNode child in this.GetChildren()) {
				child.WriteTo(output, memberMapping);
				if (!child.WritesNewLine)
					output.WriteLine();
			}
			WriteHiddenEnd(output, memberMapping);
		}
	}
	
	public class ILBlock: ILBlockBase
	{
		public ILExpression EntryGoto;
		
		public ILBlock(params ILNode[] body) : base(body)
		{
		}
		
		public ILBlock(List<ILNode> body) : base(body)
		{
		}
		
		public override IEnumerable<ILNode> GetChildren()
		{
			if (this.EntryGoto != null)
				yield return this.EntryGoto;
			foreach(ILNode child in this.Body) {
				yield return child;
			}
		}
	}
	
	public class ILBasicBlock: ILBlockBase
	{
		// Body has to start with a label and end with unconditional control flow
	}
	
	public class ILLabel: ILNode
	{
		public string Name;

		public override bool SafeToAddToEndILRanges {
			get { return true; }
		}

		public override void WriteTo(ITextOutput output, MemberMapping memberMapping)
		{
			var location = output.Location;
			output.WriteDefinition(Name, this, TextTokenType.Label);
			output.Write(':', TextTokenType.Operator);
			UpdateMemberMapping(memberMapping, location, output.Location, ILRanges);
		}
	}
	
	public class ILTryCatchBlock: ILNode
	{
		public class CatchBlock: ILBlock
		{
			public bool IsFilter;
			public TypeSig ExceptionType;
			public ILVariable ExceptionVariable;
			public List<ILRange> StlocILRanges = new List<ILRange>(1);

			public override IEnumerable<ILRange> AllILRanges {
				get {
					foreach (var ilr in base.AllILRanges)
						yield return ilr;
					foreach (var ilr in StlocILRanges)
						yield return ilr;
				}
			}

			public CatchBlock()
			{
			}

			public CatchBlock(List<ILNode> body)
			{
				this.Body = body;
				if (body.Count > 0 && body[0].Match(ILCode.Pop))
					StlocILRanges.AddRange(body[0].GetSelfAndChildrenRecursiveILRanges());
			}
			
			public override void WriteTo(ITextOutput output, MemberMapping memberMapping)
			{
				var startLoc = output.Location;
				if (IsFilter) {
					output.Write("filter", TextTokenType.Keyword);
					output.WriteSpace();
					output.WriteReference(ExceptionVariable.Name, ExceptionVariable, TextTokenType.Local);
				}
				else if (ExceptionType != null) {
					output.Write("catch", TextTokenType.Keyword);
					output.WriteSpace();
					output.WriteReference(ExceptionType.FullName, ExceptionType, TextTokenHelper.GetTextTokenType(ExceptionType));
					if (ExceptionVariable != null) {
						output.WriteSpace();
						output.WriteReference(ExceptionVariable.Name, ExceptionVariable, TextTokenType.Local);
					}
				}
				else {
					output.Write("handler", TextTokenType.Keyword);
					output.WriteSpace();
					output.WriteReference(ExceptionVariable.Name, ExceptionVariable, TextTokenType.Local);
				}
				UpdateMemberMapping(memberMapping, startLoc, output.Location, StlocILRanges);
				output.WriteSpace();
				base.WriteTo(output, memberMapping);
			}
		}
		public class FilterILBlock: CatchBlock
		{
			public FilterILBlock()
			{
				IsFilter = true;
			}

			public CatchBlock HandlerBlock;
			
			public override void WriteTo(ITextOutput output, MemberMapping memberMapping)
			{
				base.WriteTo(output, memberMapping);
				HandlerBlock.WriteTo(output, memberMapping);
			}
		}
		
		public ILBlock          TryBlock;
		public List<CatchBlock> CatchBlocks;
		public ILBlock          FinallyBlock;
		public ILBlock          FaultBlock;
		public FilterILBlock    FilterBlock;
		
		public override IEnumerable<ILNode> GetChildren()
		{
			if (this.TryBlock != null)
				yield return this.TryBlock;
			foreach (var catchBlock in this.CatchBlocks) {
				yield return catchBlock;
			}
			if (this.FaultBlock != null)
				yield return this.FaultBlock;
			if (this.FinallyBlock != null)
				yield return this.FinallyBlock;
			if (this.FilterBlock != null) {
				yield return this.FilterBlock;
				yield return this.FilterBlock.HandlerBlock;
			}
		}
		
		public override void WriteTo(ITextOutput output, MemberMapping memberMapping)
		{
			output.Write(".try", TextTokenType.Keyword);
			output.WriteSpace();
			TryBlock.WriteTo(output, memberMapping, ILRanges);
			foreach (CatchBlock block in CatchBlocks) {
				block.WriteTo(output, memberMapping);
			}
			if (FaultBlock != null) {
				output.Write("fault", TextTokenType.Keyword);
				output.WriteSpace();
				FaultBlock.WriteTo(output, memberMapping);
			}
			if (FinallyBlock != null) {
				output.Write("finally", TextTokenType.Keyword);
				output.WriteSpace();
				FinallyBlock.WriteTo(output, memberMapping);
			}
			if (FilterBlock != null) {
				output.Write("filter", TextTokenType.Keyword);
				output.WriteSpace();
				FilterBlock.WriteTo(output, memberMapping);
			}
		}
	}
	
	public class ILVariable
	{
		public string Name;
		public bool   IsGenerated;
		public TypeSig Type;
		public Local OriginalVariable;
		public dnlib.DotNet.Parameter OriginalParameter;
		
		public bool IsPinned {
			get { return OriginalVariable != null && OriginalVariable.Type is PinnedSig; }
		}
		
		public bool IsParameter {
			get { return OriginalParameter != null; }
		}
		
		public override string ToString()
		{
			return Name;
		}
	}
	
	public struct ILRange : IEquatable<ILRange>
	{
		readonly uint from, to;
		public uint From {
			get { return from; }
		}
		public uint To {	// Exlusive
			get { return to; }
		}

		public static bool operator ==(ILRange a, ILRange b)
		{
			return a.Equals(b);
		}

		public static bool operator !=(ILRange a, ILRange b)
		{
			return !a.Equals(b);
		}

		public bool IsDefault {
			get { return from == 0 && to == 0; }
		}

		public ILRange(uint from, uint to)
		{
			this.from = from;
			this.to = to;
		}

		public bool Equals(ILRange other)
		{
			return from == other.from && to == other.to;
		}

		public override bool Equals(object obj)
		{
			if (!(obj is ILRange))
				return false;
			return Equals((ILRange)obj);
		}

		public override int GetHashCode()
		{
			return (int)(((from << 16) | from >> 32) | to);
		}
		
		public override string ToString()
		{
			return string.Format("{0}-{1}", from.ToString("X"), to.ToString("X"));
		}
		
		public static List<ILRange> OrderAndJoin(IEnumerable<ILRange> input)
		{
			if (input == null)
				throw new ArgumentNullException("Input is null!");
			
			List<ILRange> ranges = ranges = input.ToList();
			ranges.Sort((a, b) => a.from.CompareTo(b.from));
			for (int i = 0; i < ranges.Count - 1; ) {
				ILRange curr = ranges[i];
				ILRange next = ranges[i + 1];
				// Merge consequtive ranges if they intersect
				if (curr.from <= next.from && next.from <= curr.to) {
					ranges[i] = new ILRange(curr.from, Math.Max(curr.to, next.to));
					ranges.RemoveAt(i + 1);
				} else {
					i++;
				}
				result.Add(curr);
			}
			return result;
		}
		
		public static List<ILRange> Invert(IEnumerable<ILRange> input, int codeSize)
		{
			if (input == null)
				throw new ArgumentNullException("Input is null!");
			
			if (codeSize <= 0)
				throw new ArgumentException("Code size must be grater than 0");
			
			List<ILRange> ordered = OrderAndJoin(input);
			List<ILRange> result = new List<ILRange>(ordered.Count + 1);
			if (ordered.Count == 0) {
				result.Add(new ILRange(0, (uint)codeSize));
			} else {
				// Gap before the first element
				if (ordered.First().From != 0)
					result.Add(new ILRange(0, ordered.First().From));
				
				// Gaps between elements
				for (int i = 0; i < ordered.Count - 1; i++)
					result.Add(new ILRange(ordered[i].To, ordered[i + 1].From));
				
				// Gap after the last element
				Debug.Assert(ordered.Last().To <= codeSize);
				if (ordered.Last().To != codeSize)
					result.Add(new ILRange(ordered.Last().To, (uint)codeSize));
			}
			return result;
		}
	}
	
	public class ILExpressionPrefix
	{
		public readonly ILCode Code;
		public readonly object Operand;
		
		public ILExpressionPrefix(ILCode code, object operand = null)
		{
			this.Code = code;
			this.Operand = operand;
		}
	}
	
	public class ILExpression : ILNode
	{
		public ILCode Code { get; set; }
		public object Operand { get; set; }
		public List<ILExpression> Arguments { get; set; }
		public ILExpressionPrefix[] Prefixes { get; set; }
		
		public TypeSig ExpectedType { get; set; }
		public TypeSig InferredType { get; set; }

		public override bool SafeToAddToEndILRanges {
			get { return true; }
		}
		
		public static readonly object AnyOperand = new object();
		
		public ILExpression(ILCode code, object operand, List<ILExpression> args)
		{
			if (operand is ILExpression)
				throw new ArgumentException("operand");
			
			this.Code = code;
			this.Operand = operand;
			this.Arguments = new List<ILExpression>(args);
		}
		
		public ILExpression(ILCode code, object operand, params ILExpression[] args)
		{
			if (operand is ILExpression)
				throw new ArgumentException("operand");
			
			this.Code = code;
			this.Operand = operand;
			this.Arguments = new List<ILExpression>(args);
		}
		
		public void AddPrefix(ILExpressionPrefix prefix)
		{
			ILExpressionPrefix[] arr = this.Prefixes;
			if (arr == null)
				arr = new ILExpressionPrefix[1];
			else
				Array.Resize(ref arr, arr.Length + 1);
			arr[arr.Length - 1] = prefix;
			this.Prefixes = arr;
		}
		
		public ILExpressionPrefix GetPrefix(ILCode code)
		{
			var prefixes = this.Prefixes;
			if (prefixes != null) {
				foreach (ILExpressionPrefix p in prefixes) {
					if (p.Code == code)
						return p;
				}
			}
			return null;
		}
		
		public override IEnumerable<ILNode> GetChildren()
		{
			return Arguments;
		}
		
		public bool IsBranch()
		{
			return this.Operand is ILLabel || this.Operand is ILLabel[];
		}
		
		public IEnumerable<ILLabel> GetBranchTargets()
		{
			if (this.Operand is ILLabel) {
				return new ILLabel[] { (ILLabel)this.Operand };
			} else if (this.Operand is ILLabel[]) {
				return (ILLabel[])this.Operand;
			} else {
				return new ILLabel[] { };
			}
		}
		
		public override void WriteTo(ITextOutput output, MemberMapping memberMapping)
		{
			var startLoc = output.Location;
			if (Operand is ILVariable && ((ILVariable)Operand).IsGenerated) {
				if (Code == ILCode.Stloc && this.InferredType == null) {
					output.WriteReference(((ILVariable)Operand).Name, Operand, ((ILVariable)Operand).IsParameter ? TextTokenType.Parameter : TextTokenType.Local);
					output.WriteSpace();
					output.Write('=', TextTokenType.Operator);
					output.WriteSpace();
					Arguments.First().WriteTo(output, null);
					UpdateMemberMapping(memberMapping, startLoc, output.Location, this.GetSelfAndChildrenRecursiveILRanges());
					return;
				} else if (Code == ILCode.Ldloc) {
					output.WriteReference(((ILVariable)Operand).Name, Operand, ((ILVariable)Operand).IsParameter ? TextTokenType.Parameter : TextTokenType.Local);
					if (this.InferredType != null) {
						output.Write(':', TextTokenType.Operator);
						this.InferredType.WriteTo(output, ILNameSyntax.ShortTypeName);
						if (this.ExpectedType != null && this.ExpectedType.FullName != this.InferredType.FullName) {
							output.Write('[', TextTokenType.Operator);
							output.Write("exp", TextTokenType.Keyword);
							output.Write(':', TextTokenType.Operator);
							this.ExpectedType.WriteTo(output, ILNameSyntax.ShortTypeName);
							output.Write(']', TextTokenType.Operator);
						}
					}
					UpdateMemberMapping(memberMapping, startLoc, output.Location, this.GetSelfAndChildrenRecursiveILRanges());
					return;
				}
			}
			
			if (this.Prefixes != null) {
				foreach (var prefix in this.Prefixes) {
					output.Write(prefix.Code.GetName() + ".", TextTokenType.OpCode);
					output.WriteSpace();
				}
			}
			
			output.Write(Code.GetName(), TextTokenType.OpCode);
			if (this.InferredType != null) {
				output.Write(':', TextTokenType.Operator);
				this.InferredType.WriteTo(output, ILNameSyntax.ShortTypeName);
				if (this.ExpectedType != null && this.ExpectedType.FullName != this.InferredType.FullName) {
					output.Write('[', TextTokenType.Operator);
					output.Write("exp", TextTokenType.Keyword);
					output.Write(':', TextTokenType.Operator);
					this.ExpectedType.WriteTo(output, ILNameSyntax.ShortTypeName);
					output.Write(']', TextTokenType.Operator);
				}
			} else if (this.ExpectedType != null) {
				output.Write('[', TextTokenType.Operator);
				output.Write("exp", TextTokenType.Keyword);
				output.Write(':', TextTokenType.Operator);
				this.ExpectedType.WriteTo(output, ILNameSyntax.ShortTypeName);
				output.Write(']', TextTokenType.Operator);
			}
			output.Write('(', TextTokenType.Operator);
			bool first = true;
			if (Operand != null) {
				if (Operand is ILLabel) {
					output.WriteReference(((ILLabel)Operand).Name, Operand, TextTokenType.Label);
				} else if (Operand is ILLabel[]) {
					ILLabel[] labels = (ILLabel[])Operand;
					for (int i = 0; i < labels.Length; i++) {
						if (i > 0) {
							output.Write(',', TextTokenType.Operator);
							output.WriteSpace();
						}
						output.WriteReference(labels[i].Name, labels[i], TextTokenType.Label);
					}
				} else if (Operand is IMethod && (Operand as IMethod).MethodSig != null) {
					IMethod method = (IMethod)Operand;
					if (method.DeclaringType != null) {
						method.DeclaringType.WriteTo(output, ILNameSyntax.ShortTypeName);
						output.Write("::", TextTokenType.Operator);
					}
					output.WriteReference(method.Name, method, TextTokenHelper.GetTextTokenType(method));
				} else if (Operand is IField) {
					IField field = (IField)Operand;
					field.DeclaringType.WriteTo(output, ILNameSyntax.ShortTypeName);
					output.Write("::", TextTokenType.Operator);
					output.WriteReference(field.Name, field, TextTokenHelper.GetTextTokenType(field));
				} else if (Operand is ILVariable) {
					var ilvar = (ILVariable)Operand;
					output.WriteReference(ilvar.Name, Operand, ilvar.IsParameter ? TextTokenType.Parameter : TextTokenType.Local);
				} else {
					DisassemblerHelpers.WriteOperand(output, Operand);
				}
				first = false;
			}
			foreach (ILExpression arg in this.Arguments) {
				if (!first) {
					output.Write(',', TextTokenType.Operator);
					output.WriteSpace();
				}
				arg.WriteTo(output, null);
				first = false;
			}
			output.Write(')', TextTokenType.Operator);
			UpdateMemberMapping(memberMapping, startLoc, output.Location, this.GetSelfAndChildrenRecursiveILRanges());
		}
	}
	
	public class ILWhileLoop : ILNode
	{
		public ILExpression Condition;
		public ILBlock      BodyBlock;
		
		public override IEnumerable<ILNode> GetChildren()
		{
			if (this.Condition != null)
				yield return this.Condition;
			if (this.BodyBlock != null)
				yield return this.BodyBlock;
		}
		
		public override void WriteTo(ITextOutput output, MemberMapping memberMapping)
		{
			var startLoc = output.Location;
			output.Write("loop", TextTokenType.Keyword);
			output.WriteSpace();
			output.Write('(', TextTokenType.Operator);
			if (this.Condition != null)
				this.Condition.WriteTo(output, null);
			output.Write(')', TextTokenType.Operator);
			var ilRanges = new List<ILRange>(ILRanges);
			if (this.Condition != null)
				ilRanges.AddRange(this.Condition.GetSelfAndChildrenRecursiveILRanges());
			UpdateMemberMapping(memberMapping, startLoc, output.Location, ilRanges);
			output.WriteSpace();
			this.BodyBlock.WriteTo(output, memberMapping);
		}
	}
	
	public class ILCondition : ILNode
	{
		public ILExpression Condition;
		public ILBlock TrueBlock;   // Branch was taken
		public ILBlock FalseBlock;  // Fall-though
		
		public override IEnumerable<ILNode> GetChildren()
		{
			if (this.Condition != null)
				yield return this.Condition;
			if (this.TrueBlock != null)
				yield return this.TrueBlock;
			if (this.FalseBlock != null)
				yield return this.FalseBlock;
		}
		
		public override void WriteTo(ITextOutput output, MemberMapping memberMapping)
		{
			var startLoc = output.Location;
			output.Write("if", TextTokenType.Keyword);
			output.WriteSpace();
			output.Write('(', TextTokenType.Operator);
			Condition.WriteTo(output, null);
			output.Write(')', TextTokenType.Operator);
			var ilRanges = new List<ILRange>(ILRanges);
			ilRanges.AddRange(Condition.GetSelfAndChildrenRecursiveILRanges());
			UpdateMemberMapping(memberMapping, startLoc, output.Location, ilRanges);
			output.WriteSpace();
			TrueBlock.WriteTo(output, memberMapping);
			if (FalseBlock != null) {
				output.Write("else", TextTokenType.Keyword);
				output.WriteSpace();
				FalseBlock.WriteTo(output, memberMapping);
			}
		}
	}
	
	public class ILSwitch: ILNode
	{
		public class CaseBlock: ILBlock
		{
			public List<int> Values;  // null for the default case
			
			public override void WriteTo(ITextOutput output, MemberMapping memberMapping)
			{
				if (this.Values != null) {
					foreach (int i in this.Values) {
						output.Write("case", TextTokenType.Keyword);
						output.WriteSpace();
						output.Write(string.Format("{0}", i), TextTokenType.Number);
						output.WriteLine(":", TextTokenType.Operator);
					}
				} else {
					output.Write("default", TextTokenType.Keyword);
					output.WriteLine(":", TextTokenType.Operator);
				}
				output.Indent();
				base.WriteTo(output, memberMapping);
				output.Unindent();
			}
		}
		
		public ILExpression Condition;
		public List<CaseBlock> CaseBlocks = new List<CaseBlock>();
		public List<ILRange> endILRanges = new List<ILRange>(1);

		public override List<ILRange> EndILRanges {
			get { return endILRanges; }
		}
		public override IEnumerable<ILRange> AllILRanges {
			get {
				foreach (var ilr in ILRanges)
					yield return ilr;
				foreach (var ilr in endILRanges)
					yield return ilr;
			}
		}

		public override bool SafeToAddToEndILRanges {
			get { return true; }
		}
		
		public override IEnumerable<ILNode> GetChildren()
		{
			if (this.Condition != null)
				yield return this.Condition;
			foreach (ILBlock caseBlock in this.CaseBlocks) {
				yield return caseBlock;
			}
		}
		
		public override void WriteTo(ITextOutput output, MemberMapping memberMapping)
		{
			var startLoc = output.Location;
			output.Write("switch", TextTokenType.Keyword);
			output.WriteSpace();
			output.Write('(', TextTokenType.Operator);
			Condition.WriteTo(output, null);
			output.Write(')', TextTokenType.Operator);
			var ilRanges = new List<ILRange>(ILRanges);
			ilRanges.AddRange(Condition.GetSelfAndChildrenRecursiveILRanges());
			UpdateMemberMapping(memberMapping, startLoc, output.Location, ilRanges);
			output.WriteSpace();
			WriteHiddenStart(output, memberMapping);
			foreach (CaseBlock caseBlock in this.CaseBlocks) {
				caseBlock.WriteTo(output, memberMapping);
			}
			WriteHiddenEnd(output, memberMapping);
		}
	}
	
	public class ILFixedStatement : ILNode
	{
		public List<ILExpression> Initializers = new List<ILExpression>();
		public ILBlock      BodyBlock;
		
		public override IEnumerable<ILNode> GetChildren()
		{
			foreach (ILExpression initializer in this.Initializers)
				yield return initializer;
			if (this.BodyBlock != null)
				yield return this.BodyBlock;
		}
		
		public override void WriteTo(ITextOutput output, MemberMapping memberMapping)
		{
			var startLoc = output.Location;
			output.Write("fixed", TextTokenType.Keyword);
			output.WriteSpace();
			output.Write('(', TextTokenType.Operator);
			for (int i = 0; i < this.Initializers.Count; i++) {
				if (i > 0) {
					output.Write(',', TextTokenType.Operator);
					output.WriteSpace();
				}
				this.Initializers[i].WriteTo(output, null);
			}
			output.Write(')', TextTokenType.Operator);
			var ilRanges = new List<ILRange>(ILRanges);
			foreach (var i in Initializers)
				ilRanges.AddRange(i.GetSelfAndChildrenRecursiveILRanges());
			UpdateMemberMapping(memberMapping, startLoc, output.Location, ilRanges);
			output.WriteSpace();
			this.BodyBlock.WriteTo(output, memberMapping);
		}
	}
}