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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnSpy.Decompiler.Shared;
using ICSharpCode.Decompiler.Disassembler;

namespace ICSharpCode.Decompiler.ILAst {
	public abstract class ILNode : IEnumerable<ILNode>
	{
		public readonly List<ILRange> ILRanges = new List<ILRange>(1);

		public virtual List<ILRange> EndILRanges {
			get { return ILRanges; }
		}
		public virtual ILRange GetAllILRanges(ref long index, ref bool done) {
			if (index < ILRanges.Count)
				return ILRanges[(int)index++];
			done = true;
			return default(ILRange);
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
			foreach (var node in GetSelfAndChildrenRecursive<ILNode>()) {
				long index = 0;
				bool done = false;
				for (;;) {
					var b = node.GetAllILRanges(ref index, ref done);
					if (done)
						break;
					yield return b;
				}
			}
		}

		public void AddSelfAndChildrenRecursiveILRanges(List<ILRange> coll)
		{
			foreach (var a in GetSelfAndChildrenRecursive<ILNode>()) {
				long index = 0;
				bool done = false;
				for (;;) {
					var b = a.GetAllILRanges(ref index, ref done);
					if (done)
						break;
					coll.Add(b);
				}
			}
		}

		public List<ILRange> GetSelfAndChildrenRecursiveILRanges_OrderAndJoin() {
			// The current callers save the list as an annotation so always create a new list here
			// instead of having them pass in a cached list.
			var list = new List<ILRange>();
			AddSelfAndChildrenRecursiveILRanges(list);
			return ILRange.OrderAndJoinList(list);
		}

		public List<T> GetSelfAndChildrenRecursive<T>(Func<T, bool> predicate = null) where T: ILNode
		{
			List<T> result = new List<T>(16);
			AccumulateSelfAndChildrenRecursive(result, predicate);
			return result;
		}

		public List<T> GetSelfAndChildrenRecursive<T>(List<T> result, Func<T, bool> predicate = null) where T: ILNode
		{
			result.Clear();
			AccumulateSelfAndChildrenRecursive(result, predicate);
			return result;
		}
		
		void AccumulateSelfAndChildrenRecursive<T>(List<T> list, Func<T, bool> predicate) where T:ILNode
		{
			// Note: RemoveEndFinally depends on self coming before children
			T thisAsT = this as T;
			if (thisAsT != null && (predicate == null || predicate(thisAsT)))
				list.Add(thisAsT);
			int index = 0;
			for (;;) {
				var node = GetNext(ref index);
				if (node == null)
					break;
				node.AccumulateSelfAndChildrenRecursive(list, predicate);
			}
		}

		internal virtual ILNode GetNext(ref int index)
		{
			return null;
		}
		
		public ILNode GetChildren()
		{
			return this;
		}

		public ILNode_Enumerator GetEnumerator()
		{
			return new ILNode_Enumerator(this);
		}

		IEnumerator<ILNode> IEnumerable<ILNode>.GetEnumerator()
		{
			return new ILNode_Enumerator(this);
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return new ILNode_Enumerator(this);
		}

		public struct ILNode_Enumerator : IEnumerator<ILNode>
		{
			readonly ILNode node;
			int index;
			ILNode current;

			internal ILNode_Enumerator(ILNode node)
			{
				this.node = node;
				this.index = 0;
				this.current = null;
			}

			public ILNode Current
			{
				get { return current; }
			}

			object IEnumerator.Current
			{
				get { return current; }
			}

			public void Dispose()
			{
			}

			public bool MoveNext()
			{
				return (this.current = this.node.GetNext(ref index)) != null;
			}

			public void Reset()
			{
				this.index = 0;
			}
		}

		public override string ToString()
		{
			StringWriter w = new StringWriter();
			WriteTo(new PlainTextOutput(w), null);
			return w.ToString().Replace("\r\n", "; ");
		}
		
		public abstract void WriteTo(ITextOutput output, MemberMapping memberMapping);

