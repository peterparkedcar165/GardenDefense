# reads the tutorial popups out of the game code into tutorials.json for the Tutorial Popup
# Editor page (index.html), and writes icon_jobs.json for make_icons.ps1 (the speaker portraits).
# run from anywhere: python -I Tools/TutorialEditor/extract.py
import json, os, re, glob, struct, sys

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
A = os.path.join(ROOT, 'Assets')
T = os.path.join(A, 'Scripts', 'Tutorial')


def read(p):
    with open(p, encoding='utf-8') as f:
        return f.read()


def cs_string(src, i):
    """parses the C# regular string literal starting at src[i] == '"'; returns (value, end index)"""
    assert src[i] == '"'
    out, i = [], i + 1
    esc = {'n': '\n', 't': '\t', '"': '"', '\\': '\\', 'r': '\r', '0': '\0'}
    while src[i] != '"':
        if src[i] == '\\':
            out.append(esc.get(src[i + 1], src[i + 1]))
            i += 2
        else:
            out.append(src[i])
            i += 1
    return ''.join(out), i + 1


def consts(path, cls):
    src = read(path)
    return dict(re.findall(r'public const string (\w+) = "([^"]*)";', src))


ids = consts(os.path.join(T, 'TutorialIds.cs'), 'TutorialIds')
events = consts(os.path.join(T, 'TutorialEvents.cs'), 'TutorialEvents')
speakers = re.findall(r'^\s*(\w+),?\s*$', read(os.path.join(T, 'TutorialSpeaker.cs')).split('{', 1)[1].split('}', 1)[0], re.M)

# where each event is announced in the game, for the page's event picker
event_sites = {k: [] for k in events}
for p in glob.glob(os.path.join(A, 'Scripts', '**', '*.cs'), recursive=True):
    if os.path.basename(p) in ('TutorialEvents.cs', 'TutorialContent.cs'):
        continue
    src = read(p)
    for k in events:
        if re.search(r'NotifyEvent\(TutorialEvents\.' + k + r'\b', src):
            event_sites[k].append(os.path.splitext(os.path.basename(p))[0])

# when each tutorial shows, by array name. written from the TryShow call sites (Spawning/
# ProceduralLevel, UI/FertilizerSelectionUI, Entities/Insects/SoldierAnt, Mulch/MulchManager,
# StatusEffect/ElementalEffects/ElementalDebuff); update this when a call site changes
TRIGGERS = {
    'Level1PlantSelection': (1, 'Level 1 starts, on the loadout screen.', False, 'ProceduralLevel.Start'),
    'Level1FertilizerSelection': (1, 'Level 1, when the fertilizer screen opens (the game is already paused there).', False, 'FertilizerSelectionUI.Show'),
    'Level1StartLevel': (1, 'Level 1, right after a fertilizer is picked, in the setup phase before the first wave.', False, 'FertilizerSelectionUI.CloseAfterSelect'),
    'Level1PlantInfo': (1, 'Level 1, once wave 1\'s second group has started and you have more than 18 Sun.', True, 'ProceduralLevel (wave timer)'),
    'Level1FirstMulch': (1, 'Level 1, the first time a mulch drops.', True, 'MulchManager.OnDropSpawned'),
    'Level1SoldierAntAppears': (1, '4 seconds after the first Soldier Ant ever spawns.', True, 'SoldierAnt (spawn coroutine)'),
    'Level2StartLevel': (2, 'Level 2, right after a fertilizer is picked, if Sunflower is in the loadout.', False, 'FertilizerSelectionUI.CloseAfterSelect'),
    'Level2Primers': (2, 'Level 2, 1 second after the first elemental primer lands on an insect.', True, 'ElementalDebuff.OnApply'),
    'Level3StartLevel': (3, 'Level 3, right after a fertilizer is picked, if Waterlily is in the loadout.', False, 'FertilizerSelectionUI.CloseAfterSelect'),
}

# the speaker each TryShow call passes, read from the call sites themselves
speaker_of = {}
for p in glob.glob(os.path.join(A, 'Scripts', '**', '*.cs'), recursive=True):
    src = read(p)
    for m in re.finditer(r'TryShow\(TutorialIds\.(\w+),\s*TutorialContent\.(\w+),\s*TutorialSpeaker\.(\w+)', src):
        speaker_of[m.group(2)] = m.group(3)

