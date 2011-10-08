// Copyright (c) AlphaSierraPapa for the SharpDevelop Team
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
using System.Linq;
using System.Linq.Expressions;
using System.Xml;

public class ExpressionTrees
{
	class GenericClass<X>
	{
		public static X StaticField;
		public X InstanceField;
		public static X StaticProperty { get; set; }
		public X InstanceProperty { get; set; }
		
		public static bool GenericMethod<Y>()
		{
			return false;
		}
	}
	
	int field;
	
	static object ToCode<R>(object x, Expression<Func<R>> expr)
	{
		return expr;
	}
	
	static object ToCode<T, R>(object x, Expression<Func<T, R>> expr)
	{
		return expr;
	}
	
	static object X()
	{
		return null;
	}
	
	public void Parameter(bool a)
	{
		ToCode(X(), () => a);
	}
	
	public void LocalVariable()
	{
		bool a = true;
		ToCode(X(), () => a);
	}
	
	public void LambdaParameter()
	{
		ToCode(X(), (bool a) => a);
	}
	
	public void AddOperator(int x)
	{
		ToCode(X(), () => 1 + x + 2);
	}
	
	public void AnonymousClasses()
	{
		ToCode(X(), () => new { X = 3, A = "a" });
	}
	
	public void ArrayIndex()
	{
		ToCode(X(), () => new[] { 3, 4, 5 }[0 + (int)(DateTime.Now.Ticks % 3)]);
	}
	
	public void ArrayLengthAndDoubles()
	{
		ToCode(X(), () => new[] { 1.0, 2.01, 3.5 }.Concat(new[] { 1.0, 2.0 }).ToArray().Length);
	}
	
	public void AsOperator()
	{
		ToCode(X(), () => new object() as string);
	}
	
	public void ComplexGenericName()
	{
		ToCode(X(), () => ((Func<int, bool>)(x => x > 0))(0));
	}
	
	public void DefaultValue()
	{
		ToCode(X(), () => new TimeSpan(1, 2, 3) == default(TimeSpan));
	}
	
	public void EnumConstant()
	{
		ToCode(X(), () => new object().Equals(MidpointRounding.ToEven));
	}
	
	public void IndexerAccess()
	{
		var dict = Enumerable.Range(1, 20).ToDictionary(n => n.ToString());
		ToCode(X(), () => dict["3"] == 3);
	}
	
	public void IsOperator()
	{
		ToCode(X(), () => new object() is string);
	}
	
	public void ListInitializer()
	{
		ToCode(X(), () => new Dictionary<int, int> { { 1, 1 }, { 2, 2 }, { 3, 4 } }.Count == 3);
	}
	
	public void ListInitializer2()
	{
		ToCode(X(), () => new List<int>(50) { 1, 2, 3 }.Count == 3);
	}
	
	public void ListInitializer3()
	{
		ToCode(X(), () => new List<int> { 1, 2, 3 }.Count == 3);
	}
	
	public void LiteralCharAndProperty()
	{
		ToCode(X(), () => new string(' ', 3).Length == 1);
	}
	
	public void CharNoCast()
	{
		ToCode(X(), () => "abc"[1] == 'b');
	}
	
	public void StringsImplicitCast()
	{
		int i = 1;
		string x = "X";
		ToCode(X(), () => (("a\n\\b" ?? x) + x).Length == 2 ? false : true && (1m + -i > 0 || false));
	}
	
	public void NotImplicitCast()
	{
		byte z = 42;
		ToCode(X(), () => ~z == 0);
	}
	
	public void MembersBuiltin()
	{
		ToCode(X(), () => 1.23m.ToString());
		ToCode(X(), () => AttributeTargets.All.HasFlag((Enum)AttributeTargets.Assembly));
		ToCode(X(), () => "abc".Length == 3);
		ToCode(X(), () => 'a'.CompareTo('b') < 0);
	}
	
	public void MembersDefault()
	{
		ToCode(X(), () => default(DateTime).Ticks == 0);
		ToCode(X(), () => default(int[]).Length == 0);
		ToCode(X(), () => default(Type).IsLayoutSequential);
		ToCode(X(), () => default(List<int>).Count);
		ToCode(X(), () => default(int[]).Clone() == null);
		ToCode(X(), () => default(Type).IsInstanceOfType(new object()));
		ToCode(X(), () => default(List<int>).AsReadOnly());
	}
	
	public void DoAssert()
	{
		field = 37;
		ToCode(X(), () => field != C());
		ToCode(X(), () => !ReferenceEquals(this, new ExpressionTrees()));
		ToCode(X(), () => MyEquals(this) && !MyEquals(default(ExpressionTrees)));
	}
	
	int C()
	{
		return field + 5;
	}
	
	bool MyEquals(ExpressionTrees other)
	{
		return other != null && field == other.field;
	}
	
