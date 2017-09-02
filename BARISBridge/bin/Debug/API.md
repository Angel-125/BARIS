# BARISBridge


# ModuleBreakableConverter
            
ModuleBreakableConverter is designed to replace ModuleResourceConverter and interface ModuleQualityControl. When a part is declared broken, it disables the harvester, and when the part is declared fixed, it re-enables it again. Almost all of the functionality is internal; all the part config designer needs to do is make sure that ModuleBreakableConverter appears before ModuleQualityControl. In addition to the MTBF checks done over time, a quality check occurs whenever the converter is started or stopped. It is part of the BARISBridge plugin.
        
## Fields

### qualityCheckSkill
What skill to use when performing the quality check. This is not always the same skill required to repair or maintain the part.
### isBroken
Flag to indicate that the part module is broken. If broken, then it can't be declared broken again by the ModuleQualityControl.
## Methods


### StartConverter
This method will start the converter. When starting, it will make a quality check. Use this method in place of StartResourceConverter.

# ICanBreak
            
The ICanBreak interface is used by ModuleQualityControl to determine which part modules in the configuration support breakable part modules. All the ModuleBreakableXXX part modules in BARIS implement this interface.
        
## Methods


### SubscribeToEvents(WildBlueIndustries.BaseQualityControl)
This method asks the implementer to subscribe to any events that it needs from ModuleQualityControl.
> #### Parameters
> **moduleQualityControl:** The BaseQualityControl object that is making the request.


### OnPartBroken(WildBlueIndustries.BaseQualityControl)
Asks the implementer to perform whatever actions are needed when the part is declared broken.
> #### Parameters
> **moduleQualityControl:** The BaseQualityControl object that is making the request.


### OnPartFixed(WildBlueIndustries.BaseQualityControl)
Called when the part is declared fixed, this method gives implementers a chance to restore their functionality.
> #### Parameters
> **moduleQualityControl:** The BaseQualityControl object that is making the request.


### ModuleIsActivated
Asks the implementer if the module is activated. Only activated modules will be considered during quality checks. The return value varies with the type of breakable part module; fuel tanks are always active, while converters, drills, and engines are only active when they are running.
> #### Return value
> True if the module is activated, false if not.

### GetCheckSkill
Asks the implementer for the trait skill used for the quality check. Examples include ScienceSkill and RepairSkill.
> #### Return value
> A string consisting of the skill used by the part module for quality checks.

# ModuleBreakableHarvester
            
ModuleBreakableHarvester is designed to replace ModuleResourceHarvester and interface ModuleQualityControl. When a part is declared broken, it disables the harvester, and when the part is declared fixed, it re-enables it again. Almost all of the functionality is internal; all the part config designer needs to do is make sure that ModuleBreakableHarvester appears before ModuleQualityControl. In addition to the MTBF checks done over time, a quality check occurs whenever the drill is started or stopped. It is part of the BARISBridge plugin.
        
## Fields

### qualityCheckSkill
What skill to use when performing the quality check. This is not always the same skill required to repair or maintain the part.
### isBroken
Flag to indicate that the part module is broken. If broken, then it can't be declared broken again by the ModuleQualityControl.
## Methods


### StartConverter
This method will start the converter. When starting, it will make a quality check. Use this method in place of StartResourceConverter.

# ModuleBreakableAsteroidDrill
            
ModuleBreakableAsteroidDrill is designed to replace ModuleAsteroidDrill and interface ModuleQualityControl. When a part is declared broken, it disables the drill, and when the part is declared fixed, it re-enables it again. Almost all of the functionality is internal; all the part config designer needs to do is make sure that ModuleBreakableAsteroidDrill appears before ModuleQualityControl. In addition to the MTBF checks done over time, a quality check occurs whenever the drill is started or stopped. It is part of the BARISBridge plugin.
        
## Fields

### isBroken
Flag to indicate that the part module is broken. If broken, then it can't be declared broken again by the ModuleQualityControl.
### qualityCheckSkill
What skill to use when performing the quality check. This is not always the same skill required to repair or maintain the part.
## Methods


### StartConverter
This method will start the asteroid drill. When starting, it will make a quality check. Use this method in place of StartResourceConverter.

# BaseQualityControl
            
This is a stub class designed to create a bridge between BARIS and mods that use BARIS. It also serves as the base class for ModuleQualityControl. It is part of the BARISBridge plugin.
        
## Fields

### qualityDisplay
Human readable quality display. Broken (0), Poor (1-24), Fair (25-49), Good (50-74), Excellent (75-100)