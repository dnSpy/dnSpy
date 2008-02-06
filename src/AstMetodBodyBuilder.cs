using System;
using System.Collections.Generic;

using Ast = ICSharpCode.NRefactory.Ast;
using ICSharpCode.NRefactory.Ast;

using Cecil = Mono.Cecil;
using Mono.Cecil;
using Mono.Cecil.Cil;

using Decompiler.ControlFlow;

namespace Decompiler
{
	public class AstMetodBodyBuilder
	{
		MethodDefinition methodDef;
		static Dictionary<string, Cecil.TypeReference> localVarTypes = new Dictionary<string, Cecil.TypeReference>();
		static Dictionary<string, bool> localVarDefined = new Dictionary<string, bool>();
		
		public static BlockStatement CreateMetodBody(MethodDefinition methodDef)
		{
			AstMetodBodyBuilder builder = new AstMetodBodyBuilder();
			builder.methodDef = methodDef;
			return builder.CreateMetodBody();
		}
		
		public BlockStatement CreateMetodBody()
		{
			Ast.BlockStatement astBlock = new Ast.BlockStatement();
			
			methodDef.Body.Simplify();
			
			ByteCodeCollection body = new ByteCodeCollection(methodDef);
			StackExpressionCollection exprCollection = new StackExpressionCollection(body);
			try {
				exprCollection.Optimize();
			} catch (StopOptimizations) {
			}
			
			MethodBodyGraph bodyGraph = new MethodBodyGraph(exprCollection);
			try {
				bodyGraph.Optimize();
			} catch (StopOptimizations) {
			}
			
			List<string> intNames = new List<string>(new string[] {"i", "j", "k", "l", "m", "n", "o", "p", "q", "r", "s", "t"});
			
			foreach(VariableDefinition varDef in methodDef.Body.Variables) {
				if (varDef.VariableType.FullName == Cecil.Constants.Int32 &&
				    varDef.Name.StartsWith("V_") &&
				    intNames.Count > 0)
				{
					varDef.Name = intNames[0];
					intNames.RemoveAt(0);
				}
				localVarTypes[varDef.Name] = varDef.VariableType;
				localVarDefined[varDef.Name] = false;
				
//				Ast.VariableDeclaration astVar = new Ast.VariableDeclaration(varDef.Name);
//				Ast.LocalVariableDeclaration astLocalVar = new Ast.LocalVariableDeclaration(astVar);
//				astLocalVar.TypeReference = new Ast.TypeReference(varDef.VariableType.FullName);
//				astBlock.Children.Add(astLocalVar);
			}
			
			astBlock.Children.AddRange(TransformNodes(bodyGraph.Childs));
			
			return astBlock;
		}
		
		IEnumerable<Ast.INode> TransformNodes(IEnumerable<Node> nodes)
		{
			foreach(Node node in nodes) {
				foreach(Ast.Statement stmt in TransformNode(node)) {
					yield return stmt;
				}
			}
		}
		
