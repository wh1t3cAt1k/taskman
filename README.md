# TaskMan - a CLI todo list
## Description

TaskMan is a simple CLI application that allows you to manage your everyday tasks in the form of a todo list.

**Current version**: 0.1.0

## Why TaskMan?

* CLI is old school
* Improves typing speed
* You look like a hacker

## System requirements

TaskMan requires Microsoft .NET 4.5 (or later), or Mono Runtime 4.0.0 to run.

## Installation and usage

The installation is pretty straightforward - just extract/copy the contents of the release into a directory on your machine, `cd` into that directory, then:

* In Windows / .NET environment, run TaskMan.exe;  
* Using Mono, run 'mono TaskMan.exe';

**For OSX / Linux users**: if you want to run TaskMan from any working directory, two bash scripts (named `taskman_install` and `taskman`) are included in the release for that purpose. `taskman_install` creates/appends your `~/.bash_profile` file so as to add the location of TaskMan to `PATH`. The second script can then be called from any working directory in the shell: it simply wraps the `mono TaskMan.exe` command to save you some typing. It uses whatever Mono version is returned by `which mono`.

Unfortunately, Windows users currently have to modify their `PATH` themselves (if they want to run TaskMan from anywhere), but I guess anyone using a CLI todo list is perfectly capable of doing so.

## Licensing

TaskMan is free software (as in Freedom): http://www.gnu.org/philosophy/free-sw.html
It is licensed under GNU GPLv3. The licensing terms can be found here: http://www.gnu.org/licenses/gpl-3.0.html
