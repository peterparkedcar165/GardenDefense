# builds index.html (the page Tommy opens) from page.html, inlining the game font and the popup
# frame sprite. run extract.py first when the tutorials change, and make_icons.ps1 for new plants
import base64, os
HERE = os.path.dirname(os.path.abspath(__file__))
ASSETS = os.path.join(HERE, '..', '..', 'Assets')
font = base64.b64encode(open(os.path.join(ASSETS, 'Fonts', 'GardenDefense.ttf'), 'rb').read()).decode()
frame = base64.b64encode(open(os.path.join(ASSETS, 'Sprites', 'UI', 'chatFrame.png'), 'rb').read()).decode()
page = open(os.path.join(HERE, 'page.html'), encoding='utf-8').read()
out = page.replace('__FONT_B64__', font).replace('__FRAME_B64__', frame)
open(os.path.join(HERE, 'index.html'), 'w', encoding='utf-8', newline='\n').write(out)
print('index.html', len(out) // 1024, 'KB')
