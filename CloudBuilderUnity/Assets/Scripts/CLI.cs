
using UnityEngine;
using System.Collections;
using CloudBuilderLibrary;
using UnityEngine.UI;

namespace CLI
{

	public class CLI : MonoBehaviour {
		public Commands Commands;
		public InputField InputField;
		internal Clan Clan;
		internal DomainEventLoop EventLoop;
		internal Gamer Gamer;

		// Inherited
		void Start() {
		}

		public void OnGUI() {
			if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Return) {
				string command = InputField.text;
				try {
					Lexer lex = new Lexer(command);
					Parser parser = new Parser(Commands, lex);
					parser.RunAllCommands();
				}
				catch (ScriptException ex) {
					Debug.LogError("Error in script around " + command.Substring(ex.Position - 1, 15));
				}
				InputField.text = "";
			}
		}
	}
}
