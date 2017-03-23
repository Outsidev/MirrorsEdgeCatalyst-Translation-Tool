# MirrorsEdgeCatalyst-Translation-Tool

## What it does? 
It exports Mirror's Edge: Catalyst language subtitles to excel file and lets you import back that edited excel file.


## Parameters
-export [Game Directory] [Language shortcode]  
example : -export "C:\Games\Mirrors Edge" de  
  
-import [Game Directory] [Language shortcode] [Excel File Location]  
example : -import "C:\Games\Mirrors Edge" de "C:\Users\User\Desktop\Translation\de.xlsl"  
  
## How it works?
Export : Program must locate "[Language shortcode].toc" file inside "[Game Directory]\Patch" for extraction.  
After that it creates excel file which contains subtitles of that language at program location.    
  
Import : After editing subtitle file, program must locate corresponding cas files inside "[Game Directory]\Patch" folder.   
Then it creates backups of those files and writes new subtitles.  

##

Language shortcodes: You must use language shortcodes of toc files, which is in loc folder.  

This program uses EPPlus for exporting excel file.   
