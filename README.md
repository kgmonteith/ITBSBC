# ITBSBC

ITBSBC is a script for playing Into The Breach with a Steel Battalion Controller, because why not.

The aiming lever acts as the mouse; the trigger button is left click, the lock on button is right click. The script assumes you're playing the game on your primary display.

# Installation

1. Plug in your Steel Battalion controller. You'll need an Xbox-to-USB adapter no longer than 3 feet. Amazon has some for sale. Make sure the Xbox end is female and the USB end is male.
1. Download and install the latest [steel-batallion-64](https://sourceforge.net/projects/steel-batallion-64/) driver
    * SBC will install vJoy and the USB controller driver, as well as some required .NET packages.
1. Run the (newly-installed) "Configure vJoy" application
    * Change the "Number of Buttons" for vJoy Device #1 from 8 to 39 and click apply.
    * You should now have only one implemented vJoy device.
1. Clone this project, or download the ITBSBC.cs script.
1. Run the SBC driver wrapper (`Release\Steel_Batallion_64_v2.exe` in the SBC folder)
    * Select File > Open, choose the ITBSBC.cs script, and click "Start"
    * The controller buttons will flash five times if the driver launched successfully.
1. Enjoy the game with your completely over-the-top controller!

# Button mapping

![Button mapping](button-mapping.png "Button mapping")
