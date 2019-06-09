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
package com.googlecode.termsat.core.utils;
import java.io.BufferedReader;
import java.io.InputStream;
import java.io.InputStreamReader;
import java.io.PrintStream;
import java.util.ArrayList;
import java.util.HashMap;
import java.util.HashSet;
import java.util.Iterator;
import java.util.List;
import java.util.Map;
import java.util.StringTokenizer;

/**
 * A simple command line interface for server applications.
 * Use the shutdown command to gracefully shutdown the server.
 * 
 * This class implements the command line functionality.
 * It can be connected to many input/output streams (so for instance, 
 * to allow users to connect to a server's command line iterface via 
 * a socket).
 * Use the connect method to connect a pair of streams. 
 * 
 * @author ted stockwell
 */
public class ServerCommandLineInterface 
{
    
    public static interface Command {
        public String help();
        public String description();
        public void run(String[] args, PrintStream out) throws Exception;
    }

    Command _helpCommand= new Command() {
		public void run(String[] args, PrintStream out) {
            if (1 < args.length) {
                Command c= (Command)commands.get(args[1]);
                if (c == null) {
                    out.println("unknown command '"+args[1]+"'");
                    return;
                }
                out.println(c.description());
                out.println(c.help());
                out.flush();
                return;
            }

            HashSet<Command> set= new HashSet<Command>(commands.values());
            for (Iterator<Command> i= set.iterator(); i.hasNext();)
                out.println(_prompt+((Command)i.next()).help());
        }
        public String description() {
            return "Get help on a given command"
                +"\nIf no command is specified the list a summary of all commands.";
        }
        public String help() {
            return "h[elp] [<command>]";
        }
    };

    Command _shutdownCommand= new Command() {
        public void run(String[] args, PrintStream out) {
            shutDown(); // shut down bundle manager
        }
        public String description() {
            return "Shutdown the "+_serverName;
        }
        public String help() {
            return "sh[utdown]";
        }
    };
    
	Map<String, Command> commands= new HashMap<String, Command>();
    {
        commands.put("shutdown", _shutdownCommand);
        commands.put("sh", _shutdownCommand);
        commands.put("q", _shutdownCommand);
        commands.put("help", _helpCommand);
        commands.put("h", _helpCommand);
        commands.put("?", _helpCommand);
    }

    boolean _halted= false;
    String _serverName;
    String _prompt;

    /**
     * @param in	stream from which user input is read 
     * @param out	stream to which prompts are written
     * @param serverName	a name, something like "RSWT Application Server".
     * @param prompt	prompt displayed at beginning of each line, something like "RSWT>> ".
     */
    public ServerCommandLineInterface(String serverName, String prompt)
    {
        _serverName= serverName;
        _prompt= prompt;
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
			_helpCommand.run(new String[0], out);
            for (;!_halted;) {
                out.print(_prompt);
                out.flush();
                String readLine= in.readLine();
                if (readLine == null)
                	continue;
                StringTokenizer tokenizer= new StringTokenizer(readLine, " \t");
                List<String> l= new ArrayList<String>();
                for (; tokenizer.hasMoreTokens();)
                    l.add(tokenizer.nextToken());
                if (l.size() <= 0)
                    continue;
                String[] args= new String[l.size()];
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
