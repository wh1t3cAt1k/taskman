# TaskMan - a CLI todo list
## Description

TaskMan is a convenient CLI application that allows you to manage your everyday tasks in the form of a to-do list.

## Example usage

    > taskman add remember the milk --p critical --due tomorrow

    > taskman show --like milk --orderby isfinished+desc-

    > taskman config sortorder priority+id+

    > taskman set --id 1,2,10 priority Important

    > taskman reopen --like 'change tires'

    > taskman delete --all --interactive

## Why TaskMan?

* CLI is old school.
* Improves typing speed.
* You look like a hacker!

**Current version**: 0.3.0

TaskMan follows (or at least intends to follow) [semantic versioning](http://semver.org/). No version until 1.0.0 can be considered 'stable' with regards to the program's functionality or public interface.

## System requirements

TaskMan requires Microsoft .NET 4.5 (or later), or Mono Runtime 4.0.0 to run.

## Installation and running

The installation is pretty straightforward - just extract/copy the contents of the release into a directory on your machine, `cd` into that directory, then:

* In Windows / .NET environment, run TaskMan.exe;  
* Using Mono, run 'mono TaskMan.exe';

**For OSX / Linux users**: if you want to run TaskMan from any working directory, two bash scripts (named `taskman_install` and `taskman`) are included in the release for that purpose. `taskman_install` creates/appends your `~/.bash_profile` file so as to add the location of TaskMan to `PATH`. The second script can then be called from any working directory in the shell: it simply wraps the `mono TaskMan.exe` command to save you some typing. It uses whatever Mono version is returned by `which mono`.

Unfortunately, Windows users currently have to modify their `PATH` themselves (if they want to run TaskMan from anywhere), but I guess anyone using a CLI todo list is perfectly capable of doing so.

## Usage 

The following help text can also be obtained by running `taskman --help`. You are highly encouraged to do so, because `--help` will likely be more up-to-date than what follows.

	--> Syntax: taskman [operation] [flags] [args]

	--> Operations:

	--> to show this help text: 

		taskman (/?|--help|help)

	--> to add a new task:

		taskman (add|new) [flags] description

		> use flags to specify a task's priority or a due date.

		> ID will be assigned to a task automatically.
	    
	--> to update task parameters:
		
		taskman (update|change|modify|set) [flags] parameter args

		> use flags to filter the tasks to be updated.

		> if 'parameter' is 'description',
		then 'args' will be concatenated into the new description.
			
		> if 'parameter' is 'importance' or 'priority',
		then 'args' should be a number from 1 to 3, or the
		priority's text value (e.g. 'Normal').

		> if 'parameter' is 'finished'/'completed'/'accomplished',
		then 'args' should be '0', 'false', '1', or 'true'.

		> if 'parameter' is 'duedate'/'due',
		then 'args' should be the due date representation.

	--> to show tasks:

		taskman (show|display) [flags]

		> use flags to filter the tasks to be displayed
		or specify the task display order.

	--> to complete tasks:

		taskman (finish|complete|accomplish) [flags]

		> use flags to filter the tasks to be marked as finished.

	--> to reopen tasks:

		taskman (reopen|unfinish|uncomplete) [flags]

		> use flags to filter the tasks to be reopened.

	--> to renumber tasks:

		taskman renumber [--orderby sortorder]

		> renumbers the tasks, assigning them new zero-based IDs
		in the order of display.

		> use an --orderby flag to override the sorting order
		defined in the configuration file.

	--> to remove a task:

		taskman (delete|remove) [flags]
		 
		> use flags to filter the tasks to be deleted.

		> there is a 'clear' alias that removes
		all tasks interactively.

	--> to configure program parameters:

		taskman config [param] [--global|--default] [value]

		> raw 'config' displays a list of available
		parameters along with their current values.

		> 'config param' displays the current value of
		the parameter along with its default value

		> 'config param value' assigns the provided value to
		the specified parameter

		> use --global flag to configure the parameter
		globally and not just for the current user. 

		> use --default flag to reset a parameter
		to its default value.

	--> to view / switch the current task list:

		taskman list [name]

		> raw 'list' displays the current list name
		along with the available non-empty task lists.

		> no empty lists are displayed because the list
		files are deleted as soon as they become empty.

		> specifying a name argument will change the
		currently used list. no spaces are allowed.

	--> not to type 'taskman' each time:

		taskman (repl|shell)

		> enters a looped read-eval-print mode 
		continuously listening to new commands.

		> 'repl' and 'shell' are meta-commands
		that are unavailable when already in REPL 
		mode, so Leo would be disappointed.

		> to exit REPL mode, type 'quit' or 'exit'.

		> to clear screen in the shell mode, type 'cls'.

	--> available command line options:

	  -?, --help                 displays TaskMan's help text
	      --license              displays TaskMan's licensing terms
	      --version              displays TaskMan's version
	  -G, --global               specifies that the provided configuration
	                               parameter should be set globally and not just
	                               for the current user
	  -I, --interactive          displays a confirmation prompt before executing an
	                               operation
	  -v, --verbose              increase error message verbosity
	  -S, --silent               do not display any messages except errors
	  -A, --all                  forces an operation to be executed upon all tasks
	  -d, --due, --duedate=VALUE filters tasks by being due on the specified date
	                               or specifies a new task's due date
	  -D, --before=VALUE         filters tasks by being due no later than the
	                               specified date
	  -p, --priority=VALUE       filters tasks by priority or specifies a new task'
	                               s priority
	  -i, --id=VALUE             filters tasks by their ID or ID range
	  -P, --pending, --unfinished
	                             filters out any finished tasks
	  -F, --finished, --completed
	                             filters out any unfinished tasks
	  -l, --like=VALUE           filters tasks by their description matching a regex
	      --skip=VALUE           skips a given number of tasks when displaying the
	                               result
	  -n, --limit=VALUE          limits the total number of tasks displayed
	  -s, --orderby, --sort=VALUE
	                             orders the tasks by the specified criteria
	  -r, --renumber             before showing tasks, reassign task IDs in the
	                               display order
	      --default, --reset     resets a parameter to its default value

	--> detailed discussion:

		--id

		> you can specify a list of comma-separated id's
		like '1,2,10'.

		> alternatively, you can specify a range of id's
		like '5-20'.

		--orderby

		> the tasks exhibit the following properties that
		you can order by: 'id', 'isfinished', 'duedate',
		'description', and 'priority'.

		> the value of the --orderby option should be a
		sequence of task properties, followed by a '+'
		(sort ascending) or a '-' (sort descending).

		> you can specify only a part of a property name
		as long as it does not produce ambiguity.

		> for example, --orderby pr+is+desc- will first
		sort tasks ascending by priority, then ascending
		by being finished, and then descending by description.

		--duedate

		> there are three ways to specify a task due date -
		absolute, relative, and using natural language.

		> using absolute syntax, just specify a string that
		can be parsed into a datetime, like '21 Jan 2025'
		or '2025-01-21'.

		> relative syntax adds a given number of years,
		months, weeks or days to a given date.

			* for example, '+2m-2d' will add two months
			and subtract two days relative to today.

			* '2025-01-21::+2w' will add two weeks to
			21 January 2025.

		> natural language syntax supports 'today',
		'tomorrow', 'this/next monday..sunday', and
		'this/next week/month/year'. 

	--> example usages:

		> taskman add remember the milk --priority critical --due tomorrow

		> taskman add pay bills --due 'this month'

		> taskman show --like milk --orderby isfinished+desc-

		> taskman show --skip 100 --limit 10

		> taskman config sortorder priority+id+

		> taskman set --id 1,2,10 priority Important

		> taskman reopen --like 'change tires'

		> taskman delete --all --interactive

## Licensing

TaskMan is [free software](http://www.gnu.org/philosophy/free-sw.html) (as in Freedom). It is licensed under GNU GPLv3. The licensing terms can be found here: http://www.gnu.org/licenses/gpl-3.0.html

The program author provides this software "as is", without any warranty. Anyone who uses the software does so at their own risk. In no event will the author be held responsible for any negative consequences arising from using TaskMan, including (but not limited to) frustration, depression, shot legs, brain and/or computer damage, alien abductions, Donald Trump winning elections etc.