	public void MethodGroupAsExtensionMethod()
	{
		ToCode(X(), () => (Func<bool>)new[] { 2000, 2004, 2008, 2012 }.Any);
	}
	
	public void MethodGroupConstant()
	{
		ToCode(X(), () => Array.TrueForAll(new[] { 2000, 2004, 2008, 2012 }, DateTime.IsLeapYear));
		
		HashSet<int> set = new HashSet<int>();
		ToCode(X(), () => new[] { 2000, 2004, 2008, 2012 }.All(set.Add));
		
		Func<Func<object, object, bool>, bool> sink = f => f(null, null);
		ToCode(X(), () => sink(int.Equals));
	}
	
	public void MultipleCasts()
	{
		ToCode(X(), () => 1 == (int)(object)1);
	}
	
	public void MultipleDots()
	{
		ToCode(X(), () => 3.ToString().ToString().Length > 0);
	}
	
	public void NestedLambda()
	{
		Func<Func<int>, int> call = f => f();
		//no params
		ToCode(X(), () => call(() => 42));
		//one param
		ToCode(X(), () => new[] { 37, 42 }.Select(x => x * 2));
		//two params
		ToCode(X(), () => new[] { 37, 42 }.Select((x, i) => x * 2));
	}
	
	public void CurriedLambda()
	{
		ToCode<int, Func<int, Func<int, int>>>(X(), a => b => c => a + b + c);
	}
	
	bool Fizz(Func<int, bool> a)
	{
		return a(42);
	}
	
	bool Buzz(Func<int, bool> a)
	{
		return a(42);
	}
	
	bool Fizz(Func<string, bool> a)
	{
		return a("42");
	}
	
	public void NestedLambda2()
	{
		ToCode(X(), () => Fizz(x => x == "a"));
		ToCode(X(), () => Fizz(x => x == 37));
		
		ToCode(X(), () => Fizz((int x) => true));
		ToCode(X(), () => Buzz(x => true));
	}
	
	public void NewArrayAndExtensionMethod()
	{
		ToCode(X(), () => new[] { 1.0, 2.01, 3.5 }.SequenceEqual(new[] { 1.0, 2.01, 3.5 }));
	}
	
	public void NewMultiDimArray()
	{
		ToCode(X(), () => new int[3, 4].Length == 1);
	}
	
	public void NewObject()
	{
		ToCode(X(), () => new object() != new object());
	}
	
	public void NotOperator()
	{
		bool x = true;
		int y = 3;
		byte z = 42;
		ToCode(X(), () => ~(int)z == 0);
		ToCode(X(), () => ~y == 0);
		ToCode(X(), () => !x);
	}
	
	public void ObjectInitializers()
	{
		XmlReaderSettings s = new XmlReaderSettings {
			CloseInput = false,
			CheckCharacters = false
		};
		ToCode(X(), () => new XmlReaderSettings { CloseInput = s.CloseInput, CheckCharacters = s.CheckCharacters }.Equals(s));
	}
	
	public void Quoted()
	{
		ToCode(X(), () => (Expression<Func<int, string, string>>)((n, s) => s + n.ToString()) != null);
	}
	
	public void Quoted2()
	{
		ToCode(X(), () => ToCode(X(), () => true).Equals(null));
	}
	
	public void QuotedWithAnonymous()
	{
		ToCode(X(), () => new[] { new { X = "a", Y = "b" } }.Select(o => o.X + o.Y).Single());
	}
	
	public void StaticCall()
	{
		ToCode(X(), () => Equals(3, 0));
	}
	
	public void ThisCall()
	{
		ToCode(X(), () => !Equals(3));
	}
	
	public void ThisExplicit()
	{
		ToCode(X(), () => object.Equals(this, 3));
	}
	
	public void TypedConstant()
	{
		ToCode(X(), () => new[] { typeof(int), typeof(string) });
	}
	
	public void StaticCallImplicitCast()
	{
		ToCode(X(), () => Equals(3, 0));
	}
	
	public void StaticMembers()
	{
		ToCode(X(), () => (DateTime.Now > DateTime.Now + TimeSpan.FromMilliseconds(10.001)).ToString() == "False");
	}
	
	public void Strings()
	{
		int i = 1;
		string x = "X";
		ToCode(X(), () => (("a\n\\b" ?? x) + x).Length == 2 ? false : true && (1m + (decimal)-i > 0m || false));
	}
	
	public void StringAccessor()
	{
		ToCode(X(), () => (int)"abc"[1] == 98);
	}
	
	public void GenericClassInstance()
	{
		ToCode(X(), () => new GenericClass<int>().InstanceField + new GenericClass<double>().InstanceProperty);
	}
	
	public void GenericClassStatic()
	{
		ToCode(X(), () => GenericClass<int>.StaticField + GenericClass<double>.StaticProperty);
	}
	
	public void InvokeGenericMethod()
	{
		ToCode(X(), () => GenericClass<int>.GenericMethod<double>());
	}
}
