BARIS: Building A Rocket Isn't Simple

Building rockets can take time!
Staging events can fail!
Parts can wear out!
Event cards!

---INSTALLATION---

Copy the contents of the mod's GameData directory into your KSP's GameData folder. Your folders should look like this:
GameData/WildBlueIndustries/000ABARISBridgeDoNotDelete
GameData/WildBlueIndustries/000BARIS
GameData/ModuleManager.dll

NOTE: ModuleManager is REQUIRED.

---REVISION HISTORY---

1.10.0
- Updated to KSP 1.8

1.9.0 Test Stand Mini-Game

BE SURE TO REPLACE EXISTING BARIS BRIDGE TOO!

New Part Capability
- Launch Clamp: The stock Launch Clamp now contains a test stand. If you enable the test stand and then manipulate your vessel via the Part Action Windows or action buttons, and a part fails, then there's a chance that the part's quality will improve. The higher the part's quality, the less likely that it'll experience a part failure, and the less likely that it has a chance to improve. There's also a chance that the vessel will explode when performing tests... and the chance goes up each time that a part fails...

Bug fixes & enhancements
- If Snacks is installed and Stress is enabled, part failures and staging failures will cause Stress!
- Seconds per day and seconds per year are now calculated based on the homeworld's solar day and solar year instead of hard coded from KSP's 6hr/24hr day.
- Fixed issue where repair projects weren't being saved and loaded properly.
- Performance improvements when applying game settings.
- NRE fixes.

1.8.3
- Fuel tanks now have a new action: Detonate Explosives.

1.8.2
- Fixed event cards that shouldn't appear when Kerbal Construction Time is installed.
- Fixed vehicle integration button in space center view being shown when Kerbal Construction Time is installed.

1.8.1
- Bug fixes

1.8.0
- The Mk1 LES can now fail.
- Parachutes can now fail to deploy and might be cut loose in the event of a part failure.
- Recompiled for KSP 1.6
- Bug fixes.

1.7.0
- New setting: Email vessel repair requests - Instead of interrupting your game with a popup containing a list of broken vessels, email you the list instead.
- New setting: Report astronaut skill checks - Turns on/off the screen messages telling you when an astronaut's skill saved the day.
- Added blacklist to resources that can't leak. Ablator, SolidFuel, and SRMFuel are on that list.
- Removed Astronaut On Vacation event card since it kills astronauts. it will be revisited later.
- Reduced explosion potential of parts that explode during launch failures.

1.6.5
- Minor fixes

1.6.4
- Bug fixes

1.6.3
- Bug fixes

1.6.2
- Bug Fixes

1.6.1
- Bug fixes

1.6
- You can now mothball vessels that are landed or splashed. Mothballing a vessel means that it'll be drained of all resources (and will dump any resources transfered to it) and all its breakable part modules will disable functionality as if broken. However, the vessel will retain its quality and MTBF ratings, and it won't be checked for quality updates. When you mothball a vessel, it can be reactivated, but it takes one day per metric ton of vessel. A highly skilled kerbal with the RepairSkill can reactivate a mothballed vessel immediately. You can mothball a vessel from BARIS's in-flight screen.
- Fixed thumbnail images not showing up in the editor.
- Renamed file snapshots so that the save folder doesn't get clogged with vessel snapshots.
- Made sure that the vessel snapshots are cleared when the editor bay is cleared.
- Fixed issues with Rush job when Kerbal Alarm Clock is installed.
- Fixed divide by zero error when adding a new vessel to integrate into an editor bay, and the bay has no workers.
- Fixed issue with the problem report GUI where it kept showing up repeatedly even after dismissing it.
- Fixed initial quality issue when vehicle required integration but launch failures aren't enabled.
- Improved GUI rendering in the Tracking Station view.
- Reduced funding and science costs of tiger teams attempting to repair a vessel in flight from the Tracking Station.
- Recompiled for KSP 1.4.3

1.5.0
- Recompiled for KSP 1.4.1

1.4.9
- Added 0-workers and max-workers buttons to the Space Center hiring screen and High bay/hangar bay views.
- Adding/removing workers won't happen at a glacial pace any more.
- Maintenance result feedback messages now displayed even when performing maintenance during EVAs.
- Part condition summary info panels in the flight screen can now be scrolled.
- Fixed Intestinal Fortitude event card's tendency to kill astronauts when they should become BadS.
- Fixed situations where event cards would show empty events.
- Fixed incorrect parts mismatch error message that appears when loading a vessel from a High Bay/Hangar Bay when you already have another vessel in the editor.
- Fixed NREs that happen when changing BARIS settings while creating a new game.
- Fixed vessel quality ratings that exceed the maximum allowed quality cap.

1.4.8
- MM Patch syntax updates.
- SSTU MM patch- thanks AkiraR!

1.4.7

