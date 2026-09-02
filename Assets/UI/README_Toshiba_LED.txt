TOSHIBA LED TEMPLATE
====================

Requirements:
- Unity 6 Universal 3D (URP)
- Unity UI (uGUI)

After importing this package, run:
Tools > Toshiba LED > Create Templates > Create All Templates

For the Boxing-AR-main project specifically, run:
Tools > Toshiba LED > Integrate Boxing Main Scene (640x1920)

Then run:
Tools > Toshiba LED > Apply Boxing Portrait UI Layout

To create full Boxing game scenes with the same UI at every supported size, run:
Tools > Toshiba LED > Recreate Boxing Game In All LED Sizes

This creates Main_LED_608x1080.unity and Main_LED_1920x1080.unity while keeping
Main_LED_640x1920.unity as the working master and first build scene.

That command creates Assets/Scenes/Main_LED_640x1920.unity without modifying
the original Assets/Scenes/Main.unity. It moves the existing Boxing UI into the
capture stage, keeps UIManager references, and disables the old separate UI stretch.

This creates three internal-design tests:
- LEDTemplateTest_608x1080.unity -> stretched to 1920 x 1080 output
- LEDTemplateTest_640x1920.unity -> stretched to 1920 x 1080 output
- LEDTemplateTest_1920x1080.unity -> normal 1:1 1920 x 1080 output

Open any generated scene and press Play to view its final HDMI output.
Use Tools > Toshiba LED > Open LED Preview to compare each original design
with its 1920 x 1080 HDMI output. The normal template is not distorted.

Place future game UI under:
CaptureCanvas > GameUI

Do not place game UI under OutputCanvas. OutputCanvas is reserved for displaying
the completed Render Texture across the final Windows display.

The included TestBackground.png remains inside the capture stage. Its centered
608-pixel, 640-pixel, or 1920-pixel area is selected for the matching template.
