using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Decompiler.ControlFlow;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.Disassembler;
using ICSharpCode.NRefactory.Utils;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.CSharp;
using Cecil = Mono.Cecil;

namespace Decompiler
{
	public abstract class ILNode
	{
		public IEnumerable<T> GetSelfAndChildrenRecursive<T>() where T: ILNode
		{
			return TreeTraversal.PreOrder(this, c => c != null ? c.GetChildren() : null).OfType<T>();
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
		public ILExpression EntryGoto;
		
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
			if (this.EntryGoto != null)
				yield return this.EntryGoto;
			foreach(ILNode child in this.Body) {
				yield return child;
			}
		}
		
		public override void WriteTo(ITextOutput output)
		{
			foreach(ILNode child in this.GetChildren()) {
				child.WriteTo(output);
				output.WriteLine();
			}
		}
	}
	
	public class ILBasicBlock: ILNode
	{
		public ILLabel      EntryLabel;
		public List<ILNode> Body = new List<ILNode>();
		public ILExpression FallthoughGoto;
		
		public override IEnumerable<ILNode> GetChildren()
		{
			if (this.EntryLabel != null)
				yield return this.EntryLabel;
			foreach (ILNode child in this.Body) {
				yield return child;
			}
			if (this.FallthoughGoto != null)
				yield return this.FallthoughGoto;
		}
		
		public override void WriteTo(ITextOutput output)
		{
			foreach(ILNode child in this.GetChildren()) {
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
	
	public class ILComment: ILNode
	{
		public string Text;
		public List<ILRange> ILRanges { get; set; }
		
		public override void WriteTo(ITextOutput output)
		{
			output.WriteLine("// " + this.Text);
		}
	}
	
	public class ILTryCatchBlock: ILNode
	{
		public class CatchBlock: ILBlock
		{
			public TypeReference ExceptionType;
			public ILVariable ExceptionVariable;
			
			public override void WriteTo(ITextOutput output)
			{
				output.Write("catch ");
				output.WriteReference(ExceptionType.FullName, ExceptionType);
				output.WriteLine(" {");
				output.Indent();
				base.WriteTo(output);
				output.Unindent();
				output.WriteLine("}");
			}
		}
		
		public ILBlock          TryBlock;
		public List<CatchBlock> CatchBlocks;
		public ILBlock          FinallyBlock;
		
		public override IEnumerable<ILNode> GetChildren()
		{
			if (this.TryBlock != null)
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
			output.Indent();
			TryBlock.WriteTo(output);
			output.Unindent();
			output.WriteLine("}");
			foreach (CatchBlock block in CatchBlocks) {
				block.WriteTo(output);
			}
			if (FinallyBlock != null) {
				output.WriteLine("finally {");
				output.Indent();
				FinallyBlock.WriteTo(output);
				output.Unindent();
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
	
	public class ILExpression : ILNode
	{
		public ILCode Code { get; set; }
		public object Operand { get; set; }
		public List<ILExpression> Arguments { get; set; }
		// Mapping to the original instructions (useful for debugging)
		public List<ILRange> ILRanges { get; set; }
		
		public TypeReference InferredType { get; set; }
		
		public ILExpression(ILCode code, object operand, params ILExpression[] args)
		{
			this.Code = code;
			this.Operand = operand;
			this.Arguments = new List<ILExpression>(args);
			this.ILRanges  = new List<ILRange>(1);
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
		
		public override void WriteTo(ITextOutput output)
		{
			if (Operand is ILVariable && ((ILVariable)Operand).IsGenerated) {
				if (Code == ILCode.Stloc && this.InferredType == null) {
					output.Write(((ILVariable)Operand).Name);
					output.Write(" = ");
					Arguments.First().WriteTo(output);
					return;
				} else if (Code == ILCode.Ldloc) {
					output.Write(((ILVariable)Operand).Name);
					if (this.InferredType != null) {
						output.Write(':');
						this.InferredType.WriteTo(output, true, true);
					}
					return;
				}
			}
			
			output.Write(Code.GetName());
			if (this.InferredType != null) {
				output.Write(':');
				this.InferredType.WriteTo(output, true, true);
			}
			output.Write('(');
			bool first = true;
			if (Operand != null) {
				if (Operand is ILLabel) {
					output.WriteReference(((ILLabel)Operand).Name, Operand);
				} else if (Operand is MethodReference) {
					MethodReference method = (MethodReference)Operand;
					method.DeclaringType.WriteTo(output, true, true);
					output.Write("::");
					output.WriteReference(method.Name, method);
				} else if (Operand is FieldReference) {
					FieldReference field = (FieldReference)Operand;
					field.DeclaringType.WriteTo(output, true, true);
					output.Write("::");
					output.WriteReference(field.Name, field);
				} else {
					DisassemblerHelpers.WriteOperand(output, Operand);
				}
				first = false;
			}
			foreach (ILExpression arg in this.Arguments) {
				if (!first) output.Write(", ");
				arg.WriteTo(output);
				first = false;
			}
			output.Write(')');
		}
	}
	
	public class ILLoop : ILNode
	{
		public ILBlock ContentBlock;
		
		public override IEnumerable<ILNode> GetChildren()
		{
			if (this.ContentBlock != null)
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
		public ILExpression DefaultGoto;
		
		public override IEnumerable<ILNode> GetChildren()
		{
			if (this.Condition != null)
				yield return this.Condition;
			foreach (ILBlock caseBlock in this.CaseBlocks) {
				yield return caseBlock;
			}
			if (this.DefaultGoto != null)
				yield return this.DefaultGoto;
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