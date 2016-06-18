# TaskMan - a command line todo list
## Description

TaskMan is a convenient command line application that allows you to manage your everyday tasks in the form of a to-do list.

**Current version**: 0.3.0

## Why TaskMan?

* CLI is old school.
* Improves typing speed.
* You look like a hacker!

## Example usage

TaskMan was designed to be intuitive and easy-to-use. See some examples below, all of which should be pretty much self-explanatory:

If you wish to study all available commands and command line flags, run `taskman --help` or access the [help file in the repository](https://github.com/wh1t3cAt1k/taskman/blob/master/TaskMan/HELP.txt). You can also obtain short help for a specific command by running `taskman <commandname> --help`.

	> taskman add remember the milk --priority critical --due tomorrow

	> taskman add pay bills --due 'this month'

	> taskman show --like milk --orderby isfinished+desc-

	> taskman show --skip 100 --limit 10

	> taskman config sortorder priority+id+

	> taskman delete --id 1,2,10 priority Important

	> taskman reopen --like 'change tires'

	> taskman delete --all --interactive

## System requirements

TaskMan requires [Microsoft .NET](https://www.microsoft.com/net) 4.5 (or later), or [Mono Runtime](http://www.mono-project.com/download/) 4.0.0 (or later) to run.

## Installation and running

The installation is pretty straightforward - just extract/copy the contents of the release into a directory on your machine, `cd` into that directory, then:

* In Windows / .NET environment, run TaskMan.exe;  
* Using Mono, run 'mono TaskMan.exe';

**For OSX / Linux users**: if you want to run TaskMan from any working directory, two bash scripts (named `taskman_install` and `taskman`) are included in the release for that purpose. `taskman_install` creates/appends your `~/.bash_profile` file so as to add the location of TaskMan to `PATH`. The second script can then be called from any working directory in the shell: it simply wraps the `mono TaskMan.exe` command to save you some typing. It uses whatever Mono version is returned by `which mono`.

Unfortunately, Windows users currently have to modify their `PATH` themselves (if they want to run TaskMan from anywhere), but I presume anyone using a command line todo list is perfectly capable of doing so.

## Important considerations

TaskMan follows [semantic versioning](http://semver.org/). No version until 1.0.0 can be considered 'stable' with regards to the program's functionality or public interface. It **doesn't** mean that the program isn't already cool as it is!

## Licensing

TaskMan is [free software](http://www.gnu.org/philosophy/free-sw.html) (as in Freedom). It is licensed under GNU GPLv3. The licensing terms can be found here: http://www.gnu.org/licenses/gpl-3.0.html

The program author provides this software "as is", without any warranty. Anyone who uses the software does so at their own risk. In no event will the author be held responsible for any negative consequences arising from using TaskMan, including (but not limited to) frustration, depression, shot legs, brain and/or computer damage, alien abductions, Donald Trump winning elections etc.