KAC Vehicle Integration
- Vessel integration timer now relies on the KAC alarm, significantly improving accuracy.
- Vessel integration timer now accurately reflects delays due to work stoppage events.
- If you delete your integration alarm before vehicle integration is completed, then the corresponding VAB/SPH bay will be cleared as well and its integration efforts lost.
- The High Bay/Hangar Bay Load button won't be available until vehicle integration has completed.
- The High Bay/Hangar Bay vessel integration shows the final reliability after vehicle integration.
NOTE: These improvements only apply to new vessel integration, not existing projects.

Bug Fixes & Enhancements
- Fixed missing user message during launch failures.

1.4.5
- Flight control state changes ignored when performing an EVA or switching vessels.
- Fixed crash that happened when installing BARIS into an existing save and switching to an active vessel.
- Fixed crash that happened in career mode when trying to integrate vessels in the VAB/SPH.
- Fixed GUI issues in Tracking Station view.
- Max workers per facility is now calculated based on max workers per bay and max number of bays.
- Vehicle integration can now be done independently of launch failures.
- Reduced Test Bench science costs for improving part quality.

1.4
- Max workers increased to 200.
- You can adjust worker productivity in the configuration file. See IntegrationPerWorker.
- Reputation now affects worker productivity during vehicle integration.
- Facility level now affects worker productivity. See FacilityIntegrationMultiplier in the constants file.
- Fix for images not matching up properly with vehicle in the bay.
- Fix for error message requiring equipment when material requirements are disabled.
- Removed quality checks that happened when a fuel tank was emptied or filled to capacity- it was causing spam upon vehicle load.
- Timers are now reset when you activate or deactivate BARIS to prevent reliability check spamming when you leave BARIS off in your save for long periods of in-game time.
- In the options screen, clarified that any part that stores resources, not just fuel tanks, can spring leaks and fail.
- Add ability to customize the ModuleBreakableFuelTank leak messages.
- Parts with the Ablator resource now have more appropriate resource leak messages.
- Added option to allow command pods, cockpits, and probe cores to fail. If unchecked, they will never fail.
- Added option to allow parts to fail if they have crew capacity. If unchecked, a part with crew capacity will never fail.
- Command pods, cockpits, and probe cores are no longer excluded from part failures- unless they're not allowed to fail.
- Parts with ElectricCharge can now fail.
- Removed overall vessel reliability check that was set up during launch; it wasn't doing much except causing people to complain that their launches failed near the end of their flight to orbit.
- Fixed vessel modified spam that occurs when you attempt to modify a vessel after it has been integrated.
- Vessel integration completed dialog no longer shows integration bays that are empty.
- Edited vehicle integration status messages to improve clarity.
- Fixed missing title in "Has suffered another component failure" message.
- Fix for KCT vehicle integration.
- KSP 1.3.1 Update

1.3.9.3 PRE-RELEASE
- Removed quality checks that happened when a fuel tank was emptied or filled to capacity- it was causing spam upon vehicle load.
- Timers are now reset when you activate or deactivate BARIS to prevent reliability check spamming when you leave BARIS off in your save for long periods of in-game time.
- In the options screen, clarified that any part that stores resources, not just fuel tanks, can spring leaks and fail.
- Add ability to customize the ModuleBreakableFuelTank leak messages.
- Parts with the Ablator resource now have more appropriate resource leak messages.
- Added option to allow command pods, cockpits, and probe cores to fail. If unchecked, they will never fail.
- Added option to allow parts to fail if they have crew capacity. If unchecked, a part with crew capacity will never fail.
- Command pods, cockpits, and probe cores are no longer excluded from part failures- unless they're not allowed to fail.
- Experimental: Parts with ElectricCharge can now fail.

1.3.9.2 PRE-RELEASE
- Removed overall vessel reliability check that was set up during launch; it wasn't doing much except causing people to complain that their launches failed near the end of their flight to orbit.
- Fixed vessel modified spam that occurs when you attempt to modify a vessel after it has been integrated.
- Vessel integration completed dialog no longer shows integration bays that are empty.
- Edited vehicle integration status messages to improve clarity.
- Fixed missing title in "Has suffered another component failure" message.

1.3.9.1 PRE-RELEASE
- Reworked MM patches to exclude command pods, cockpits, and probe cores from part failures. Investigating an alternate means to add them back in as an option.
- Replaced individual MM patches for fuel tank resources with a generic patch.

1.3.8 PRE-RELEASE
- Fix for KCT vehicle integration.

1.3.7
- Tracking Station view now displays vehicle healths based upon the enabled filter buttons.

