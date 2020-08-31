# SharpBuster
This is C# implementation of a directory brute forcing tool.

## Why another directory brute forcing tool?
I couldn't find one written in C# for use with tools such as Cobalt Strike's execute-assembly, where it's not feasible to perform directory fuzzing over a SOCKS proxy.

There's also an option to download a wordlist from a remote host to avoid dropping files to disk.

## Usage
Using a wordlist on disk:

SharpBuster.exe -u http://google.com -w C:\Windows\Users\Public\wordlist.txt

Downloading a wordlist from a remote host:

SharpBuster.exe -u http://google.com --wordlisturl http://example.com/wordlist.txt

Run with extensions appended to the wordlist:

SharpBuster.exe -u http://google.com --wordlisturl http://example.com/wordlist.txt -e php,aspx