		protected void UpdateMemberMapping(MemberMapping memberMapping, TextPosition startLoc, TextPosition endLoc, IEnumerable<ILRange> ranges)
		{
			if (memberMapping == null)
				return;
			foreach (var range in ILRange.OrderAndJoin(ranges))
				memberMapping.MemberCodeMappings.Add(new SourceCodeMapping(range, startLoc, endLoc, memberMapping));
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
		public override ILRange GetAllILRanges(ref long index, ref bool done) {
			if (index < ILRanges.Count)
				return ILRanges[(int)index++];
			int i = (int)index - ILRanges.Count;
			if (i < endILRanges.Count) {
				index++;
				return endILRanges[i];
			}
			done = true;
			return default(ILRange);
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

		internal override ILNode GetNext(ref int index)
		{
			if (index < this.Body.Count)
				return this.Body[index++];
			return null;
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
		
		internal override ILNode GetNext(ref int index)
		{
			if (index == 0) {
				index = 1;
				if (this.EntryGoto != null)
					return this.EntryGoto;
			}
			if (index <= this.Body.Count)
				return this.Body[index++ - 1];

			return null;
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
			output.WriteDefinition(Name, this, TextTokenKind.Label);
			output.Write(":", TextTokenKind.Operator);
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

			public override ILRange GetAllILRanges(ref long index, ref bool done) {
				if (index < ILRanges.Count)
					return ILRanges[(int)index++];
				int i = (int)index - ILRanges.Count;
				if (i < StlocILRanges.Count) {
					index++;
					return StlocILRanges[i];
				}
				done = true;
				return default(ILRange);
			}

			public CatchBlock()
			{
			}

			public CatchBlock(bool calculateILRanges, List<ILNode> body)
			{
				this.Body = body;
				if (calculateILRanges && body.Count > 0 && body[0].Match(ILCode.Pop))
					body[0].AddSelfAndChildrenRecursiveILRanges(StlocILRanges);
			}
			
			public override void WriteTo(ITextOutput output, MemberMapping memberMapping)
			{
				var startLoc = output.Location;
				if (IsFilter) {
					output.Write("filter", TextTokenKind.Keyword);
					output.WriteSpace();
					output.WriteReference(ExceptionVariable.Name, ExceptionVariable, TextTokenKind.Local);
				}
				else if (ExceptionType != null) {
					output.Write("catch", TextTokenKind.Keyword);
					output.WriteSpace();
					output.WriteReference(ExceptionType.FullName, ExceptionType, TextTokenKindUtils.GetTextTokenType(ExceptionType));
					if (ExceptionVariable != null) {
						output.WriteSpace();
						output.WriteReference(ExceptionVariable.Name, ExceptionVariable, TextTokenKind.Local);
					}
				}
				else {
					output.Write("handler", TextTokenKind.Keyword);
					output.WriteSpace();
					output.WriteReference(ExceptionVariable.Name, ExceptionVariable, TextTokenKind.Local);
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
		
		internal override ILNode GetNext(ref int index)
		{
			if (index == 0) {
				index = 1;
				if (this.TryBlock != null)
					return this.TryBlock;
			}
			if (index <= this.CatchBlocks.Count)
				return this.CatchBlocks[index++ - 1];
			if (index == this.CatchBlocks.Count + 1) {
				index++;
				if (this.FaultBlock != null)
					return this.FaultBlock;
			}
			if (index == this.CatchBlocks.Count + 2) {
				index++;
				if (this.FinallyBlock != null)
					return this.FinallyBlock;
			}
			if (index == this.CatchBlocks.Count + 3) {
				index++;
				if (this.FilterBlock != null)
					return this.FilterBlock;
			}
			if (index == this.CatchBlocks.Count + 4) {
				index++;
				if (this.FilterBlock != null && this.FilterBlock.HandlerBlock != null)
					return this.FilterBlock.HandlerBlock;
			}
			return null;
		}

		public override void WriteTo(ITextOutput output, MemberMapping memberMapping)
		{
			output.Write(".try", TextTokenKind.Keyword);
			output.WriteSpace();
			TryBlock.WriteTo(output, memberMapping, ILRanges);
			foreach (CatchBlock block in CatchBlocks) {
				block.WriteTo(output, memberMapping);
			}
			if (FaultBlock != null) {
				output.Write("fault", TextTokenKind.Keyword);
				output.WriteSpace();
				FaultBlock.WriteTo(output, memberMapping);
			}
			if (FinallyBlock != null) {
				output.Write("finally", TextTokenKind.Keyword);
				output.WriteSpace();
				FinallyBlock.WriteTo(output, memberMapping);
			}
			if (FilterBlock != null) {
				output.Write("filter", TextTokenKind.Keyword);
				output.WriteSpace();
				FilterBlock.WriteTo(output, memberMapping);
			}
		}
	}
	
	public class ILVariable : IILVariable
	{
		public string Name { get; set; }
		public bool GeneratedByDecompiler { get; set; }
		public bool GeneratedByDecompilerButCanBeRenamed;
		public TypeSig Type;
		public Local OriginalVariable { get; set; }
		public Parameter OriginalParameter;
		
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

		public ILExpression(ILCode code, object operand)
		{
			if (operand is ILExpression)
				throw new ArgumentException("operand");
			
			this.Code = code;
			this.Operand = operand;
			this.Arguments = new List<ILExpression>();
		}

		public ILExpression(ILCode code, object operand, ILExpression arg1)
		{
			if (operand is ILExpression)
				throw new ArgumentException("operand");
			
			this.Code = code;
			this.Operand = operand;
			this.Arguments = new List<ILExpression>() { arg1 };
		}

		public ILExpression(ILCode code, object operand, ILExpression arg1, ILExpression arg2)
		{
			if (operand is ILExpression)
				throw new ArgumentException("operand");
			
			this.Code = code;
			this.Operand = operand;
			this.Arguments = new List<ILExpression>() { arg1, arg2 };
		}

		public ILExpression(ILCode code, object operand, ILExpression arg1, ILExpression arg2, ILExpression arg3)
		{
			if (operand is ILExpression)
				throw new ArgumentException("operand");
			
			this.Code = code;
			this.Operand = operand;
			this.Arguments = new List<ILExpression>() { arg1, arg2, arg3 };
		}

		public ILExpression(ILCode code, object operand, ILExpression[] args)
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
		
		internal override ILNode GetNext(ref int index)
		{
			if (index < Arguments.Count)
				return Arguments[index++];
			return null;
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
			if (Operand is ILVariable && ((ILVariable)Operand).GeneratedByDecompiler) {
				if (Code == ILCode.Stloc && this.InferredType == null) {
					output.WriteReference(((ILVariable)Operand).Name, Operand, ((ILVariable)Operand).IsParameter ? TextTokenKind.Parameter : TextTokenKind.Local);
					output.WriteSpace();
					output.Write("=", TextTokenKind.Operator);
					output.WriteSpace();
					Arguments.First().WriteTo(output, null);
					UpdateMemberMapping(memberMapping, startLoc, output.Location, this.GetSelfAndChildrenRecursiveILRanges());
					return;
				} else if (Code == ILCode.Ldloc) {
					output.WriteReference(((ILVariable)Operand).Name, Operand, ((ILVariable)Operand).IsParameter ? TextTokenKind.Parameter : TextTokenKind.Local);
					if (this.InferredType != null) {
						output.Write(":", TextTokenKind.Operator);
						this.InferredType.WriteTo(output, ILNameSyntax.ShortTypeName);
						if (this.ExpectedType != null && this.ExpectedType.FullName != this.InferredType.FullName) {
							output.Write("[", TextTokenKind.Operator);
							output.Write("exp", TextTokenKind.Keyword);
							output.Write(":", TextTokenKind.Operator);
							this.ExpectedType.WriteTo(output, ILNameSyntax.ShortTypeName);
							output.Write("]", TextTokenKind.Operator);
						}
					}
					UpdateMemberMapping(memberMapping, startLoc, output.Location, this.GetSelfAndChildrenRecursiveILRanges());
					return;
				}
			}
			
			if (this.Prefixes != null) {
				foreach (var prefix in this.Prefixes) {
					output.Write(prefix.Code.GetName() + ".", TextTokenKind.OpCode);
					output.WriteSpace();
				}
			}
			
			output.Write(Code.GetName(), TextTokenKind.OpCode);
			if (this.InferredType != null) {
				output.Write(":", TextTokenKind.Operator);
				this.InferredType.WriteTo(output, ILNameSyntax.ShortTypeName);
				if (this.ExpectedType != null && this.ExpectedType.FullName != this.InferredType.FullName) {
					output.Write("[", TextTokenKind.Operator);
					output.Write("exp", TextTokenKind.Keyword);
					output.Write(":", TextTokenKind.Operator);
					this.ExpectedType.WriteTo(output, ILNameSyntax.ShortTypeName);
					output.Write("]", TextTokenKind.Operator);
				}
			} else if (this.ExpectedType != null) {
				output.Write("[", TextTokenKind.Operator);
				output.Write("exp", TextTokenKind.Keyword);
				output.Write(":", TextTokenKind.Operator);
				this.ExpectedType.WriteTo(output, ILNameSyntax.ShortTypeName);
				output.Write("]", TextTokenKind.Operator);
			}
			output.Write("(", TextTokenKind.Operator);
			bool first = true;
			if (Operand != null) {
				if (Operand is ILLabel) {
					output.WriteReference(((ILLabel)Operand).Name, Operand, TextTokenKind.Label);
				} else if (Operand is ILLabel[]) {
					ILLabel[] labels = (ILLabel[])Operand;
					for (int i = 0; i < labels.Length; i++) {
						if (i > 0) {
							output.Write(",", TextTokenKind.Operator);
							output.WriteSpace();
						}
						output.WriteReference(labels[i].Name, labels[i], TextTokenKind.Label);
					}
				} else if (Operand is IMethod && (Operand as IMethod).MethodSig != null) {
					IMethod method = (IMethod)Operand;
					if (method.DeclaringType != null) {
						method.DeclaringType.WriteTo(output, ILNameSyntax.ShortTypeName);
						output.Write("::", TextTokenKind.Operator);
					}
					output.WriteReference(method.Name, method, TextTokenKindUtils.GetTextTokenType(method));
				} else if (Operand is IField) {
					IField field = (IField)Operand;
					field.DeclaringType.WriteTo(output, ILNameSyntax.ShortTypeName);
					output.Write("::", TextTokenKind.Operator);
					output.WriteReference(field.Name, field, TextTokenKindUtils.GetTextTokenType(field));
				} else if (Operand is ILVariable) {
					var ilvar = (ILVariable)Operand;
					output.WriteReference(ilvar.Name, Operand, ilvar.IsParameter ? TextTokenKind.Parameter : TextTokenKind.Local);
				} else {
					DisassemblerHelpers.WriteOperand(output, Operand);
				}
				first = false;
			}
			foreach (ILExpression arg in this.Arguments) {
				if (!first) {
					output.Write(",", TextTokenKind.Operator);
					output.WriteSpace();
				}
				arg.WriteTo(output, null);
				first = false;
			}
			output.Write(")", TextTokenKind.Operator);
			UpdateMemberMapping(memberMapping, startLoc, output.Location, this.GetSelfAndChildrenRecursiveILRanges());
		}
	}
	
	public class ILWhileLoop : ILNode
	{
		public ILExpression Condition;
		public ILBlock      BodyBlock;
		
		internal override ILNode GetNext(ref int index)
		{
			if (index == 0) {
				index = 1;
				if (this.Condition != null)
					return this.Condition;
			}
			if (index == 1) {
				index = 2;
				if (this.BodyBlock != null)
					return this.BodyBlock;
			}
			return null;
		}

		public override void WriteTo(ITextOutput output, MemberMapping memberMapping)
		{
			var startLoc = output.Location;
			output.Write("loop", TextTokenKind.Keyword);
			output.WriteSpace();
			output.Write("(", TextTokenKind.Operator);
			if (this.Condition != null)
				this.Condition.WriteTo(output, null);
			output.Write(")", TextTokenKind.Operator);
			var ilRanges = new List<ILRange>(ILRanges);
			if (this.Condition != null)
				this.Condition.AddSelfAndChildrenRecursiveILRanges(ilRanges);
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
		
		internal override ILNode GetNext(ref int index)
		{
			if (index == 0) {
				index = 1;
				if (this.Condition != null)
					return this.Condition;
			}
			if (index == 1) {
				index = 2;
				if (this.TrueBlock != null)
					return this.TrueBlock;
			}
			if (index == 2) {
				index = 3;
				if (this.FalseBlock != null)
					return this.FalseBlock;
			}
			return null;
		}

		public override void WriteTo(ITextOutput output, MemberMapping memberMapping)
		{
			var startLoc = output.Location;
			output.Write("if", TextTokenKind.Keyword);
			output.WriteSpace();
			output.Write("(", TextTokenKind.Operator);
			Condition.WriteTo(output, null);
			output.Write(")", TextTokenKind.Operator);
			var ilRanges = new List<ILRange>(ILRanges);
			Condition.AddSelfAndChildrenRecursiveILRanges(ilRanges);
			UpdateMemberMapping(memberMapping, startLoc, output.Location, ilRanges);
			output.WriteSpace();
			TrueBlock.WriteTo(output, memberMapping);
			if (FalseBlock != null) {
				output.Write("else", TextTokenKind.Keyword);
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
						output.Write("case", TextTokenKind.Keyword);
						output.WriteSpace();
						output.Write(string.Format("{0}", i), TextTokenKind.Number);
						output.WriteLine(":", TextTokenKind.Operator);
					}
				} else {
					output.Write("default", TextTokenKind.Keyword);
					output.WriteLine(":", TextTokenKind.Operator);
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
		public override ILRange GetAllILRanges(ref long index, ref bool done) {
			if (index < ILRanges.Count)
				return ILRanges[(int)index++];
			int i = (int)index - ILRanges.Count;
			if (i < endILRanges.Count) {
				index++;
				return endILRanges[i];
			}
			done = true;
			return default(ILRange);
		}

		public override bool SafeToAddToEndILRanges {
			get { return true; }
		}
		
		internal override ILNode GetNext(ref int index)
		{
			if (index == 0) {
				index = 1;
				return this.Condition;
			}
			if (index <= this.CaseBlocks.Count)
				return this.CaseBlocks[index++ - 1];
			return null;
		}
		
		public override void WriteTo(ITextOutput output, MemberMapping memberMapping)
		{
			var startLoc = output.Location;
			output.Write("switch", TextTokenKind.Keyword);
			output.WriteSpace();
			output.Write("(", TextTokenKind.Operator);
			Condition.WriteTo(output, null);
			output.Write(")", TextTokenKind.Operator);
			var ilRanges = new List<ILRange>(ILRanges);
			Condition.AddSelfAndChildrenRecursiveILRanges(ilRanges);
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
		
		internal override ILNode GetNext(ref int index)
		{
			if (index < this.Initializers.Count)
				return this.Initializers[index++];
			if (index == this.Initializers.Count) {
				index++;
				if (this.BodyBlock != null)
					return this.BodyBlock;
			}
			return null;
		}

		public override void WriteTo(ITextOutput output, MemberMapping memberMapping)
		{
			var startLoc = output.Location;
			output.Write("fixed", TextTokenKind.Keyword);
			output.WriteSpace();
			output.Write("(", TextTokenKind.Operator);
			for (int i = 0; i < this.Initializers.Count; i++) {
				if (i > 0) {
					output.Write(",", TextTokenKind.Operator);
					output.WriteSpace();
				}
				this.Initializers[i].WriteTo(output, null);
			}
			output.Write(")", TextTokenKind.Operator);
			var ilRanges = new List<ILRange>(ILRanges);
			foreach (var i in Initializers)
				i.AddSelfAndChildrenRecursiveILRanges(ilRanges);
			UpdateMemberMapping(memberMapping, startLoc, output.Location, ilRanges);
			output.WriteSpace();
			this.BodyBlock.WriteTo(output, memberMapping);
		}
	}
}