using System;

namespace ICSharpCode.NRefactory.CSharp
{
	/// <summary>
	/// A specific role only used for C# tokens
	/// </summary>
	public sealed class TokenRole : Role<CSharpTokenNode>
	{
		/// <summary>
		/// Gets the token as string. Note that the token Name and Token value may differ.
		/// </summary>
		public string Token {
			get;
			private set;
		}
		
		/// <summary>
		/// Gets the char length of the token.
		/// </summary>
		public int Length {
			get;
			private set;
		}
		
		public TokenRole (string token) : base (token, CSharpTokenNode.Null)
		{
			this.Token = token;
			this.Length = token.Length;
		}
	}
}

