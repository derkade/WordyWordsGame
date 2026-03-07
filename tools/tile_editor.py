"""Visual tile assembly tool for building the Underwater Diving foreground.
Launch from Unity: Tools > Tile Editor (Underwater Diving)

Controls:
  LEFT PALETTE: click a tile to select it
  CANVAS: click to place, right-click to erase area
  F = flip vertical, M = mirror horizontal
  B = toggle dark fill brush (click+drag)
  E = toggle eraser (click+drag to erase)
  G = cycle grid snap (off / 8 / 16 / 32)
  Ctrl+Z = undo, S = save
  +/- = zoom, arrow keys = scroll, mouse wheel = scroll
  [ / ] = adjust brush/eraser size
"""
import tkinter as tk
from tkinter import messagebox
from PIL import Image, ImageOps, ImageTk, ImageDraw
import os

BASE = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
ENV = os.path.join(BASE, "Assets", "Underwater Diving", "Art", "environment")
OUT = os.path.join(BASE, "Assets", "Art", "Backgrounds", "UnderwaterDiving", "foreground.png")

CW, CH = 1920, 1080
DARK = (18, 38, 38, 255)
GRID_SIZES = [0, 8, 16, 32]

class TileEditor:
    def __init__(self, root):
        self.root = root
        root.title("Underwater Tile Editor")
        root.state("zoomed")

        self.canvas_img = Image.new("RGBA", (CW, CH), (0, 0, 0, 0))
        # Load existing foreground if it exists
        if os.path.exists(OUT):
            try:
                self.canvas_img = Image.open(OUT).convert("RGBA")
                print(f"Loaded existing: {OUT}")
            except:
                pass

        self.history = []
        self.selected_tile = None
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
        self.mouse_cx = 0  # canvas coords
        self.mouse_cy = 0
        self.needs_refresh = False

        self.tiles = {}
        self._load_tiles()
        self._build_ui()

        # Start render loop
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

    def _build_ui(self):
        # Left palette
        pf = tk.Frame(self.root, width=280, bg="#222")
        pf.pack(side=tk.LEFT, fill=tk.Y)
        pf.pack_propagate(False)

        self.status = tk.Label(pf, text="Select a tile", bg="#222", fg="white", font=("Consolas", 10))
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
        tk.Button(bf2, text="Save(S)", command=self._save, width=7, font=("Arial", 9)).pack(side=tk.LEFT, padx=1)

        bf3 = tk.Frame(pf, bg="#222")
        bf3.pack(pady=2)
        tk.Button(bf3, text="Undo ^Z", command=self._undo, width=7, font=("Arial", 9)).pack(side=tk.LEFT, padx=1)
        tk.Button(bf3, text="Clear", command=self._clear, width=7, font=("Arial", 9)).pack(side=tk.LEFT, padx=1)

        self.info_label = tk.Label(pf, text="", bg="#222", fg="yellow", font=("Consolas", 9))
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

        # Enable mouse wheel scrolling on palette
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
            btn.bind("<Button-1>", lambda e, n=name: self._select_tile(n))
            # Also bind wheel on each palette item
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
        self.canvas.bind("<MouseWheel>", self._mouse_wheel)

        self.root.bind("f", lambda e: self._toggle_flip_v())
        self.root.bind("m", lambda e: self._toggle_flip_h())
        self.root.bind("b", lambda e: self._toggle_brush())
        self.root.bind("e", lambda e: self._toggle_eraser())
        self.root.bind("g", lambda e: self._cycle_grid())
        self.root.bind("s", lambda e: self._save())
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

    def _snap(self, x, y):
        g = GRID_SIZES[self.grid_idx]
        if g == 0:
            return x, y
        return (x // g) * g, (y // g) * g

    def _screen_to_canvas(self, sx, sy):
        cx = (sx - self.offset_x) / self.zoom
        cy = (sy - self.offset_y) / self.zoom
        return int(cx), int(cy)

    def _get_tile_image(self):
        if not self.selected_tile:
            return None
        img = self.tiles[self.selected_tile].copy()
        if self.flipped_v:
            img = ImageOps.flip(img)
        if self.flipped_h:
            img = ImageOps.mirror(img)
        return img

    def _select_tile(self, name):
        self.selected_tile = name
        self.brush_mode = False
        self.eraser_mode = False
        self.flipped_v = False
        self.flipped_h = False
        w, h = self.tiles[name].size
        self.status.config(text=f"{name} ({w}x{h})")
        self._update_info()

    def _toggle_flip_v(self):
        self.flipped_v = not self.flipped_v
        self._update_info()

    def _toggle_flip_h(self):
        self.flipped_h = not self.flipped_h
        self._update_info()

    def _toggle_brush(self):
        self.brush_mode = not self.brush_mode
        self.eraser_mode = False
        self._update_info()

    def _toggle_eraser(self):
        self.eraser_mode = not self.eraser_mode
        self.brush_mode = False
        self._update_info()

    def _cycle_grid(self):
        self.grid_idx = (self.grid_idx + 1) % len(GRID_SIZES)
        self._update_info()

    def _resize_brush(self, delta):
        self.brush_size = max(8, min(200, self.brush_size + delta))
        self._update_info()

    def _update_info(self):
        parts = []
        g = GRID_SIZES[self.grid_idx]
        parts.append(f"Grid: {g}px" if g else "Grid: OFF")
        if self.flipped_v: parts.append("FLIP")
        if self.flipped_h: parts.append("MIRR")
        if self.brush_mode: parts.append(f"BRUSH({self.brush_size})")
        if self.eraser_mode: parts.append(f"ERASE({self.brush_size})")
        self.info_label.config(text=" | ".join(parts))
        self.needs_refresh = True

    def _canvas_motion(self, event):
        self.mouse_cx, self.mouse_cy = self._screen_to_canvas(event.x, event.y)
        self.needs_refresh = True

    def _save_state(self):
        self.history.append(self.canvas_img.copy())
        if len(self.history) > 40:
            self.history.pop(0)

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

        tile_img = self._get_tile_image()
        if tile_img is None:
            return

        self._save_state()
        tw, th = tile_img.size
        px, py = self._snap(cx - tw // 2, cy - th // 2)
        self.canvas_img.paste(tile_img, (px, py), tile_img)
        self.needs_refresh = True

    def _canvas_drag(self, event):
        cx, cy = self._screen_to_canvas(event.x, event.y)
        self.mouse_cx, self.mouse_cy = cx, cy
        if self.brush_mode and self.dragging:
            self._paint_brush(cx, cy)
        elif self.eraser_mode and self.dragging:
            self._paint_eraser(cx, cy)
        self.needs_refresh = True

    def _canvas_release(self, event):
        self.dragging = False

    def _canvas_right_click(self, event):
        cx, cy = self._screen_to_canvas(event.x, event.y)
        self._save_state()
        self._paint_eraser(cx, cy)
        self.needs_refresh = True

    def _canvas_right_drag(self, event):
        cx, cy = self._screen_to_canvas(event.x, event.y)
        self.mouse_cx, self.mouse_cy = cx, cy
        self._paint_eraser(cx, cy)
        self.needs_refresh = True

    def _paint_brush(self, cx, cy):
        draw = ImageDraw.Draw(self.canvas_img)
        r = self.brush_size
        draw.ellipse([cx - r, cy - r, cx + r, cy + r], fill=DARK)

    def _paint_eraser(self, cx, cy):
        draw = ImageDraw.Draw(self.canvas_img)
        r = self.brush_size
        draw.ellipse([cx - r, cy - r, cx + r, cy + r], fill=(0, 0, 0, 0))

    def _undo(self):
        if self.history:
            self.canvas_img = self.history.pop()
            self.needs_refresh = True

    def _clear(self):
        if messagebox.askyesno("Clear", "Clear the entire canvas?"):
            self._save_state()
            self.canvas_img = Image.new("RGBA", (CW, CH), (0, 0, 0, 0))
            self.needs_refresh = True

    def _save(self):
        self.canvas_img.save(OUT, "PNG")
        self.status.config(text=f"Saved! ({CW}x{CH})")
        print(f"Saved to {OUT}")

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
        self._scroll(0, event.delta // 2)

    def _render_loop(self):
        """Render at ~30fps, only when dirty."""
        if self.needs_refresh:
            self.needs_refresh = False
            self._refresh_canvas()
        self.root.after(33, self._render_loop)

    def _refresh_canvas(self):
        cw = self.canvas.winfo_width() or 1100
        ch = self.canvas.winfo_height() or 900

        # Checkerboard + canvas image
        checker = Image.new("RGBA", (CW, CH), (30, 30, 50, 255))
        cdraw = ImageDraw.Draw(checker)
        sq = 32
        for y in range(0, CH, sq):
            for x in range(0, CW, sq):
                if (x // sq + y // sq) % 2:
                    cdraw.rectangle([x, y, x + sq, y + sq], fill=(40, 40, 60, 255))

        composite = Image.alpha_composite(checker, self.canvas_img)

        # Draw grid overlay
        g = GRID_SIZES[self.grid_idx]
        if g > 0 and self.zoom >= 0.4:
            gdraw = ImageDraw.Draw(composite)
            grid_color = (80, 80, 100, 40)
            step = g
            # Only draw if not too dense
            if step * self.zoom >= 4:
                for x in range(0, CW, step):
                    gdraw.line([(x, 0), (x, CH)], fill=grid_color)
                for y in range(0, CH, step):
                    gdraw.line([(0, y), (CW, y)], fill=grid_color)

        # Ghost preview of selected tile at cursor
        if not self.brush_mode and not self.eraser_mode:
            tile_img = self._get_tile_image()
            if tile_img is not None:
                tw, th = tile_img.size
                gx, gy = self._snap(self.mouse_cx - tw // 2, self.mouse_cy - th // 2)
                # Semi-transparent ghost
                ghost = tile_img.copy()
                alpha = ghost.split()[3]
                alpha = alpha.point(lambda a: int(a * 0.5))
                ghost.putalpha(alpha)
                if 0 <= gx < CW and 0 <= gy < CH:
                    composite.paste(ghost, (gx, gy), ghost)
                    # Outline
                    odraw = ImageDraw.Draw(composite)
                    odraw.rectangle([gx, gy, gx + tw, gy + th], outline=(255, 255, 100, 120), width=1)

        # Brush/eraser cursor preview
        if self.brush_mode or self.eraser_mode:
            odraw = ImageDraw.Draw(composite)
            r = self.brush_size
            cx, cy = self.mouse_cx, self.mouse_cy
            color = (200, 100, 100, 120) if self.eraser_mode else (100, 200, 100, 120)
            odraw.ellipse([cx - r, cy - r, cx + r, cy + r], outline=color, width=2)

        # Scale to screen
        sw, sh = int(CW * self.zoom), int(CH * self.zoom)
        method = Image.NEAREST if self.zoom >= 1.0 else Image.LANCZOS
        disp = composite.resize((sw, sh), method)

        # Crop to visible window
        x1 = max(0, -self.offset_x)
        y1 = max(0, -self.offset_y)
        x2 = min(sw, cw - self.offset_x)
        y2 = min(sh, ch - self.offset_y)

        if x2 > x1 and y2 > y1:
            visible = disp.crop((x1, y1, x2, y2))
        else:
            visible = Image.new("RGBA", (max(1, cw), max(1, ch)), (30, 30, 50, 255))

        self._photo = ImageTk.PhotoImage(visible)
        self.canvas.delete("all")
        self.canvas.create_image(max(0, self.offset_x), max(0, self.offset_y), anchor="nw", image=self._photo)


if __name__ == "__main__":
    root = tk.Tk()
    app = TileEditor(root)
    root.mainloop()
