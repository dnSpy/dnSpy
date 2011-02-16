using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Decompiler.ControlFlow;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.Disassembler;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Cecil = Mono.Cecil;

namespace Decompiler
{
	public abstract class ILNode
	{
		public IEnumerable<T> GetSelfAndChildrenRecursive<T>() where T: ILNode
		{
			if (this is T)
				yield return (T)this;
			
			Stack<IEnumerator<ILNode>> stack = new Stack<IEnumerator<ILNode>>();
			try {
				stack.Push(GetChildren().GetEnumerator());
				while (stack.Count > 0) {
					while (stack.Peek().MoveNext()) {
						ILNode element = stack.Peek().Current;
						if (element != null) {
							if (element is T)
								yield return (T)element;
							IEnumerable<ILNode> children = element.GetChildren();
							if (children != null) {
								stack.Push(children.GetEnumerator());
							}
						}
					}
					stack.Pop().Dispose();
				}
			} finally {
				while (stack.Count > 0) {
					stack.Pop().Dispose();
				}
			}
		}
		
		public virtual IEnumerable<ILNode> GetChildren()
		{
			yield break;
		}
		
		public override string ToString()
		{
			StringWriter w = new StringWriter();
			WriteTo(new PlainTextOutput(w));
			return w.ToString();
		}
		
		public abstract void WriteTo(ITextOutput output);
	}
	
	public class ILBlock: ILNode
	{
		// TODO: This should really be a goto, not a label
		public ILLabel EntryPoint;
		
		public List<ILNode> Body;
		
		public ILBlock(params ILNode[] body)
		{
			this.Body = new List<ILNode>(body);
		}
		
		public ILBlock(List<ILNode> body)
		{
			this.Body = body;
		}
		
		public override IEnumerable<ILNode> GetChildren()
		{
			yield return EntryPoint;
			foreach(ILNode child in this.Body) {
				yield return child;
			}
		}
		
		public override void WriteTo(ITextOutput output)
		{
			EntryPoint.WriteTo(output);
			foreach(ILNode child in this.Body) {
				child.WriteTo(output);
				output.WriteLine();
			}
		}
	}
	
	public class ILLabel: ILNode
	{
		public string Name;

		public override void WriteTo(ITextOutput output)
		{
			output.WriteDefinition(Name + ":", this);
		}
	}
	
	public class ILTryCatchBlock: ILNode
	{
		public class CatchBlock: ILBlock
		{
			public TypeReference ExceptionType;
			
			public override void WriteTo(ITextOutput output)
			{
				output.Write("catch ");
				output.WriteReference(ExceptionType.FullName, ExceptionType);
				output.WriteLine(" {");
				base.WriteTo(output);
				output.WriteLine("}");
			}
		}
		
		public ILBlock          TryBlock;
		public List<CatchBlock> CatchBlocks;
		public ILBlock          FinallyBlock;
		
		public override IEnumerable<ILNode> GetChildren()
		{
			yield return this.TryBlock;
			foreach (var catchBlock in this.CatchBlocks) {
				yield return catchBlock;
			}
			if (this.FinallyBlock != null)
				yield return this.FinallyBlock;
		}
		
		public override void WriteTo(ITextOutput output)
		{
			output.WriteLine(".try {");
			TryBlock.WriteTo(output);
			output.WriteLine("}");
			foreach (CatchBlock block in CatchBlocks) {
				block.WriteTo(output);
			}
			if (FinallyBlock != null) {
				output.WriteLine("finally {");
				FinallyBlock.WriteTo(output);
				output.WriteLine("}");
			}
		}
	}
	
	public class ILVariable
	{
		public string Name;
		public bool   IsGenerated;
		public TypeReference Type;
		
		public override string ToString()
		{
			return Name;
		}
	}
	
	public class ILRange
	{
		public int From;
		public int To;   // Exlusive
		
		public override string ToString()
		{
			return string.Format("{0}-{1}", From, To);
		}
	}
	
	public class ILExpression: ILNode
	{
		public OpCode OpCode { get; set; }
		public object Operand { get; set; }
		public List<ILExpression> Arguments { get; set; }
		// Mapping to the original instructions (useful for debugging)
		public List<ILRange> ILRanges { get; set; }
		
