
using UnityEngine;
using System.Collections;
using CloudBuilderLibrary;
using System;
using System.Reflection;
using System.Globalization;

namespace CLI
{
	public enum TokenType {
		Identifier,
		Number,
		String,
		Comma,
		Dot,
		End
	}

	public class Token {
		public TokenType Type;
		public string Text;
		public int CharPos;

		public Token(TokenType t, int pos, string text) {
			Type = t;
			Text = text;
			CharPos = pos;
		}
		public Token(TokenType t, int pos, char text) : this(t, pos, text.ToString()) { }

		public override string ToString() {
			return string.Format("[Token {0}, {1}]", Type.ToString(), Text);
		}
	}

	public class ScriptException : Exception {
		public int Position {
			get; private set;
		}
		public ScriptException(Token token, string message)
			: base(
				string.Format("At {0}: {1}", token.CharPos, message)) {
			Position = token.CharPos;
		}
		public ScriptException(string originalCommand, int pos, string message)
			: base(
				string.Format("At {0}: {1}", pos, message)) {
			Position = pos;
		}
	}

	public class Lexer {
		string Command;
		int CharNo;

		public Lexer(string command) {
			Command = command;
			CharNo = 0;
			NextToken = FetchNextToken();
		}

		public Lexer GetCopyHere() {
			Lexer copy = new Lexer(Command);
			copy.CharNo = CharNo;
			copy.NextToken = NextToken;
			return copy;
		}

		public bool NextIs(TokenType type) {
			return NextToken.Type == type;
		}

		public Token NextToken {
			get;
			private set;
		}

		public Token PullNextToken() {
			Token result = NextToken;
			NextToken = FetchNextToken();
			return result;
		}

		private int CurrentIndex {
			get { return CharNo; }
		}

		private Token FetchNextToken() {
			char c = NextChar();
			while (IsWhitespace(c)) c = NextChar();

			if (c == '\0') {
				return new Token(TokenType.End, CurrentIndex - 1, c);
			}
			else if (c >= '0' && c <= '9') {
				int startIndex = CurrentIndex - 1;
				do {
					c = NextChar();
				} while (c >= '0' && c <= '9' || c == '.');
				PutBack(c);
				return new Token(TokenType.Number, startIndex, Slice(startIndex, CurrentIndex));
			}
			else if (c >= 'A' && c <= 'Z' || c >= 'a' && c <= 'z' || c == '_') {
				int startIndex = CurrentIndex - 1;
				do {
					c = NextChar();
				} while (c >= 'A' && c <= 'Z' || c >= 'a' && c <= 'z' || c >= '0' && c <= '9' || c == '_');
				PutBack(c);
				return new Token(TokenType.Identifier, startIndex, Slice(startIndex, CurrentIndex));
			}
			else {
				switch (c) {
					case '.': return new Token(TokenType.Dot, CurrentIndex - 1, c);
					case ',': return new Token(TokenType.Comma, CurrentIndex - 1, c);
					case '\"':
					case '\'':
						int startIndex = CurrentIndex;
						char curChar = NextChar();
						while (curChar != c && curChar != '\0') {
							curChar = NextChar();
						}
						return new Token(TokenType.String, startIndex, Slice(startIndex, CurrentIndex - 1));
				}
			}
			throw new ScriptException(Command, CurrentIndex - 1, "Unrecognized token: " + c);
		}

		private bool IsWhitespace(char c) {
			return c == ' ' || c == '	' || c == '\n';
		}

		private char NextChar() {
			return CharNo >= Command.Length ? '\0' : Command[CharNo++];
		}

		private void PutBack(char c) {
			if (c != '\0' && Command[--CharNo] != c)
				throw new ScriptException(Command, CharNo, "Invalid char put back: " + c + " (expecting " + Command[CharNo] + ")");
		}

		private string Slice(int startIndex, int endIndex) {
			return Command.Substring(startIndex, endIndex - startIndex);
		}
	}

	public class Parser {
		public Lexer Lex {
			get;
			private set;
		}
		private Commands Commands;
		private object[] EmptyArray = new object[0];

