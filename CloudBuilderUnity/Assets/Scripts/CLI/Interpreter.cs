
using UnityEngine;
using System.Collections;
using CloudBuilderLibrary;
using System;
using System.Reflection;
using System.Globalization;
using System.Collections.Generic;

namespace CLI {
	public enum TokenType {
		Identifier,
		Number,
		String,
		Comma,
		Dot,
		Assignment,
		Ampersand,
		EndOfLine,
		End,
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
			get;
			private set;
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
			LastToken = NextToken = FetchNextToken();
		}

		public Lexer GetCopyHere() {
			Lexer copy = new Lexer(Command);
			copy.CharNo = CharNo;
			copy.NextToken = NextToken;
			copy.LastToken = LastToken;
			return copy;
		}

		public bool NextIs(TokenType type) {
			return NextToken.Type == type;
		}

		public Token NextToken {
			get;
			private set;
		}

		public Token LastToken {
			get;
			private set;
		}

		public bool EatTokenIf(TokenType type) {
			if (NextIs(type)) {
				PullNextToken();
				return true;
			}
			return false;
		}

		public Token PullNextToken() {
			Token result = NextToken;
			LastToken = NextToken;
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
				TokenType type = TokenType.Number;
				do {
					c = NextChar();
				} while (c >= '0' && c <= '9' || c == '.');
				// Case of string beginning by a number
				if (IsLitteral(c)) {
					type = TokenType.String;
					do {
						c = NextChar();
					} while (IsLitteralOrNumeric(c));
				}
				PutBack(c);
				return new Token(type, startIndex, Slice(startIndex, CurrentIndex));
			}
			else if (IsLitteral(c)) {
				int startIndex = CurrentIndex - 1;
				do {
					c = NextChar();
				} while (IsLitteralOrNumeric(c));
				PutBack(c);
				return new Token(TokenType.Identifier, startIndex, Slice(startIndex, CurrentIndex));
			}
			else {
				switch (c) {
					case '.': return new Token(TokenType.Dot, CurrentIndex - 1, c);
					case ',': return new Token(TokenType.Comma, CurrentIndex - 1, c);
					case '\n': return new Token(TokenType.EndOfLine, CurrentIndex - 1, c);
					case '=': return new Token(TokenType.Assignment, CurrentIndex - 1, c);
					case '&': return new Token(TokenType.Ampersand, CurrentIndex - 1, c);
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

		private bool IsLitteral(char c) {
			return c >= 'A' && c <= 'Z' || c >= 'a' && c <= 'z' || c == '_';
		}

		private bool IsLitteralOrNumeric(char c) {
			return IsLitteral(c) || c >= '0' && c <= '9';
		}

		private bool IsWhitespace(char c) {
			return c == ' ' || c == '	';
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
		public Commands Commands {
			get;
			private set;
		}
		public Action<Exception> AllDoneCallback;

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

		public Variable EvaluateNext() {
			if (Lex.NextIs(TokenType.String) || Lex.NextIs(TokenType.Identifier)) {
				return new Variable(new Bundle(Lex.PullNextToken().Text));
			}
			else if (Lex.NextIs(TokenType.Number)) {
				string str = Lex.PullNextToken().Text;
				double result = Double.Parse(str, NumberStyles.Any, CultureInfo.InvariantCulture);
				return new Variable(new Bundle(result));
			}
			else if (Lex.EatTokenIf(TokenType.Ampersand)) {
				string varName = Expect(TokenType.Identifier).Text;
				if (!Commands.Variables.ContainsKey(varName)) {
					throw new ScriptException(Lex.LastToken, "Variable " + varName + " does not exist");
				}
				// Can return the value?
				if (!Lex.EatTokenIf(TokenType.Dot)) {
					return Commands.Variables[varName];
				}
				// Or need to search more (after dot)
				string member = Expect(TokenType.Identifier).Text;
				var memberVal = Commands.Variables[varName].Value[member];
				if (memberVal == Bundle.Empty) {
					throw new ScriptException(Lex.NextToken, "No such member " + member + " in " + varName);
				}
				return new Variable(memberVal);
			}
			throw new ScriptException(Lex.NextToken, "Could not evaluate expression");
		}

		public void RunNextCommand() {
			try {
				if (Lex.NextIs(TokenType.End)) {
					if (AllDoneCallback != null) AllDoneCallback(null);
					return;
				}

				// Compose name
				Token token = Expect(TokenType.Identifier);
				string name = token.Text.ToLower();

				// Assignment
				if (Lex.EatTokenIf(TokenType.Assignment)) {
					HandleAssignment(name);
					ContinueToNextCommand();
					return;
				}

				while (Lex.EatTokenIf(TokenType.Dot)) {
					name += '_' + Expect(TokenType.Identifier).Text;
				}

				// Call method
				MethodInfo info = Commands.GetType().GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance);
				if (info == null) {
					throw new ScriptException(token, "Command " + name + " not found");
				}
				object[] args = new object[] { new Arguments(GetCopyHere(), ContinueToNextCommand) };

				try {
					info.Invoke(Commands, args);
				}
				catch (TargetInvocationException tie) {
					throw tie.InnerException;
				}
			}
			catch (Exception e) {
				if (AllDoneCallback != null) AllDoneCallback(e);
			}
		}

		private void ContinueToNextCommand() {
			// Skip to the end of the line
			while (!Lex.NextIs(TokenType.End) && !Lex.EatTokenIf(TokenType.EndOfLine))
				Lex.PullNextToken();
			RunNextCommand();
		}

		private void HandleAssignment(string name) {
			Commands.Variables[name] = EvaluateNext();
		}
	}

	public enum ArgumentType {
		String,
		Double,
	}

	/**
	 * Class only made to help running async commands. This serves as a context for a running command.
	 * Usage in a method:
	 * args.Expecting(...);
	 * args.StringArg(0...);
	 * args.Return(...);
	 */
	public class Arguments {
		public Parser Parser {
			get;
			private set;
		}
		public Lexer Lex {
			get;
			private set;
		}
		private object[] ParsedArgs;
		private Action CommandDone;

		public Arguments(Parser parser, Action whenDone) {
			Parser = parser;
			Lex = Parser.Lex;
			CommandDone = whenDone;
		}

		public void Expecting(int minimumArgs, params ArgumentType[] types) {
			object[] result = new object[types.Length];
			int i;
			for (i = 0; i < types.Length; i++) {
				bool hasArg = true;
				// Check for end of arg list (comma is optional)
				if (i > 0) {
					Lex.EatTokenIf(TokenType.Comma);
				}
				else {
					hasArg = !Lex.NextIs(TokenType.EndOfLine) && !Lex.NextIs(TokenType.End);
				}

				// Do we have enough args then?
				if (!hasArg) {
					if (i < minimumArgs) {
						throw new ScriptException(Lex.NextToken, "Invalid: " + minimumArgs + " args required, " + i + " passed");
					}
					else {
						break;
					}
				}

				Variable var = Parser.EvaluateNext();
				switch (types[i]) {
					case ArgumentType.String: result[i] = var.Value.AsString(); break;
					case ArgumentType.Double: result[i] = var.Value.AsDouble(); break;
				}
			}
			ParsedArgs = new object[i];
			Array.Copy(result, ParsedArgs, i);
		}

		public void Return(Bundle result = null) {
			Parser.Commands.Variables["result"] = new Variable(result);
			if (CommandDone != null) CommandDone();
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
	}

	public class CommandInfo : Attribute {
		public readonly string Usage, Description;

		public CommandInfo(string description, string usage = "") {
			Description = description; Usage = usage;
		}
	}

	public class Variable {
		public Bundle Value;
		public Variable(Bundle value) {
			Value = value;
		}
	}
}
