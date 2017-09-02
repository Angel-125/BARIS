            
ModuleBreakableHarvester is designed to replace ModuleResourceHarvester and interface ModuleQualityControl. When a part is declared broken, it disables the harvester, and when the part is declared fixed, it re-enables it again. Almost all of the functionality is internal; all the part config designer needs to do is make sure that ModuleBreakableHarvester appears before ModuleQualityControl. In addition to the MTBF checks done over time, a quality check occurs whenever the drill is started or stopped. It is part of the BARISBridge plugin.
        
## Fields

### qualityCheckSkill
What skill to use when performing the quality check. This is not always the same skill required to repair or maintain the part.
### isBroken
Flag to indicate that the part module is broken. If broken, then it can't be declared broken again by the ModuleQualityControl.
## Methods


### StartConverter
This method will start the converter. When starting, it will make a quality check. Use this method in place of StartResourceConverter.

