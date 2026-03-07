"""Assemble tiles.png and props.png into a claustrophobic cave frame."""
from PIL import Image, ImageOps, ImageDraw, ImageFilter
import numpy as np
import os, math

BASE = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
ENV = os.path.join(BASE, "Assets", "Underwater Diving", "Art", "environment")
OUT = os.path.join(BASE, "Assets", "Art", "Backgrounds", "UnderwaterDiving", "foreground.png")

tiles = Image.open(os.path.join(ENV, "tiles.png")).convert("RGBA")
props = Image.open(os.path.join(ENV, "props.png")).convert("RGBA")

# Cave fill color sampled from solid tile
dark_color = (18, 38, 38)
fill = dark_color + (255,)

# --- Extract tiles ---
ground_long = tiles.crop((16, 108, 192, 176))
ground_med1 = tiles.crop((208, 108, 288, 176))
ground_med2 = tiles.crop((320, 108, 400, 176))
corner_left = tiles.crop((16, 188, 128, 448))
corner_right = tiles.crop((144, 188, 256, 448))
stalactite = tiles.crop((13, 452, 194, 640))

# --- Props ---
totem_vine = props.crop((596, 34, 716, 272))
rock_pillar = props.crop((49, 34, 134, 272))
arch_big = props.crop((177, 34, 375, 272))
ruins = props.crop((752, 34, 997, 272))
seaweed = props.crop((614, 310, 666, 470))
coral_pink = props.crop((514, 330, 562, 470))
coral_bush = props.crop((434, 360, 480, 470))
arch_small = props.crop((176, 275, 374, 480))

def mir(p): return ImageOps.mirror(p)
def flp(p): return ImageOps.flip(p)

W, H = 1920, 1080
canvas = Image.new("RGBA", (W, H), (0, 0, 0, 0))
draw = ImageDraw.Draw(canvas)

def paste(piece, x, y):
    canvas.paste(piece, (int(x), int(y)), piece)

# Wall thicknesses
TOP = 170
BOT = 160
LEFT = 190
RIGHT = 210

# --- STEP 1: Thick solid dark walls with wavy inner edges ---

# Top wall
pts = [(0, 0), (W, 0)]
for x in range(W, -1, -3):
    wave = math.sin(x*0.007)*35 + math.sin(x*0.02)*18 + math.sin(x*0.055)*8
    y = TOP + wave
    if x < 250: y += (250-x)*0.6
    if x > W-300: y += (x-(W-300))*0.7
    # Extra bulge for stalactite positions
    if 350 < x < 550: y += 40 * math.exp(-((x-450)/80)**2)
    if W-600 < x < W-400: y += 40 * math.exp(-((x-(W-500))/80)**2)
    # Arch bulge center
    if W//2-120 < x < W//2+120: y += 50 * math.exp(-((x-W//2)/60)**2)
    pts.append((x, int(y)))
draw.polygon(pts, fill=fill)

# Bottom wall
pts = [(0, H), (W, H)]
for x in range(W, -1, -3):
    wave = math.sin(x*0.009+2)*30 + math.sin(x*0.025)*14 + math.sin(x*0.06)*7
    y = H - BOT - wave
    if x < 250: y -= (250-x)*0.5
    if x > W-300: y -= (x-(W-300))*0.6
    pts.append((x, int(y)))
draw.polygon(pts, fill=fill)

# Left wall
pts = [(0, 0), (0, H)]
for y in range(H, -1, -3):
    wave = math.sin(y*0.01)*28 + math.sin(y*0.03)*14
    x = LEFT + wave
    cd = abs(y - H/2) / (H/2)
    x += (1-cd)*45
    pts.append((int(x), y))
draw.polygon(pts, fill=fill)

# Right wall
pts = [(W, 0), (W, H)]
for y in range(H, -1, -3):
    wave = math.sin(y*0.011+1)*32 + math.sin(y*0.035)*16
    x = W - RIGHT - wave
    cd = abs(y - H/2) / (H/2)
    x -= (1-cd)*55
    pts.append((int(x), y))
draw.polygon(pts, fill=fill)

# --- STEP 2: Ground surface textures at inner wall boundaries ---

# Bottom floor surface — pushed INTO the dark fill so it blends
overlap = 8
x = -30
floor_inner = H - BOT - 10
while x < W + 100:
    for piece in [ground_long, ground_med1, ground_long, ground_med2]:
        paste(piece, x, floor_inner)
        x += piece.width - overlap

# Top ceiling surface — pushed into dark fill
x = -30
ceil_inner = TOP - ground_long.height + 10
while x < W + 100:
    for piece in [ground_long, ground_med2, ground_long, ground_med1]:
        paste(flp(piece), x, ceil_inner)
        x += piece.width - overlap

# --- STEP 3: Corner formations embedded in walls ---
# Bottom-left: extends from wall into open area
paste(corner_left, LEFT - 50, H - BOT - corner_left.height + 90)
# Bottom-right
paste(mir(corner_right), W - RIGHT - corner_right.width + 50, H - BOT - corner_right.height + 90)
# Top-left
paste(flp(corner_right), LEFT - 50, TOP - 90)
# Top-right
paste(mir(flp(corner_left)), W - RIGHT - corner_left.width + 50, TOP - 90)

# --- STEP 4: Stalactites + arch ---
paste(stalactite, LEFT + 180, TOP - 20)
paste(mir(stalactite), W - RIGHT - 330, TOP - 20)
paste(arch_big, W//2 - arch_big.width//2, TOP - 50)

# --- STEP 5: Props embedded in walls ---
# Totem on left wall edge
paste(totem_vine, LEFT - 40, floor_inner - totem_vine.height + 40)
# Rock pillar on bottom right
paste(rock_pillar, W - RIGHT - 100, floor_inner - rock_pillar.height + 50)
# Ruins partially embedded in right wall
paste(ruins, W - RIGHT - 80, TOP + 10)

# Flora along the floor (inside the cave opening)
paste(seaweed, LEFT + 120, floor_inner - seaweed.height + 45)
paste(coral_pink, LEFT + 320, floor_inner - coral_pink.height + 38)
paste(coral_bush, W//2 - 30, floor_inner - coral_bush.height + 34)
paste(seaweed, W//2 + 180, floor_inner - seaweed.height + 45)
paste(coral_pink, W - RIGHT - 320, floor_inner - coral_pink.height + 38)
paste(seaweed, W - RIGHT - 520, floor_inner - seaweed.height + 45)

# Small arch on left wall interior
paste(arch_small, LEFT + 30, H//2 - arch_small.height//2 - 30)

canvas.save(OUT, "PNG")
print(f"Saved foreground frame: {OUT} ({W}x{H})")
