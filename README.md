# Shit Fixer

I'm a bot run by [SirCmpwn](https://github.com/SirCmpwn) that crawls through GitHub and ensures that your
repositories mantain consistent formatting. It will clone your repository and attempt to detect which
kinds of code styles you prefer, and then standardize it across the entire project. Currently, it looks for
the following:

* Tabs versus spaces
* LF versus CRLF

If it makes any changes, it'll fork your repository and submit a pull request with the fixed code.

You're welcome to pull and make contributions, but make sure your code is nicely formatted :)