import os
import struct
import json
import tkinter as tk
from tkinter import ttk
import matplotlib
matplotlib.use("TkAgg")
from matplotlib.backends.backend_tkagg import FigureCanvasTkAgg
import matplotlib.pyplot as plt
import numpy as np

CSV_INPUT_DIR = "CSVInput"
BIN_DIR = "bin"
OUTPUT_DIR = "Output"

def save_json_auto(values, track_name):
    os.makedirs(OUTPUT_DIR, exist_ok=True)
    save_path = os.path.join(OUTPUT_DIR, f"{track_name}.json")
    with open(save_path, "w") as f:
        json.dump(values, f, indent=4)
    print(f"Transform values saved to {save_path}")

def list_csv_files():
    csv_files = [f for f in os.listdir(CSV_INPUT_DIR) if f.endswith(".csv")]
    if not csv_files:
        print(f"No CSV files found in {CSV_INPUT_DIR}")
        return None
    print("\nAvailable CSV files:")
    for i, f in enumerate(csv_files, 1):
        print(f" {i}. {f}")
    choice = int(input("Select a CSV file: ")) - 1
    return os.path.join(CSV_INPUT_DIR, csv_files[choice])

def load_bin(track_name):
    bin_path = os.path.join(BIN_DIR, f"{track_name}.bin")
    if not os.path.exists(bin_path):
        raise FileNotFoundError(f"Bin file not found: {bin_path}")

    def read_points(reader):
        count = struct.unpack("<i", reader.read(4))[0]
        pts = [struct.unpack("<ff", reader.read(8)) for _ in range(count)]
        return np.array(pts)

    with open(bin_path, "rb") as f:
        outer = read_points(f)
        inner = read_points(f)
        raceline = read_points(f)
        playerline = []
        try:
            playerline = read_points(f)
        except:
            pass
    return outer, inner, raceline, playerline

def parse_csv(csv_path):
    with open(csv_path, "r") as f:
        header = f.readline().strip().split("\t")
        x_idx = header.index("world_position_X")
        y_idx = header.index("world_position_Y")
        track_idx = header.index("trackId")

        xs, ys = [], []
        track_name = "Unknown"

        for line in f:
            fields = line.strip().split("\t")
            if len(fields) <= max(x_idx, y_idx):
                continue
            xs.append(float(fields[x_idx]))
            ys.append(float(fields[y_idx]))
            if track_idx < len(fields):
                track_name = fields[track_idx]

    return np.array(xs), np.array(ys), track_name

def apply_transform(points, tx=0, ty=0, scale=1.0, rotation_deg=0, reflect_x=False, reflect_y=False):
    angle = np.radians(rotation_deg)
    rot_matrix = np.array([[np.cos(angle), -np.sin(angle)],
                           [np.sin(angle),  np.cos(angle)]])
    transformed = (points @ rot_matrix.T) * scale
    if reflect_x:
        transformed[:, 0] *= -1
    if reflect_y:
        transformed[:, 1] *= -1
    transformed[:, 0] += tx
    transformed[:, 1] += ty
    return transformed

def interactive_align(csv_points, outer, inner, track_name):
    root = tk.Tk()
    root.title(f"Track Alignment Tool - {track_name}")

    # Tkinter variables
    tx = tk.DoubleVar(value=0)
    ty = tk.DoubleVar(value=0)
    scale = tk.DoubleVar(value=1.0)
    rotation = tk.DoubleVar(value=0)
    reflect_x = tk.BooleanVar(value=False)
    reflect_y = tk.BooleanVar(value=False)

    # Matplotlib figure
    fig, ax = plt.subplots(figsize=(6,6))
    ax.set_aspect("equal")
    ax.plot(outer[:, 0], outer[:, 1], "r-", label="Outer")
    ax.plot(inner[:, 0], inner[:, 1], "b-", label="Inner")
    player_line, = ax.plot([], [], "g-", label="Playerline")
    ax.legend()

    canvas = FigureCanvasTkAgg(fig, master=root)
    canvas.get_tk_widget().pack(side=tk.LEFT, fill=tk.BOTH, expand=True)

    # Update function
    def update(*args):
        transformed = apply_transform(
            csv_points.copy(),
            tx.get(), ty.get(),
            scale.get(),
            rotation.get(),
            reflect_x.get(),
            reflect_y.get()
        )
        player_line.set_data(transformed[:, 0], transformed[:, 1])
        canvas.draw_idle()

    # Controls panel
    panel = ttk.Frame(root)
    panel.pack(side=tk.RIGHT, fill=tk.Y, padx=5, pady=5)

    def add_spinbox(label, var, from_, to_, step):
        frame = ttk.Frame(panel)
        frame.pack(fill="x", pady=2)
        ttk.Label(frame, text=label, width=8).pack(side="left")
        spin = ttk.Spinbox(frame, textvariable=var, from_=from_, to=to_, increment=step, width=8)
        spin.pack(side="left", fill="x", expand=True)

    add_spinbox("Tx", tx, -2000, 2000, 1)
    add_spinbox("Ty", ty, -2000, 2000, 1)
    add_spinbox("Scale", scale, 0.01, 100, 0.01)
    add_spinbox("Rot", rotation, -360, 360, 1)

    ttk.Checkbutton(panel, text="Reflect X", variable=reflect_x).pack(anchor="w", pady=2)
    ttk.Checkbutton(panel, text="Reflect Y", variable=reflect_y).pack(anchor="w", pady=2)

    # Save button
    ttk.Button(panel, text="Save JSON", command=lambda: save_json_auto({
        "tx": tx.get(),
        "ty": ty.get(),
        "scale": scale.get(),
        "rotation": rotation.get(),
        "reflect_x": reflect_x.get(),
        "reflect_y": reflect_y.get()
    }, track_name)).pack(fill="x", pady=10)

    # Trace all variables
    for var in (tx, ty, scale, rotation, reflect_x, reflect_y):
        var.trace_add("write", update)

    update()
    root.mainloop()

def main():
    csv_path = list_csv_files()
    if not csv_path:
        return

    xs, ys, track_name = parse_csv(csv_path)
    csv_points = np.column_stack([xs, ys])
    outer, inner, raceline, playerline = load_bin(track_name)

    print(f"\nLoaded track: {track_name}")
    print("Adjust transforms using the Tkinter panel. Use 'Save JSON' to export values.\n")

    interactive_align(csv_points, outer, inner, track_name)

if __name__ == "__main__":
    main()
