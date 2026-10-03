# NOTES

## disable crash dialog

```zsh
defaults write com.apple.CrashReporter DialogType developer
```

none → no dialog at all

developer → logs only, no modal

server → similar to developer mode

turn it on again with

```zsh
defaults delete com.apple.CrashReporter DialogType
```
