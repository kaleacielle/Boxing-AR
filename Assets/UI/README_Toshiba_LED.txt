TOSHIBA LED TEMPLATE
====================

Requirements:
- Unity 6 Universal 3D (URP)
- Unity UI (uGUI)

After importing this package, run:
Tools > Toshiba LED > Create Templates > Create Both Templates

This creates two internal-design tests:
- 608 x 1080 -> stretched to 1920 x 1080 output
- 640 x 1920 -> stretched to 1920 x 1080 output

Open either generated scene and press Play to view the final HDMI output.
Use Tools > Toshiba LED > Open LED Preview to compare the original design
proportions with the stretched 1920 x 1080 output.

Place future game UI under:
CaptureCanvas > GameUI

Do not place game UI under OutputCanvas. OutputCanvas is reserved for displaying
the completed Render Texture across the final Windows display.

The included TestBackground.png remains inside the capture stage. Its centered
608-pixel or 640-pixel strip is selected for the matching template; the areas
labelled DO NOT SHOW are excluded before the final stretch.

