"""Restore pinned upstream dependencies and portable Roslyn; no global installation."""
from concurrent.futures import ThreadPoolExecutor
from pathlib import Path
import urllib.request
import zipfile

ROOT = Path(__file__).resolve().parents[1]
PACKAGES = [('Crc32.NET', '1.2.0'), ('JetBrains.Annotations', '2020.1.0'),
            ('Nefarius.ViGEm.Client', '1.17.178'), ('WindowsInput', '6.3.0'),
            ('Microsoft.NET.Compilers', '3.11.0')]

def restore(item):
    name, version = item
    dest = ROOT / 'packages' / (name + '.' + version)
    if dest.exists():
        print('Using', dest.name)
        return
    url = f'https://api.nuget.org/v3-flatcontainer/{name.lower()}/{version}/{name.lower()}.{version}.nupkg'
    opener = urllib.request.build_opener(urllib.request.ProxyHandler({}))
    archive = ROOT / 'tools' / (name + '.zip')
    with opener.open(url, timeout=60) as response:
        archive.write_bytes(response.read())
    with zipfile.ZipFile(archive) as package:
        package.extractall(dest)
    print('Restored', dest.name)

if __name__ == '__main__':
    with ThreadPoolExecutor(max_workers=5) as pool:
        list(pool.map(restore, PACKAGES))
