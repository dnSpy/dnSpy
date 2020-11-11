/*
    Copyright (C) 2014-2019 de4dot@gmail.com

    This file is part of dnSpy

    dnSpy is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    dnSpy is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with dnSpy.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using dnlib.DotNet.MD;
using DNE = dnlib.DotNet.Emit;

namespace dnSpy.Debugger.DotNet.Metadata.Impl {
	/// <summary>
	/// 
	/// </summary>
	public static class ReflectionTests {
		const bool TESTEXCEPTIONS = false;

		/* CorDebug code:
		1. Change compile action from None to Compile in the file properties in VS
		2. Load some non-loaded asms in dnSpy's StartupClass.Main. This prevents some ResolveExceptions from being thrown
			System.Reflection.Assembly.Load(@"System.Security, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a");
			System.Reflection.Assembly.Load(@"System.Data.SqlXml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");
		3. Add Test(); to the end of DbgEngineImpl.BreakCore()
		4. Add the code below to the same class (DbgEngineImpl)
		5. Debug dnSpy with VS
		6. Debug dnSpy with dnSpy, and when CPU usage is 0%, break the process
		7. Check the Output window, eg.:
			...
			(88/101): System.ObjectModel, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
			(89/101): System.Reflection.Primitives, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
			(90/101): System.Collections.Concurrent, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
			...
		8. If it fails, you'll hit a BP in Verify(). COM MD API doesn't have a way of getting the entry point
		   token so it will fail when testing an exe file.

		sealed class DmdEvaluatorImpl2 : DmdEvaluator {
			public override object Invoke(IDmdEvaluationContext context, DmdMethodBase method, object obj, object[] parameters) => throw new NotImplementedException();
			public override object LoadField(IDmdEvaluationContext context, DmdFieldInfo field, object obj) => throw new NotImplementedException();
			public override void StoreField(IDmdEvaluationContext context, DmdFieldInfo field, object obj, object value) => throw new NotImplementedException();
			public override void Invoke(IDmdEvaluationContext context, DmdMethodBase method, object obj, object[] parameters, Action<object> callback) => throw new NotImplementedException();
			public override void LoadField(IDmdEvaluationContext context, DmdFieldInfo field, object obj, Action<object> callback) => throw new NotImplementedException();
			public override void StoreField(IDmdEvaluationContext context, DmdFieldInfo field, object obj, object value, Action callback) => throw new NotImplementedException();
		}
		sealed class ModuleNameProvider {
			readonly Dictionary<string, string> toName;
			public ModuleNameProvider() {
				toName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
				foreach (ProcessModule module in Process.GetCurrentProcess().Modules) {
					var name = Path.GetFileName(module.FileName);
					if (name.EndsWith(".ni.dll", StringComparison.OrdinalIgnoreCase))
						toName[name.Substring(0, name.Length - ".ni.dll".Length) + ".dll"] = module.FileName;
				}
			}
			public string GetModuleFilename(DbgModule module) {
				if (toName.TryGetValue(Path.GetFileName(module.Filename), out var filename))
					return filename;
				return module.Filename;
			}
		}
		sealed class DmdDataStreamImpl : DmdDataStream {
			readonly DataReader reader;
			public DmdDataStreamImpl(DataReader reader) => this.reader = reader;
			public override long Position { get => reader.Position; set => reader.Position = (uint)value; }
			public override long Length => reader.Length;
			public override byte ReadByte() => reader.ReadByte();
			public override ushort ReadUInt16() => reader.ReadUInt16();
			public override uint ReadUInt32() => reader.ReadUInt32();
			public override ulong ReadUInt64() => reader.ReadUInt64();
			public override float ReadSingle() => reader.ReadSingle();
			public override double ReadDouble() => reader.ReadDouble();
			public override byte[] ReadBytes(int length) => reader.ReadBytes(length);
			public override void Dispose() { }
		}
		void Test() {
			var runtime = DmdRuntimeFactory.CreateRuntime(new DmdEvaluatorImpl2(), IntPtr.Size == 4 ? DmdImageFileMachine.I386 : DmdImageFileMachine.AMD64);
			var dad = TryGetEngineAppDomain(dnDebugger.Processes.First().AppDomains[0]).AppDomain;
			var ad = runtime.CreateAppDomain(dad.Id);
			var moduleNameProvider = new ModuleNameProvider();
			bool useComMD = true;
			useComMD = false;
			var comMdDispatcher = useComMD ? new DmdDispatcherImpl(this) : null;
			foreach (var dmod in dad.Modules) {
				if (!dmod.IsDotNetModule())
					continue;
				if (dmod.IsDynamic || dmod.IsInMemory)
					continue;
				DmdAssembly asm;
				if (useComMD) {
					if (!TryGetModuleData(dmod, out var data))
						throw new InvalidOperationException();
					var mdi = data.DnModule.CorModule.GetMetaDataInterface<dndbg.COM.MetaData.IMetaDataImport>();
					var dynamicModuleHelper = new DmdDynamicModuleHelperImpl(this);
					asm = ad.CreateAssembly(mdi, dynamicModuleHelper, comMdDispatcher, dmod.IsInMemory, dmod.IsDynamic, dmod.Filename, dmod.Filename);
				}
				else
					asm = ad.CreateAssembly(moduleNameProvider.GetModuleFilename(dmod), true, dmod.IsInMemory, dmod.IsDynamic, dmod.Filename, dmod.Filename);
				asm.ManifestModule.GetOrCreateData<DbgModule>(() => dmod);
			}
			// Run it on the UI thread to test the COM MD code
			System.Windows.Application.Current.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background, new Action(() => {
				ReflectionTests.Test(ad);
			}));
		}
		*/

		/// <summary>
		/// 
		/// </summary>
		/// <param name="ad"></param>
		public static void Test(DmdAppDomain ad) {
			{
				var t2 = typeof(Dictionary<,>);
				var t1 = ad.GetType(t2);
				TestTypeSpecs(t1, t2);
				TestTypeSpecs(t1.MakeGenericType(ad.System_String, ad.GetType(typeof(List<IEnumerable<IntPtr>>))), t2.MakeGenericType(typeof(string), typeof(List<IEnumerable<IntPtr>>)));
				TestTypeSpecs(ad.GetType(typeof(Dictionary<,>)).GetGenericArguments()[0], typeof(Dictionary<,>).GetGenericArguments()[0]);
				TestTypeSpecs(ad.GetType(typeof(Nullable<>)).GetGenericArguments()[0], typeof(Nullable<>).GetGenericArguments()[0]);
			}

			{
				var t1 = ad.System_Array;
				var t2 = typeof(Array);
				var m1 = t1.GetMethod("Resize");
				var m2 = t2.GetMethod("Resize");
				Test(m1, m2);
				Test(m1.MakeGenericMethod(ad.System_SByte), m2.MakeGenericMethod(typeof(sbyte)));

				Verify(ad.System_Int32.IsSubclassOf(ad.System_ValueType) == typeof(int).IsSubclassOf(typeof(ValueType)));
				Verify(ad.System_ValueType.IsSubclassOf(ad.System_Int32) == typeof(ValueType).IsSubclassOf(typeof(int)));
				Verify(ad.System_Int32.IsAssignableFrom(ad.System_ValueType) == typeof(int).IsAssignableFrom(typeof(ValueType)));
				Verify(ad.System_ValueType.IsAssignableFrom(ad.System_Int32) == typeof(ValueType).IsAssignableFrom(typeof(int)));
				Verify(ad.System_Int32.IsEquivalentTo(ad.System_ValueType) == typeof(int).IsEquivalentTo(typeof(ValueType)));
				Verify(ad.System_ValueType.IsEquivalentTo(ad.System_Int32) == typeof(ValueType).IsEquivalentTo(typeof(int)));
				Verify(ad.System_Int32.IsEquivalentTo(ad.System_Int32) == typeof(int).IsEquivalentTo(typeof(int)));

				t2 = typeof(List<IntPtr>);
				t1 = ad.GetType(t2);
				{
					SimpleTest(t1.GetMember("CopyTo"), t2.GetMember("CopyTo"));
					var names = new[] {
						"CopyTo",
						"ToString",
						".ctor",
						".cctor",
						"ToArray",
						"EnsureCapacity",
						"Synchronized",
						"IsCompatibleObject",
					};
					foreach (var name in names) {
						SimpleTest(t1.GetMember(name, DmdBindingFlags.Public | DmdBindingFlags.Instance), t2.GetMember(name, BindingFlags.Public | BindingFlags.Instance));
						SimpleTest(t1.GetMember(name, DmdBindingFlags.NonPublic | DmdBindingFlags.Instance), t2.GetMember(name, BindingFlags.NonPublic | BindingFlags.Instance));
						SimpleTest(t1.GetMember(name, DmdBindingFlags.Public | DmdBindingFlags.Static), t2.GetMember(name, BindingFlags.Public | BindingFlags.Static));
						SimpleTest(t1.GetMember(name, DmdBindingFlags.NonPublic | DmdBindingFlags.Static), t2.GetMember(name, BindingFlags.NonPublic | BindingFlags.Static));

						SimpleTest(t1.GetMember(name, DmdMemberTypes.Method, DmdBindingFlags.Public | DmdBindingFlags.Instance), t2.GetMember(name, MemberTypes.Method, BindingFlags.Public | BindingFlags.Instance));
						SimpleTest(t1.GetMember(name, DmdMemberTypes.Method, DmdBindingFlags.NonPublic | DmdBindingFlags.Instance), t2.GetMember(name, MemberTypes.Method, BindingFlags.NonPublic | BindingFlags.Instance));
						SimpleTest(t1.GetMember(name, DmdMemberTypes.Method, DmdBindingFlags.Public | DmdBindingFlags.Static), t2.GetMember(name, MemberTypes.Method, BindingFlags.Public | BindingFlags.Static));
						SimpleTest(t1.GetMember(name, DmdMemberTypes.Method, DmdBindingFlags.NonPublic | DmdBindingFlags.Static), t2.GetMember(name, MemberTypes.Method, BindingFlags.NonPublic | BindingFlags.Static));

						SimpleTest(t1.GetMember(name, DmdMemberTypes.Constructor, DmdBindingFlags.Public | DmdBindingFlags.Instance), t2.GetMember(name, MemberTypes.Constructor, BindingFlags.Public | BindingFlags.Instance));
						SimpleTest(t1.GetMember(name, DmdMemberTypes.Constructor, DmdBindingFlags.NonPublic | DmdBindingFlags.Instance), t2.GetMember(name, MemberTypes.Constructor, BindingFlags.NonPublic | BindingFlags.Instance));
						SimpleTest(t1.GetMember(name, DmdMemberTypes.Constructor, DmdBindingFlags.Public | DmdBindingFlags.Static), t2.GetMember(name, MemberTypes.Constructor, BindingFlags.Public | BindingFlags.Static));
						SimpleTest(t1.GetMember(name, DmdMemberTypes.Constructor, DmdBindingFlags.NonPublic | DmdBindingFlags.Static), t2.GetMember(name, MemberTypes.Constructor, BindingFlags.NonPublic | BindingFlags.Static));

						SimpleTest(t1.GetMember(name, DmdMemberTypes.Method | DmdMemberTypes.Constructor, DmdBindingFlags.Public | DmdBindingFlags.Instance), t2.GetMember(name, MemberTypes.Method | MemberTypes.Constructor, BindingFlags.Public | BindingFlags.Instance));
						SimpleTest(t1.GetMember(name, DmdMemberTypes.Method | DmdMemberTypes.Constructor, DmdBindingFlags.NonPublic | DmdBindingFlags.Instance), t2.GetMember(name, MemberTypes.Method | MemberTypes.Constructor, BindingFlags.NonPublic | BindingFlags.Instance));
						SimpleTest(t1.GetMember(name, DmdMemberTypes.Method | DmdMemberTypes.Constructor, DmdBindingFlags.Public | DmdBindingFlags.Static), t2.GetMember(name, MemberTypes.Method | MemberTypes.Constructor, BindingFlags.Public | BindingFlags.Static));
						SimpleTest(t1.GetMember(name, DmdMemberTypes.Method | DmdMemberTypes.Constructor, DmdBindingFlags.NonPublic | DmdBindingFlags.Static), t2.GetMember(name, MemberTypes.Method | MemberTypes.Constructor, BindingFlags.NonPublic | BindingFlags.Static));

						SimpleTest(t1.GetMember(name, DmdMemberTypes.Method | DmdMemberTypes.Constructor | DmdMemberTypes.Field, DmdBindingFlags.Public | DmdBindingFlags.Instance), t2.GetMember(name, MemberTypes.Method | MemberTypes.Constructor | MemberTypes.Field, BindingFlags.Public | BindingFlags.Instance));
						SimpleTest(t1.GetMember(name, DmdMemberTypes.Method | DmdMemberTypes.Constructor | DmdMemberTypes.Field, DmdBindingFlags.NonPublic | DmdBindingFlags.Instance), t2.GetMember(name, MemberTypes.Method | MemberTypes.Constructor | MemberTypes.Field, BindingFlags.NonPublic | BindingFlags.Instance));
						SimpleTest(t1.GetMember(name, DmdMemberTypes.Method | DmdMemberTypes.Constructor | DmdMemberTypes.Field, DmdBindingFlags.Public | DmdBindingFlags.Static), t2.GetMember(name, MemberTypes.Method | MemberTypes.Constructor | MemberTypes.Field, BindingFlags.Public | BindingFlags.Static));
						SimpleTest(t1.GetMember(name, DmdMemberTypes.Method | DmdMemberTypes.Constructor | DmdMemberTypes.Field, DmdBindingFlags.NonPublic | DmdBindingFlags.Static), t2.GetMember(name, MemberTypes.Method | MemberTypes.Constructor | MemberTypes.Field, BindingFlags.NonPublic | BindingFlags.Static));
					}
				}
				{
					SimpleTest(t1.GetMember("_items"), t2.GetMember("_items"));
					var names = new[] {
						"_defaultCapacity",
						"_items",
						"_size",
						"_emptyArray",
					};
					foreach (var name in names) {
						SimpleTest(t1.GetMember(name, DmdBindingFlags.Public | DmdBindingFlags.Instance), t2.GetMember(name, BindingFlags.Public | BindingFlags.Instance));
						SimpleTest(t1.GetMember(name, DmdBindingFlags.NonPublic | DmdBindingFlags.Instance), t2.GetMember(name, BindingFlags.NonPublic | BindingFlags.Instance));
						SimpleTest(t1.GetMember(name, DmdBindingFlags.Public | DmdBindingFlags.Static), t2.GetMember(name, BindingFlags.Public | BindingFlags.Static));
						SimpleTest(t1.GetMember(name, DmdBindingFlags.NonPublic | DmdBindingFlags.Static), t2.GetMember(name, BindingFlags.NonPublic | BindingFlags.Static));

						SimpleTest(t1.GetMember(name, DmdMemberTypes.Field, DmdBindingFlags.Public | DmdBindingFlags.Instance), t2.GetMember(name, MemberTypes.Field, BindingFlags.Public | BindingFlags.Instance));
						SimpleTest(t1.GetMember(name, DmdMemberTypes.Field, DmdBindingFlags.NonPublic | DmdBindingFlags.Instance), t2.GetMember(name, MemberTypes.Field, BindingFlags.NonPublic | BindingFlags.Instance));
						SimpleTest(t1.GetMember(name, DmdMemberTypes.Field, DmdBindingFlags.Public | DmdBindingFlags.Static), t2.GetMember(name, MemberTypes.Field, BindingFlags.Public | BindingFlags.Static));
						SimpleTest(t1.GetMember(name, DmdMemberTypes.Field, DmdBindingFlags.NonPublic | DmdBindingFlags.Static), t2.GetMember(name, MemberTypes.Field, BindingFlags.NonPublic | BindingFlags.Static));

						SimpleTest(t1.GetMember(name, DmdMemberTypes.Field | DmdMemberTypes.Constructor, DmdBindingFlags.Public | DmdBindingFlags.Instance), t2.GetMember(name, MemberTypes.Field | MemberTypes.Constructor, BindingFlags.Public | BindingFlags.Instance));
						SimpleTest(t1.GetMember(name, DmdMemberTypes.Field | DmdMemberTypes.Constructor, DmdBindingFlags.NonPublic | DmdBindingFlags.Instance), t2.GetMember(name, MemberTypes.Field | MemberTypes.Constructor, BindingFlags.NonPublic | BindingFlags.Instance));
						SimpleTest(t1.GetMember(name, DmdMemberTypes.Field | DmdMemberTypes.Constructor, DmdBindingFlags.Public | DmdBindingFlags.Static), t2.GetMember(name, MemberTypes.Field | MemberTypes.Constructor, BindingFlags.Public | BindingFlags.Static));
						SimpleTest(t1.GetMember(name, DmdMemberTypes.Field | DmdMemberTypes.Constructor, DmdBindingFlags.NonPublic | DmdBindingFlags.Static), t2.GetMember(name, MemberTypes.Field | MemberTypes.Constructor, BindingFlags.NonPublic | BindingFlags.Static));
					}
				}
				{
					SimpleTest(t1.GetMember("Count"), t2.GetMember("Count"));
					var names = new[] {
						"Count",
						"Capacity",
						"Item",
					};
					foreach (var name in names) {
						SimpleTest(t1.GetMember(name, DmdBindingFlags.Public | DmdBindingFlags.Instance), t2.GetMember(name, BindingFlags.Public | BindingFlags.Instance));
						SimpleTest(t1.GetMember(name, DmdBindingFlags.NonPublic | DmdBindingFlags.Instance), t2.GetMember(name, BindingFlags.NonPublic | BindingFlags.Instance));
						SimpleTest(t1.GetMember(name, DmdBindingFlags.Public | DmdBindingFlags.Static), t2.GetMember(name, BindingFlags.Public | BindingFlags.Static));
						SimpleTest(t1.GetMember(name, DmdBindingFlags.NonPublic | DmdBindingFlags.Static), t2.GetMember(name, BindingFlags.NonPublic | BindingFlags.Static));

						SimpleTest(t1.GetMember(name, DmdMemberTypes.Property, DmdBindingFlags.Public | DmdBindingFlags.Instance), t2.GetMember(name, MemberTypes.Property, BindingFlags.Public | BindingFlags.Instance));
						SimpleTest(t1.GetMember(name, DmdMemberTypes.Property, DmdBindingFlags.NonPublic | DmdBindingFlags.Instance), t2.GetMember(name, MemberTypes.Property, BindingFlags.NonPublic | BindingFlags.Instance));
						SimpleTest(t1.GetMember(name, DmdMemberTypes.Property, DmdBindingFlags.Public | DmdBindingFlags.Static), t2.GetMember(name, MemberTypes.Property, BindingFlags.Public | BindingFlags.Static));
						SimpleTest(t1.GetMember(name, DmdMemberTypes.Property, DmdBindingFlags.NonPublic | DmdBindingFlags.Static), t2.GetMember(name, MemberTypes.Property, BindingFlags.NonPublic | BindingFlags.Static));

						SimpleTest(t1.GetMember(name, DmdMemberTypes.Property | DmdMemberTypes.Constructor, DmdBindingFlags.Public | DmdBindingFlags.Instance), t2.GetMember(name, MemberTypes.Property | MemberTypes.Constructor, BindingFlags.Public | BindingFlags.Instance));
						SimpleTest(t1.GetMember(name, DmdMemberTypes.Property | DmdMemberTypes.Constructor, DmdBindingFlags.NonPublic | DmdBindingFlags.Instance), t2.GetMember(name, MemberTypes.Property | MemberTypes.Constructor, BindingFlags.NonPublic | BindingFlags.Instance));
						SimpleTest(t1.GetMember(name, DmdMemberTypes.Property | DmdMemberTypes.Constructor, DmdBindingFlags.Public | DmdBindingFlags.Static), t2.GetMember(name, MemberTypes.Property | MemberTypes.Constructor, BindingFlags.Public | BindingFlags.Static));
						SimpleTest(t1.GetMember(name, DmdMemberTypes.Property | DmdMemberTypes.Constructor, DmdBindingFlags.NonPublic | DmdBindingFlags.Static), t2.GetMember(name, MemberTypes.Property | MemberTypes.Constructor, BindingFlags.NonPublic | BindingFlags.Static));
					}
				}
				{
					SimpleTest(t1.GetMember("Enumerator"), t2.GetMember("Enumerator"));
					var names = new[] {
						"Enumerator",
						"SynchronizedList",
					};
					foreach (var name in names) {
						SimpleTest(t1.GetMember(name, DmdBindingFlags.Public | DmdBindingFlags.Instance), t2.GetMember(name, BindingFlags.Public | BindingFlags.Instance));
						SimpleTest(t1.GetMember(name, DmdBindingFlags.NonPublic | DmdBindingFlags.Instance), t2.GetMember(name, BindingFlags.NonPublic | BindingFlags.Instance));
						SimpleTest(t1.GetMember(name, DmdBindingFlags.Public | DmdBindingFlags.Static), t2.GetMember(name, BindingFlags.Public | BindingFlags.Static));
						SimpleTest(t1.GetMember(name, DmdBindingFlags.NonPublic | DmdBindingFlags.Static), t2.GetMember(name, BindingFlags.NonPublic | BindingFlags.Static));

						SimpleTest(t1.GetMember(name, DmdMemberTypes.NestedType, DmdBindingFlags.Public | DmdBindingFlags.Instance), t2.GetMember(name, MemberTypes.NestedType, BindingFlags.Public | BindingFlags.Instance));
						SimpleTest(t1.GetMember(name, DmdMemberTypes.NestedType, DmdBindingFlags.NonPublic | DmdBindingFlags.Instance), t2.GetMember(name, MemberTypes.NestedType, BindingFlags.NonPublic | BindingFlags.Instance));
						SimpleTest(t1.GetMember(name, DmdMemberTypes.NestedType, DmdBindingFlags.Public | DmdBindingFlags.Static), t2.GetMember(name, MemberTypes.NestedType, BindingFlags.Public | BindingFlags.Static));
						SimpleTest(t1.GetMember(name, DmdMemberTypes.NestedType, DmdBindingFlags.NonPublic | DmdBindingFlags.Static), t2.GetMember(name, MemberTypes.NestedType, BindingFlags.NonPublic | BindingFlags.Static));

						SimpleTest(t1.GetMember(name, DmdMemberTypes.NestedType | DmdMemberTypes.Constructor, DmdBindingFlags.Public | DmdBindingFlags.Instance), t2.GetMember(name, MemberTypes.NestedType | MemberTypes.Constructor, BindingFlags.Public | BindingFlags.Instance));
						SimpleTest(t1.GetMember(name, DmdMemberTypes.NestedType | DmdMemberTypes.Constructor, DmdBindingFlags.NonPublic | DmdBindingFlags.Instance), t2.GetMember(name, MemberTypes.NestedType | MemberTypes.Constructor, BindingFlags.NonPublic | BindingFlags.Instance));
						SimpleTest(t1.GetMember(name, DmdMemberTypes.NestedType | DmdMemberTypes.Constructor, DmdBindingFlags.Public | DmdBindingFlags.Static), t2.GetMember(name, MemberTypes.NestedType | MemberTypes.Constructor, BindingFlags.Public | BindingFlags.Static));
						SimpleTest(t1.GetMember(name, DmdMemberTypes.NestedType | DmdMemberTypes.Constructor, DmdBindingFlags.NonPublic | DmdBindingFlags.Static), t2.GetMember(name, MemberTypes.NestedType | MemberTypes.Constructor, BindingFlags.NonPublic | BindingFlags.Static));

						SimpleTest(t1.GetMember(name, DmdMemberTypes.TypeInfo, DmdBindingFlags.Public | DmdBindingFlags.Instance), t2.GetMember(name, MemberTypes.TypeInfo, BindingFlags.Public | BindingFlags.Instance));
						SimpleTest(t1.GetMember(name, DmdMemberTypes.TypeInfo, DmdBindingFlags.NonPublic | DmdBindingFlags.Instance), t2.GetMember(name, MemberTypes.TypeInfo, BindingFlags.NonPublic | BindingFlags.Instance));
						SimpleTest(t1.GetMember(name, DmdMemberTypes.TypeInfo, DmdBindingFlags.Public | DmdBindingFlags.Static), t2.GetMember(name, MemberTypes.TypeInfo, BindingFlags.Public | BindingFlags.Static));
						SimpleTest(t1.GetMember(name, DmdMemberTypes.TypeInfo, DmdBindingFlags.NonPublic | DmdBindingFlags.Static), t2.GetMember(name, MemberTypes.TypeInfo, BindingFlags.NonPublic | BindingFlags.Static));

						SimpleTest(t1.GetMember(name, DmdMemberTypes.TypeInfo | DmdMemberTypes.Constructor, DmdBindingFlags.Public | DmdBindingFlags.Instance), t2.GetMember(name, MemberTypes.TypeInfo | MemberTypes.Constructor, BindingFlags.Public | BindingFlags.Instance));
						SimpleTest(t1.GetMember(name, DmdMemberTypes.TypeInfo | DmdMemberTypes.Constructor, DmdBindingFlags.NonPublic | DmdBindingFlags.Instance), t2.GetMember(name, MemberTypes.TypeInfo | MemberTypes.Constructor, BindingFlags.NonPublic | BindingFlags.Instance));
						SimpleTest(t1.GetMember(name, DmdMemberTypes.TypeInfo | DmdMemberTypes.Constructor, DmdBindingFlags.Public | DmdBindingFlags.Static), t2.GetMember(name, MemberTypes.TypeInfo | MemberTypes.Constructor, BindingFlags.Public | BindingFlags.Static));
						SimpleTest(t1.GetMember(name, DmdMemberTypes.TypeInfo | DmdMemberTypes.Constructor, DmdBindingFlags.NonPublic | DmdBindingFlags.Static), t2.GetMember(name, MemberTypes.TypeInfo | MemberTypes.Constructor, BindingFlags.NonPublic | BindingFlags.Static));

						SimpleTest(t1.GetMember(name, DmdMemberTypes.NestedType | DmdMemberTypes.TypeInfo, DmdBindingFlags.Public | DmdBindingFlags.Instance), t2.GetMember(name, MemberTypes.NestedType | MemberTypes.TypeInfo, BindingFlags.Public | BindingFlags.Instance));
						SimpleTest(t1.GetMember(name, DmdMemberTypes.NestedType | DmdMemberTypes.TypeInfo, DmdBindingFlags.NonPublic | DmdBindingFlags.Instance), t2.GetMember(name, MemberTypes.NestedType | MemberTypes.TypeInfo, BindingFlags.NonPublic | BindingFlags.Instance));
						SimpleTest(t1.GetMember(name, DmdMemberTypes.NestedType | DmdMemberTypes.TypeInfo, DmdBindingFlags.Public | DmdBindingFlags.Static), t2.GetMember(name, MemberTypes.NestedType | MemberTypes.TypeInfo, BindingFlags.Public | BindingFlags.Static));
						SimpleTest(t1.GetMember(name, DmdMemberTypes.NestedType | DmdMemberTypes.TypeInfo, DmdBindingFlags.NonPublic | DmdBindingFlags.Static), t2.GetMember(name, MemberTypes.NestedType | MemberTypes.TypeInfo, BindingFlags.NonPublic | BindingFlags.Static));

						SimpleTest(t1.GetMember(name, DmdMemberTypes.NestedType | DmdMemberTypes.TypeInfo | DmdMemberTypes.Constructor, DmdBindingFlags.Public | DmdBindingFlags.Instance), t2.GetMember(name, MemberTypes.NestedType | MemberTypes.TypeInfo | MemberTypes.Constructor, BindingFlags.Public | BindingFlags.Instance));
						SimpleTest(t1.GetMember(name, DmdMemberTypes.NestedType | DmdMemberTypes.TypeInfo | DmdMemberTypes.Constructor, DmdBindingFlags.NonPublic | DmdBindingFlags.Instance), t2.GetMember(name, MemberTypes.NestedType | MemberTypes.TypeInfo | MemberTypes.Constructor, BindingFlags.NonPublic | BindingFlags.Instance));
						SimpleTest(t1.GetMember(name, DmdMemberTypes.NestedType | DmdMemberTypes.TypeInfo | DmdMemberTypes.Constructor, DmdBindingFlags.Public | DmdBindingFlags.Static), t2.GetMember(name, MemberTypes.NestedType | MemberTypes.TypeInfo | MemberTypes.Constructor, BindingFlags.Public | BindingFlags.Static));
						SimpleTest(t1.GetMember(name, DmdMemberTypes.NestedType | DmdMemberTypes.TypeInfo | DmdMemberTypes.Constructor, DmdBindingFlags.NonPublic | DmdBindingFlags.Static), t2.GetMember(name, MemberTypes.NestedType | MemberTypes.TypeInfo | MemberTypes.Constructor, BindingFlags.NonPublic | BindingFlags.Static));
					}
				}
			}

			foreach (var asm in ad.GetAssemblies()) {
				if (asm.IsDynamic || asm.IsInMemory)
					continue;
				try {
					Assembly.Load(asm.GetName().ToString());
				}
				catch (System.IO.FileNotFoundException) {
					Assembly.LoadFrom(asm.Location);
				}
			}
			var dict = new Dictionary<string, Assembly>(StringComparer.Ordinal);
			foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
				dict.Add(asm.GetName().ToString(), asm);

			var allAsms = ad.GetAssemblies();
			for (int i = 0; i < allAsms.Length; i++) {
				var asm = allAsms[i];
				Debug.WriteLine($"({i + 1}/{allAsms.Length}): {asm.ToString()}");
				switch (asm.GetName().Name) {
				case "Microsoft.CodeAnalysis.CSharp":
				case "Microsoft.CodeAnalysis.CSharp.Features":
				case "Microsoft.CodeAnalysis.VisualBasic":
				case "Microsoft.CodeAnalysis.VisualBasic.Features":
					// Reflection returns dupe props so ignore these assemblies.
					// Seems like it compares the raw property signatures but it should compare the signatures
					// after replacing generic params with generic args.
					continue;
				}
				Test(asm, dict[asm.GetName().ToString()]);
			}
		}
		static bool Verify(bool result) {
			if (!result)
				System.Diagnostics.Debugger.Break();
			return result;
		}
		const DmdBindingFlags all1a = DmdBindingFlags.Public | DmdBindingFlags.NonPublic | DmdBindingFlags.Instance | DmdBindingFlags.Static;
		const BindingFlags all2a = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
		const DmdBindingFlags all1b = all1a | DmdBindingFlags.FlattenHierarchy;
		const BindingFlags all2b = all2a | BindingFlags.FlattenHierarchy;
		static readonly DmdBindingFlags[] bindingFlags1 = new[] { all1a, all1b };
		static readonly BindingFlags[] bindingFlags2 = new[] { all2a, all2b };
		static void Test(DmdAssembly asm1, Assembly asm2) {
			Verify(asm1.ToString() == asm2.ToString());
			Verify(DmdAssembly.CreateQualifiedName("xyz", "123") == Assembly.CreateQualifiedName("xyz", "123"));
			Test(asm1.GetName(), asm2.GetName(), asm1.Location.IndexOf("GAC", StringComparison.OrdinalIgnoreCase) < 0);
			Verify(asm1.FullName == asm2.FullName);
			//Verify(asm1.Location == asm2.Location);
			Verify(asm1.ImageRuntimeVersion == asm2.ImageRuntimeVersion);
			Verify(asm1.EntryPoint?.ToString() == asm2.EntryPoint?.ToString());
			Verify(asm1.ManifestModule.ToString() == asm2.ManifestModule.ToString());
			Test(FilterOutComObject(asm1.AppDomain, asm1.ExportedTypes.ToArray()), asm2.ExportedTypes.ToArray());
			Test(FilterOutComObject(asm1.AppDomain, asm1.GetExportedTypes()), asm2.GetExportedTypes());
			try {
				Test(FilterOutTransparentProxy(asm1.GetTypes().Skip(1).ToArray()), asm2.GetTypes());
			}
			catch (ReflectionTypeLoadException) {
				Debug.WriteLine($"Couldn't load all types: {asm2}");
				return;
			}
			Test(FilterOutSecurityAttributes(asm1.AppDomain, asm1.GetCustomAttributesData()), asm2.GetCustomAttributesData());
			Test(asm1.GetReferencedAssemblies(), asm2.GetReferencedAssemblies());


			TestAssemblyName();

			var mod1 = asm1.ManifestModule;
			var mod2 = asm2.ManifestModule;
			Test(mod1, mod2);

			for (int rid = 2; ; rid++) {
				var t1 = mod1.ResolveType(0x02000000 + rid, DmdResolveOptions.None);
				Type t2;
				try {
					t2 = mod2.ResolveType(0x02000000 + rid);
				}
				catch (ArgumentOutOfRangeException) {
					t2 = null;
				}
				catch (ArgumentException) {
					continue;
				}
				Verify((t1 is not null) == (t2 is not null));
				if (t1 is null)
					break;
				TestTypes(t1, t2);
			}

		}
		static void TestAssemblyName() {
			var n1 = new DmdAssemblyName();
			var n2 = new AssemblyName();
			Test(n1, n2);

			n1 = new DmdAssemblyName("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");
			n2 = new AssemblyName("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");
			Test(n1, n2);

			n1.ContentType = DmdAssemblyContentType.WindowsRuntime;
			n2.ContentType = AssemblyContentType.WindowsRuntime;
			Verify((int)n1.ContentType == (int)n2.ContentType);
			Verify((int)n1.Flags == (int)n2.Flags);
			n1.ContentType = DmdAssemblyContentType.Default;
			n2.ContentType = AssemblyContentType.Default;
			Verify((int)n1.ContentType == (int)n2.ContentType);
			Verify((int)n1.Flags == (int)n2.Flags);

			n1.ProcessorArchitecture = DmdProcessorArchitecture.IA64;
			n2.ProcessorArchitecture = ProcessorArchitecture.IA64;
			Verify((int)n1.ProcessorArchitecture == (int)n2.ProcessorArchitecture);
			Verify((int)n1.Flags == (int)n2.Flags);
			n1.ProcessorArchitecture = DmdProcessorArchitecture.Arm;
			n2.ProcessorArchitecture = ProcessorArchitecture.Arm;
			Verify((int)n1.ProcessorArchitecture == (int)n2.ProcessorArchitecture);
			Verify((int)n1.Flags == (int)n2.Flags);

			n1.Flags = (DmdAssemblyNameFlags)(-1);
			n2.Flags = (AssemblyNameFlags)(-1);
			Verify((int)n1.Flags == (int)n2.Flags);
			Verify((int)n1.ContentType == (int)n2.ContentType);
			Verify((int)n1.ProcessorArchitecture == (int)n2.ProcessorArchitecture);
			n1.Flags = 0;
			n2.Flags = 0;
			Verify((int)n1.Flags == (int)n2.Flags);
			Verify((int)n1.ContentType == (int)n2.ContentType);
			Verify((int)n1.ProcessorArchitecture == (int)n2.ProcessorArchitecture);
		}

		static DmdType[] FilterOutTransparentProxy(DmdType[] t2) => t2.Where(a => a.FullName != "System.Runtime.Remoting.Proxies.__TransparentProxy").ToArray();
		static DmdType[] FilterOutComObject(DmdAppDomain appDomain, DmdType[] t1) {
			var comObjType = appDomain.GetWellKnownType(DmdWellKnownType.System___ComObject);
			return t1.Where(a => (object)a != comObjType).ToArray();
		}
		static void Test(IDmdAssemblyName n1, AssemblyName n2) => Test(n1, n2, true);
		static void Test(IDmdAssemblyName n1, AssemblyName n2, bool checkProcArch) {
			Verify(n1.ToString() == n2.ToString());
			Verify(n1.Name == n2.Name);
			Verify(n1.Version == n2.Version);
			Verify(n1.CultureName == n2.CultureName);
			Verify((int)n1.Flags == (int)n2.Flags);
			if (checkProcArch)
				Verify((int)n1.ProcessorArchitecture == (int)n2.ProcessorArchitecture);
			Verify((int)n1.ContentType == (int)n2.ContentType);
			Verify(Equals(n1.GetPublicKey(), n2.GetPublicKey()));
			Verify(Equals(n1.GetPublicKeyToken(), n2.GetPublicKeyToken()));
			Verify((int)n1.HashAlgorithm == (int)n2.HashAlgorithm);
			Verify(n1.FullName == n2.FullName);
		}
		static bool Equals(byte[] a, byte[] b) {
			if (a is null && b is null)
				return true;
			if (a is null || b is null)
				return false;
			if (a.Length != b.Length)
				return false;
			for (int i = 0; i < a.Length; i++) {
				if (a[i] != b[i])
					return false;
			}
			return true;
		}
		static void Test(DmdModule m1, Module m2) {
			Verify(m1.ToString() == m2.ToString());
			//Verify(m1.FullyQualifiedName == m2.FullyQualifiedName);
			try {
				Test(FilterOutTransparentProxy(m1.GetTypes().Skip(1).ToArray()), m2.GetTypes());
			}
			catch (ReflectionTypeLoadException) {
				Debug.WriteLine($"Couldn't load all types: {m2}");
				return;
			}
			Verify(m1.ModuleVersionId == m2.ModuleVersionId);
			Verify(m1.MetadataToken == m2.MetadataToken);
			Verify(m1.MDStreamVersion == m2.MDStreamVersion);
			Verify(m1.ScopeName == m2.ScopeName);
			Verify(m1.Name == m2.Name);
			Verify(m1.Assembly.GetName().ToString() == m2.Assembly.GetName().ToString());
			Test(m1.GetCustomAttributesData(), m2.GetCustomAttributesData());
			m1.GetPEKind(out var peKind1, out var machine1);
			m2.GetPEKind(out var peKind2, out var machine2);
			if (m1.FullyQualifiedName.IndexOf("GAC", StringComparison.OrdinalIgnoreCase) < 0)
				Verify((int)peKind1 == (int)peKind2);
			Verify((int)machine1 == (int)machine2);
			for (int i = 0; i < bindingFlags1.Length; i++) {
				Test(m1.GetFields(bindingFlags1[i]), m2.GetFields(bindingFlags2[i]));
				Test(m1.GetMethods(bindingFlags1[i]), m2.GetMethods(bindingFlags2[i]));
			}
		}
		static void Test(IList<DmdType> t1, Type[] t2) {
			if (!Verify(t1.Count == t2.Length))
				return;
			for (int i = 0; i < t2.Length; i++) {
				var t2t = t2[i];
				if (t2t is not null)
					SimpleTest(t1[i], t2t);
			}
		}
		static void Test(IDmdAssemblyName[] n1, AssemblyName[] n2) {
			if (!Verify(n1.Length == n2.Length))
				return;
			for (int i = 0; i < n1.Length; i++)
				Test(n1[i], n2[i]);
		}
		static void TestModule(DmdModule m1, Module m2) {
			Verify(m1.Assembly.GetName().ToString() == m2.Assembly.GetName().ToString());
			Verify(m1.ScopeName == m2.ScopeName);
		}
		static void TestMember(DmdMemberInfo m1, MemberInfo m2) {
			var m1s = m1.ToString();
			bool b = m1s == m2.ToString();
			// FnPtr is sometimes written as IntPtr, and other times as (fnptr)
			if (b || !m1s.Contains("*"))
				Verify(b);

			Verify((int)m1.MemberType == (int)m2.MemberType);
			Verify(m1.Name == m2.Name);
			bool isGlobal = (m2.MemberType == MemberTypes.Field || m2.MemberType == MemberTypes.Method) && m2.DeclaringType is null;
			if (isGlobal) {
				Verify((m1.DeclaringType is not null) == (m2.DeclaringType is null));
				Verify((m1.ReflectedType is not null) == (m2.ReflectedType is null));
			}
			else {
				Verify((m1.DeclaringType is not null) == (m2.DeclaringType is not null));
				if (m1.DeclaringType is not null)
					SimpleTest(m1.DeclaringType, m2.DeclaringType);
				Verify((m1.ReflectedType is not null) == (m2.ReflectedType is not null));
				if (m1.ReflectedType is not null)
					SimpleTest(m1.ReflectedType, m2.ReflectedType);
			}
			Verify(m1.MetadataToken == m2.MetadataToken);
			TestModule(m1.Module, m2.Module);
			Test(m1.MemberType == DmdMemberTypes.Constructor ? FilterOutSecurityAttributes(m1.AppDomain, m1.GetCustomAttributesData()) : m1.GetCustomAttributesData(), m2.GetCustomAttributesData());
		}
		static IList<DmdCustomAttributeData> FilterOutSecurityAttributes(DmdAppDomain appDomain, IEnumerable<DmdCustomAttributeData> c) {
			var saType = appDomain.GetWellKnownType(DmdWellKnownType.System_Security_Permissions_SecurityAttribute);
			return c.Where(a => !saType.IsAssignableFrom(a.AttributeType)).ToArray();
		}
		static void TestSameException<T1, T2>(Func<T1> func1, Func<T2> func2, Func<T1, T2, bool> compare) {
			Exception ex1, ex2;
			T1 t1;
			T2 t2;
			try {
				t1 = func1();
				ex1 = null;
			}
			catch (Exception ex) {
				ex1 = ex;
				t1 = default(T1);
			}
			try {
				t2 = func2();
				ex2 = null;
			}
			catch (Exception ex) {
				ex2 = ex;
				t2 = default(T2);
			}
			if (Verify((ex1 is not null) == (ex2 is not null))) {
				if (ex1 is not null)
					Verify(ex1.GetType() == ex2.GetType());
				else
					Verify(compare(t1, t2));
			}
		}
		static void TestTypes(DmdType t1, Type t2) {
			try {
				TestTypesCore(t1, t2);
			}
			catch (TypeResolveException trex) {
				Debug.WriteLine($"{t2}: Couldn't resolve a type: {trex.Type}");
			}
		}
		static void TestTypesCore(DmdType t1, Type t2) {
			TestMember(t1, t2);
			Verify((int)t1.MemberType == (int)t2.MemberType);
			if (TESTEXCEPTIONS || t1.IsGenericParameter)
				TestSameException(() => t1.DeclaringMethod, () => t2.DeclaringMethod, (a, b) => a?.ToString() == b?.ToString());
			Verify(t1.Assembly.ToString() == t2.Assembly.ToString());
			Verify(t1.FullName == t2.FullName);
			Verify(t1.Namespace == t2.Namespace);
			Verify(t1.AssemblyQualifiedName == t2.AssemblyQualifiedName);
			TestBaseTypes(t1, t2);
			Test(t1.StructLayoutAttribute, t2.StructLayoutAttribute);
			Verify(t1.IsNested == t2.IsNested);
			if (TESTEXCEPTIONS || t1.IsGenericParameter)
				TestSameException(() => t1.GenericParameterAttributes, () => t2.GenericParameterAttributes, (a, b) => (int)a == (int)b);
			if (t1 != t1.AppDomain.GetWellKnownType(DmdWellKnownType.System___ComObject))
				Verify(t1.IsVisible == t2.IsVisible);
			Verify((int)t1.Attributes == (int)t2.Attributes);
			Verify(t1.IsNotPublic == t2.IsNotPublic);
			Verify(t1.IsPublic == t2.IsPublic);
			Verify(t1.IsNestedPublic == t2.IsNestedPublic);
			Verify(t1.IsNestedPrivate == t2.IsNestedPrivate);
			Verify(t1.IsNestedFamily == t2.IsNestedFamily);
			Verify(t1.IsNestedAssembly == t2.IsNestedAssembly);
			Verify(t1.IsNestedFamANDAssem == t2.IsNestedFamANDAssem);
			Verify(t1.IsNestedFamORAssem == t2.IsNestedFamORAssem);
			Verify(t1.IsAutoLayout == t2.IsAutoLayout);
			Verify(t1.IsLayoutSequential == t2.IsLayoutSequential);
			Verify(t1.IsExplicitLayout == t2.IsExplicitLayout);
			Verify(t1.IsClass == t2.IsClass);
			Verify(t1.IsInterface == t2.IsInterface);
			Verify(t1.IsValueType == t2.IsValueType);
			Verify(t1.IsAbstract == t2.IsAbstract);
			Verify(t1.IsSealed == t2.IsSealed);
			Verify(t1.IsEnum == t2.IsEnum);
			Verify(t1.IsSpecialName == t2.IsSpecialName);
			Verify(t1.IsImport == t2.IsImport);
			Verify(t1.IsAnsiClass == t2.IsAnsiClass);
			Verify(t1.IsUnicodeClass == t2.IsUnicodeClass);
			Verify(t1.IsAutoClass == t2.IsAutoClass);
			Verify(t1.IsSerializable == t2.IsSerializable);
			Verify(DmdType.GetTypeCode(t1) == Type.GetTypeCode(t2));
			Verify(t1.TypeInitializer?.ToString() == t2.TypeInitializer?.ToString());
			Verify(t1.IsArray == t2.IsArray);
			Verify(t1.IsGenericType == t2.IsGenericType);
			Verify(t1.IsGenericTypeDefinition == t2.IsGenericTypeDefinition);
			Verify(t1.IsConstructedGenericType == t2.IsConstructedGenericType);
			Verify(t1.IsGenericParameter == t2.IsGenericParameter);
			if (TESTEXCEPTIONS || t1.IsGenericParameter)
				TestSameException(() => t1.GenericParameterPosition, () => t2.GenericParameterPosition, (a, b) => a == b);
			Verify(t1.ContainsGenericParameters == t2.ContainsGenericParameters);
			Verify(t1.IsByRef == t2.IsByRef);
			Verify(t1.IsPointer == t2.IsPointer);
			Verify(t1.IsPrimitive == t2.IsPrimitive);
			Verify(t1.HasElementType == t2.HasElementType);
			Verify(t1.IsCOMObject == t2.IsCOMObject);
			Verify(t1.IsContextful == t2.IsContextful);
			Verify(t1.IsMarshalByRef == t2.IsMarshalByRef);
			VerifyTypes(t1.GenericTypeArguments, t2.GenericTypeArguments);
			Test(t1.GetInterfaces(), t2.GetInterfaces());
			if (TESTEXCEPTIONS || t1.IsArray)
				TestSameException(() => t1.GetArrayRank(), () => t2.GetArrayRank(), (a, b) => a == b);
			if (TESTEXCEPTIONS || t1.IsGenericParameter)
				TestSameException(() => t1.GetGenericParameterConstraints(), () => t2.GetGenericParameterConstraints(), (a, b) => { Test(a, b); return true; });
			SimpleTest(t1.GetElementType(), t2.GetElementType());
			Test(t1.GetGenericArguments(), t2.GetGenericArguments());
			Test(t1.GenericTypeArguments, t2.GenericTypeArguments);
			if (TESTEXCEPTIONS || t1.IsGenericType)
				TestSameException(() => t1.GetGenericTypeDefinition(), () => t2.GetGenericTypeDefinition(), (a, b) => { SimpleTest(a, b); return true; });
			if (TESTEXCEPTIONS || t1.IsEnum)
				TestSameException(() => t1.GetEnumNames(), () => t2.GetEnumNames(), (a, b) => EqualsIgnoreOrder(a, b));
			if (TESTEXCEPTIONS || t1.IsEnum)
				TestSameException(() => t1.GetEnumUnderlyingType(), () => t2.GetEnumUnderlyingType(), (a, b) => { SimpleTest(a, b); return true; });
			for (int i = 0; i < bindingFlags1.Length; i++) {
				Test(t1.GetFields(bindingFlags1[i]), t2.GetFields(bindingFlags2[i]));
				Test(t1.GetConstructors(bindingFlags1[i]), t2.GetConstructors(bindingFlags2[i]));
				Test(t1.GetMethods(bindingFlags1[i]), t2.GetMethods(bindingFlags2[i]));
				Test(t1, t1.GetProperties(bindingFlags1[i]), t2.GetProperties(bindingFlags2[i]));
				Test(t1.GetEvents(bindingFlags1[i]), t2.GetEvents(bindingFlags2[i]));
				Test(t1.GetNestedTypes(bindingFlags1[i]), Sort2(t2.GetNestedTypes(bindingFlags2[i])));
				SimpleTest(t1.GetMembers(bindingFlags1[i]), t2.GetMembers(bindingFlags2[i]));
			}
			SimpleTest(t1.GetConstructors(), t2.GetConstructors());
			SimpleTest(t1.GetMethods(), t2.GetMethods());
			SimpleTest(t1.GetFields(), t2.GetFields());
			SimpleTest(t1.GetEvents(), t2.GetEvents());
			SimpleTest(t1.GetProperties(), t2.GetProperties());
			SimpleTest(t1.GetNestedTypes(), t2.GetNestedTypes());
			SimpleTest(t1.GetMembers(), t2.GetMembers());
			SimpleTest(t1.GetDefaultMembers(), t2.GetDefaultMembers());
		}
		static bool EqualsIgnoreOrder(string[] a, string[] b) {
			if (!Verify(a.Length == b.Length))
				return false;
			Array.Sort(a, StringComparer.Ordinal);
			Array.Sort(b, StringComparer.Ordinal);
			for (int i = 0; i < a.Length; i++) {
				if (a[i] != b[i])
					return false;
			}
			return true;
		}
		static void SimpleTest(DmdMemberInfo[] m1, MemberInfo[] m2) {
			if (!Verify(m1.Length == m2.Length))
				return;
			Verify(
				m1.GetType().GetElementType() is var m1t &&
				m2.GetType().GetElementType() is var m2t &&
				(
					(m1t == typeof(DmdMemberInfo) && m2t == typeof(MemberInfo)) ||
					(m1t == typeof(DmdType) && m2t == typeof(Type)) ||
					(m1t == typeof(DmdMethodBase) && m2t == typeof(MethodBase)) ||
					(m1t == typeof(DmdMethodInfo) && m2t == typeof(MethodInfo)) ||
					(m1t == typeof(DmdConstructorInfo) && m2t == typeof(ConstructorInfo)) ||
					(m1t == typeof(DmdFieldInfo) && m2t == typeof(FieldInfo)) ||
					(m1t == typeof(DmdPropertyInfo) && m2t == typeof(PropertyInfo)) ||
					(m1t == typeof(DmdEventInfo) && m2t == typeof(EventInfo))
				)
			);
			Sort1(m1);
			Sort2(m2);
			for (int i = 0; i < m1.Length; i++) {
				var a = m1[i];
				var b = m2[i];
				if (!Verify((int)a.MemberType == (int)b.MemberType))
					continue;
				switch (a.MemberType) {
				case DmdMemberTypes.Constructor:
				case DmdMemberTypes.Method:
				case DmdMemberTypes.Event:
				case DmdMemberTypes.Field:
				case DmdMemberTypes.Property:
					SimpleTest(a, b);
					break;
				case DmdMemberTypes.TypeInfo:
				case DmdMemberTypes.NestedType:
					SimpleTest((DmdType)a, (Type)b);
					break;
				default:
					Verify(false);
					break;
				}
			}
		}
		static void VerifyTypes(IList<DmdType> t1, IList<Type> t2) {
			if (!Verify(t1.Count == t2.Count))
				return;
			for (int i = 0; i < t1.Count; i++) {
				var a = t1[i];
				var b = t2[i];
				if (!Verify(a.AssemblyQualifiedName == b.AssemblyQualifiedName ||
					(a.IsMetadataReference && a.ResolveNoThrow() is null && a.FullName == b.FullName)))
					break;
			}
		}
		static void Test(StructLayoutAttribute a, StructLayoutAttribute b) {
			if (!Verify((a is null) == (b is null)))
				return;
			if (a is null)
				return;
			Verify(a.Pack == b.Pack);
			Verify(a.Size == b.Size);
			Verify(a.CharSet == b.CharSet);
			Verify(a.Value == b.Value);
		}
		static void TestBaseTypes(DmdType t1, Type t2) {
			for (;;) {
				DmdType bt1;
				try {
					bt1 = t1.BaseType;
				}
				catch (ResolveException) {
					return;
				}
				var bt2 = t2.BaseType;
				SimpleTest(bt1, bt2);
				Verify((bt1 is null) == (bt2 is null));
				if (bt1 is not null) {
					if (!Verify(bt1.AssemblyQualifiedName == bt2.AssemblyQualifiedName ||
								(bt1.IsMetadataReference && bt1.ResolveNoThrow() is null && bt1.FullName == bt2.FullName)))
						break;
				}
				if (bt1 is null)
					break;
				t1 = bt1;
				t2 = bt2;
			}
		}
		static T[] Sort1<T>(T[] array) where T : DmdMemberInfo {
			Array.Sort(array, (a, b) => {
				var c = a.MetadataToken.CompareTo(b.MetadataToken);
				if (c != 0)
					return c;
				return StringComparer.Ordinal.Compare(a.Name, b.Name);
			});
			return array;
		}
		static T[] Sort2<T>(T[] array) where T : MemberInfo {
			Array.Sort(array, (a, b) => {
				var c = a.MetadataToken.CompareTo(b.MetadataToken);
				if (c != 0)
					return c;
				return StringComparer.Ordinal.Compare(a.Name, b.Name);
			});
			return array;
		}
		static void Test(DmdFieldInfo[] f1, FieldInfo[] f2) {
			if (!Verify(f1.Length == f2.Length))
				return;
			Sort1(f1);
			Sort2(f2);
			for (int i = 0; i < f1.Length; i++)
				Test(f1[i], f2[i]);
		}
		static void Test(DmdMethodBase[] m1, MethodBase[] m2) {
			if (!Verify(m1.Length == m2.Length))
				return;
			Sort1(m1);
			Sort2(m2);
			for (int i = 0; i < m1.Length; i++)
				Test(m1[i], m2[i]);
		}
		static void Test(DmdType t1, DmdPropertyInfo[] p1, PropertyInfo[] p2) {
			if (!Verify(p1.Length == p2.Length))
				return;
			Sort1(p1);
			Sort2(p2);
			for (int i = 0; i < p1.Length; i++)
				Test(p1[i], p2[i]);
		}
		static void Test(DmdEventInfo[] e1, EventInfo[] e2) {
			if (!Verify(e1.Length == e2.Length))
				return;
			Sort1(e1);
			Sort2(e2);
			for (int i = 0; i < e1.Length; i++)
				Test(e1[i], e2[i]);
		}
		static void Test(DmdFieldInfo f1, FieldInfo f2) {
			TestMember(f1, f2);
			Test(f1.FieldType, f2.FieldType, f2.DeclaringType);
			Verify((int)f1.Attributes == (int)f2.Attributes);
			Verify(f1.IsPublic == f2.IsPublic);
			Verify(f1.IsPrivate == f2.IsPrivate);
			Verify(f1.IsFamily == f2.IsFamily);
			Verify(f1.IsAssembly == f2.IsAssembly);
			Verify(f1.IsFamilyAndAssembly == f2.IsFamilyAndAssembly);
			Verify(f1.IsFamilyOrAssembly == f2.IsFamilyOrAssembly);
			Verify(f1.IsStatic == f2.IsStatic);
			Verify(f1.IsInitOnly == f2.IsInitOnly);
			Verify(f1.IsLiteral == f2.IsLiteral);
			Verify(f1.IsNotSerialized == f2.IsNotSerialized);
			Verify(f1.IsSpecialName == f2.IsSpecialName);
			Verify(f1.IsPinvokeImpl == f2.IsPinvokeImpl);

			if (TESTEXCEPTIONS || (f1.HasDefault && f2.GetType().FullName == "System.Reflection.MdFieldInfo")) {
				object f2Cons = null;
				bool ok = true;
				try {
					f2Cons = f2.GetRawConstantValue();
				}
				catch (InvalidOperationException) {
					ok = false;
				}
				if (ok)
					Verify(Equals(f1.GetRawConstantValue(), f2Cons));
			}

			Test(f1.GetRequiredCustomModifiers(), f2.GetRequiredCustomModifiers());
			Test(f1.GetOptionalCustomModifiers(), f2.GetOptionalCustomModifiers());
		}
		static void Test(DmdMethodBase m1, MethodBase m2) {
			TestMember(m1, m2);
			Verify((int)m1.Attributes == (int)m2.Attributes);
			Verify((int)m1.MethodImplementationFlags == (int)m2.MethodImplementationFlags);
			Verify((int)m1.CallingConvention == (int)m2.CallingConvention);
			Verify(m1.IsGenericMethodDefinition == m2.IsGenericMethodDefinition);
			Verify(m1.IsGenericMethod == m2.IsGenericMethod);
			Verify(m1.ContainsGenericParameters == m2.ContainsGenericParameters);
			Verify(m1.IsPublic == m2.IsPublic);
			Verify(m1.IsPrivate == m2.IsPrivate);
			Verify(m1.IsFamily == m2.IsFamily);
			Verify(m1.IsAssembly == m2.IsAssembly);
			Verify(m1.IsFamilyAndAssembly == m2.IsFamilyAndAssembly);
			Verify(m1.IsFamilyOrAssembly == m2.IsFamilyOrAssembly);
			Verify(m1.IsStatic == m2.IsStatic);
			Verify(m1.IsFinal == m2.IsFinal);
			Verify(m1.IsVirtual == m2.IsVirtual);
			Verify(m1.IsHideBySig == m2.IsHideBySig);
			Verify(m1.IsAbstract == m2.IsAbstract);
			Verify(m1.IsSpecialName == m2.IsSpecialName);
			Verify(m1.IsConstructor == m2.IsConstructor);
			Test(m1.GetParameters(), m2.GetParameters());
			if (TESTEXCEPTIONS || m1 is DmdMethodInfo)
				TestSameException(() => m1.GetGenericArguments(), () => m2.GetGenericArguments(), (a, b) => { Test(a, b); return true; });
			Test(m1, m2, m1.GetMethodBody(), m2.GetMethodBody(), m2.DeclaringType);
			if (m1 is DmdMethodInfo) {
				var mi1 = (DmdMethodInfo)m1;
				var mi2 = (MethodInfo)m2;
				Test(mi1.ReturnParameter, mi2.ReturnParameter);
				Test(mi1.ReturnType, mi2.ReturnType, mi2.DeclaringType);
				Verify(((object)mi1.ReturnTypeCustomAttributes == mi1.ReturnParameter) == ((object)mi2.ReturnTypeCustomAttributes == mi2.ReturnParameter));
				SimpleTest(mi1.GetBaseDefinition(), mi2.GetBaseDefinition());
				if (TESTEXCEPTIONS || m1.IsGenericMethod)
					TestSameException(() => mi1.GetGenericMethodDefinition(), () => mi2.GetGenericMethodDefinition(), (a, b) => { SimpleTest(a, b); return true; });
			}
		}
		static void Test(DmdMethodBase m1, MethodBase m2, DmdMethodBody b1, MethodBody b2, Type declaringType2) {
			if (!Verify((b1 is null) == (b2 is null)))
				return;
			if (b1 is null)
				return;
			Verify((b1.ToString() == b1.GetType().ToString()) == (b2.ToString() == b2.GetType().ToString()));
			Verify(b1.LocalSignatureMetadataToken == b2.LocalSignatureMetadataToken);
			Test(b1.LocalVariables, b2.LocalVariables, declaringType2);
			Verify(b1.MaxStackSize == b2.MaxStackSize);
			Verify(b1.InitLocals == b2.InitLocals);
			TestInstructions(m1, m2, b1.GetILAsByteArray(), b2.GetILAsByteArray());
			Test(b1.ExceptionHandlingClauses, b2.ExceptionHandlingClauses);
		}
		static void TestInstructions(DmdMethodBase m1, MethodBase m2, byte[] a1, byte[] a2) {
			if (!Equals(a1, a2))
				return;
			var module1 = m1.Module;
			var module2 = m2.Module;
			IList<DmdType> gta1, gma1;
			Type[] gta2, gma2;
			gta1 = m1.DeclaringType.GetGenericArguments();
			gta2 = m2.DeclaringType?.GetGenericArguments().ToArray();
			if (m1 is DmdMethodInfo mi1)
				gma1 = mi1.GetGenericArguments();
			else
				gma1 = null;
			if (m2 is MethodInfo mi2)
				gma2 = mi2.GetGenericArguments().ToArray();
			else
				gma2 = null;
			int pos = 0;
			int token;
			while (pos < a1.Length) {
				var opc = ReadOpCode(a1, ref pos);
				switch (opc.OperandType) {
				case DNE.OperandType.InlineNone:
				case DNE.OperandType.InlinePhi:
					break;

				case DNE.OperandType.ShortInlineVar:
				case DNE.OperandType.ShortInlineBrTarget:
				case DNE.OperandType.ShortInlineI:
					pos++;
					break;

				case DNE.OperandType.InlineVar:
					pos += 2;
					break;

				case DNE.OperandType.InlineBrTarget:
				case DNE.OperandType.InlineI:
				case DNE.OperandType.ShortInlineR:
					pos += 4;
					break;

				case DNE.OperandType.InlineI8:
				case DNE.OperandType.InlineR:
					pos += 8;
					break;

				case DNE.OperandType.InlineSwitch:
					int count = BitConverter.ToInt32(a1, pos);
					pos += 4 + count * 4;
					break;

				case DNE.OperandType.InlineString:
					token = BitConverter.ToInt32(a1, pos);
					pos += 4;
					Verify(module1.ResolveString(token) == module2.ResolveString(token));
					break;

				case DNE.OperandType.InlineType:
				case DNE.OperandType.InlineField:
				case DNE.OperandType.InlineMethod:
				case DNE.OperandType.InlineTok:
					token = BitConverter.ToInt32(a1, pos);
					pos += 4;
					switch ((Table)(token >> 24)) {
					case Table.TypeRef:
					case Table.TypeDef:
					case Table.TypeSpec:
						try {
							var t1 = module1.ResolveType(token, gta1, gma1);
							var t2 = module2.ResolveType(token, gta2, gma2);
							FixTypes(ref t1, t2, m2.DeclaringType);
							SimpleTest(t1, t2);
						}
						catch (ResolveException) {
						}
						catch (FileLoadException) {
						}
						break;

					case Table.Field:
					case Table.Method:
					case Table.MethodSpec:
					case Table.MemberRef:
						try {
							var memb1 = module1.ResolveMember(token, gta1, gma1);
							var memb2 = module2.ResolveMember(token, gta2, gma2);
							FixReflectedType(ref memb1, memb2);
							SimpleTest(memb1, memb2);
						}
						catch (ResolveException) {
						}
						catch (FileLoadException) {
						}
						break;
					}
					break;

				case DNE.OperandType.InlineSig:
					token = BitConverter.ToInt32(a1, pos);
					pos += 4;
					try {
						var s1 = module1.ResolveSignature(token);
						var s2 = module2.ResolveSignature(token);
						Verify(Equals(s1, s2));
					}
					catch (ResolveException) {
					}
					catch (FileLoadException) {
					}
					break;

				default: throw new InvalidOperationException("Invalid OpCode.OperandType");
				}
			}
		}
		static void FixReflectedType(ref DmdMemberInfo memb1, MemberInfo memb2) {
			if (memb2.ReflectedType != memb2.DeclaringType)
				return;
			if (memb1.ReflectedType == memb1.DeclaringType)
				return;
			if (memb1 is DmdMethodBase && memb2 is MethodBase) {
				memb1 = memb1.DeclaringType.GetMethod(memb1.DeclaringType.Module, memb1.MetadataToken, throwOnError: true);
				return;
			}
			if (memb1 is DmdFieldInfo && memb2 is FieldInfo) {
				memb1 = memb1.DeclaringType.GetField(memb1.DeclaringType.Module, memb1.MetadataToken, throwOnError: true);
				return;
			}
		}
		static DNE.OpCode ReadOpCode(byte[] a, ref int pos) {
			var op = a[pos++];
			if (op != 0xFE)
				return DNE.OpCodes.OneByteOpCodes[op];
			return DNE.OpCodes.TwoByteOpCodes[a[pos++]];
		}
		static void Test(IList<DmdLocalVariableInfo> l1, IList<LocalVariableInfo> l2, Type declaringType2) {
			Verify(l1 is ReadOnlyCollection<DmdLocalVariableInfo>);
			Verify(l2 is ReadOnlyCollection<LocalVariableInfo>);
			if (!Verify(l1.Count == l2.Count))
				return;
			for (int i = 0; i < l1.Count; i++)
				Test(l1[i], l2[i], declaringType2);
		}
		static void Test(DmdLocalVariableInfo l1, LocalVariableInfo l2, Type declaringType2) {
			var l1s = l1.ToString();
			Verify(l1s == l2.ToString() || l1s.Contains("*"));
			Test(l1.LocalType, l2.LocalType, declaringType2);
			Verify(l1.IsPinned == l2.IsPinned);
			Verify(l1.LocalIndex == l2.LocalIndex);
		}
		static void Test(IList<DmdExceptionHandlingClause> e1, IList<ExceptionHandlingClause> e2) {
			Verify(e1 is ReadOnlyCollection<DmdExceptionHandlingClause>);
			Verify(e2 is ReadOnlyCollection<ExceptionHandlingClause>);
			if (!Verify(e1.Count == e2.Count))
				return;
			for (int i = 0; i < e1.Count; i++)
				Test(e1[i], e2[i]);
		}
		static void Test(DmdExceptionHandlingClause e1, ExceptionHandlingClause e2) {
			Verify(e1.ToString() == e2.ToString());
			Verify((int)e1.Flags == (int)e2.Flags);
			Verify(e1.TryOffset == e2.TryOffset);
			Verify(e1.TryLength == e2.TryLength);
			Verify(e1.HandlerOffset == e2.HandlerOffset);
			Verify(e1.HandlerLength == e2.HandlerLength);
			if (TESTEXCEPTIONS || e1.Flags == DmdExceptionHandlingClauseOptions.Filter)
				TestSameException(() => e1.FilterOffset, () => e2.FilterOffset, (a, b) => a == b);
			if (TESTEXCEPTIONS || e1.Flags == DmdExceptionHandlingClauseOptions.Clause)
				TestSameException(() => e1.CatchType, () => e2.CatchType, (a, b) => { SimpleTest(a, b); return true; });
		}
		static void Test(DmdPropertyInfo p1, PropertyInfo p2) {
			TestMember(p1, p2);
			Test(p1.PropertyType, p2.PropertyType, p2.DeclaringType);
			Verify((int)p1.Attributes == (int)p2.Attributes);
			Verify(p1.IsSpecialName == p2.IsSpecialName);
			Verify(p1.CanRead == p2.CanRead);
			Verify(p1.CanWrite == p2.CanWrite);

			if (TESTEXCEPTIONS || p1.HasDefault) {
				object f2Cons = null;
				bool ok = true;
				try {
					f2Cons = p2.GetRawConstantValue();
				}
				catch (InvalidOperationException) {
					ok = false;
				}
				if (ok)
					Verify(Equals(p1.GetRawConstantValue(), f2Cons));
			}

			SimpleTest(p1.GetAccessors(false), p2.GetAccessors(false));
			SimpleTest(p1.GetAccessors(true), p2.GetAccessors(true));
			SimpleTest(p1.GetGetMethod(false), p2.GetGetMethod(false));
			SimpleTest(p1.GetGetMethod(true), p2.GetGetMethod(true));
			SimpleTest(p1.GetSetMethod(false), p2.GetSetMethod(false));
			SimpleTest(p1.GetSetMethod(true), p2.GetSetMethod(true));
			Test(p1.GetIndexParameters(), p2.GetIndexParameters());
			Test(p1.GetRequiredCustomModifiers(), p2.GetRequiredCustomModifiers());
			Test(p1.GetOptionalCustomModifiers(), p2.GetOptionalCustomModifiers());
			SimpleTest(p1.GetAccessors(), p2.GetAccessors());
			SimpleTest(p1.GetMethod, p2.GetMethod);
			SimpleTest(p1.SetMethod, p2.SetMethod);
			SimpleTest(p1.GetGetMethod(), p2.GetGetMethod());
			SimpleTest(p1.GetSetMethod(), p2.GetSetMethod());
		}
		static void SimpleTest(DmdMemberInfo m1, MemberInfo m2) {
			if (!Verify((m1 is null) == (m2 is null)))
				return;
			if (m1 is null)
				return;
			var m1s = m1.ToString();
			bool b = m1s == m2.ToString();
			Verify(b || m1s.Contains("*"));
			Verify(m1.MetadataToken == m2.MetadataToken);
			if (m2.ReflectedType is not null)
				Test(m1.ReflectedType, m2.ReflectedType, m2.DeclaringType);
			if (m2.DeclaringType is not null)
				Test(m1.DeclaringType, m2.DeclaringType, m2.DeclaringType);
		}
		static void Test(DmdEventInfo e1, EventInfo e2) {
			TestMember(e1, e2);
			Verify((int)e1.Attributes == (int)e2.Attributes);
			Verify(e1.IsSpecialName == e2.IsSpecialName);
			Test(e1.EventHandlerType, e2.EventHandlerType, e2.DeclaringType);
			Verify(e1.IsMulticast == e2.IsMulticast);
			SimpleTest(e1.AddMethod, e2.AddMethod);
			SimpleTest(e1.RemoveMethod, e2.RemoveMethod);
			SimpleTest(e1.RaiseMethod, e2.RaiseMethod);
			SimpleTest(e1.GetOtherMethods(), e2.GetOtherMethods());
			SimpleTest(e1.GetAddMethod(), e2.GetAddMethod());
			SimpleTest(e1.GetRemoveMethod(), e2.GetRemoveMethod());
			SimpleTest(e1.GetRaiseMethod(), e2.GetRaiseMethod());
			SimpleTest(e1.GetOtherMethods(false), e2.GetOtherMethods(false));
			SimpleTest(e1.GetOtherMethods(true), e2.GetOtherMethods(true));
			SimpleTest(e1.GetAddMethod(false), e2.GetAddMethod(false));
			SimpleTest(e1.GetAddMethod(true), e2.GetAddMethod(true));
			SimpleTest(e1.GetRemoveMethod(false), e2.GetRemoveMethod(false));
			SimpleTest(e1.GetRemoveMethod(true), e2.GetRemoveMethod(true));
			SimpleTest(e1.GetRaiseMethod(false), e2.GetRaiseMethod(false));
			SimpleTest(e1.GetRaiseMethod(true), e2.GetRaiseMethod(true));
		}
		static void Test(IList<DmdCustomAttributeData> cas1, IList<CustomAttributeData> cas2) {
			if (!Verify(cas1.Count == cas2.Count))
				return;
			cas1 = Sort1(cas1);
			cas2 = Sort2(cas2);
			for (int i = 0; i < cas1.Count; i++)
				Test(cas1[i], cas2[i]);
		}
		static IList<DmdCustomAttributeData> Sort1(IList<DmdCustomAttributeData> input) {
			var list = new List<DmdCustomAttributeData>(input);
			list.Sort((a, b) => {
				int c = a.AttributeType.FullName.CompareTo(b.AttributeType.FullName);
				if (c != 0)
					return c;
				c = a.ConstructorArguments.Count - b.ConstructorArguments.Count;
				if (c != 0)
					return c;
				c = a.NamedArguments.Count - b.NamedArguments.Count;
				if (c != 0)
					return c;
				for (int i = 0; i < a.ConstructorArguments.Count; i++) {
					c = a.ConstructorArguments[i].ToString().CompareTo(b.ConstructorArguments[i].ToString());
					if (c != 0)
						return c;
				}
				for (int i = 0; i < a.NamedArguments.Count; i++) {
					c = a.NamedArguments[i].ToString().CompareTo(b.NamedArguments[i].ToString());
					if (c != 0)
						return c;
				}
				return 0;
			});
			return list;
		}
		static IList<CustomAttributeData> Sort2(IList<CustomAttributeData> input) {
			var list = new List<CustomAttributeData>(input);
			list.Sort((a, b) => {
				int c = a.AttributeType.FullName.CompareTo(b.AttributeType.FullName);
				if (c != 0)
					return c;
				c = a.ConstructorArguments.Count - b.ConstructorArguments.Count;
				if (c != 0)
					return c;
				c = a.NamedArguments.Count - b.NamedArguments.Count;
				if (c != 0)
					return c;
				for (int i = 0; i < a.ConstructorArguments.Count; i++) {
					c = a.ConstructorArguments[i].ToString().CompareTo(b.ConstructorArguments[i].ToString());
					if (c != 0)
						return c;
				}
				for (int i = 0; i < a.NamedArguments.Count; i++) {
					c = a.NamedArguments[i].ToString().CompareTo(b.NamedArguments[i].ToString());
					if (c != 0)
						return c;
				}
				return 0;
			});
			return list;
		}
		static void TestTypeSpecs(DmdType t1, Type t2) {
			TestTypes(t1, t2);

			TestTypes(t1.MakeByRefType(), t2.MakeByRefType());
			TestTypes(t1.MakePointerType(), t2.MakePointerType());
			TestTypes(t1.MakeArrayType(), t2.MakeArrayType());
			TestTypes(t1.MakeArrayType(1), t2.MakeArrayType(1));
			TestTypes(t1.MakeArrayType(2), t2.MakeArrayType(2));
			TestTypes(t1.MakeArrayType(32), t2.MakeArrayType(32));
			TestTypes(t1.MakeArrayType().MakePointerType(), t2.MakeArrayType().MakePointerType());
			TestTypes(t1.MakeArrayType(1).MakePointerType(), t2.MakeArrayType(1).MakePointerType());
			TestTypes(t1.MakeArrayType(2).MakePointerType(), t2.MakeArrayType(2).MakePointerType());
			TestTypes(t1.MakeArrayType(32).MakePointerType(), t2.MakeArrayType(32).MakePointerType());

			TestTypes(t1.MakePointerType().MakeByRefType(), t2.MakePointerType().MakeByRefType());
			TestTypes(t1.MakeArrayType().MakeByRefType(), t2.MakeArrayType().MakeByRefType());
			TestTypes(t1.MakeArrayType(1).MakeByRefType(), t2.MakeArrayType(1).MakeByRefType());
			TestTypes(t1.MakeArrayType(2).MakeByRefType(), t2.MakeArrayType(2).MakeByRefType());
			TestTypes(t1.MakeArrayType(32).MakeByRefType(), t2.MakeArrayType(32).MakeByRefType());
			TestTypes(t1.MakeArrayType().MakePointerType().MakeByRefType(), t2.MakeArrayType().MakePointerType().MakeByRefType());
			TestTypes(t1.MakeArrayType(1).MakePointerType().MakeByRefType(), t2.MakeArrayType(1).MakePointerType().MakeByRefType());
			TestTypes(t1.MakeArrayType(2).MakePointerType().MakeByRefType(), t2.MakeArrayType(2).MakePointerType().MakeByRefType());
			TestTypes(t1.MakeArrayType(32).MakePointerType().MakeByRefType(), t2.MakeArrayType(32).MakePointerType().MakeByRefType());
		}
		static void FixTypes(ref DmdType t1, Type t2, Type declaringType2) {
			try {
				if (t1.IsByRef) {
					if (t2.IsByRef) {
						var t1e = t1.GetElementType();
						var t2e = t2.GetElementType();
						if (t1e.IsConstructedGenericType && t2e.IsGenericTypeDefinition)
							t1 = t1e.GetGenericTypeDefinition().MakeByRefType();
					}
				}
				else if (t1.IsPointer) {
					if (t2.IsPointer) {
						var t1e = t1.GetElementType();
						var t2e = t2.GetElementType();
						if (t1e.IsConstructedGenericType && t2e.IsGenericTypeDefinition)
							t1 = t1e.GetGenericTypeDefinition().MakePointerType();
					}
				}
				else if (t1.IsArray) {
					if (t2.IsArray) {
						int rank = t2.GetArrayRank();
						var t1e = t1.GetElementType();
						var t2e = t2.GetElementType();
						if (t1e.IsConstructedGenericType && t2e.IsGenericTypeDefinition)
							t1 = rank == 1 ?
								t1e.GetGenericTypeDefinition().MakeArrayType() :
								t1e.GetGenericTypeDefinition().MakeArrayType(rank);
					}
				}
				else {
					if (t1.IsConstructedGenericType && t2.IsGenericTypeDefinition)
						t1 = t1.GetGenericTypeDefinition();
				}
			}
			catch (ResolveException) {
			}
		}
		static void Test(DmdType t1, Type t2, Type declaringType2) {
			FixTypes(ref t1, t2, declaringType2);
			SimpleTest(t1, t2);
		}
		static void SimpleTest(DmdType t1, Type t2) {
			if (!Verify((t1 is null) == (t2 is null)))
				return;
			if (t1 is null)
				return;
			Verify((int)t1.MemberType == (int)t2.MemberType);
			try {
				var t1s = t1.ToString();
				bool b = t1s == t2.ToString();
				// FnPtr is sometimes written as IntPtr, and other times as (fnptr)
				if (b || !t1s.Contains("*")) {
					Verify(b);
					Verify(t1.AssemblyQualifiedName == t2.AssemblyQualifiedName ||
						(t1.IsMetadataReference && t1.ResolveNoThrow() is null && t1.FullName == t2.FullName));
					Verify(t1.FullName == t2.FullName);
					Verify(t1.Name == t2.Name);
				}
				Verify(t1.IsGenericType == t2.IsGenericType);
				Verify(t1.IsGenericTypeDefinition == t2.IsGenericTypeDefinition);
			}
			catch (ResolveException) {
			}
		}
		static void Test(DmdCustomAttributeData ca1, CustomAttributeData ca2) {
			Verify(ca1.ToString() == ca2.ToString());
			SimpleTest(ca1.AttributeType, ca2.AttributeType);
			Verify(ca1.Constructor.ToString() == ca2.Constructor.ToString());
			Test(ca1.ConstructorArguments, ca2.ConstructorArguments);
			Test(ca1.NamedArguments, ca2.NamedArguments);
		}
		static void Test(IList<DmdCustomAttributeTypedArgument> args1, IList<CustomAttributeTypedArgument> args2) {
			if (!Verify(args1.Count == args2.Count))
				return;
			for (int i = 0; i < args1.Count; i++)
				Test(args1[i], args2[i]);
		}
		static void Test(IList<DmdCustomAttributeNamedArgument> args1, IList<CustomAttributeNamedArgument> args2) {
			if (!Verify(args1.Count == args2.Count))
				return;
			for (int i = 0; i < args1.Count; i++)
				Test(args1[i], args2[i]);
		}
		static void Test(DmdCustomAttributeTypedArgument arg1, CustomAttributeTypedArgument arg2) {
			Verify(arg1.ToString() == arg2.ToString());
			SimpleTest(arg1.ArgumentType, arg2.ArgumentType);

			if (arg1.Value is ReadOnlyCollection<DmdCustomAttributeTypedArgument> ar1 && arg2.Value is ReadOnlyCollection<CustomAttributeTypedArgument> ar2) {
				if (!Verify(ar1.Count == ar2.Count))
					return;
				Test(ar1, ar2);
			}
			else
				Verify(arg1.Value?.ToString() == arg2.Value?.ToString());
		}
		static void Test(DmdCustomAttributeNamedArgument arg1, CustomAttributeNamedArgument arg2) {
			Verify(arg1.ToString() == arg2.ToString());
			Verify(arg1.MemberInfo.ToString() == arg2.MemberInfo.ToString());
			Verify(arg1.MemberName == arg2.MemberName);
			Verify(arg1.IsField == arg2.IsField);
			Test(arg1.TypedValue, arg2.TypedValue);
		}
		static void Test(ReadOnlyCollection<DmdParameterInfo> p1, ParameterInfo[] p2) {
			if (!Verify(p1.Count == p2.Length))
				return;
			for (int i = 0; i < p1.Count; i++)
				Test(p1[i], p2[i]);
		}
		static void Test(DmdParameterInfo p1, ParameterInfo p2) {
			var p1s = p1.ToString();
			Verify(p1s == p2.ToString() || p1s.Contains("*"));
			Test(p1.ParameterType, p2.ParameterType, p2.Member.DeclaringType);
			Verify(p1.Name == p2.Name);
			Verify(p1.HasDefaultValue == p2.HasDefaultValue);
			Verify((p1.RawDefaultValue is null && p2.RawDefaultValue == DBNull.Value) ||
				(p1.RawDefaultValue is null && p2.RawDefaultValue == Missing.Value && p1.IsOptional) ||
				Equals(p1.RawDefaultValue, p2.RawDefaultValue));
			Verify(p1.Position == p2.Position);
			Verify((int)p1.Attributes == (int)p2.Attributes);
			SimpleTest(p1.Member, p2.Member);
			Verify(p1.MetadataToken == p2.MetadataToken);
			Test(p1.GetRequiredCustomModifiers(), p2.GetRequiredCustomModifiers());
			Test(p1.GetOptionalCustomModifiers(), p2.GetOptionalCustomModifiers());
			Test(p1.GetCustomAttributesData(), p2.GetCustomAttributesData());
		}
	}
}
