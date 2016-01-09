---| TaskMan v. 0.1 by wH1t3cAt1k |--

--> Syntax: taskman [operation] [args]
--> Operations:

--> to show this information: 

	taskman (/?|--help|help)

--> to add a new task:

	taskman (add|new) description [priority]
    
	> [priority] optional, must be 1..3 and enclosed in []
	> ID will be set to the task automatically.

--> to change properties of a task:
	
	taskman set id parameter args
	
	> if 'parameter' is 'desc',
	then 'args' is the new description.	
	> if 'parameter' is 'importance' or 'priority'
	then 'args' is a number from 1 to 3.
	> currently, you can only change properties of one task at a time.

--> to show tasks:

	> pending: 	taskman showp parameters
	> finished:	taskman showf parameters
	> all:		taskman (show|showall) parameters

	parameters syntax:
	(no parameters) - shows all
	ID		- shows the task with specified ID
	n-m		- shows all tasks in n-m ID range

--> to mark a task 'finished':

	taskman (finish|complete|accomplish) id

--> to remove a task:

	taskman delete id
	> currently, you can only delete one task at a time.

--> to clear the task list:

	taskman clear
	> confirmation message will be displayed before clearing;
	> be careful, you cannot undo this action.