/*******************************************************************************
 *     termsat SAT solver
 *     Copyright (C) 2010 Ted Stockwell <emorning@yahoo.com>
 * 
 *     This program is free software: you can redistribute it and/or modify
 *     it under the terms of the GNU Affero General Public License as
 *     published by the Free Software Foundation, either version 3 of the
 *     License, or (at your option) any later version.
 * 
 *     This program is distributed in the hope that it will be useful,
 *     but WITHOUT ANY WARRANTY; without even the implied warranty of
 *     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *     GNU Affero General Public License for more details.
 * 
 *     You should have received a copy of the GNU Affero General Public License
 *     along with this program.  If not, see <http://www.gnu.org/licenses/>.
 ******************************************************************************/

using System.Collections.Generic;
using System.IO;

namespace TermSAT.Common
{

/**
 * A simple command line interface for server applications.
 * Use the shutdown command to gracefully shutdown the server.
 * 
 * This class implements the command line functionality.
 * It can be connected to many input/output streams (so for instance, 
 * to allow users to connect to a server's command line interface via 
 * a socket).
 * Use the connect method to connect a pair of streams. 
 * 
 * @author ted stockwell
 */
public class ServerCommandLineInterface 
{
    
    public interface ICommand {
        string Help { get; }
        string Description { get; }
        void Run(string[] args, TextWriter output);
        ServerCommandLineInterface CommandLine { get; set; }
    }
    
	Dictionary<string, ICommand> commands= new Dictionary<string, ICommand>();
    
    public IndexAccessor<string, ICommand> Commands= new IndexAccessor<string, ICommand>()
    {
        Set= (commandName, command) => { 
            commands[commandName]= command; 
        },
        Get= commandName => { return command[commandName]; } 
    };

    public class CommandAccessor
    {
        ServerCommandLineInterface cli;
        public CommandAccessor(ServerCommandLineInterface cli) { this.cli= cli; }
        public ICommand this[string commandName]
        {
            get { return cli.commands[commandName]; }
            set
            {
                cli.commands[commandName]= value;
                value.CommandLine= cli;
            }
        }
    }

    public CommandAccessor Commands {  get; private set; }

    public class HelpCommand : ICommand {
        public string Help => "h[elp] [<command>]";
        public string Description => 
                "Get help on a given command"
                +"\nIf no command is specified then list a summary of all commands.";
		public void Run(string[] args, TextWriter output) {
            if (1 < args.Length) {
                var c= Commands[args[1]];
                if (c == null) {
                    out.println("unknown command '"+args[1]+"'");
                    return;
                }
                out.println(c.description());
                out.println(c.help());
                out.flush();
                return;
            }

            HashSet<ICommand> set= new HashSet<ICommand>(commands.values());
            for (Iterator<ICommand> i= set.iterator(); i.hasNext();)
                out.println(_prompt+((ICommand)i.next()).help());
        }
    };

    ICommand _shutdownCommand= new ICommand() {
        public void run(string[] args, PrintStream out) {
            shutDown(); // shut down bundle manager
        }
        public string description() {
            return "Shutdown the "+_serverName;
        }
        public string help() {
            return "sh[utdown]";
        }
    };

    boolean _halted= false;
    string _serverName;
    string _prompt;

    /**
     * @param in	stream from which user input is read 
     * @param out	stream to which prompts are written
     * @param serverName	a name, something like "RSWT Application Server".
     * @param prompt	prompt displayed at beginning of each line, something like "RSWT>> ".
     */
    public ServerCommandLineInterface(string serverName, string prompt)
    {
        Commands= new CommandAccessor(this);
        _serverName= serverName;
        _prompt= prompt;

        var helpCommand= new HelpCommand();
        commands["shutdown"]= _shutdownCommand;
        commands["sh"]= _shutdownCommand;
        commands["q"]= _shutdownCommand;
        commands["help"]= helpCommand;
        commands["h"]= helpCommand;
        commands["?"]= helpCommand;

    }
    
    synchronized public void shutDown()
    {
        _halted= true;
        notifyAll();
    }
    
	public void connect(InputStream inputStream, PrintStream out) {
		BufferedReader in= new BufferedReader(new InputStreamReader(inputStream));
        try {
			out.println(_prompt+"This is the "+_serverName+".");
			out.println(_prompt+"Press the Enter key at any time to get to the command prompt.");
			out.println(_prompt+"Available commands:");
			_helpCommand.run(new string[0], out);
            for (;!_halted;) {
                out.print(_prompt);
                out.flush();
                string readLine= in.readLine();
                if (readLine == null)
                	continue;
                stringTokenizer tokenizer= new stringTokenizer(readLine, " \t");
                List<string> l= new ArrayList<string>();
                for (; tokenizer.hasMoreTokens();)
                    l.add(tokenizer.nextToken());
                if (l.size() <= 0)
                    continue;
                string[] args= new string[l.size()];
                l.toArray(args);
                Command c= (Command)commands.get(args[0]);
                if (c == null) {
                    out.println("Invalid command:"+args[0]);
                    out.flush();
                    continue;
                }
                try {
                    c.run(args, out);
                }
                catch (Exception x) {
                    out.print("Internal Error: ");
                    x.printStackTrace(out);
                    out.flush();
                }
            }
        }
        catch (Exception x) {
            x.printStackTrace(out);
            out.flush();
        }
	}

	public boolean isShutdown() {
		return _halted;
	}

	public static void start(ServerCommandLineInterface commandLine, InputStream in, PrintStream out) {
		Thread t= new Thread() {
			public void run() {
				commandLine.connect(in, out);
			}
		};
		t.setDaemon(true);
		t.start();
	}



}

}
