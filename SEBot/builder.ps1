(ls *.cs | where { !$_.FullName.Contains('Ignore')} | Select-String -NotMatch "[\n]|[\t]//|(^using)|namespace|sealed").Line.TrimStart('	') | 
where {$_ -ne ""} | 
select -Skip 2 | 
select -SkipLast 2 | 
#clip.exe;
Out-File 'Compressed.txt';
notepad2 'Compressed.txt'
