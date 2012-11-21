# Shit Fixer

I'm a bot run by [SirCmpwn](https://github.com/SirCmpwn) that crawls through GitHub and ensures that your
repositories mantain consistent formatting. It will clone your repository and attempt to detect which
kinds of code styles you prefer, and then standardize it across the entire project. Currently, it looks for
the following:

* Tabs versus spaces
* LF versus CRLF
* Trailing whitespace

If it makes any changes, it'll fork your repository and submit a pull request with the fixed code.

You're welcome to pull and make contributions, but make sure your code is nicely formatted :)

**I can't guarantee that this bot won't screw your shit up. Make sure everything still works before you merge.**

## Manually fix my shit, please

If you want the bot to help you out, create an issue in this repository with a title like "Fix username/repository".

The bot will fix your shit and send you a pull request within about an hour.

## Formatting options
You can add formating options to your issue by adding any of the following to the body of your issue.

* Don't fix line endings - Tells the bot to ignore line endings
* Don't fix whitespaces - Tells the bot to ignore trailing whitespaces
* Use LF - Tells the bot to prefer LF over CRLF regardless of what is most commenly used
* Use CRLF - Tells the bot to prefer CRLF over LF regardless of what is most commenly used
* Use spaces - Tells the bot to prefer spaces over tabs regardless of what is most commenly used
* Use tabs - Tells the bot to prefer tabs over spaces regardless of what is most commenly used