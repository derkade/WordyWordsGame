"""Visual tile assembly tool for building the Underwater Diving foreground.
Launch from Unity: Tools > Tile Editor (Underwater Diving)

Controls:
  LEFT PALETTE: click a tile to select it for placement
  CANVAS (no tile selected):
    LEFT CLICK = select/grab a placed tile, drag to move it
    DELETE / X = delete selected placed tile
  CANVAS (tile selected from palette):
    LEFT CLICK = place a new instance
    ESC = deselect palette tile
  SCROLL WHEEL = zoom in/out (centered on cursor)
  RIGHT CLICK + DRAG = pan the canvas
  F = flip vertical, M = mirror horizontal
  B = toggle dark fill brush (click+drag)
  E = toggle eraser (click+drag to erase paint layer)
  G = cycle grid snap (off / 8 / 16 / 32)
  Ctrl+Z = undo, S = save project, Ctrl+S = export to game
  +/- = zoom, arrow keys = scroll
  [ / ] = adjust brush/eraser size
  TAB = toggle reference backdrop visibility
  PgUp/PgDn = move selected tile forward/backward in layer order
"""
import tkinter as tk
from tkinter import messagebox
from PIL import Image, ImageOps, ImageTk, ImageDraw
import os, json

BASE = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
ENV = os.path.join(BASE, "Assets", "Art", "Backgrounds", "UnderwaterDiving", "source")
OUT = os.path.join(BASE, "Assets", "Art", "Backgrounds", "UnderwaterDiving", "foreground.png")
PROJECT = os.path.join(BASE, "tools", "tile_project.json")
PAINT_FILE = os.path.join(BASE, "tools", "tile_paint_layer.png")

CW, CH = 1920, 1080
DARK = (18, 38, 38, 255)
GRID_SIZES = [0, 8, 16, 32]


class PlacedTile:
    """A tile instance on the canvas that can be selected and moved."""
    __slots__ = ("name", "x", "y", "flip_v", "flip_h")

    def __init__(self, name, x, y, flip_v=False, flip_h=False):
        self.name = name
        self.x = x
        self.y = y
        self.flip_v = flip_v
        self.flip_h = flip_h

    def to_dict(self):
        return {"name": self.name, "x": self.x, "y": self.y,
                "flip_v": self.flip_v, "flip_h": self.flip_h}

    @staticmethod
    def from_dict(d):
        return PlacedTile(d["name"], d["x"], d["y"],
                          d.get("flip_v", False), d.get("flip_h", False))