		public ILExpression(OpCode opCode, object operand, params ILExpression[] args)
		{
			this.OpCode = opCode;
			this.Operand = operand;
			this.Arguments = new List<ILExpression>(args);
			this.ILRanges  = new List<ILRange>(1);
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
		
		public List<ILRange> GetILRanges()
		{
			List<ILRange> ranges = new List<ILRange>();
			foreach(ILExpression expr in this.GetSelfAndChildrenRecursive<ILExpression>()) {
				ranges.AddRange(expr.ILRanges);
			}
			ranges = ranges.OrderBy(r => r.From).ToList();
			for (int i = 0; i < ranges.Count - 1;) {
				ILRange curr = ranges[i];
				ILRange next = ranges[i + 1];
				// Merge consequtive ranges if they intersect
				if (curr.From <= next.From && next.From <= curr.To) {
					curr.To = Math.Max(curr.To, next.To);
					ranges.RemoveAt(i + 1);
				} else {
					i++;
				}
			}
			return ranges;
		}
		
		public override IEnumerable<ILNode> GetChildren()
		{
			return Arguments;
		}
		
		public override void WriteTo(ITextOutput output)
		{
			if (Operand is ILVariable && ((ILVariable)Operand).IsGenerated) {
				if (OpCode.Name == "stloc") {
					output.Write(((ILVariable)Operand).Name + " = ");
					Arguments.First().WriteTo(output);
					return;
				} else if (OpCode.Name == "ldloc") {
					output.Write(((ILVariable)Operand).Name);
					return;
				}
			}
			
			output.Write(OpCode.Name);
			output.Write('(');
			bool first = true;
			if (Operand != null) {
				if (Operand is ILLabel)
					output.Write(((ILLabel)Operand).Name);
				else
					DisassemblerHelpers.WriteOperand(output, Operand);
				first = false;
			}
			foreach (ILExpression arg in this.Arguments) {
				if (!first) output.Write(',');
				arg.WriteTo(output);
				first = false;
			}
			output.Write(')');
		}
	}
	
	public class ILLoop: ILNode
	{
		public ILBlock ContentBlock;
		
		public override IEnumerable<ILNode> GetChildren()
		{
			yield return ContentBlock;
		}
		
		public override void WriteTo(ITextOutput output)
		{
			output.WriteLine("loop {");
			output.Indent();
			ContentBlock.WriteTo(output);
			output.Unindent();
			output.WriteLine("}");
		}
	}
	
	public class ILCondition: ILNode
	{
		public ILExpression Condition;
		public ILBlock TrueBlock;   // Branch was taken
		public ILBlock FalseBlock;  // Fall-though
		
		public override IEnumerable<ILNode> GetChildren()
		{
			yield return Condition;
			yield return TrueBlock;
			yield return FalseBlock;
		}
		
		public override void WriteTo(ITextOutput output)
		{
			output.Write("if (");
			Condition.WriteTo(output);
			output.WriteLine(") {");
			output.Indent();
			TrueBlock.WriteTo(output);
			output.Unindent();
			output.Write("}");
			if (FalseBlock != null) {
				output.WriteLine(" else {");
				output.Indent();
				FalseBlock.WriteTo(output);
				output.Unindent();
				output.WriteLine("}");
			}
		}
	}
	
	public class ILSwitch: ILNode
	{
		public ILExpression Condition;
		public List<ILBlock> CaseBlocks = new List<ILBlock>();
		
		public override IEnumerable<ILNode> GetChildren()
		{
			yield return Condition;
			foreach (ILBlock caseBlock in this.CaseBlocks) {
				yield return caseBlock;
			}
		}
		
		public override void WriteTo(ITextOutput output)
		{
			output.Write("switch (");
			Condition.WriteTo(output);
			output.WriteLine(") {");
			output.Indent();
			for (int i = 0; i < CaseBlocks.Count; i++) {
				output.WriteLine("case {0}:", i);
				output.Indent();
				CaseBlocks[i].WriteTo(output);
				output.Unindent();
			}
			output.Unindent();
			output.WriteLine("}");
		}
	}
}