		IEnumerable<Ast.INode> TransformNode(Node node)
		{
			if (Options.NodeComments) {
				yield return MakeComment("// " + node.Description);
			}
			
			yield return new Ast.LabelStatement(node.Label);
			
			if (node is BasicBlock) {
				foreach(StackExpression expr in ((BasicBlock)node).Body) {
					yield return TransformExpressionToStatement(expr);
				}
				Node fallThroughNode = ((BasicBlock)node).FallThroughBasicBlock;
				// If there is default branch and it is not the following node
				if (fallThroughNode != null) {
					yield return new Ast.GotoStatement(fallThroughNode.Label);
				}
			} else if (node is AcyclicGraph) {
				Ast.BlockStatement blockStatement = new Ast.BlockStatement();
				blockStatement.Children.AddRange(TransformNodes(node.Childs));
				yield return blockStatement;
			} else if (node is Loop) {
				Ast.BlockStatement blockStatement = new Ast.BlockStatement();
				blockStatement.Children.AddRange(TransformNodes(node.Childs));
				yield return new Ast.ForStatement(
					null,
					null,
					null,
					blockStatement
				);
			} else if (node is Block) {
				foreach(Ast.INode inode in TransformNodes(node.Childs)) {
					yield return inode;
				}
			} else if (node is Branch) {
				yield return new Ast.LabelStatement(((Branch)node).FirstBasicBlock.Label);
				
				Ast.BlockStatement trueBlock = new Ast.BlockStatement();
				trueBlock.Children.Add(new Ast.GotoStatement(((Branch)node).TrueSuccessor.Label));
				
				Ast.BlockStatement falseBlock = new Ast.BlockStatement();
				falseBlock.Children.Add(new Ast.GotoStatement(((Branch)node).FalseSuccessor.Label));
				
				Ast.IfElseStatement ifElseStmt = new Ast.IfElseStatement(
					MakeBranchCondition((Branch)node),
					trueBlock,
					falseBlock
				);
				trueBlock.Parent = ifElseStmt;
				falseBlock.Parent = ifElseStmt;
				
				yield return ifElseStmt;
			} else if (node is ConditionalNode) {
				ConditionalNode conditionalNode = (ConditionalNode)node;
				yield return new Ast.LabelStatement(conditionalNode.Condition.FirstBasicBlock.Label);
				
				Ast.BlockStatement trueBlock = new Ast.BlockStatement();
				// The block entry code
				trueBlock.Children.Add(new Ast.GotoStatement(conditionalNode.Condition.TrueSuccessor.Label));
				// Sugested content
				trueBlock.Children.AddRange(TransformNode(conditionalNode.TrueBody));
				
				Ast.BlockStatement falseBlock = new Ast.BlockStatement();
				// The block entry code
				falseBlock.Children.Add(new Ast.GotoStatement(conditionalNode.Condition.FalseSuccessor.Label));
				// Sugested content
				falseBlock.Children.AddRange(TransformNode(conditionalNode.FalseBody));
				
				Ast.IfElseStatement ifElseStmt = new Ast.IfElseStatement(
					// Method bodies are swapped
					new Ast.UnaryOperatorExpression(
						new Ast.ParenthesizedExpression(
							MakeBranchCondition(conditionalNode.Condition)
						),
						UnaryOperatorType.Not
					),
					falseBlock,
					trueBlock
				);
				trueBlock.Parent = ifElseStmt;
				falseBlock.Parent = ifElseStmt;
				
				yield return ifElseStmt;
			} else {
				throw new Exception("Bad node type");
			}
			
			if (Options.NodeComments) {
				yield return MakeComment("");
			}
		}
		
		List<Ast.Expression> TransformExpressionArguments(StackExpression expr)
		{
			List<Ast.Expression> args = new List<Ast.Expression>();
			foreach(CilStackSlot stackSlot in expr.StackBefore.PeekCount(expr.PopCount)) {
				string name = string.Format("expr{0:X2}", stackSlot.AllocadedBy.Offset);
				args.Add(new Ast.IdentifierExpression(name));
			}
			// Args generated by nested expressions (which must be closed)
			foreach(StackExpression nestedExpr in expr.LastArguments) {
				args.Add((Ast.Expression)TransformExpression(nestedExpr));
			}
			return args;
		}
		
		object TransformExpression(StackExpression expr)
		{
			List<Ast.Expression> args = TransformExpressionArguments(expr);
			return TransformByteCode(methodDef, expr.LastByteCode, args);
		}
		
		Ast.Statement TransformExpressionToStatement(StackExpression expr)
		{
			object codeExpr = TransformExpression(expr);
			if (codeExpr is Ast.Expression) {
				if (expr.PushCount == 1) {
					string type = expr.LastByteCode.Type.FullName;
					string name = string.Format("expr{0:X2}", expr.LastByteCode.Offset);
					Ast.LocalVariableDeclaration astLocal = new Ast.LocalVariableDeclaration(new Ast.TypeReference(type.ToString()));
					astLocal.Variables.Add(new Ast.VariableDeclaration(name, (Ast.Expression)codeExpr));
					return astLocal;
				} else {
					return new Ast.ExpressionStatement((Ast.Expression)codeExpr);
				}
			} else if (codeExpr is Ast.Statement) {
				return (Ast.Statement)codeExpr;
			} else {
				throw new Exception();
			}
		}
		
