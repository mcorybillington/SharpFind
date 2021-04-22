# SharpFind
Simple c# application to mimic some of the functionality of the linux `find` command.
- Lists full path of files
- Can search by file name/wildcard
- Can set CPU priority to limit resource usage (in comparison to other processes)
- Can find files modified in the last n minutes
- Can find writable files by the current user
- Identify .NET assemblies

## Usage
```
SharpFind.exe /p:<absolute-or-relative-path> /e:<search-pattern> /c:<cpu-priority> /m:<minutes-since-last-modification> /w

/p:<path> path to search. Relative or absolute is acceptable
/e:<search-pattern> * is wildcard
/c:<cpu-priority> OS CPU priority compared to other proccesses. Valid values are 0-5, 5 being highest priority
/m:<minutes-since-last-modification> Find files modified in the last n minutes
/w Only return files wrtitable by the current user. Will show '[LOCKED]' for files that are locked for writing.
```
## Example
```
SharpFind.exe /p:c:\users\bob /e:*lolcats.ext* /c:0 /m:10 /w
```
This will look for all files in `c:\users\bob` where the file name contains `lolcats.ext`. It will set CPU priority to `0`, thus nearly everything except for other processes set to zero will get priority over `SharpFind.exe`. It will only return files that have been modified in the last ten minutes and that are writable by the user running `SharpFind.exe`
## Credits
M. Cory Billington [@_th3y](https://twitter.com/_th3y)


