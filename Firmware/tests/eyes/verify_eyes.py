"""Run actual RoboEyes/U8g2-adapter code on Windows using MSVC, without flashing.

python verify_eyes.py --compiler path/to/cl.exe --output path/to/test-build
Pillow is needed only with --preview. Test stubs never enter the ESP32 sketch.
"""
import argparse
import ctypes
from pathlib import Path
import subprocess

parser = argparse.ArgumentParser()
parser.add_argument('--compiler', required=True)
parser.add_argument('--output', required=True, type=Path)
parser.add_argument('--preview', action='store_true')
options = parser.parse_args()
options.output.mkdir(parents=True, exist_ok=True)
directory = Path(__file__).resolve().parent
sketch = directory.parents[1] / 'DeskBuddy_DisplayClient'
subprocess.run([
    options.compiler, '/nologo', '/utf-8', '/Od', '/GS-', '/GR-', '/Zl', '/LD',
    '/I' + str(directory), '/I' + str(sketch), str(directory / 'eyes_test.cpp'),
    '/Foeyes_test.obj', '/link', '/NOENTRY', '/NODEFAULTLIB', '/OUT:eyes_test.dll'
], cwd=options.output, check=True)
library = ctypes.CDLL(str(options.output.resolve() / 'eyes_test.dll'))
result = library.verifyEyes()
assert result == 0, f'Renderer assertion group failed: {result}'
print('PASS: 5 moods, 450 simulated seconds; blinking, left/right gaze, footer integrity, bounds.')
if options.preview:
    from PIL import Image, ImageDraw
    canvas = Image.new('RGB', (640, 92), '#333333')
    for index, name in enumerate(['Neutral', 'Happy', 'Sad', 'Angry', 'Surprised']):
        data = (ctypes.c_ubyte * 8192)()
        library.previewEyes(index, data)
        frame = Image.frombytes('L', (128, 64), bytes(data))
        canvas.paste(frame.convert('RGB'), (index * 128, 0))
        ImageDraw.Draw(canvas).text((index * 128 + 8, 73), name, fill='white')
    canvas.resize((1280, 184), Image.Resampling.NEAREST).save(options.output / 'eyes-preview.png')