		static Ast.ExpressionStatement MakeComment(string text)
		{
			text = "/***" + text + "***/";
			return new Ast.ExpressionStatement(new PrimitiveExpression(text, text));
		}
		
		Ast.Expression MakeBranchCondition(Branch branch)
		{
			return new ParenthesizedExpression(MakeBranchCondition_Internal(branch));
		}
		
		Ast.Expression MakeBranchCondition_Internal(Branch branch)
		{
			if (branch is SimpleBranch) {
				List<Ast.Expression> args = TransformExpressionArguments(((SimpleBranch)branch).BasicBlock.Body[0]);
				Ast.Expression arg1 = args.Count >= 1 ? args[0] : null;
				Ast.Expression arg2 = args.Count >= 2 ? args[1] : null;
				switch(((SimpleBranch)branch).BasicBlock.Body[0].LastByteCode.OpCode.Code) {
					case Code.Brfalse: return new Ast.UnaryOperatorExpression(arg1, UnaryOperatorType.Not);
					case Code.Brtrue:  return arg1;
					case Code.Beq:     return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.Equality, arg2);
					case Code.Bge:     return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.GreaterThanOrEqual, arg2);
					case Code.Bge_Un:  return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.GreaterThanOrEqual, arg2);
					case Code.Bgt:     return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.GreaterThan, arg2);
					case Code.Bgt_Un:  return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.GreaterThan, arg2);
					case Code.Ble:     return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.LessThanOrEqual, arg2);
					case Code.Ble_Un:  return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.LessThanOrEqual, arg2);
					case Code.Blt:     return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.LessThan, arg2);
					case Code.Blt_Un:  return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.LessThan, arg2);
					case Code.Bne_Un:  return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.InEquality, arg2);
					default: throw new Exception("Bad opcode");
				}
			} else if (branch is ShortCircuitBranch) {
				ShortCircuitBranch scBranch = (ShortCircuitBranch)branch;
				switch(scBranch.Operator) {
					case ShortCircuitOperator.LeftAndRight:
						return new BinaryOperatorExpression(
							MakeBranchCondition(scBranch.Left),
							BinaryOperatorType.LogicalAnd,
							MakeBranchCondition(scBranch.Right)
						);
					case ShortCircuitOperator.LeftOrRight:
						return new BinaryOperatorExpression(
							MakeBranchCondition(scBranch.Left),
							BinaryOperatorType.LogicalOr,
							MakeBranchCondition(scBranch.Right)
						);
					case ShortCircuitOperator.NotLeftAndRight:
						return new BinaryOperatorExpression(
							new UnaryOperatorExpression(MakeBranchCondition(scBranch.Left), UnaryOperatorType.Not),
							BinaryOperatorType.LogicalAnd,
							MakeBranchCondition(scBranch.Right)
						);
					case ShortCircuitOperator.NotLeftOrRight:
						return new BinaryOperatorExpression(
							new UnaryOperatorExpression(MakeBranchCondition(scBranch.Left), UnaryOperatorType.Not),
							BinaryOperatorType.LogicalOr,
							MakeBranchCondition(scBranch.Right)
						);
					default:
						throw new Exception("Bad operator");
				}
			} else {
				throw new Exception("Bad type");
			}
		}
		
		static object TransformByteCode(MethodDefinition methodDef, ByteCode byteCode, List<Ast.Expression> args)
		{
			try {
				object ret = TransformByteCode_Internal(methodDef, byteCode, args);
				if (ret is Ast.Expression) {
					ret = new ParenthesizedExpression((Ast.Expression)ret);
				}
				return ret;
			} catch (NotImplementedException) {
				// Output the operand of the unknown IL code as well
				if (byteCode.Operand != null) {
					args.Insert(0, new IdentifierExpression(byteCode.FormatedOperand));
				}
				return new Ast.InvocationExpression(new IdentifierExpression("IL__" + byteCode.OpCode.Name), args);
			}
		}
		
		static object TransformByteCode_Internal(MethodDefinition methodDef, ByteCode byteCode, List<Ast.Expression> args)
		{
			OpCode opCode = byteCode.OpCode;
			object operand = byteCode.Operand;
			Ast.TypeReference operandAsTypeRef = operand is Cecil.TypeReference ? new Ast.TypeReference(((Cecil.TypeReference)operand).FullName) : null;
			ByteCode operandAsByteCode = operand as ByteCode;
			Ast.Expression arg1 = args.Count >= 1 ? args[0] : null;
			Ast.Expression arg2 = args.Count >= 2 ? args[1] : null;
			Ast.Expression arg3 = args.Count >= 3 ? args[2] : null;
			
			Ast.Statement branchCommand = null;
			if (operand is ByteCode) {
				branchCommand = new Ast.GotoStatement(((ByteCode)operand).Expression.BasicBlock.Label);
			}
			
			switch(opCode.Code) {
				#region Arithmetic
					case Code.Add:        return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.Add, arg2);
					case Code.Add_Ovf:    return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.Add, arg2);
					case Code.Add_Ovf_Un: return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.Add, arg2);
					case Code.Div:        return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.Divide, arg2);
					case Code.Div_Un:     return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.Divide, arg2);
					case Code.Mul:        return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.Multiply, arg2);
					case Code.Mul_Ovf:    return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.Multiply, arg2);
					case Code.Mul_Ovf_Un: return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.Multiply, arg2);
					case Code.Rem:        return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.Modulus, arg2);
					case Code.Rem_Un:     return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.Modulus, arg2);
					case Code.Sub:        return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.Subtract, arg2);
					case Code.Sub_Ovf:    return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.Subtract, arg2);
					case Code.Sub_Ovf_Un: return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.Subtract, arg2);
					case Code.And:        return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.BitwiseAnd, arg2);
					case Code.Xor:        return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.ExclusiveOr, arg2);
					case Code.Shl:        return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.ShiftLeft, arg2);
					case Code.Shr:        return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.ShiftRight, arg2);
					case Code.Shr_Un:     return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.ShiftRight, arg2);
					
					case Code.Neg:        return new Ast.UnaryOperatorExpression(arg1, UnaryOperatorType.Minus);
					case Code.Not:        return new Ast.UnaryOperatorExpression(arg1, UnaryOperatorType.BitNot);
				#endregion
				#region Arrays
					case Code.Newarr:
						operandAsTypeRef.RankSpecifier = new int[] {0};
						return new Ast.ArrayCreateExpression(operandAsTypeRef, new List<Expression>(new Expression[] {arg1}));
					
					case Code.Ldlen: return new Ast.MemberReferenceExpression(arg1, "Length");
					
					case Code.Ldelem_I:   
					case Code.Ldelem_I1:  
					case Code.Ldelem_I2:  
					case Code.Ldelem_I4:  
					case Code.Ldelem_I8:  
					case Code.Ldelem_U1:  
					case Code.Ldelem_U2:  
					case Code.Ldelem_U4:  
					case Code.Ldelem_R4:  
					case Code.Ldelem_R8:  
					case Code.Ldelem_Ref: return new Ast.IndexerExpression(arg1, new List<Expression>(new Expression[] {arg2}));
					case Code.Ldelem_Any: throw new NotImplementedException();
					case Code.Ldelema:    return new Ast.IndexerExpression(arg1, new List<Expression>(new Expression[] {arg2}));
					
					case Code.Stelem_I:   
					case Code.Stelem_I1:  
					case Code.Stelem_I2:  
					case Code.Stelem_I4:  
					case Code.Stelem_I8:  
					case Code.Stelem_R4:  
					case Code.Stelem_R8:  
					case Code.Stelem_Ref: return new Ast.AssignmentExpression(new Ast.IndexerExpression(arg1, new List<Expression>(new Expression[] {arg2})), AssignmentOperatorType.Assign, arg3);
					case Code.Stelem_Any: throw new NotImplementedException();
				#endregion
				#region Branching
					case Code.Br:      return branchCommand;
					case Code.Brfalse: return new Ast.IfElseStatement(new Ast.UnaryOperatorExpression(arg1, UnaryOperatorType.Not), branchCommand);
					case Code.Brtrue:  return new Ast.IfElseStatement(arg1, branchCommand);
					case Code.Beq:     return new Ast.IfElseStatement(new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.Equality, arg2), branchCommand);
					case Code.Bge:     return new Ast.IfElseStatement(new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.GreaterThanOrEqual, arg2), branchCommand);
					case Code.Bge_Un:  return new Ast.IfElseStatement(new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.GreaterThanOrEqual, arg2), branchCommand);
					case Code.Bgt:     return new Ast.IfElseStatement(new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.GreaterThan, arg2), branchCommand);
					case Code.Bgt_Un:  return new Ast.IfElseStatement(new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.GreaterThan, arg2), branchCommand);
					case Code.Ble:     return new Ast.IfElseStatement(new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.LessThanOrEqual, arg2), branchCommand);
					case Code.Ble_Un:  return new Ast.IfElseStatement(new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.LessThanOrEqual, arg2), branchCommand);
					case Code.Blt:     return new Ast.IfElseStatement(new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.LessThan, arg2), branchCommand);
					case Code.Blt_Un:  return new Ast.IfElseStatement(new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.LessThan, arg2), branchCommand);
					case Code.Bne_Un:  return new Ast.IfElseStatement(new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.InEquality, arg2), branchCommand);
				#endregion
				#region Comparison
					case Code.Ceq:    return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.Equality, ConvertIntToBool(arg2));
					case Code.Cgt:    return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.GreaterThan, arg2);
					case Code.Cgt_Un: return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.GreaterThan, arg2);
					case Code.Clt:    return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.LessThan, arg2);
					case Code.Clt_Un: return new Ast.BinaryOperatorExpression(arg1, BinaryOperatorType.LessThan, arg2);
				#endregion
				#region Conversions
					case Code.Conv_I:    return new Ast.CastExpression(new Ast.TypeReference(typeof(int).FullName), arg1, CastType.Cast); // TODO
					case Code.Conv_I1:   return new Ast.CastExpression(new Ast.TypeReference(typeof(SByte).FullName), arg1, CastType.Cast);
					case Code.Conv_I2:   return new Ast.CastExpression(new Ast.TypeReference(typeof(Int16).FullName), arg1, CastType.Cast);
					case Code.Conv_I4:   return new Ast.CastExpression(new Ast.TypeReference(typeof(Int32).FullName), arg1, CastType.Cast);
					case Code.Conv_I8:   return new Ast.CastExpression(new Ast.TypeReference(typeof(Int64).FullName), arg1, CastType.Cast);
					case Code.Conv_U:    return new Ast.CastExpression(new Ast.TypeReference(typeof(uint).FullName), arg1, CastType.Cast); // TODO
					case Code.Conv_U1:   return new Ast.CastExpression(new Ast.TypeReference(typeof(Byte).FullName), arg1, CastType.Cast);
					case Code.Conv_U2:   return new Ast.CastExpression(new Ast.TypeReference(typeof(UInt16).FullName), arg1, CastType.Cast);
					case Code.Conv_U4:   return new Ast.CastExpression(new Ast.TypeReference(typeof(UInt32).FullName), arg1, CastType.Cast);
					case Code.Conv_U8:   return new Ast.CastExpression(new Ast.TypeReference(typeof(UInt64).FullName), arg1, CastType.Cast);
					case Code.Conv_R4:   return new Ast.CastExpression(new Ast.TypeReference(typeof(float).FullName), arg1, CastType.Cast);
					case Code.Conv_R8:   return new Ast.CastExpression(new Ast.TypeReference(typeof(double).FullName), arg1, CastType.Cast);
					case Code.Conv_R_Un: return new Ast.CastExpression(new Ast.TypeReference(typeof(double).FullName), arg1, CastType.Cast); // TODO
					
					case Code.Conv_Ovf_I:  return new Ast.CastExpression(new Ast.TypeReference(typeof(int).FullName), arg1, CastType.Cast); // TODO
					case Code.Conv_Ovf_I1: return new Ast.CastExpression(new Ast.TypeReference(typeof(SByte).FullName), arg1, CastType.Cast);
					case Code.Conv_Ovf_I2: return new Ast.CastExpression(new Ast.TypeReference(typeof(Int16).FullName), arg1, CastType.Cast);
					case Code.Conv_Ovf_I4: return new Ast.CastExpression(new Ast.TypeReference(typeof(Int32).FullName), arg1, CastType.Cast);
					case Code.Conv_Ovf_I8: return new Ast.CastExpression(new Ast.TypeReference(typeof(Int64).FullName), arg1, CastType.Cast);
					case Code.Conv_Ovf_U:  return new Ast.CastExpression(new Ast.TypeReference(typeof(uint).FullName), arg1, CastType.Cast); // TODO
					case Code.Conv_Ovf_U1: return new Ast.CastExpression(new Ast.TypeReference(typeof(Byte).FullName), arg1, CastType.Cast);
					case Code.Conv_Ovf_U2: return new Ast.CastExpression(new Ast.TypeReference(typeof(UInt16).FullName), arg1, CastType.Cast);
					case Code.Conv_Ovf_U4: return new Ast.CastExpression(new Ast.TypeReference(typeof(UInt32).FullName), arg1, CastType.Cast);
					case Code.Conv_Ovf_U8: return new Ast.CastExpression(new Ast.TypeReference(typeof(UInt64).FullName), arg1, CastType.Cast);
					
					case Code.Conv_Ovf_I_Un:  return new Ast.CastExpression(new Ast.TypeReference(typeof(int).FullName), arg1, CastType.Cast); // TODO
					case Code.Conv_Ovf_I1_Un: return new Ast.CastExpression(new Ast.TypeReference(typeof(SByte).FullName), arg1, CastType.Cast);
					case Code.Conv_Ovf_I2_Un: return new Ast.CastExpression(new Ast.TypeReference(typeof(Int16).FullName), arg1, CastType.Cast);
					case Code.Conv_Ovf_I4_Un: return new Ast.CastExpression(new Ast.TypeReference(typeof(Int32).FullName), arg1, CastType.Cast);
					case Code.Conv_Ovf_I8_Un: return new Ast.CastExpression(new Ast.TypeReference(typeof(Int64).FullName), arg1, CastType.Cast);
					case Code.Conv_Ovf_U_Un:  return new Ast.CastExpression(new Ast.TypeReference(typeof(uint).FullName), arg1, CastType.Cast); // TODO
					case Code.Conv_Ovf_U1_Un: return new Ast.CastExpression(new Ast.TypeReference(typeof(Byte).FullName), arg1, CastType.Cast);
					case Code.Conv_Ovf_U2_Un: return new Ast.CastExpression(new Ast.TypeReference(typeof(UInt16).FullName), arg1, CastType.Cast);
					case Code.Conv_Ovf_U4_Un: return new Ast.CastExpression(new Ast.TypeReference(typeof(UInt32).FullName), arg1, CastType.Cast);
					case Code.Conv_Ovf_U8_Un: return new Ast.CastExpression(new Ast.TypeReference(typeof(UInt64).FullName), arg1, CastType.Cast);
				#endregion
				#region Indirect
					case Code.Ldind_I: throw new NotImplementedException();
					case Code.Ldind_I1: throw new NotImplementedException();
					case Code.Ldind_I2: throw new NotImplementedException();
					case Code.Ldind_I4: throw new NotImplementedException();
					case Code.Ldind_I8: throw new NotImplementedException();
					case Code.Ldind_U1: throw new NotImplementedException();
					case Code.Ldind_U2: throw new NotImplementedException();
					case Code.Ldind_U4: throw new NotImplementedException();
					case Code.Ldind_R4: throw new NotImplementedException();
					case Code.Ldind_R8: throw new NotImplementedException();
					case Code.Ldind_Ref: throw new NotImplementedException();
					
					case Code.Stind_I: throw new NotImplementedException();
					case Code.Stind_I1: throw new NotImplementedException();
					case Code.Stind_I2: throw new NotImplementedException();
					case Code.Stind_I4: throw new NotImplementedException();
					case Code.Stind_I8: throw new NotImplementedException();
					case Code.Stind_R4: throw new NotImplementedException();
					case Code.Stind_R8: throw new NotImplementedException();
					case Code.Stind_Ref: throw new NotImplementedException();
				#endregion
				case Code.Arglist: throw new NotImplementedException();
				case Code.Box: throw new NotImplementedException();
				case Code.Break: throw new NotImplementedException();
				case Code.Call:
				case Code.Callvirt:
					// TODO: Diferentiate vitual and non-vitual dispach
					Cecil.MethodReference cecilMethod = ((MethodReference)operand);
					Ast.Expression target;
					List<Ast.Expression> methodArgs = new List<Ast.Expression>(args);
					if (cecilMethod.HasThis) {
						target = methodArgs[0];
						methodArgs.RemoveAt(0);
					} else {
						target = new Ast.IdentifierExpression(cecilMethod.DeclaringType.FullName);
					}
					// TODO: Hack, detect properties properly
					if (cecilMethod.Name.StartsWith("get_")) {
						return new Ast.MemberReferenceExpression(target, cecilMethod.Name.Remove(0, 4));
					} else if (cecilMethod.Name.StartsWith("set_")) {
						return new Ast.AssignmentExpression(
							new Ast.MemberReferenceExpression(target, cecilMethod.Name.Remove(0, 4)),
							AssignmentOperatorType.Assign,
							methodArgs[0]
						);
					} else {
						return new Ast.InvocationExpression(
							new Ast.MemberReferenceExpression(target, cecilMethod.Name),
							methodArgs
						);
					}
				case Code.Calli: throw new NotImplementedException();
				case Code.Castclass: return new Ast.CastExpression(operandAsTypeRef, arg1, CastType.Cast);
				case Code.Ckfinite: throw new NotImplementedException();
				case Code.Constrained: throw new NotImplementedException();
				case Code.Cpblk: throw new NotImplementedException();
				case Code.Cpobj: throw new NotImplementedException();
				case Code.Dup: throw new NotImplementedException();
				case Code.Endfilter: throw new NotImplementedException();
				case Code.Endfinally: throw new NotImplementedException();
				case Code.Initblk: throw new NotImplementedException();
				case Code.Initobj: throw new NotImplementedException();
				case Code.Isinst: throw new NotImplementedException();
				case Code.Jmp: throw new NotImplementedException();
				case Code.Ldarg:
					if (methodDef.HasThis && ((ParameterDefinition)operand).Sequence == 0) {
						return new Ast.ThisReferenceExpression();
					} else {
						return new Ast.IdentifierExpression(((ParameterDefinition)operand).Name);
					}
				case Code.Ldarga: throw new NotImplementedException();
				case Code.Ldc_I4: 
				case Code.Ldc_I8: 
				case Code.Ldc_R4: 
				case Code.Ldc_R8: return new Ast.PrimitiveExpression(operand, null);
				case Code.Ldfld:
				case Code.Ldsfld: {
					FieldDefinition field = (FieldDefinition) operand;
					if (field.IsStatic) {
						return new Ast.MemberReferenceExpression(
							new Ast.IdentifierExpression(field.DeclaringType.FullName),
							field.Name
						);
					} else {
						return new Ast.MemberReferenceExpression(arg1, field.Name);
					}
				}
				case Code.Stfld:
				case Code.Stsfld: {
					FieldDefinition field = (FieldDefinition) operand;
					if (field.IsStatic) {
						return new AssignmentExpression(
							new Ast.MemberReferenceExpression(
								new Ast.IdentifierExpression(field.DeclaringType.FullName),
								field.Name
							),
							AssignmentOperatorType.Assign,
							arg1
						);
					} else {
						return new AssignmentExpression(
							new Ast.MemberReferenceExpression(arg1, field.Name),
							AssignmentOperatorType.Assign,
							arg2
						);
					}
				}
				case Code.Ldflda:
				case Code.Ldsflda: throw new NotImplementedException();
				case Code.Ldftn: throw new NotImplementedException();
				case Code.Ldloc: return new Ast.IdentifierExpression(((VariableDefinition)operand).Name);
				case Code.Ldloca: throw new NotImplementedException();
				case Code.Ldnull: return new Ast.PrimitiveExpression(null, null);
				case Code.Ldobj: throw new NotImplementedException();
				case Code.Ldstr: return new Ast.PrimitiveExpression(operand, null);
				case Code.Ldtoken:
					if (operand is Cecil.TypeReference) {
						return new Ast.MemberReferenceExpression(
							new Ast.TypeOfExpression(operandAsTypeRef),
							"TypeHandle"
						);
					} else {
						throw new NotImplementedException();
					}
				case Code.Ldvirtftn: throw new NotImplementedException();
				case Code.Leave: throw new NotImplementedException();
				case Code.Localloc: throw new NotImplementedException();
				case Code.Mkrefany: throw new NotImplementedException();
				case Code.Newobj:
					// TODO: Ensure that the corrent overloaded constructor is called
					return new Ast.ObjectCreateExpression(
						new Ast.TypeReference(((MethodReference)operand).DeclaringType.FullName),
						new List<Expression>(args)
					);
				case Code.No: throw new NotImplementedException();
				case Code.Nop: return new Ast.PrimitiveExpression("/* No-op */", "/* No-op */");
				case Code.Or: throw new NotImplementedException();
				case Code.Pop: throw new NotImplementedException();
				case Code.Readonly: throw new NotImplementedException();
				case Code.Refanytype: throw new NotImplementedException();
				case Code.Refanyval: throw new NotImplementedException();
				case Code.Ret: return new Ast.ReturnStatement(methodDef.ReturnType.ReturnType.FullName != Cecil.Constants.Void ? arg1 : null);
				case Code.Rethrow: throw new NotImplementedException();
				case Code.Sizeof: throw new NotImplementedException();
				case Code.Starg: throw new NotImplementedException();
				case Code.Stloc: 
					string name = ((VariableDefinition)operand).Name;
					if (localVarDefined[name]) {
						return new Ast.AssignmentExpression(new Ast.IdentifierExpression(name), AssignmentOperatorType.Assign, arg1);
					} else {
						Ast.LocalVariableDeclaration astLocalVar = new Ast.LocalVariableDeclaration(new Ast.VariableDeclaration(name, arg1));
						astLocalVar.TypeReference = new Ast.TypeReference(localVarTypes[name].FullName);
						localVarDefined[name] = true;
						return astLocalVar;
					}
				case Code.Stobj: throw new NotImplementedException();
				case Code.Switch: throw new NotImplementedException();
				case Code.Tail: throw new NotImplementedException();
				case Code.Throw: throw new NotImplementedException();
				case Code.Unaligned: throw new NotImplementedException();
				case Code.Unbox: throw new NotImplementedException();
				case Code.Unbox_Any: throw new NotImplementedException();
				case Code.Volatile: throw new NotImplementedException();
				default: throw new Exception("Unknown OpCode: " + opCode);
			}
		}
		
		static Ast.Expression ConvertIntToBool(Ast.Expression astInt)
		{
			return new Ast.ParenthesizedExpression(new Ast.BinaryOperatorExpression(astInt, BinaryOperatorType.InEquality, new Ast.PrimitiveExpression(0, "0")));
		}
	}
}