src = read(os.path.join(T, 'TutorialContent.cs'))
body = src.split('public static class TutorialContent', 1)[1]
class_end = None
tutorials = []
pos = 0
for m in re.finditer(r'public static readonly TutorialPage\[\] (\w+) =([^\n]*)\n', body):
    key = m.group(1)
    # the comment lines right above the array, and one on its own line
    before = body[:m.start()].rstrip().split('\n')
    lead = []
    for line in reversed(before):
        s = line.strip()
        if s.startswith('//'):
            lead.insert(0, s[2:].strip())
        elif s == '':
            continue
        else:
            break
    tail = m.group(2).strip()
    if tail.startswith('//'):
        lead.append(tail[2:].strip())
    # the array body: from the next { to its matching };
    i = body.index('{', m.end())
    j = body.index('};', i)
    arr = body[i + 1:j]
    pages = []
    for pm in re.finditer(r'new TutorialPage\(', arr):
        k = pm.end()
        while arr[k] in ' \t\r\n':
            k += 1
        text, k = cs_string(arr, k)
        rest = arr[k:].split('\n', 1)[0]
        wait = re.match(r'\s*,\s*TutorialEvents\.(\w+)', rest)
        note = re.search(r'\)\s*,?\s*//(.*)$', rest)
        pages.append({
            'text': text,
            'wait': wait.group(1) if wait else '',
            'note': note.group(1).strip() if note else '',
        })
    id_const = key if key in ids else None
    level, trigger, pauses, site = TRIGGERS.get(key, (0, '', False, ''))
    tutorials.append({
        'key': key,
        'id': ids.get(key, ''),
        'level': level,
        'speaker': speaker_of.get(key, 'AcornKnight'),
        'trigger': trigger,
        'pauses': pauses,
        'site': site,
        'comment': ' '.join(lead),
        'pages': pages,
    })
    class_end = j

# the to do comments after the class
after = src.rsplit('}', 1)[0]
tail_comments = src[src.index('\n}', src.index('public static readonly TutorialPage[] ' + tutorials[-1]['key'])) + 2:]
ideas = [l.strip()[2:].strip() for l in tail_comments.split('\n') if l.strip().startswith('//')]

# plants (speaker choices), in unlock order, from the plant registry
guid_path = {}
for meta in glob.glob(os.path.join(A, '**', '*.meta'), recursive=True):
    try:
        g = re.search(r'^guid: (\w+)', read(meta), re.M)
    except Exception:
        continue
    if g:
        guid_path[g.group(1)] = meta[:-5]
ELEMENTS = ['Fire', 'Water', 'Grass', 'Poison', 'Ice', 'Wind', 'Neutral']
# PlantData.ElementalColor, as Unity's ColorUtility reads those names (html colours)
ELEMENT_COLOR = {'Fire': '#FFA500', 'Water': '#4FC3F7', 'Grass': '#008000', 'Wind': '#B2EBF2', 'Poison': '#800080', 'Ice': '#00FFFF', 'Neutral': '#FFFFFF'}
reg = read(os.path.join(A, 'Resources', 'PlantRegistry.asset'))
plants, jobs = [], []
for n, g in enumerate(re.findall(r'guid: (\w+)', reg.split('plants:')[1]), 1):
    p = guid_path.get(g)
    if not p:
        continue
    s = read(p)
    cls = re.search(r'(?m)^  plantName: (.*)$', s).group(1).strip()
    name = re.search(r'(?m)^  displayName: (.*)$', s).group(1).strip()
    el = ELEMENTS[int(re.search(r'(?m)^  elementalType: (\d+)$', s).group(1))]
    fid, tg = re.search(r'(?m)^  icon: \{fileID: (-?\d+), guid: (\w+)', s).groups()
    tex = guid_path[tg]
    W, H = struct.unpack('>II', open(tex, 'rb').read(32)[16:24])
    x, y, w, h = 0, 0, W, H
    if fid != '21300000':
        meta = read(tex + '.meta')
        for sm in re.finditer(r'\n    - serializedVersion: 2\n      name: [^\n]*\n      rect:\n        serializedVersion: 2\n        x: ([\d.]+)\n        y: ([\d.]+)\n        width: ([\d.]+)\n        height: ([\d.]+)\n(.*?)internalID: (-?\d+)', meta, re.S):
            if sm.group(6) == fid:
                x, y, w, h = [float(v) for v in sm.group(1, 2, 3, 4)]
                break
    jobs.append({'cls': cls, 'src': os.path.relpath(tex, ROOT).replace(os.sep, '/'), 'x': int(x), 'y': int(H - y - h), 'w': int(w), 'h': int(h)})
    plants.append({'cls': cls, 'name': name, 'element': el, 'color': ELEMENT_COLOR[el], 'unlock': n, 'speaker': cls in speakers})

out = {
    'source': 'Assets/Scripts/Tutorial/TutorialContent.cs',
    'tutorials': tutorials,
    'events': [{'name': k, 'id': v, 'sites': event_sites[k]} for k, v in events.items()],
    'speakers': speakers,
    'plants': plants,
    'ideas': ideas,
}
with open(os.path.join(HERE, 'tutorials.json'), 'w', encoding='utf-8') as f:
    json.dump(out, f, ensure_ascii=False, indent=1)
with open(os.path.join(HERE, 'icon_jobs.json'), 'w', encoding='utf-8') as f:
    json.dump(jobs, f, indent=1)
sys.stdout.reconfigure(encoding='utf-8')
print(f"{len(tutorials)} tutorials, {sum(len(t['pages']) for t in tutorials)} pages, {len(events)} events, {len(plants)} plants, {len(ideas)} ideas")
