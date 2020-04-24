/*
	Copyright (c) 2015 Ki

	Permission is hereby granted, free of charge, to any person obtaining a copy
	of this software and associated documentation files (the "Software"), to deal
	in the Software without restriction, including without limitation the rights
	to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
	copies of the Software, and to permit persons to whom the Software is
	furnished to do so, subject to the following conditions:

	The above copyright notice and this permission notice shall be included in
	all copies or substantial portions of the Software.

	THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
	IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
	FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
	AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
	LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
	OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
	THE SOFTWARE.
*/

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace Test {
	internal class Program {
		static T GetMember<T>(Func<string, BindingFlags, T> func, string name) =>
			func(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);

		static Assembly GetDeclAssembly(Type type) {
			if (type.IsDefined(typeof(TypeForwardedFromAttribute), false)) {
				var attr = (TypeForwardedFromAttribute)type.GetCustomAttributes(typeof(TypeForwardedFromAttribute), false)[0];
				return Assembly.Load(attr.AssemblyFullName);
			}
			return type.Assembly;
		}

		static void Main(string[] args) {
			var asmName = "PresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
			var assembly = Assembly.Load(asmName);
			var ctx = assembly.GetType("System.Windows.Baml2006.WpfSharedBamlSchemaContext");
			var instance = Activator.CreateInstance(ctx);

			var assemblies = ExtractAssemblies(ctx, instance);
			var types = ExtractTypes(ctx, instance);
			var members = ExtractMembers(ctx, instance);
			var strings = ExtractStrings(ctx, instance);
			var resources = ExtractResources(assembly);

			var code = new StringBuilder();

			foreach (var type in types) {
				if (type is null)
					continue;
				if (!assemblies.Contains(GetDeclAssembly(type)))
					assemblies.Add(GetDeclAssembly(type));
			}

			foreach (var member in members) {
				if (member is null)
					continue;
				if (!assemblies.Contains(GetDeclAssembly(member.Item3)))
					assemblies.Add(GetDeclAssembly(member.Item3));
			}

			code.AppendLine("\tinternal enum KnownTypes : short {");
			code.AppendLine("\t\tUnknown = 0,");
			for (int i = 1; i < types.Count; i++) {
				if (types[i] is null) {
					code.AppendLine();
					continue;
				}
				var line = "\t\t{0} = {1},";
				code.AppendLine(string.Format(line, types[i].Name, i));
			}
			code.AppendLine("\t}").AppendLine();

			code.AppendLine("\tinternal enum KnownMembers : short {");
			code.AppendLine("\t\tUnknown = 0,");
			for (int i = 1; i < members.Count; i++) {
				if (members[i] is null) {
					code.AppendLine();
					continue;
				}
				var line = "\t\t{0}_{1} = {2},";
				code.AppendLine(string.Format(line, members[i].Item1.Name, members[i].Item2, i));
			}
			code.AppendLine("\t}").AppendLine();

			code.AppendLine("\t\tvoid InitAssemblies() {");
			for (int i = 0; i < assemblies.Count; i++) {
				var line = "\t\t\tassemblies[{0}] = ResolveThrow(\"{1}\");";
				code.AppendLine(string.Format(line, i, assemblies[i]));
			}
			code.AppendLine("\t\t}").AppendLine();

			code.AppendLine("\t\tvoid InitTypes() {");
			for (int i = 0; i < types.Count; i++) {
				var type = types[i];
				if (type is null) {
					code.AppendLine();
					continue;
				}
				var line = "\t\t\ttypes[KnownTypes.{0}] = InitType(assemblies[{1}], \"{2}\", \"{3}\");";
				code.AppendLine(string.Format(line, new object[] {
					type.Name,
					assemblies.IndexOf(GetDeclAssembly(type)),
					type.Namespace,
					type.Name
				}));
			}
			code.AppendLine("\t\t}").AppendLine();

			code.AppendLine("\t\tvoid InitMembers() {");
			for (int i = 0; i < members.Count; i++) {
				var member = members[i];
				if (member is null) {
					code.AppendLine();
					continue;
				}
				var line = "\t\t\tmembers[KnownMembers.{0}_{1}] = InitMember(KnownTypes.{0}, \"{1}\", InitType(assemblies[{2}], \"{3}\", \"{4}\"));";
				code.AppendLine(string.Format(line, new object[] {
					member.Item1.Name,
					member.Item2,
					assemblies.IndexOf(GetDeclAssembly(member.Item3)),
					member.Item3.Namespace,
					member.Item3.Name
				}));
			}
			code.AppendLine("\t\t}").AppendLine();

			code.AppendLine("\t\tvoid InitStrings() {");
			for (int i = 0; i < strings.Count; i++) {
				if (strings[i] is null)
					continue;
				var line = "\t\t\tstrings[{0}] = \"{1}\";";
				code.AppendLine(string.Format(line, i, strings[i]));
			}
			code.AppendLine("\t\t}").AppendLine();

			code.AppendLine("\t\tvoid InitResources() {");
			for (int i = 0; i < resources.Count; i++) {
				if (resources[i] is null) {
					code.AppendLine();
					continue;
				}
				var res = resources[i];
				var line = "\t\t\tresources[{0}] = Tuple.Create(\"{1}\", \"{2}\", \"{3}\");";
				code.AppendLine(string.Format(line, i, res.Item1.Name, res.Item2, res.Item3));
			}
			code.AppendLine("\t\t}").AppendLine();

			Console.WriteLine(code);
		}

		static List<Assembly> ExtractAssemblies(Type ctx, object instance) {
			var getAssembly = GetMember(ctx.GetMethod, "GetKnownBamlAssembly");
			var assemblies = (Array)GetMember(ctx.GetField, "_knownBamlAssemblies").GetValue(instance);

			var bamlAssembly = ctx.Assembly.GetType("System.Windows.Baml2006.Baml6Assembly");
			var property = GetMember(bamlAssembly.GetProperty, "Assembly");

			var extract = new List<Assembly>();
			for (int i = 0; i < assemblies.Length; i++) {
				try {
					var asm = getAssembly.Invoke(instance, new object[] { (short)(-i) });
					var asmValue = (Assembly)property.GetValue(asm, null);
					extract.Add(asmValue);
				}
				catch {
					extract.Add(null);
				}
			}
			return extract;
		}

		static List<Type> ExtractTypes(Type ctx, object instance) {
			var getType = GetMember(ctx.GetMethod, "GetKnownBamlType");
			var types = (Array)GetMember(ctx.GetField, "_knownBamlTypes").GetValue(instance);

			var bamlType = ctx.Assembly.GetType("System.Windows.Baml2006.WpfKnownType");
			var field = GetMember(bamlType.GetField, "_underlyingType");

			var extract = new List<Type>() { null };
			for (int i = 1; i < types.Length; i++) {
				try {
					var type = getType.Invoke(instance, new object[] { (short)(-i) });
					var typeValue = (Type)field.GetValue(type);
					extract.Add(typeValue);
				}
				catch {
					extract.Add(null);
				}
			}
			return extract;
		}

		static List<Tuple<Type, string, Type>> ExtractMembers(Type ctx, object instance) {
			var getMember = GetMember(ctx.GetMethod, "GetKnownBamlMember");
			var members = (Array)GetMember(ctx.GetField, "_knownBamlMembers").GetValue(instance);

			var bamlMember = ctx.Assembly.GetType("System.Windows.Baml2006.WpfKnownMember");
			var propertyName = GetMember(bamlMember.GetProperty, "Name");
			var propertyType = GetMember(bamlMember.GetProperty, "Type");

			var bamlType = ctx.Assembly.GetType("System.Windows.Baml2006.WpfKnownType");
			var propertyUnderlyType = GetMember(bamlType.GetProperty, "UnderlyingType");

			var mapTable = ctx.Assembly.GetType("System.Windows.Markup.BamlMapTable");
			var getAttrRecord = mapTable.GetMethod("GetAttributeInfoFromId", BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { typeof(short) }, null);
			var getAttrOwner = GetMember(mapTable.GetMethod, "GetAttributeOwnerType");
			var tbl = mapTable.GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)[0].Invoke(new object[] { null });

			var extract = new List<Tuple<Type, string, Type>>() { null };
			for (int i = 1; i < members.Length; i++) {
				try {
					var member = getMember.Invoke(instance, new object[] { (short)(-i) });
					var name = (string)propertyName.GetValue(member, null);
					var type = (Type)propertyUnderlyType.GetValue(propertyType.GetValue(member, null), null);

					var declType = (Type)getAttrOwner.Invoke(tbl, new object[] { getAttrRecord.Invoke(tbl, new object[] { (short)(-i) }) });

					extract.Add(Tuple.Create(declType, name, type));
				}
				catch {
					extract.Add(null);
				}
			}
			return extract;
		}

		static List<string> ExtractStrings(Type ctx, object instance) {
			var getString = GetMember(ctx.GetMethod, "GetKnownBamlString");

			var extract = new List<string>();
			for (int i = 0; i < 10; i++) {
				try {
					var str = (string)getString.Invoke(instance, new object[] { (short)(-i) });
					extract.Add(str);
				}
				catch {
					extract.Add(null);
				}
			}
			return extract;
		}

		static List<Tuple<Type, string, string>> ExtractResources(Assembly asm) {
			var resIdType = asm.GetType("System.Windows.SystemResourceKeyID");
			var cvrtType = asm.GetType("System.Windows.Markup.SystemKeyConverter");

			var getSysType = GetMember(cvrtType.GetMethod, "GetSystemClassType");
			var getSysKeyName = GetMember(cvrtType.GetMethod, "GetSystemKeyName");
			var getSysPropertyName = GetMember(cvrtType.GetMethod, "GetSystemPropertyName");

			var values = Enum.GetValues(resIdType);

			var extract = new List<Tuple<Type, string, string>>();
			for (int i = 0; i < values.Length; i++) {
				var value = values.GetValue(i);
				if (Enum.GetName(resIdType, value).StartsWith("Internal")) {
					extract.Add(null);
					continue;
				}

				var type = (Type)getSysType.Invoke(null, new object[] { value });
				var keyName = (string)getSysKeyName.Invoke(null, new object[] { value });
				var propName = (string)getSysPropertyName.Invoke(null, new object[] { value });
				extract.Add(Tuple.Create(type, keyName, propName));
			}
			return extract;
		}
	}
}
