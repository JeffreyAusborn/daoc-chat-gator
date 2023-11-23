# DAoC Chat Gator

Ready to finally see some performance numbers of your Damage, Heals, or how your armor took damage? This is where Chat Gator comes in, a pseudo overlay to help 
provide the visbility of your performance. See how often you use one ability over another or how often you crit. Show your team you actually did heal them during the fight!

Chat Gator parses the chat.log file from Dark Age of Camelot. While parsing the log, it's aggregating what it can into different buckets. 
This will seem near real time, sometimes, because of how the chat.log file works in DAoC. The client will buffer the logs and flush once full.
After a flush, Chat Gator will re-parse the log file.

# How to use
- In DAoC - enable logging at least once with /chatlog
- Download the latest release zip file
- Extract zip file
- Open Daoc Chat Gator executable
- Click the Log File button and open the chat.log file
	- Typically under Documents/Eletronic Arts/Dark Age of Camelot/chat.log
- Chat Gator will start reading from the chat.log
	- If there is data already there, you can reset the log file by
		- disabling the in game chat log with /chatlog (if it's on)
		- type /resetlog
			- This will not work in game and will say the command does not exist, thats okay!
		- enable logging
	- Note: logging in game must be disabled before resetting the logs with Chat Gator
- If logs aren't populating
	- Make sure you chose the correct log file
	- Make sure logs are enabled


Releases

0.0.4
- Added a resetlog regex in game. Chat Gator will look for /resetlog commang in your logs and will attempt to delete your log file and clear the aggregation objects.

0.0.3
- Configurable window size

0.0.2
- Column sorting
- Column selection button

0.0.1
- init


TODOs:
- Resists
- Misses
- Update UI pwease -> Iceforceones
