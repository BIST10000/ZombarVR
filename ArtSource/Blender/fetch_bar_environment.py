"""Acquire CC0 originals to a review directory, retaining provider hashes."""
import concurrent.futures, hashlib, json, pathlib, urllib.request

ROOT = pathlib.Path(__file__).resolve().parents[2]
DEST = ROOT / 'ArtSource/EnvironmentReview'
DEST.mkdir(parents=True, exist_ok=True)
OPENER = urllib.request.build_opener(urllib.request.ProxyHandler({}))

def fetch(url):
    return OPENER.open(urllib.request.Request(url, headers={'User-Agent': 'ZombarAssetImport/1.0'}), timeout=90).read()

def save(url, path, md5=None):
    if path.exists() and (not md5 or hashlib.md5(path.read_bytes()).hexdigest() == md5):
        return
    data = fetch(url)
    if md5 and hashlib.md5(data).hexdigest() != md5:
        raise ValueError('Provider hash mismatch: ' + url)
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_bytes(data)

def poly(asset):
    meta = json.loads(fetch('https://api.polyhaven.com/files/' + asset))
    folder = DEST / asset
    folder.mkdir(exist_ok=True)
    (folder / 'source-files.json').write_text(json.dumps(meta, indent=2))
    entry = meta['gltf']['2k']['gltf']
    save(entry['url'], folder / (asset + '.gltf'), entry['md5'])
    for relative, dependency in entry['include'].items():
        save(dependency['url'], folder / relative, dependency['md5'])
    save('https://cdn.polyhaven.com/asset_img/thumbs/' + asset + '.png?width=384&height=384', folder / 'preview.png')
    print('Downloaded and hash-verified:', asset, flush=True)

if __name__ == '__main__':
    assets = ['wine_bottles_01', 'Lantern_01', 'hanging_industrial_lamp',
              'rollershutter_window_02', 'GothicCabinet_01', 'bar_chair_round_01',
              'wooden_bookshelf_worn', 'wine_barrel_01']
    catalog = json.loads(fetch('https://api.polyhaven.com/assets?t=models'))
    (DEST / 'catalog-selection.json').write_text(json.dumps({a: catalog[a] for a in assets}, indent=2))
    with concurrent.futures.ThreadPoolExecutor(max_workers=4) as pool:
        list(pool.map(poly, assets))
    save('https://vibeassets.io/download/th0sl00gfq', DEST / 'tavern_counter.glb')
    save('https://vibeassets.io/shots/th0sl00gfq.png', DEST / 'tavern_counter.png')
    (DEST / 'counter-source.html').write_bytes(fetch('https://vibeassets.io/a/th0sl00gfq'))
    (DEST / 'polyhaven-license.html').write_bytes(fetch('https://polyhaven.com/license'))
    roster = {str(p.relative_to(DEST)).replace('\\','/'): hashlib.sha256(p.read_bytes()).hexdigest()
              for p in DEST.rglob('*') if p.is_file() and p.name != 'source-sha256.json'}
    (DEST / 'source-sha256.json').write_text(json.dumps(roster, indent=2))
    from PIL import Image, ImageDraw
    sheet = Image.new('RGB', (1280, 960), '#242326')
    draw = ImageDraw.Draw(sheet)
    for i, (name, path) in enumerate([(a, DEST / a / 'preview.png') for a in assets] + [('tavern_counter', DEST / 'tavern_counter.png')]):
        im = Image.open(path).convert('RGB'); im.thumbnail((310, 280))
        x, y = (i % 4) * 320, (i // 4) * 320
        sheet.paste(im, (x, y)); draw.text((x + 8, y + 287), name, fill='white')
    sheet.save(DEST / 'selection.jpg')
