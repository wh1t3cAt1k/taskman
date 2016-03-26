# TaskMan - a CLI todo list
## Description

TaskMan is a simple CLI application that allows you to manage your everyday tasks in the form of a todo list.

**Current version**: 0.2.0

TaskMan follows (or at least intends to follow) [semantic versioning](http://semver.org/). No version until 1.0.0 can be considered 'stable' with regards to the program's functionality or public interface.

## Why TaskMan?

* CLI is old school.
* Improves typing speed.
* You look like a hacker!

## System requirements

TaskMan requires Microsoft .NET 4.5 (or later), or Mono Runtime 4.0.0 to run.

## Installation and running

The installation is pretty straightforward - just extract/copy the contents of the release into a directory on your machine, `cd` into that directory, then:

* In Windows / .NET environment, run TaskMan.exe;  
* Using Mono, run 'mono TaskMan.exe';

**For OSX / Linux users**: if you want to run TaskMan from any working directory, two bash scripts (named `taskman_install` and `taskman`) are included in the release for that purpose. `taskman_install` creates/appends your `~/.bash_profile` file so as to add the location of TaskMan to `PATH`. The second script can then be called from any working directory in the shell: it simply wraps the `mono TaskMan.exe` command to save you some typing. It uses whatever Mono version is returned by `which mono`.

Unfortunately, Windows users currently have to modify their `PATH` themselves (if they want to run TaskMan from anywhere), but I guess anyone using a CLI todo list is perfectly capable of doing so.

## Usage 

The following help text can also be obtained by running `taskman --help`. You are encouraged to do so, because `--help` will likely be more up-to-date than what follows.

	--> Syntax: taskman [operation] [flags] [args]
	--> Operations:

	--> to show this help text: 

		taskman (/?|--help|help)

	--> to add a new task:

		taskman (add|new) [flags] description

		> use flags to specify a task's priority.

		> ID will be assigned to a task automatically.
	    
	--> to update task parameters:
		
		taskman (update|change|modify|set) [flags] parameter args

		> use flags to filter the tasks to be updated.

		> if 'parameter' is 'description',
		then 'args' will be concatenated into the new description.
			
		> if 'parameter' is 'importance' or 'priority'
		then 'args' should be a number from 1 to 3, or the
		priority's text value (e.g. 'Normal').

		> if 'parameter' is 'finished'/'completed'/'accomplished'
		then 'args' should be '0', 'false', '1', or 'true'.

	--> to show tasks:

		taskman (show|display) [flags]

		> use flags to filter the tasks to be displayed.

	--> to complete tasks:

		taskman (finish|complete|accomplish) [flags]

		> use flags to filter the tasks to be marked as finished.

	--> to remove a task:

		taskman (delete|remove) [flags]
		 
		> use flags to filter the tasks to be deleted.

	--> available command line options:

	  -?, --help                 displays TaskMan's help text
	      --license              displays TaskMan's licensing terms
	      --version              displays TaskMan's version
	  -I, --interactive          displays a confirmation prompt before executing an
	                               operation (not functional yet)
	  -v, --verbose              increase error message verbosity
	  -S, --silent               do not display any messages except errors
	  -A, --all                  forces an operation to be executed upon all tasks
	  -d, --description          specifies the description for a task (not
	                               functional yet)
	  -p, --priority=VALUE       filters tasks by priority or specifies a task's
	                               priority
	  -i, --id=VALUE             filters tasks by their ID or ID range
	  -P, --pending, --unfinished
	                             filters out any finished tasks
	  -F, --finished, --completed
	                             filters out any unfinished tasks
	  -r, --like=VALUE           filters tasks by their description matching a regex
	  -s, --skip=VALUE           skips a given number of tasks when displaying the
	                               result
	  -n, --limit=VALUE          limits the total number of tasks displayed

## Licensing

TaskMan is [free software](http://www.gnu.org/philosophy/free-sw.html) (as in Freedom). It is licensed under GNU GPLv3. The licensing terms can be found here: http://www.gnu.org/licenses/gpl-3.0.html

The program author provides this software "as is", without any warranty. Anyone who uses the software does so at their own risk. In no event will the author be held responsible for any negative consequences arising from using TaskMan, including (but not limited to) frustration, depression, shot legs, brain and/or computer damage, alien abductions, Donald Trump winning the elections etc.