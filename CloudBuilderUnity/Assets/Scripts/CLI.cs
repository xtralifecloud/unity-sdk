
using UnityEngine;
using System.Collections;
using CloudBuilderLibrary;
using UnityEngine.UI;

namespace CLI
{

	public class CLI : MonoBehaviour {
		public Commands Commands;
		public InputField InputField;
		public GUIStyle FixedFontStyle;
		private string ConsoleText = "Welcome to Unity CLI! Type help to list available commands!";
		private Vector2 ScrollPosition = new Vector2(0, float.PositiveInfinity);
		private bool RunningCommand = false;

		// Inherited
		void Start() {
			InputField.name = "InputField";
		}

		public void OnGUI() {
			GUILayout.BeginArea(new Rect(0, 0, Screen.width, Screen.height - 29));
			ScrollPosition = GUILayout.BeginScrollView(ScrollPosition, GUILayout.Width(Screen.width), GUILayout.Height(Screen.height - 29));
			GUI.skin.box.alignment = TextAnchor.UpperLeft;
			GUI.skin.box.wordWrap = true;    // set the wordwrap on for box only.
			GUILayout.Box(ConsoleText);        // just your message as parameter.
			GUILayout.EndScrollView();
			GUILayout.EndArea();

			if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Return) {
				string command = InputField.text;
				AppendLine("\n> " + command);
				if (RunningCommand) {
					AppendLine(">> A command is already running, please wait.");
					return;
				}
				Lexer lex = new Lexer(command);
				Parser parser = new Parser(Commands, lex);
				// Print some feedback at the end
				parser.AllDoneCallback = ex => {
					if (ex is ScriptException) {
						ScriptException e = ex as ScriptException;
						AppendLine(">> Error in script around " + command.Substring(e.Position) + ": " + e.Message);
						Debug.LogError("Error in script around " + command.Substring(e.Position) + ", " + e.ToString());
					}
					else {
						if (Commands.Variables.ContainsKey("result") && Commands.Variables["result"].Value != null) {
							AppendLine(">> result = " + Commands.Variables["result"].Value.ToJson());
						}
					}
					RunningCommand = false;
				};
				RunningCommand = true;
				parser.RunNextCommand();
				InputField.text = "";
			}
			GUI.FocusControl("InputField");
		}

		public void AppendLine(string text) {
			ConsoleText += "\n" + text;
			ScrollPosition = new Vector2(0, float.PositiveInfinity);
		}
	}
}
