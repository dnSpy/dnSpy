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
		public string EventName, MethodName, AttachSourceType;
		public bool IsAttached;
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
			
			ILSwitch ilSwitch = ilMethod.Body.OfType<ILSwitch>().FirstOrDefault();
			ILCondition condition = ilMethod.Body.OfType<ILCondition>().FirstOrDefault();
			
			if (ilSwitch != null) {
				foreach (var caseBlock in ilSwitch.CaseBlocks) {
					if (caseBlock.Values == null)
						continue;
					var events = FindEvents(caseBlock);
					foreach (int id in caseBlock.Values)
						result.Add(id, events);
				}
			} else if (condition != null) {
				result.Add(1, FindEvents(condition.FalseBlock));
			}
			
			return result;
		}

		EventRegistration[] FindEvents(ILBlock block)
		{
			var events = new List<EventRegistration>();
			
			foreach (var node in block.Body) {
				var expr = node as ILExpression;
				string eventName, handlerName, attachSource;
				if (IsAddEvent(expr, out eventName, out handlerName))
					events.Add(new EventRegistration {
					           	EventName = eventName,
					           	MethodName = handlerName
					           });
				else if (IsAddAttachedEvent(expr, out eventName, out handlerName, out attachSource))
					events.Add(new EventRegistration {
					           	EventName = eventName,
					           	MethodName = handlerName,
					           	AttachSourceType = attachSource,
					           	IsAttached = true
					           });
			}
			
			return events.ToArray();
		}
		
		bool IsAddAttachedEvent(ILExpression expr, out string eventName, out string handlerName, out string attachSource)
		{
			eventName = "";
			handlerName = "";
			attachSource = "";
			
			if (expr == null || !(expr.Code == ILCode.Callvirt || expr.Code == ILCode.Call))
				return false;
			
			if (expr.Operand is MethodReference && expr.Arguments.Count == 3) {
				var addMethod = expr.Operand as MethodReference;
				if (addMethod.Name != "AddHandler" || addMethod.Parameters.Count != 2)
					return false;
				var arg = expr.Arguments[1];
				if (arg.Code != ILCode.Ldsfld || arg.Arguments.Any() || !(arg.Operand is FieldReference))
					return false;
				FieldReference fldRef = (FieldReference)arg.Operand;
				attachSource = GetAssemblyQualifiedName(fldRef.DeclaringType);
				eventName = fldRef.Name;
				if (eventName.EndsWith("Event") && eventName.Length > "Event".Length)
					eventName = eventName.Remove(eventName.Length - "Event".Length);
				var arg1 = expr.Arguments[2];
				if (arg1.Code != ILCode.Newobj)
					return false;
				var arg2 = arg1.Arguments[1];
				if (arg2.Code != ILCode.Ldftn && arg2.Code != ILCode.Ldvirtftn)
					return false;
				if (arg2.Operand is MethodReference) {
					var m = arg2.Operand as MethodReference;
					handlerName = m.Name;
					return true;
				}
			}
			
			return false;
		}
		
		string GetAssemblyQualifiedName(TypeReference declaringType)
		{
			string fullName = declaringType.FullName;
			
			if (declaringType.Scope is AssemblyNameReference)
				fullName += ", " + ((AssemblyNameReference)declaringType.Scope).FullName;
			else if (declaringType.Scope is ModuleDefinition)
				fullName += ", " + ((ModuleDefinition)declaringType.Scope).Assembly.FullName;
			
			return fullName;
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
				if (arg.Code != ILCode.Newobj || arg.Arguments.Count != 2)
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