1.3.6
- Fixed issue where parts on an unloaded vessel weren't being correctly declared fixed during a Tiger Team repair.
- Tiger Team repairs will optionally drop you out of timewarp once the research attempt is completed. See the Constants file to set the option.
- Fixed Tiger Team wording that appears upon a failed attempt.
- Tiger Team repairs now ensure that the repaired quality doesn't exceed the max quality.
- Slight increase to the base chance of a Tiger Team repair attempt to balance fun and frustration factor.
- Focus Vessel view no longer appears to report broken vessels that are in physics range.
- Added broken vessel screen messages for any vessel in physics range that isn't the active vessel and that develops a problem.
- Vessels classified as debris won't appear in the Tracking Station view or in the Focus Vessel view.

1.3.5
Tiger Teams
- When vessels break, you can now go to the Tracking Station and assemble a Tiger Team on the ground to try to resolve the problem. Think of the brilliant engineers who came up with a plan to save Apollo 13. It will cost Funds, Science, and time. You also need a working CommNet connection to the vessel (if CommNet is enabled). There is no guarantee of success. In fact, the odds of success are pretty low. If successful, your Tiger Team will fix the striken vessel without the need for an onboard mechanic or the need for Equipment. If unsuccessful, you can assemble another Tiger Team and try to come up with a fresh perspective.

Constants
- Added MinimumTigerTeamRepairDays and TigerTeamRepairTarget.

1.3.0
MTBF
- Deactivated parts will lose MTBF at a slower rate (default: 1/10th) than activated parts. Think hibernation mode... 
NOTE: the hibertating MTBF decay rate is varied per part. Deactivated engines lose MTBF slower than deactivated fuel tanks, for instance. Look at MM_BARIS.cfg for examples.

Launch Escapes
- Base chance to escape an exploding vehicle is now based on the kerbal with the highest Stupidity in the crew.

Fuel Tanks
- Additional MM patches for fuel tanks- thanks Hotaru! :)
- Fuel tanks will make quality checks when the player locks or unlocks its resources.
- Fuel tanks will make quality checks when a resource is emptied or filled to capacity.
- Fuel tanks will be considered deactivated if all their resources are locked.
- You can toggle the lock/unlock state of all resources in a tank using the new "Toggle Resource Locks" context menu button and Action Group button.

Bug Fixes
- Fixed bug where parts were losing MTBF faster than intended.
- Fixed bug where some parts were allowed to break even when prevented from breaking.

1.2.7
Event Cards
- Vehicle integration completed event result won't be available when KCT is installed.

MTBF
- Part condition is now based on MTBF instead of part quality.
- New Condition summary: Maintenance Required - When MTBF runs out, this summary will indicate that the part needs periodic maintenance in order to maintain its quality.
- As parts gain flight experience, they'll gain MTBF in addition to their flight experience bonus.
- MTBF capped at 5,112 hours (2 kerbal-years).
- ModuleQualityControl now allows part-based MTBF caps just like it does with quality rating caps. These caps override the global settings.
- Fixed issue where MTBF improvements gained through part upgrades wasn't being applied.
- Fixed issue where MTBF improvements gained through facility upgrades wasn't being applied.

Configuration
- Added new Constants.cfg file; many of BARIS's constant values can be configured with this file, including the MTBF cap...

Other Fixes
- Fixed issue where the odds of a part exploding during launch and/or post-launch weren't honoring a 0% chance.

1.2.5
Test Bench
- Test Bench now calculates the simulation costs based upon breakable parts, not all the parts in the vessel.
- Test Bench lists both initial and integrated Reliability, both before and after purchasing a Reliability upgrade.
- Test Bench lets you add 1, 5, and 10 points of Reliability at a time, with appropriate cost increases.
- For KCT users, the VAB/SPH BARIS button will show what the vessel's reliability will be after construction.

Escape Pods
- Removed escape pod flag from ModuleQualityControl; it was causing confusion.

Part Failures
- Messages are now displayed in the correct sequence during a staging event's critical failure.
- Added new Settings option to give parts the option to potentially explode during failures. It is off by default. They can explode during failed staging events and during post-launch critical failures when the part has run out of MTBF.
- Added new Settings option to control how likely a part will explode during staging events.
- Added new Settings option to control how likely a part will explode during a post-launch activity when the part critically fails and is out of MTBF.
- Critical failures are less likely to happen during post-launch activities.
- Revised engine failure modes: shutdown (75% chance unless engine can't shut down, in which case it explodes); stuck on (20% chance unless engine can't shut down, in which case it explodes); explode (5% chance).
- Increased the maximum number of seconds in which to make a staging check.
- Fixed persistence issues with broken parts.
- Crew will no longer try to escape an unkermanned vessel.

Event Cards
- The available event cards now depend upon the current game mode.

