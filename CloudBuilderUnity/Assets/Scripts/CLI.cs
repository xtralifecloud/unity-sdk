
using UnityEngine;
using System.Collections;
using CotcSdk;
using UnityEngine.UI;

namespace CLI
{

	public class CLI : MonoBehaviour {
		public Commands Commands;
		public GUIStyle FixedFontStyle;
		private string ConsoleText = "Welcome to Unity CLI! Type help to list available commands!";
		private Vector2 ScrollPosition = new Vector2(0, float.PositiveInfinity);
		private string CommandText = "";
		private bool RunningCommand = false, FirstGui = true;

		// Inherited
		void Start() {
		}

		public void OnGUI() {
			GUILayout.BeginArea(new Rect(0, 0, Screen.width, Screen.height - 26));
			ScrollPosition = GUILayout.BeginScrollView(ScrollPosition, GUILayout.Width(Screen.width), GUILayout.Height(Screen.height - 26));
			GUILayout.TextField(ConsoleText);
			GUILayout.EndScrollView();
			GUILayout.EndArea();

			GUI.SetNextControlName("commandField");
			CommandText = GUI.TextField(new Rect(0, Screen.height - 26, Screen.width, 26), CommandText);
			if (FirstGui) {
				GUI.FocusControl("commandField");
				FirstGui = false;
			}

			if (Event.current.isKey && Event.current.keyCode == KeyCode.Return) {
				AppendLine("\n> " + CommandText);
				if (RunningCommand) {
					AppendLine(">> A command is already running, please wait.");
					return;
				}
				Lexer lex = new Lexer(CommandText);
				Parser parser = new Parser(Commands, lex);
				// Print some feedback at the end
				parser.AllDoneCallback = ex => {
					if (ex is ScriptException) {
						ScriptException e = ex as ScriptException;
						AppendLine(">> Error in script around " + CommandText.Substring(e.Position) + ": " + e.Message);
						Debug.LogError("Error in script around " + CommandText.Substring(e.Position) + ", " + e.ToString());
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
				CommandText = "";
			}
		}

		public void AppendLine(string text) {
			ConsoleText += "\n" + text;
			ScrollPosition = new Vector2(0, float.PositiveInfinity);
		}
	}
}
