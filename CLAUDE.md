# Claude Development Instructions

This file contains development commands and instructions for working with Realms of Eldor in Claude Code.

This will build and immediately run the ASCII version of the game for testing.
- the entire vcmi repo has been downloaded to /tmp/vcmi-temp
- always reference the vcmi repo for code, realms of eldor is mainly reusing the code from there.
- always add summary of changes to the changes file, but keep it brief. do not add mere bug fixes, only new and solid implementations
- copy and thus reuse as much of the code from the vcmi repo on github as possible. do not reinvent the wheel!
- after having done a research task on a given subject, summarize all findings in the RESEARCH.md file for later reference
- always use var when compiler infers variable's type anyway (f.ex. int)
- read files means read PROJECT_SUMMARY.md and CHANGES_SUMMARY.md files in root
- Object.FindObjectOfType has been
deprecated. Use Object.FindFirstObjectByType instead or if finding any instance is acceptable the faster Object.FindAnyObjectByType
- always make sure code is 100% SSOT and 100% DRY
- after having updated the CHANGES file after completing something, update the CHANGES_SUMMARY file with a brief summary of the changes that is sufficient for you to get up to speed of the project state when starting a new session
- when I say "be brief!" then only explain so I can do the task myself, do it briefly and use as little tokens as possible.