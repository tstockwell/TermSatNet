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

public class ArgUtils {

	public static String getString(String[] args, String argument, boolean required) {
		String value= null;
		for (int i= 0; i < args.length; i++) {
			String arg= args[i];
			if (arg.equals(argument) && i < args.length-1)
				value= args[++i];
		}
		if (value == null && required)
			throw new RuntimeException("The "+argument+" argument must be specified");
		return value;
	}

}
