ls -Recurse *.cs | 
    where { !$_.Name.Equals("AssemblyInfo.cs") } |
    where { !$_.FullName.Contains('Ignore')} | 
    where { $_ } |
    foreach { 
    (Select-String -NotMatch "[\n]|[\t]//|(^using)|namespace|sealed" $_).Line.TrimStart('	') |
    where {$_ -ne ""} | 
    select -Skip 2 | 
    select -SkipLast 2
    } |
    Out-File 'Compressed.txt';
cat Compressed.txt | Set-Clipboard;

#notepad 'Compressed.txt'
