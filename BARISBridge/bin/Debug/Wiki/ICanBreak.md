            
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

