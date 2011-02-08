using System;
using System.IO;
using System.Diagnostics;
using System.Linq;

using Mono.Cecil.Mdb;

namespace Mono.Cecil.Debug {

	interface IFoo { }
	interface IBar : IFoo { }

	abstract class Bar : IBar { }

	delegate void Action ();

	class Program {

		static int Answer ()
		{
			return 42;
		}

		static void Main (string [] args)
		{
			Time (() => {
			    var module = GetCurrentModule ();

			    module.Write ("dbg.rt.exe");
			});
		}

		static void Time (Action action)
		{
			var watch = new Stopwatch ();
			watch.Start ();
			action ();
			watch.Stop ();

			Console.WriteLine ("Elapsed: {0}", watch.Elapsed);
		}

		//static TypeDefinition GetCurrentType ()
		//{
		//    return GetCurrentModule ().Types [typeof (Program).FullName];
		//}

		static ModuleDefinition GetModule (string module)
		{
			return ModuleDefinition.ReadModule (module, new ReaderParameters {
				ReadingMode = ReadingMode.Deferred,
			});
		}

		static ModuleDefinition GetCurrentModule ()
		{
			return GetModule (typeof (object).Module.FullyQualifiedName);
		}
	}
}