1.2.0
- Fixed issues with event card tips repeatedly showing up.
- Tool tip cards won't show up if BARIS is disabled.
- Parts on new vessels will receive default flight experience points when launches can't fail.
- Parts created in the field will receive default flight experience if none existed before. This will help with incorporating BARIS into existing games.
- Improvements to flight experience will now affect vessels currently undergoing vehicle integration.
- Fixed issue with flight experience not being added to parts after vehicle integration was completed.
- New VAB/SPH button: Test Bench - If you assemble a collection of parts in the VAB/SPH, you can spend Science in Science Sandbox and Career games and/or Funds in Career games to simulate launch conditions and gain flight experience. That flight experience will improve your vessel Reliability ratings on future craft. You can adjust the per-part Funds cost in the Difficulties screen; Science cost is based on how many flights it takes to gain a flight experience bonus point.
- Vehicle integration rush jobs are now only available in Career games.

1.1.0
- Created a bridging dll for those who want to optionally incorporate BARIS into their mods.
- Event cards won't spawn during high timewarp, but if the timer expires then you'll receive an event card once you exit timewarp.
- New debug option: In the editor, you can add flight experience to parts in the currently loaded vessel. Similarly, you can do the same for the active vessel in flight. This should help players integrate BARIS into existing saves.
- Vessels will make a Reliability check a few minutes after launch if you haven't already achieved orbit. This will cover SSTOs and add extra pucker factor.
- Added new "Event Card" hints for first-time players.
- Clarified KSPedia entry for KCT support.
- Moved stock launch clamp to Engineering 101 to help with early-game static fire tests.
- When staging events fail, parts might still gain flight experience. The odds of that happening is configurable from the BARIS settings screen.
- Fixed issue with Astronaut payroll not showing up when KCT is installed.
- Dropped support for KSP 1.2.2; you can thank the bridging dll for that...

1.0.5
- Fixed issue where Event Cards would happen even when BARIS is disabled.
- Updated "Reliability & MTBF" KSPedia page to show where to find a part's MTBF.
- Added new KSPedia pages describing how to enable BARIS.

1.0
- Eliminated duplicate warnings that appear when you revert a non-integrated vessel back to the editor.
- Quality Check event results can be performed as soon as the event card is played instead of waiting for the next quality check.
- Added Micrometeor Damage event card.

0.9
- Added event card images.
- Added KSPedia.
- Added new vehicle integration status screen to the Space Center screen.
- Updated Wiki with new pages and updated existing ones.
- Bug fixes.

0.8
- Fixed an issue where High Bays/Hangar Bays and construction worker GUI would appear even when vehicle integration is turned off and/or when launches can fail.
- Fixed an issue where the BARIS GUI was available when BARIS itself was disabled ("Parts Can Break" option is off).
- Added the Mk1 Launch Escape System. It is designed to fit over the stock Mk16 Parachute but can also be fitted to size 0 parts.

0.7.0
- Added support for KerbalConstructionTime. If installed, then the High Bay/Hangar Bay screen won't be available, and the Space Center screen won't have construction workers.

0.6.0
- Added Event Cards into BARIS. Event Cards will affect your game in various ways; you can enable/disable cards as well as control their frequency of appearing via the Settings->Difficulty dialog. With Debug mode turned on, you can play individual cards as desired from the Space Center BARIS screen.

WANTED: If you're willing to donate screen shots for the cards, I need 256 x 256 images that are appropriate for the event card's subject.

- Fixed an issue with adding/removing workers.
- Added Race Into Space music by Brian Langsbard. License: GPL V2

0.5.3
- Added KSP 1.2.2 version of the BARIS.dll. It is located in the Extras/KSP122DLL folder.

0.5.2
- Fixed another case where you'd get integration warning spam in the editor.
- Fixed an issue where, if you cancel a vehicle integration, the allocated workers were lost.
- Difficulty Modifier is now done by category instead of a raw value.

0.5.1

- Fixed integration warning spam in the editor.
- Fixed duplicate editor bays appearing after upgrading the VAB/SPH.
- Fixed NRE that happens when trying to rebuild the vessel summary cache and there are no vessels in the game.

0.5.0 Pre-release alpha

---ACKNOWLEDGEMENTS

Module Manager by Sarbian

Event Card Images By Squad: BrainDrain, CorporateSponsorship, FluShots, HazzardPay, ManufacturingDefects, Protests, SafetyInspections, SpeedChallenge, WorkerStrike

AstronautOnVacation image by Malaclypse

---LICENSE---
Race Into Space music by Brian Langsbard. License: GPL V2

Art Assets, including .mu, .mbm, and .dds files but excluding Event Card Images By Squad and AstronautOnVacatin image are copyright 2014-2016 by Michael Billard, All Rights Reserved.

Wild Blue Industries is trademarked by Michael Billard. All Rights Reserved.
Note that Wild Blue Industries is a ficticious entity 
created for entertainment purposes. It is in no way meant to represent a real entity.
Any similarity to a real entity is purely coincidental.

Source code copyright 2014-2017 by Michael Billard (Angel-125)

    This source code is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.