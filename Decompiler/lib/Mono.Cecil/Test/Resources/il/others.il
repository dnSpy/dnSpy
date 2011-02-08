.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89)
  .ver 2:0:0:0
}

.assembly Others {}

.module Others.dll

.class private auto ansi Others {

	.field private string _context

	.method public specialname instance string get_Context () cil managed
	{
		ldarg.0
		ldfld string Others::_context
		ret
	}

	.method public specialname instance void set_Context (string val) cil managed
	{
		ldarg.0
		ldarg.1
		stfld string Others::_context
		ret
	}

	.method public specialname instance void let_Context (string val) cil managed
	{
		ldarg.0
		ldarg.1
		stfld string Others::_context
		ret
	}

	.method public specialname instance void bet_Context (string val) cil managed
	{
		ldarg.0
		ldarg.1
		stfld string Others::_context
		ret
	}

	.property instance string Context () {
		.get instance string Others::get_Context ()
		.set instance void Others::set_Context (string val)
		.other instance void Others::let_Context (string val)
		.other instance void Others::bet_Context (string val)
	}

	.field private class [mscorlib]System.EventHandler _handler

	.method public specialname instance void remove_Handler (class [mscorlib]System.EventHandler) cil managed
	{
		ret
	}

	.method public specialname instance void add_Handler (class [mscorlib]System.EventHandler) cil managed
	{
		ret
	}

	.method public specialname instance void dang_Handler (class [mscorlib]System.EventHandler) cil managed
	{
		ret
	}

	.method public specialname instance void fang_Handler (class [mscorlib]System.EventHandler) cil managed
	{
		ret
	}

	.event [mscorlib]System.EventHandler Handler {
		.removeon instance void Others::remove_Handler (class [mscorlib]System.EventHandler)
		.addon instance void Others::add_Handler (class [mscorlib]System.EventHandler)
		.other instance void Others::dang_Handler (class [mscorlib]System.EventHandler)
		.other instance void Others::fang_Handler (class [mscorlib]System.EventHandler)
	}
}
