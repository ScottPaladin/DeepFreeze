DeepFreeze
==========

This is a Kerbal Space Program mod intended to allow players to remove a kerbal from being affected 
by other mods such as Life Support Mods, Acheivement mods or Fitness Mods.

This is a forked version from ScottPalandin, who has been absent from the KSP forums since December 2014.
His original code can be found in the link below and was licensed as per below, 
as such this forked version continues to be licensed the same.
https://github.com/ScottPaladin/DeepFreeze

License
==========
DeepFreeze is licensed under a Creative Commons 4.0 Share-Alike Attribution Non-Commercial license.

Attribution
==========
Sound files remixed from the following sources:
Tymaue - https://www.freesound.org/people/tymaue/sounds/79719/
KomradeJack - https://www.freesound.org/people/KomradeJack/sounds/213578/
AlaskaRobotics - https://www.freesound.org/people/AlaskaRobotics/sounds/221566/
JohnsonBrandediting - https://www.freesound.org/people/JohnsonBrandEditing/sounds/173932/

This is a release version of the original dev version of DeepFreeze by scottpalladin
As scottpalladin has been absent from the KSP forums and has not done any work on this mod since 2014
I have picked up and expanded extensively to this version. The original license allows for Sharing
and Adaption. As per the terms of the original license full credit to scottpalladin PalladinLabs 
for the original concept, ideas and dev mod.
This version will be known as
"DeepFreeze Continued..."

This mod provides the ability to freeze and thaw kerbals for those long space journeys.
Introduces glykerol resource used for freezing kerbals.
To freeze a kerbal you must have 5 glykerol units and 3000 electrical charge per kerbal.

Uses:
=====
When kerbals are frozen they do not consume life support resources (known issue with TACLS see below).
If you don't use life support mods, this mod doesn't really do much, except provide a few cool parts
and feature that doesn't do much.

Features:
=========
Large 2.5M CRY-2300 DeepFreezer for up to 10 kerbals. (with WIP internals). 
Has on-board Glykerol tank which can store up to 40 units of Glykerol.
RS-X20R radial glykerol tank for up to 20 units of extra glykerol storage.
Full featured GUI allowing you to see all frozen kerbals and freeze/thaw kerbals. 
(kerbals can also be frozen/thawed via part right click menu).
Provides monitoring capability of frozen kerbals at a cost of Electrical Charge. 
WARNING!! - use of this feature means kerbals will die if you run out of electrical charge. 
To use this feature you must change the config file settings as the default for this feature is off.
Full integration and support of Ship Manifest mod by Papa_Joe.
Module Manager config file included for support of CLS mod supported by Papa_Joe.

Known Issues:
=============
TACLS - Currently TACLS will not consume resources when kerbals are frozen, however when you thaw a 
frozen kerbal TACLS will consume life support resources for the entire time that kerbal has been frozen.
To get the current version of TACLS to ignore your kerbals and not consume resources when you thaw them 
you can either:-
Freeze on the launchpad before you launch your vessel. TACLS will not retrospectively consume any resources
 when you thaw kerbals that were frozen on the launchpad.
Freeze your kerbals and then exit the current savegame to the main menu and then reload the save game 
(because it checks for missing kerbals on startup).
Use Alternate FIX DLLS zip file in this repository. NB: License for TAC LS included in ZIP file.
You MUST first install TAC LS normally. Then install these two DLLs into 
\GameData\ThunderAerospace\TacLifeSupport directory (overwrite existing files)

Switching to/from IVA mode when freezer part only contains frozen kerbals - Camera sometimes goes a bit silly.
But you can simply zoom back out or switch view again and it corrects itself (probably as IVA mode is still WIP).
EC usage to keep kerbals alive does not operate when vessel they are aboard is not the active vessel.
EC usage turns off when timewarp is > 4x due to bugs in EC usage at high timewarp.

Planned work:
=============
Finish Internals for freezer part with textures.
New smaller freezers (4-6 kerbals) and single kerbal parts.
Extend EC usage for keeping frozen kerbals alive to include usage when vessel is not active.
Temperature controls and checks for operating the freezer parts.
Add in game config file updates via GUI. (currently can only edit outside the game and reload).

Config file setup:
==================
(located in your install directory under <kspdir>\GameData\REPOSoftTech\DeepFreeze\Plugins\Config.cfg):
DFwindowPosX, FwindowPosY - the X,Y position of the DeepFreezer GUI window.
ECreqdForFreezer - set to True or False (Default). If True will require x units of Electrical Charge per kerbal
per minute to monitor their life support systems. If the vessel they are on runs our of EC the frozen
kerbals will start dying.
UseAppLauncher - set to True (Default) or False. If True uses the stock KSP Application Launcher icons for the
DeepFreeze GUI. If False will use Toolbar by blizzy (must be installed).
debugging - Set to True or False (Default).  If true spams the KSP Log with debug messages. (leave false)
AutoRecoverFznKerbals - Set to True (Default) or False. If True (Default) frozen kerbals will be thawed on vessel
recovery automatically (for a cost, see next field). If False you have to thaw kerbals manually using
the DeepFreeze GUI (for a cost, see next field).
KSCcostToThawKerbal - This is how much thawing a kerbal at the KSC costs (only valid in career games).
