// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.ILAst;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace ICSharpCode.ILSpy.Baml
{
	[Serializable]
	sealed class EventRegistration
	{
		public string EventName, MethodName;
	}
	
	/// <summary>
	/// Description of ConnectMethodDecompiler.
	/// </summary>
	sealed class ConnectMethodDecompiler : MarshalByRefObject
	{
		LoadedAssembly assembly;
		
		public ConnectMethodDecompiler(LoadedAssembly assembly)
		{
			this.assembly = assembly;
		}
		
		public Dictionary<int, EventRegistration[]> DecompileEventMappings(string fullTypeName)
		{
			var result = new Dictionary<int, EventRegistration[]>();
			TypeDefinition type = this.assembly.AssemblyDefinition.MainModule.GetType(fullTypeName);
			
			if (type == null)
				return result;
			
			MethodDefinition def = null;
			
			foreach (var method in type.Methods) {
				if (method.Name == "System.Windows.Markup.IComponentConnector.Connect") {
					def = method;
					break;
				}
			}
			
			if (def == null)
				return result;
			
			// decompile method and optimize the switch
			ILBlock ilMethod = new ILBlock();
			ILAstBuilder astBuilder = new ILAstBuilder();
			ilMethod.Body = astBuilder.Build(def, true);
			ILAstOptimizer optimizer = new ILAstOptimizer();
			var context = new DecompilerContext(type.Module) { CurrentMethod = def, CurrentType = type };
			optimizer.Optimize(context, ilMethod, ILAstOptimizationStep.RemoveRedundantCode3);
			
			var cases = ilMethod.Body.OfType<ILSwitch>().First().CaseBlocks;
			
			foreach (var caseBlock in cases) {
				if (caseBlock.Values == null)
					continue;
				var events = new List<EventRegistration>();
				foreach (var node in caseBlock.Body) {
					var expr = node as ILExpression;
					string eventName, handlerName;
					if (IsAddEvent(expr, out eventName, out handlerName))
						events.Add(new EventRegistration() { EventName = eventName, MethodName = handlerName });
				}
				foreach (int id in caseBlock.Values)
					result.Add(id, events.ToArray());
			}
			
			return result;
		}
		
		bool IsAddEvent(ILExpression expr, out string eventName, out string handlerName)
		{
			eventName = "";
			handlerName = "";
			
			if (expr == null || !(expr.Code == ILCode.Callvirt || expr.Code == ILCode.Call))
				return false;
			
			if (expr.Operand is MethodReference && expr.Arguments.Count == 2) {
				var addMethod = expr.Operand as MethodReference;
				if (addMethod.Name.StartsWith("add_") && addMethod.Parameters.Count == 1)
					eventName = addMethod.Name.Substring("add_".Length);
				var arg = expr.Arguments[1];
				if (arg.Code != ILCode.Newobj && arg.Arguments.Count != 2)
					return false;
				var arg1 = arg.Arguments[1];
				if (arg1.Code != ILCode.Ldftn && arg1.Code != ILCode.Ldvirtftn)
					return false;
				if (arg1.Operand is MethodReference) {
					var m = arg1.Operand as MethodReference;
					handlerName = m.Name;
					return true;
				}
			}
			
			return false;
		}
	}
	
	sealed class AssemblyResolver : MarshalByRefObject
	{
		LoadedAssembly assembly;
		
		public AssemblyResolver(LoadedAssembly assembly)
		{
			this.assembly = assembly;
		}
		
		public string FindAssembly(string name)
		{
			var asm = assembly.LookupReferencedAssembly(name);
			return asm == null ? null : asm.FileName;
		}
	}
}