		public Parser(Commands commands, Lexer lex) {
			Commands = commands;
			Lex = lex;
		}

		public Token Expect(TokenType type) {
			if (!Lex.NextIs(type)) {
				throw new ScriptException(Lex.NextToken, "Expected token of type " + type + ", got " + Lex.NextToken.Type);
			}
			return Lex.PullNextToken();
		}

		public Parser GetCopyHere() {
			return new Parser(Commands, Lex.GetCopyHere());
		}

		public void RunAllCommands() {
			while (RunNextCommand()) ;
		}

		public bool RunNextCommand() {
			if (Lex.NextIs(TokenType.End)) {
				return false;
			}

			object previousObj = Commands;
			while (true) {
				string name = Expect(TokenType.Identifier).Text;
				MemberInfo[] info = previousObj.GetType().GetMember(name);
				// Subobject expected
				if (info[0].MemberType == MemberTypes.Property) {
					previousObj = previousObj.GetType().GetProperty(name).GetValue(previousObj, EmptyArray);
					Expect(TokenType.Dot);
					continue;
				}
				else if (info[0].MemberType == MemberTypes.Field) {
					previousObj = previousObj.GetType().GetField(name).GetValue(previousObj);
					Expect(TokenType.Dot);
					continue;
				}
				else if (info[0].MemberType == MemberTypes.Method) {
					object[] args = new object[] { new Arguments(GetCopyHere()) };
					previousObj.GetType().GetMethod(name).Invoke(previousObj, args);
					break;
				}
				else {
					throw new ScriptException(Lex.NextToken, "Invalid method to call: " + name);
				}
			}
			return true;
		}
	}

	public enum ArgumentType {
		String,
		Double,
	}

	public class Arguments {
		public Parser Parser {
			get;
			private set;
		}
		public Lexer Lex {
			get;
			private set;
		}
		private ArgumentType[] ExpectedArgs;
		private object[] ParsedArgs;

		public Arguments(Parser parser) {
			Parser = parser;
			Lex = Parser.Lex;
		}

		public double DoubleArg(int pos) {
			return pos >= ParsedArgs.Length ? 0 : (double)ParsedArgs[pos];
		}

		public int IntArg(int pos) {
			return pos >= ParsedArgs.Length ? 0 : (int)ParsedArgs[pos];
		}

		public string StringArg(int pos) {
			return pos >= ParsedArgs.Length ? null : (string)ParsedArgs[pos];
		}

		public void ExpectingArgs(int minimumArgs, params ArgumentType[] types) {
			object[] result = new object[types.Length];
			int i;
			ExpectedArgs = types;
			for (i = 0; i < types.Length; i++) {
				// Skip separating comma
				if (i > 0) {
					// End of arg list
					if (!Lex.NextIs(TokenType.Comma)) {
						if (i < minimumArgs) {
							throw new ScriptException(Lex.NextToken, "Invalid: " + minimumArgs + " args required, " + i + " passed");
						}
						else {
							break;
						}
					}
					Lex.PullNextToken();
				}

				switch (types[i]) {
					case ArgumentType.String:
						if (Lex.NextIs(TokenType.String) || Lex.NextIs(TokenType.Number))
							result[i] = Lex.PullNextToken().Text;
						else
							throw new ScriptException(Lex.NextToken, "Argument " + (i + 1) + " must be a string");
						break;
					case ArgumentType.Double:
						if (Lex.NextIs(TokenType.String) || Lex.NextIs(TokenType.Number)) {
							double value;
							Token token = Lex.PullNextToken();
							if (!Double.TryParse(token.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out value))
								throw new ScriptException(token, "Argument " + (i + 1) + " is not a valid double");
							result[i] = value;
						}
						else
							throw new ScriptException(Lex.NextToken, "Argument " + (i + 1) + " must be an double");
						break;
				}
			}
			ParsedArgs = new object[i];
			Array.Copy(result, ParsedArgs, i);
		}
	}
}