class TileEditor:
    def __init__(self, root):
        self.root = root
        root.title("Underwater Tile Editor")
        root.state("zoomed")

        # Paint layer (brush/eraser strokes)
        self.paint_layer = Image.new("RGBA", (CW, CH), (0, 0, 0, 0))
        if os.path.exists(PAINT_FILE):
            try:
                self.paint_layer = Image.open(PAINT_FILE).convert("RGBA")
                print(f"Loaded paint layer: {PAINT_FILE}")
            except:
                pass

        # Reference backdrop (existing baked foreground)
        self.reference_img = None
        self.show_reference = True
        if os.path.exists(OUT):
            try:
                self.reference_img = Image.open(OUT).convert("RGBA")
                print(f"Loaded reference backdrop: {OUT}")
            except:
                pass

        # Placed tile objects (not baked)
        self.placed_tiles = []
        self.selected_placed = None  # index into placed_tiles

        self.history = []  # list of (placed_tiles_snapshot, paint_layer_copy)
        self.palette_selection = None  # tile name from palette
        self.flipped_v = False
        self.flipped_h = False
        self.brush_mode = False
        self.eraser_mode = False
        self.brush_size = 40
        self.grid_idx = 2  # default 16px
        self.zoom = 0.65
        self.offset_x = 10
        self.offset_y = 10
        self.dragging = False
        self.drag_offset_x = 0
        self.drag_offset_y = 0
        self.panning = False
        self.pan_start_x = 0
        self.pan_start_y = 0
        self.mouse_cx = 0
        self.mouse_cy = 0
        self.needs_refresh = False

        self.tiles = {}
        self._load_tiles()
        self._load_project()
        self._build_ui()
        self._render_loop()

    def _load_tiles(self):
        tiles = Image.open(os.path.join(ENV, "tiles.png")).convert("RGBA")
        props = Image.open(os.path.join(ENV, "props.png")).convert("RGBA")

        self.tiles["ground_long"] = tiles.crop((16, 108, 192, 176))
        self.tiles["ground_med1"] = tiles.crop((208, 108, 288, 176))
        self.tiles["ground_med2"] = tiles.crop((320, 108, 400, 176))
        self.tiles["block_solid"] = tiles.crop((16, 16, 96, 96))
        self.tiles["block2"] = tiles.crop((112, 16, 192, 96))
        self.tiles["bump_sm1"] = tiles.crop((209, 16, 256, 96))
        self.tiles["bump_sm2"] = tiles.crop((272, 16, 319, 96))
        self.tiles["bump_lg1"] = tiles.crop((323, 16, 384, 96))
        self.tiles["bump_lg2"] = tiles.crop((400, 16, 461, 96))
        self.tiles["corner_L"] = tiles.crop((16, 188, 128, 448))
        self.tiles["corner_R"] = tiles.crop((144, 188, 256, 448))
        self.tiles["round_form"] = tiles.crop((272, 188, 452, 380))
        self.tiles["stalactite"] = tiles.crop((13, 452, 194, 640))
        self.tiles["round_sm1"] = tiles.crop((332, 452, 384, 530))
        self.tiles["round_sm2"] = tiles.crop((400, 452, 452, 530))

        self.tiles["rock_pillar"] = props.crop((49, 34, 134, 272))
        self.tiles["arch_big1"] = props.crop((177, 34, 375, 272))
        self.tiles["totem_plain"] = props.crop((432, 34, 524, 272))
        self.tiles["totem_vine"] = props.crop((596, 34, 716, 272))
        self.tiles["ruins"] = props.crop((752, 34, 997, 272))
        self.tiles["arch_big2"] = props.crop((176, 275, 374, 480))
        self.tiles["seaweed"] = props.crop((614, 310, 666, 470))
        self.tiles["coral_pink"] = props.crop((514, 330, 562, 470))
        self.tiles["coral_bush"] = props.crop((434, 360, 480, 470))
        self.tiles["rock_sm"] = props.crop((33, 275, 118, 480))

    def _get_tile_rendered(self, placed):
        """Get the rendered image for a PlacedTile."""
        img = self.tiles[placed.name].copy()
        if placed.flip_v:
            img = ImageOps.flip(img)
        if placed.flip_h:
            img = ImageOps.mirror(img)
        return img

    def _build_ui(self):
        # Left palette
        pf = tk.Frame(self.root, width=280, bg="#222")
        pf.pack(side=tk.LEFT, fill=tk.Y)
        pf.pack_propagate(False)

        self.status = tk.Label(pf, text="Click a tile to place, or click canvas to grab",
                               bg="#222", fg="white", font=("Consolas", 10), wraplength=270)
        self.status.pack(pady=4)

        bf = tk.Frame(pf, bg="#222")
        bf.pack(pady=2)
        tk.Button(bf, text="Flip(F)", command=self._toggle_flip_v, width=7, font=("Arial", 9)).pack(side=tk.LEFT, padx=1)
        tk.Button(bf, text="Mirr(M)", command=self._toggle_flip_h, width=7, font=("Arial", 9)).pack(side=tk.LEFT, padx=1)
        tk.Button(bf, text="Grid(G)", command=self._cycle_grid, width=7, font=("Arial", 9)).pack(side=tk.LEFT, padx=1)

        bf2 = tk.Frame(pf, bg="#222")
        bf2.pack(pady=2)
        tk.Button(bf2, text="Brush(B)", command=self._toggle_brush, width=7, font=("Arial", 9)).pack(side=tk.LEFT, padx=1)
        tk.Button(bf2, text="Erase(E)", command=self._toggle_eraser, width=7, font=("Arial", 9)).pack(side=tk.LEFT, padx=1)
        tk.Button(bf2, text="Save(S)", command=self._save_project, width=7, font=("Arial", 9)).pack(side=tk.LEFT, padx=1)

        bf3 = tk.Frame(pf, bg="#222")
        bf3.pack(pady=2)
        tk.Button(bf3, text="Undo ^Z", command=self._undo, width=7, font=("Arial", 9)).pack(side=tk.LEFT, padx=1)
        tk.Button(bf3, text="Export", command=self._export, width=7, font=("Arial", 9)).pack(side=tk.LEFT, padx=1)
        tk.Button(bf3, text="Clear", command=self._clear, width=7, font=("Arial", 9)).pack(side=tk.LEFT, padx=1)

        self.info_label = tk.Label(pf, text="", bg="#222", fg="yellow", font=("Consolas", 9), wraplength=270)
        self.info_label.pack(pady=2)

        # Scrollable palette
        pc = tk.Canvas(pf, bg="#333", highlightthickness=0)
        sb = tk.Scrollbar(pf, orient=tk.VERTICAL, command=pc.yview)
        sf = tk.Frame(pc, bg="#333")
        sf.bind("<Configure>", lambda e: pc.configure(scrollregion=pc.bbox("all")))
        pc.create_window((0, 0), window=sf, anchor="nw")
        pc.configure(yscrollcommand=sb.set)
        sb.pack(side=tk.RIGHT, fill=tk.Y)
        pc.pack(fill=tk.BOTH, expand=True)

        def _on_palette_wheel(event):
            pc.yview_scroll(int(-event.delta / 120), "units")
        pc.bind("<MouseWheel>", _on_palette_wheel)
        sf.bind("<MouseWheel>", _on_palette_wheel)

        self.palette_photos = {}
        for name, img in self.tiles.items():
            w, h = img.size
            scale = min(250 / w, 70 / h, 1.5)
            pw, ph = max(1, int(w * scale)), max(1, int(h * scale))
            preview = img.resize((pw, ph), Image.NEAREST)
            thumb = Image.new("RGBA", (260, ph + 4), (50, 50, 50, 255))
            thumb.paste(preview, (2, 2), preview)
            photo = ImageTk.PhotoImage(thumb)
            self.palette_photos[name] = photo

            fr = tk.Frame(sf, bg="#333")
            fr.pack(fill=tk.X, pady=1)
            lbl = tk.Label(fr, text=f"{name} ({w}x{h})", bg="#333", fg="#aaa", font=("Consolas", 8), anchor="w")
            lbl.pack(fill=tk.X)
            btn = tk.Label(fr, image=photo, bg="#333", cursor="hand2")
            btn.pack()
            btn.bind("<Button-1>", lambda e, n=name: self._select_palette_tile(n))
            btn.bind("<MouseWheel>", _on_palette_wheel)
            lbl.bind("<MouseWheel>", _on_palette_wheel)

        # Canvas
        self.canvas = tk.Canvas(self.root, bg="#1a1a2e", highlightthickness=0)
        self.canvas.pack(fill=tk.BOTH, expand=True)

        self.canvas.bind("<Motion>", self._canvas_motion)
        self.canvas.bind("<Button-1>", self._canvas_click)
        self.canvas.bind("<B1-Motion>", self._canvas_drag)
        self.canvas.bind("<ButtonRelease-1>", self._canvas_release)
        self.canvas.bind("<Button-3>", self._canvas_right_click)
        self.canvas.bind("<B3-Motion>", self._canvas_right_drag)
        self.canvas.bind("<ButtonRelease-3>", self._canvas_right_release)
        self.canvas.bind("<MouseWheel>", self._mouse_wheel)

        self.root.bind("f", lambda e: self._toggle_flip_v())
        self.root.bind("m", lambda e: self._toggle_flip_h())
        self.root.bind("b", lambda e: self._toggle_brush())
        self.root.bind("e", lambda e: self._toggle_eraser())
        self.root.bind("g", lambda e: self._cycle_grid())
        self.root.bind("s", lambda e: self._save_project())
        self.root.bind("<Control-s>", lambda e: self._export())
        self.root.bind("<Control-z>", lambda e: self._undo())
        self.root.bind("+", lambda e: self._zoom_in())
        self.root.bind("=", lambda e: self._zoom_in())
        self.root.bind("-", lambda e: self._zoom_out())
        self.root.bind("<Left>", lambda e: self._scroll(-40, 0))
        self.root.bind("<Right>", lambda e: self._scroll(40, 0))
        self.root.bind("<Up>", lambda e: self._scroll(0, -40))
        self.root.bind("<Down>", lambda e: self._scroll(0, 40))
        self.root.bind("[", lambda e: self._resize_brush(-8))
        self.root.bind("]", lambda e: self._resize_brush(8))
        self.root.bind("<Escape>", lambda e: self._deselect_all())
        self.root.bind("<Delete>", lambda e: self._delete_selected())
        self.root.bind("x", lambda e: self._delete_selected())
        self.root.bind("<Tab>", lambda e: self._toggle_reference())
        self.root.bind("<Prior>", lambda e: self._reorder_selected(1))   # PgUp
        self.root.bind("<Next>", lambda e: self._reorder_selected(-1))   # PgDn

    # --- Palette ---

    def _select_palette_tile(self, name):
        self.palette_selection = name
        self.selected_placed = None
        self.brush_mode = False
        self.eraser_mode = False
        self.flipped_v = False
        self.flipped_h = False
        w, h = self.tiles[name].size
        self.status.config(text=f"Placing: {name} ({w}x{h})")
        self._update_info()

    def _deselect_all(self):
        self.palette_selection = None
        self.selected_placed = None
        self.brush_mode = False
        self.eraser_mode = False
        self.status.config(text="Click a tile to place, or click canvas to grab")
        self._update_info()

    # --- Snap / coordinate helpers ---

    def _snap(self, x, y):
        g = GRID_SIZES[self.grid_idx]
        if g == 0:
            return x, y
        return (x // g) * g, (y // g) * g

    def _screen_to_canvas(self, sx, sy):
        cx = (sx - self.offset_x) / self.zoom
        cy = (sy - self.offset_y) / self.zoom
        return int(cx), int(cy)

    def _get_palette_tile_image(self):
        if not self.palette_selection:
            return None
        img = self.tiles[self.palette_selection].copy()
        if self.flipped_v:
            img = ImageOps.flip(img)
        if self.flipped_h:
            img = ImageOps.mirror(img)
        return img

    # --- History ---

    def _save_state(self):
        snapshot = [PlacedTile(t.name, t.x, t.y, t.flip_v, t.flip_h) for t in self.placed_tiles]
        self.history.append((snapshot, self.paint_layer.copy()))
        if len(self.history) > 40:
            self.history.pop(0)

    def _undo(self):
        if self.history:
            snapshot, paint = self.history.pop()
            self.placed_tiles = snapshot
            self.paint_layer = paint
            self.selected_placed = None
            self.needs_refresh = True

    # --- Hit test ---

    def _hit_test(self, cx, cy):
        """Find the topmost placed tile under (cx, cy). Returns index or None."""
        for i in range(len(self.placed_tiles) - 1, -1, -1):
            t = self.placed_tiles[i]
            img = self.tiles[t.name]
            w, h = img.size
            if t.x <= cx < t.x + w and t.y <= cy < t.y + h:
                # Check alpha at hit point
                rendered = self._get_tile_rendered(t)
                lx, ly = cx - t.x, cy - t.y
                if 0 <= lx < rendered.width and 0 <= ly < rendered.height:
                    pixel = rendered.getpixel((lx, ly))
                    if len(pixel) >= 4 and pixel[3] > 20:
                        return i
        return None

    # --- Canvas events ---

    def _canvas_motion(self, event):
        self.mouse_cx, self.mouse_cy = self._screen_to_canvas(event.x, event.y)
        self.needs_refresh = True

    def _canvas_click(self, event):
        cx, cy = self._screen_to_canvas(event.x, event.y)

        if self.brush_mode:
            self._save_state()
            self._paint_brush(cx, cy)
            self.dragging = True
            self.needs_refresh = True
            return

        if self.eraser_mode:
            self._save_state()
            self._paint_eraser(cx, cy)
            self.dragging = True
            self.needs_refresh = True
            return

        # Placing a new tile from palette
        if self.palette_selection:
            tile_img = self._get_palette_tile_image()
            if tile_img is None:
                return
            self._save_state()
            tw, th = tile_img.size
            px, py = self._snap(cx - tw // 2, cy - th // 2)
            new_tile = PlacedTile(self.palette_selection, px, py, self.flipped_v, self.flipped_h)
            self.placed_tiles.append(new_tile)
            self.selected_placed = len(self.placed_tiles) - 1
            self.needs_refresh = True
            return

        # Try to grab an existing placed tile
        hit = self._hit_test(cx, cy)
        if hit is not None:
            self.selected_placed = hit
            t = self.placed_tiles[hit]
            self.drag_offset_x = cx - t.x
            self.drag_offset_y = cy - t.y
            self.dragging = True
            self.status.config(text=f"Selected: {t.name} @ ({t.x},{t.y})")
        else:
            self.selected_placed = None
            self.status.config(text="Click a tile to place, or click canvas to grab")
        self.needs_refresh = True

    def _canvas_drag(self, event):
        cx, cy = self._screen_to_canvas(event.x, event.y)
        self.mouse_cx, self.mouse_cy = cx, cy

        if self.brush_mode and self.dragging:
            self._paint_brush(cx, cy)
            self.needs_refresh = True
            return

        if self.eraser_mode and self.dragging:
            self._paint_eraser(cx, cy)
            self.needs_refresh = True
            return

        # Dragging a placed tile
        if self.dragging and self.selected_placed is not None:
            t = self.placed_tiles[self.selected_placed]
            nx, ny = self._snap(cx - self.drag_offset_x, cy - self.drag_offset_y)
            if t.x != nx or t.y != ny:
                # Save state once at drag start (not every frame)
                if not hasattr(self, '_drag_state_saved') or not self._drag_state_saved:
                    self._save_state()
                    self._drag_state_saved = True
                t.x = nx
                t.y = ny
                self.status.config(text=f"Moving: {t.name} @ ({t.x},{t.y})")
            self.needs_refresh = True
            return

        self.needs_refresh = True

    def _canvas_release(self, event):
        self.dragging = False
        self._drag_state_saved = False

    def _canvas_right_click(self, event):
        self.panning = True
        self.pan_start_x = event.x
        self.pan_start_y = event.y

    def _canvas_right_drag(self, event):
        if self.panning:
            dx = event.x - self.pan_start_x
            dy = event.y - self.pan_start_y
            self.offset_x += dx
            self.offset_y += dy
            self.pan_start_x = event.x
            self.pan_start_y = event.y
            self.mouse_cx, self.mouse_cy = self._screen_to_canvas(event.x, event.y)
            self.needs_refresh = True

    def _canvas_right_release(self, event):
        self.panning = False

    # --- Paint ---

    def _paint_brush(self, cx, cy):
        draw = ImageDraw.Draw(self.paint_layer)
        r = self.brush_size
        draw.ellipse([cx - r, cy - r, cx + r, cy + r], fill=DARK)

    def _paint_eraser(self, cx, cy):
        draw = ImageDraw.Draw(self.paint_layer)
        r = self.brush_size
        draw.ellipse([cx - r, cy - r, cx + r, cy + r], fill=(0, 0, 0, 0))

    # --- Tile manipulation ---

    def _delete_selected(self):
        if self.selected_placed is not None:
            self._save_state()
            del self.placed_tiles[self.selected_placed]
            self.selected_placed = None
            self.status.config(text="Tile deleted")
            self.needs_refresh = True

    def _reorder_selected(self, direction):
        """Move selected tile forward (+1) or backward (-1) in layer order."""
        if self.selected_placed is None:
            return
        i = self.selected_placed
        j = i + direction
        if 0 <= j < len(self.placed_tiles):
            self._save_state()
            self.placed_tiles[i], self.placed_tiles[j] = self.placed_tiles[j], self.placed_tiles[i]
            self.selected_placed = j
            self.needs_refresh = True

    def _toggle_flip_v(self):
        if self.selected_placed is not None:
            self._save_state()
            self.placed_tiles[self.selected_placed].flip_v = not self.placed_tiles[self.selected_placed].flip_v
        else:
            self.flipped_v = not self.flipped_v
        self._update_info()

    def _toggle_flip_h(self):
        if self.selected_placed is not None:
            self._save_state()
            self.placed_tiles[self.selected_placed].flip_h = not self.placed_tiles[self.selected_placed].flip_h
        else:
            self.flipped_h = not self.flipped_h
        self._update_info()

    def _toggle_brush(self):
        self.brush_mode = not self.brush_mode
        self.eraser_mode = False
        self.palette_selection = None
        self._update_info()

    def _toggle_eraser(self):
        self.eraser_mode = not self.eraser_mode
        self.brush_mode = False
        self.palette_selection = None
        self._update_info()

    def _cycle_grid(self):
        self.grid_idx = (self.grid_idx + 1) % len(GRID_SIZES)
        self._update_info()

    def _resize_brush(self, delta):
        self.brush_size = max(8, min(200, self.brush_size + delta))
        self._update_info()

    def _toggle_reference(self):
        self.show_reference = not self.show_reference
        self.needs_refresh = True

    def _update_info(self):
        parts = []
        g = GRID_SIZES[self.grid_idx]
        parts.append(f"Grid: {g}px" if g else "Grid: OFF")
        if self.flipped_v: parts.append("FLIP")
        if self.flipped_h: parts.append("MIRR")
        if self.brush_mode: parts.append(f"BRUSH({self.brush_size})")
        if self.eraser_mode: parts.append(f"ERASE({self.brush_size})")
        if self.show_reference: parts.append("REF:ON")
        parts.append(f"Tiles: {len(self.placed_tiles)}")
        self.info_label.config(text=" | ".join(parts))
        self.needs_refresh = True

    # --- Zoom / scroll ---

    def _zoom_in(self):
        self.zoom = min(3.0, self.zoom + 0.1)
        self.needs_refresh = True

    def _zoom_out(self):
        self.zoom = max(0.15, self.zoom - 0.1)
        self.needs_refresh = True

    def _scroll(self, dx, dy):
        self.offset_x += dx
        self.offset_y += dy
        self.needs_refresh = True

    def _mouse_wheel(self, event):
        old_zoom = self.zoom
        if event.delta > 0:
            self.zoom = min(3.0, self.zoom * 1.15)
        else:
            self.zoom = max(0.15, self.zoom / 1.15)
        factor = self.zoom / old_zoom
        self.offset_x = event.x - (event.x - self.offset_x) * factor
        self.offset_y = event.y - (event.y - self.offset_y) * factor
        self.mouse_cx, self.mouse_cy = self._screen_to_canvas(event.x, event.y)
        self.needs_refresh = True

    # --- Save / Load / Export ---

    def _save_project(self):
        """Save tile placements + paint layer (non-destructive project file)."""
        data = {
            "tiles": [t.to_dict() for t in self.placed_tiles],
        }
        with open(PROJECT, "w") as f:
            json.dump(data, f, indent=2)
        self.paint_layer.save(PAINT_FILE, "PNG")
        self.status.config(text=f"Project saved ({len(self.placed_tiles)} tiles)")
        print(f"Project saved: {PROJECT}")

    def _load_project(self):
        """Load tile placements from project file."""
        if os.path.exists(PROJECT):
            try:
                with open(PROJECT) as f:
                    data = json.load(f)
                self.placed_tiles = [PlacedTile.from_dict(d) for d in data.get("tiles", [])]
                print(f"Loaded project: {len(self.placed_tiles)} tiles")
            except Exception as ex:
                print(f"Failed to load project: {ex}")

    def _export(self):
        """Bake all layers to foreground.png for the game."""
        result = Image.new("RGBA", (CW, CH), (0, 0, 0, 0))
        # Paint layer first (dark fill behind tiles)
        result = Image.alpha_composite(result, self.paint_layer)
        # Then placed tiles in order
        for t in self.placed_tiles:
            img = self._get_tile_rendered(t)
            result.paste(img, (t.x, t.y), img)
        result.save(OUT, "PNG")
        self.status.config(text=f"Exported to game! ({CW}x{CH})")
        print(f"Exported: {OUT}")

    def _clear(self):
        if messagebox.askyesno("Clear", "Clear all tiles and paint?"):
            self._save_state()
            self.placed_tiles.clear()
            self.paint_layer = Image.new("RGBA", (CW, CH), (0, 0, 0, 0))
            self.selected_placed = None
            self.needs_refresh = True

    # --- Rendering ---

    def _render_loop(self):
        if self.needs_refresh:
            self.needs_refresh = False
            self._refresh_canvas()
        self.root.after(33, self._render_loop)

    def _refresh_canvas(self):
        cw = self.canvas.winfo_width() or 1100
        ch = self.canvas.winfo_height() or 900

        # Checkerboard
        checker = Image.new("RGBA", (CW, CH), (30, 30, 50, 255))
        cdraw = ImageDraw.Draw(checker)
        sq = 32
        for y in range(0, CH, sq):
            for x in range(0, CW, sq):
                if (x // sq + y // sq) % 2:
                    cdraw.rectangle([x, y, x + sq, y + sq], fill=(40, 40, 60, 255))

        composite = checker.copy()

        # Reference backdrop (faded)
        if self.show_reference and self.reference_img is not None:
            ref = self.reference_img.copy()
            alpha = ref.split()[3].point(lambda a: int(a * 0.3))
            ref.putalpha(alpha)
            composite = Image.alpha_composite(composite, ref)

        # Paint layer
        composite = Image.alpha_composite(composite, self.paint_layer)

        # Placed tiles
        for i, t in enumerate(self.placed_tiles):
            img = self._get_tile_rendered(t)
            composite.paste(img, (t.x, t.y), img)
            # Selection highlight
            if i == self.selected_placed:
                odraw = ImageDraw.Draw(composite)
                w, h = img.size
                odraw.rectangle([t.x - 1, t.y - 1, t.x + w, t.y + h],
                                outline=(0, 255, 255, 200), width=2)

        # Grid overlay
        g = GRID_SIZES[self.grid_idx]
        if g > 0 and self.zoom >= 0.4:
            gdraw = ImageDraw.Draw(composite)
            grid_color = (80, 80, 100, 40)
            if g * self.zoom >= 4:
                for x in range(0, CW, g):
                    gdraw.line([(x, 0), (x, CH)], fill=grid_color)
                for y in range(0, CH, g):
                    gdraw.line([(0, y), (CW, y)], fill=grid_color)

        # Ghost preview of palette tile
        if self.palette_selection and not self.brush_mode and not self.eraser_mode:
            tile_img = self._get_palette_tile_image()
            if tile_img is not None:
                tw, th = tile_img.size
                gx, gy = self._snap(self.mouse_cx - tw // 2, self.mouse_cy - th // 2)
                ghost = tile_img.copy()
                alpha = ghost.split()[3]
                alpha = alpha.point(lambda a: int(a * 0.5))
                ghost.putalpha(alpha)
                if 0 <= gx < CW and 0 <= gy < CH:
                    composite.paste(ghost, (gx, gy), ghost)
                    odraw = ImageDraw.Draw(composite)
                    odraw.rectangle([gx, gy, gx + tw, gy + th],
                                    outline=(255, 255, 100, 120), width=1)

        # Brush/eraser cursor
        if self.brush_mode or self.eraser_mode:
            odraw = ImageDraw.Draw(composite)
            r = self.brush_size
            cx, cy = self.mouse_cx, self.mouse_cy
            color = (200, 100, 100, 120) if self.eraser_mode else (100, 200, 100, 120)
            odraw.ellipse([cx - r, cy - r, cx + r, cy + r], outline=color, width=2)

        # Scale to screen
        sw, sh = int(CW * self.zoom), int(CH * self.zoom)
        method = Image.NEAREST if self.zoom >= 1.0 else Image.LANCZOS
        disp = composite.resize((max(1, sw), max(1, sh)), method)

        # Crop to visible window
        x1 = max(0, -int(self.offset_x))
        y1 = max(0, -int(self.offset_y))
        x2 = min(sw, int(cw - self.offset_x))
        y2 = min(sh, int(ch - self.offset_y))

        if x2 > x1 and y2 > y1:
            visible = disp.crop((x1, y1, x2, y2))
        else:
            visible = Image.new("RGBA", (max(1, cw), max(1, ch)), (30, 30, 50, 255))

        self._photo = ImageTk.PhotoImage(visible)
        self.canvas.delete("all")
        self.canvas.create_image(max(0, int(self.offset_x)), max(0, int(self.offset_y)),
                                 anchor="nw", image=self._photo)


if __name__ == "__main__":
    root = tk.Tk()
    app = TileEditor(root)
    root.mainloop()
