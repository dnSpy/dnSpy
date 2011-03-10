using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.Disassembler;
using ICSharpCode.NRefactory.Utils;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.CSharp;
using Cecil = Mono.Cecil;

namespace ICSharpCode.Decompiler.ILAst
{
	public abstract class ILNode
	{
		public IEnumerable<T> GetSelfAndChildrenRecursive<T>() where T: ILNode
		{
			List<T> result = new List<T>(16);
			AccumulateSelfAndChildrenRecursive(result);
			return result;
		}
		
		void AccumulateSelfAndChildrenRecursive<T>(List<T> list) where T:ILNode
		{
			if (this is T)
				list.Add((T)this);
			foreach (ILNode node in this.GetChildren()) {
				if (node != null)
					node.AccumulateSelfAndChildrenRecursive(list);
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
			return w.ToString().Replace("\r\n", "; ");
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
		public ILBlock          FaultBlock;
		
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
			if (FaultBlock != null) {
				output.WriteLine("fault {");
				output.Indent();
				FaultBlock.WriteTo(output);
				output.Unindent();
				output.WriteLine("}");
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
		public VariableDefinition OriginalVariable;
		
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
		public Instruction[] Prefixes { get; set; }
		// Mapping to the original instructions (useful for debugging)
		public List<ILRange> ILRanges { get; set; }
		
		public TypeReference ExpectedType { get; set; }
		public TypeReference InferredType { get; set; }
		
		public static readonly object AnyOperand = new object();
		
		public ILExpression(ILCode code, object operand, List<ILExpression> args)
		{
			this.Code = code;
			this.Operand = operand;
			this.Arguments = new List<ILExpression>(args);
			this.ILRanges  = new List<ILRange>(1);
		}
		
		public ILExpression(ILCode code, object operand, params ILExpression[] args)
		{
			this.Code = code;
			this.Operand = operand;
			this.Arguments = new List<ILExpression>(args);
			this.ILRanges  = new List<ILRange>(1);
		}
		
		public Instruction GetPrefix(Code code)
		{
			var prefixes = this.Prefixes;
			if (prefixes != null) {
				foreach (Instruction i in prefixes) {
					if (i.OpCode.Code == code)
						return i;
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
		
		public virtual bool Match(ILNode other)
		{
			ILExpression expr = other as ILExpression;
			return expr != null && this.Code == expr.Code
				&& (this.Operand == AnyOperand || object.Equals(this.Operand, expr.Operand))
				&& Match(this.Arguments, expr.Arguments);
		}
		
		protected static bool Match(IList<ILExpression> a, IList<ILExpression> b)
		{
			if (a.Count != b.Count)
				return false;
			for (int i = 0; i < a.Count; i++) {
				if (!a[i].Match(b[i]))
					return false;
			}
			return true;
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
			
			if (this.Prefixes != null) {
				foreach (Instruction prefix in this.Prefixes) {
					output.Write(prefix.OpCode.Name);
					output.Write(' ');
				}
			}
			
			output.Write(Code.GetName());
			if (this.InferredType != null) {
				output.Write(':');
				this.InferredType.WriteTo(output, true, true);
				if (this.ExpectedType != null && this.ExpectedType.FullName != this.InferredType.FullName) {
					output.Write("[exp:");
					this.ExpectedType.WriteTo(output, true, true);
					output.Write(']');
				}
			} else if (this.ExpectedType != null) {
				output.Write("[exp:");
				this.ExpectedType.WriteTo(output, true, true);
				output.Write(']');
			}
			output.Write('(');
			bool first = true;
			if (Operand != null) {
				if (Operand is ILLabel) {
					output.WriteReference(((ILLabel)Operand).Name, Operand);
				} else if (Operand is ILLabel[]) {
					ILLabel[] labels = (ILLabel[])Operand;
					for (int i = 0; i < labels.Length; i++) {
						if (i > 0)
							output.Write(", ");
						output.WriteReference(labels[i].Name, labels[i]);
					}
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
		
		public override void WriteTo(ITextOutput output)
		{
			output.WriteLine("");
			output.Write("loop (");
			if (this.Condition != null)
				this.Condition.WriteTo(output);
			output.WriteLine(") {");
			output.Indent();
			this.BodyBlock.WriteTo(output);
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
		public class CaseBlock: ILBlock
		{
			public List<int> Values;  // null for the default case
			
			public override void WriteTo(ITextOutput output)
			{
				if (this.Values != null) {
					foreach (int i in this.Values) {
						output.WriteLine("case {0}:", i);
					}
				} else {
					output.WriteLine("default:");
				}
				output.Indent();
				base.WriteTo(output);
				output.Unindent();
			}
		}
		
		public ILExpression Condition;
		public List<CaseBlock> CaseBlocks = new List<CaseBlock>();
		
		public override IEnumerable<ILNode> GetChildren()
		{
			if (this.Condition != null)
				yield return this.Condition;
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
			foreach (CaseBlock caseBlock in this.CaseBlocks) {
				caseBlock.WriteTo(output);
			}
			output.Unindent();
			output.WriteLine("}");
		}
	}
}