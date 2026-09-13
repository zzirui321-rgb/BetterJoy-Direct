from pathlib import Path
import json
import re

out = Path(__file__).resolve().parents[1] / 'outputs'
text = (out / 'interactive-reconnect-test.txt').read_text(encoding='utf-8-sig')
rows = re.findall(r'xinput=(\d+) packet=(\d+) buttons=(\d+) axes=(-?\d+),(-?\d+),(-?\d+),(-?\d+)', text)
assert rows, 'No XInput evidence'
axes = [list(map(int, row[3:])) for row in rows]
summary = {
    'xinput_samples': len(rows),
    'buttons_observed': sorted({int(row[2]) for row in rows}),
    'axes_min': [min(row[i] for row in axes) for i in range(4)],
    'axes_max': [max(row[i] for row in axes) for i in range(4)],
    'final_axes': axes[-1],
    'physical_disconnects_detected': text.count('Removed dropped controller.'),
    'controller_connections': text.count('Pro controller connected.'),
    'steam_running': 'Steam running: True' in text,
    'output_errors': re.findall(r'outputError=(.+)', text),
}
assert summary['physical_disconnects_detected'] >= 1
assert summary['controller_connections'] >= 2
assert summary['final_axes'] == [0, 0, 0, 0]
assert not summary['steam_running'] and not summary['output_errors']
(out / 'hardware-summary.json').write_text(json.dumps(summary, indent=2), encoding='utf-8')
print(json.dumps(summary))
