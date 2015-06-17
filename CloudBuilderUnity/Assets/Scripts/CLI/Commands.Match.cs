using System;
using CotcSdk;

namespace CLI
{
    public partial class Commands {

		[CommandInfo("Does nothing good yet.", "name[, num_players]")]
		void match_create(Arguments args) {
			args.Expecting(1, ArgumentType.String, ArgumentType.Double);
			Log("Here: " + args.StringArg(0) + ", " + args.DoubleArg(1));
			args.Return();
		}

	}
}
