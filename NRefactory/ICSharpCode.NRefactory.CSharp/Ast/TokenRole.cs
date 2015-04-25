using System;
using System.Collections.Generic;

namespace ICSharpCode.NRefactory.CSharp
{
	/// <summary>
	/// A specific role only used for C# tokens
	/// </summary>
	public sealed class TokenRole : Role<CSharpTokenNode>
	{
		internal readonly static List<string> Tokens = new List<string> ();
		internal readonly static List<int>    TokenLengths = new List<int> ();
		internal readonly uint TokenIndex;

		static TokenRole ()
		{
			// null token
			Tokens.Add ("");
			TokenLengths.Add (0);
		}

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


		public TokenRole(string token) : base (token, CSharpTokenNode.Null)
		{
			this.Token = token;
			this.Length = token.Length;

			bool found = false;
			for (int i = 0; i < Tokens.Count; i++) {
				var existingToken = Tokens [i];
				if (existingToken == token) {
					TokenIndex = (uint)i;
					found = true;
					break;
				}
			}
			if (!found) {
				TokenIndex = (uint)Tokens.Count;
				Tokens.Add (token);
				TokenLengths.Add (this.Length);
			}
		}
	}
}

