import json
import os
import struct
import tkinter as tk
from tkinter import ttk

import matplotlib

matplotlib.use("TkAgg")
import matplotlib.pyplot as plt
import numpy as np
from matplotlib.backends.backend_tkagg import FigureCanvasTkAgg

CSV_INPUT_DIR = "CSVInput"
BIN_DIR = "bin"
OUTPUT_DIR = "Output"


def get_json_path(track_name):
    return os.path.join(OUTPUT_DIR, f"{track_name}.json")


def save_json_auto(values, track_name):
    os.makedirs(OUTPUT_DIR, exist_ok=True)
    save_path = get_json_path(track_name)
    with open(save_path, "w") as f:
        json.dump(values, f, indent=4)
    print(f"Transform values saved to {save_path}")


def load_json_auto(track_name):
    path = get_json_path(track_name)
    if os.path.exists(path):
        with open(path, "r") as f:
            try:
                data = json.load(f)
                print(f"Loaded transform values from {path}")
                return data
            except Exception as e:
                print(f"Failed to load {path}: {e}")
    return None


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


def apply_transform(
    points,
    tx=0,
    ty=0,
    scale=1.0,
    rotation_deg=0,
    reflect_x=False,
    reflect_y=False,
    shear_x=0.0,
    shear_y=0.0,
):
    angle = np.radians(rotation_deg)
    rot_matrix = np.array(
        [[np.cos(angle), -np.sin(angle)], [np.sin(angle), np.cos(angle)]]
    )
    shear_matrix = np.array([[1, shear_x], [shear_y, 1]])
    transform_matrix = rot_matrix @ shear_matrix
    transformed = (points @ transform_matrix.T) * scale
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

    defaults = load_json_auto(track_name) or {}

    tx = tk.DoubleVar(value=defaults.get("tx", 0))
    ty = tk.DoubleVar(value=defaults.get("ty", 0))
    scale = tk.DoubleVar(value=defaults.get("scale", 1.0))
    rotation = tk.DoubleVar(value=defaults.get("rotation", 0))
    shear_x = tk.DoubleVar(value=defaults.get("shear_x", 0.0))
    shear_y = tk.DoubleVar(value=defaults.get("shear_y", 0.0))
    reflect_x = tk.BooleanVar(value=defaults.get("reflect_x", False))
    reflect_y = tk.BooleanVar(value=defaults.get("reflect_y", False))
    show_boundaries = tk.BooleanVar(value=True)

    fig, ax = plt.subplots(figsize=(6, 6))
    ax.set_aspect("equal")

    (outer_line,) = ax.plot(outer[:, 0], outer[:, 1], "r-", label="Outer")
    (inner_line,) = ax.plot(inner[:, 0], inner[:, 1], "b-", label="Inner")
    (player_line,) = ax.plot([], [], "g-", label="Playerline")
    ax.legend()

    canvas = FigureCanvasTkAgg(fig, master=root)
    canvas.get_tk_widget().pack(side=tk.LEFT, fill=tk.BOTH, expand=True)

    def update(*args):
        transformed = apply_transform(
            csv_points.copy(),
            tx.get(),
            ty.get(),
            scale.get(),
            rotation.get(),
            reflect_x.get(),
            reflect_y.get(),
            shear_x.get(),
            shear_y.get(),
        )
        player_line.set_data(transformed[:, 0], transformed[:, 1])
        outer_line.set_visible(show_boundaries.get())
        inner_line.set_visible(show_boundaries.get())
        canvas.draw_idle()

    panel = ttk.Frame(root)
    panel.pack(side=tk.RIGHT, fill=tk.Y, padx=5, pady=5)

    def add_spinbox(label, var, from_, to_, step):
        frame = ttk.Frame(panel)
        frame.pack(fill="x", pady=2)
        ttk.Label(frame, text=label, width=8).pack(side="left")
        spin = ttk.Spinbox(
            frame, textvariable=var, from_=from_, to=to_, increment=step, width=8
        )
        spin.pack(side="left", fill="x", expand=True)

    add_spinbox("Tx", tx, -10000, 10000, 1)
    add_spinbox("Ty", ty, -10000, 10000, 1)
    add_spinbox("Scale", scale, 0.01, 100, 0.01)
    add_spinbox("Rot", rotation, -360, 360, 0.1)
    add_spinbox("ShearX", shear_x, -5, 5, 0.01)
    add_spinbox("ShearY", shear_y, -5, 5, 0.01)

    ttk.Checkbutton(panel, text="Reflect X", variable=reflect_x).pack(
        anchor="w", pady=2
    )
    ttk.Checkbutton(panel, text="Reflect Y", variable=reflect_y).pack(
        anchor="w", pady=2
    )
    ttk.Checkbutton(panel, text="Show Boundaries", variable=show_boundaries).pack(
        anchor="w", pady=2
    )

    def save_current():
        save_json_auto(
            {
                "tx": tx.get(),
                "ty": ty.get(),
                "scale": scale.get(),
                "rotation": rotation.get(),
                "shear_x": shear_x.get(),
                "shear_y": shear_y.get(),
                "reflect_x": reflect_x.get(),
                "reflect_y": reflect_y.get(),
            },
            track_name,
        )

    ttk.Button(panel, text="Save JSON", command=save_current).pack(fill="x", pady=10)

    selected_idx = [None]

    def on_press(event):
        if event.inaxes != ax:
            return
        if player_line.get_xdata().size == 0:
            return
        x, y = event.xdata, event.ydata
        pts = np.column_stack((player_line.get_xdata(), player_line.get_ydata()))
        dists = np.hypot(pts[:, 0] - x, pts[:, 1] - y)
        idx = np.argmin(dists)
        if dists[idx] < 20:
            selected_idx[0] = idx

    def on_motion(event):
        if selected_idx[0] is None or event.inaxes != ax:
            return
        x, y = event.xdata, event.ydata
        xs, ys = list(player_line.get_xdata()), list(player_line.get_ydata())
        xs[selected_idx[0]] = x
        ys[selected_idx[0]] = y
        player_line.set_data(xs, ys)
        canvas.draw_idle()

    def on_release(event):
        selected_idx[0] = None

    canvas.mpl_connect("button_press_event", on_press)
    canvas.mpl_connect("motion_notify_event", on_motion)
    canvas.mpl_connect("button_release_event", on_release)

    for var in (
        tx,
        ty,
        scale,
        rotation,
        reflect_x,
        reflect_y,
        shear_x,
        shear_y,
        show_boundaries,
    ):
        var.trace_add("write", update)

    def on_close():
        print("Closing window, exiting cleanly...")
        root.quit()
        root.destroy()

    root.protocol("WM_DELETE_WINDOW", on_close)

    update()
    root.mainloop()
    root.quit()
    root.destroy()


def main():
    csv_path = list_csv_files()
    if not csv_path:
        return

    xs, ys, track_name = parse_csv(csv_path)
    csv_points = np.column_stack([xs, ys])
    outer, inner, raceline, playerline = load_bin(track_name)

    print(f"\nLoaded track: {track_name}")
    print(
        "Adjust transforms using the Tkinter panel. Use 'Save JSON' to export values.\n"
    )

    interactive_align(csv_points, outer, inner, track_name)


if __name__ == "__main__":
    main()
