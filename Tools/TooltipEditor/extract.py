"""pulls every plant's description text out of the C# code as templates: the exact content of
a C# interpolated string, with {holes} kept as written. non-string pieces of a concatenation
(helper calls, ternaries) become {holes} too, so any template can be written back as one $"..." """
import os, re, json, glob
PROJ = r"C:\Users\Tommy\Desktop\Garden Defense"
PLANTS = os.path.join(PROJ, r"Assets\Scripts\Entities\Plants")
DATA = os.path.join(PROJ, r"Assets\ScriptableObjects\PlantData")
HERE = os.path.dirname(os.path.abspath(__file__))

# ---------------------------------------------------------------- C# scanning helpers
def skip_string(s, i):
    """s[i] starts a string literal ($" @" $@" or "). returns index after it"""
    interp = verbatim = False
    while s[i] in '$@':
        if s[i] == '$': interp = True
        else: verbatim = True
        i += 1
    assert s[i] == '"', s[i-5:i+5]
    i += 1
    while i < len(s):
        c = s[i]
        if verbatim and c == '"':
            if i + 1 < len(s) and s[i + 1] == '"': i += 2; continue
            return i + 1
        if not verbatim and c == '\\': i += 2; continue
        if not verbatim and c == '"': return i + 1
        if interp and c == '{':
            if s[i + 1] == '{': i += 2; continue
            i = skip_hole(s, i)
            continue
        i += 1
    raise ValueError('unterminated string')

def skip_hole(s, i):
    """s[i] == '{' opening an interpolation hole. returns index after its '}'"""
    depth = 0
    while i < len(s):
        c = s[i]
        if c in '$@"' and (c == '"' or (i + 1 < len(s) and s[i + 1] in '"@$')):
            i = skip_string(s, i); continue
        if c == "'":
            i = s.index("'", i + 2) + 1 if s[i + 1] != '\\' else s.index("'", i + 3) + 1; continue
        if c in '({[': depth += 1
        elif c in ')}]':
            depth -= 1
            if depth == 0: return i + 1
        i += 1
    raise ValueError('unterminated hole')

def is_str_start(s, i):
    return s[i] == '"' or (s[i] in '$@' and i + 1 < len(s) and s[i + 1] in '"$@')

def top_level_split(expr, sep):
    """splits on a single-char operator at depth 0, outside strings"""
    parts, depth, i, last = [], 0, 0, 0
    while i < len(expr):
        c = expr[i]
        if is_str_start(expr, i): i = skip_string(expr, i); continue
        if c == "'": i = expr.index("'", i + (3 if expr[i + 1] == '\\' else 2)) + 1; continue
        if c in '({[': depth += 1
        elif c in ')}]': depth -= 1
        elif c == sep and depth == 0:
            parts.append(expr[last:i]); last = i + 1
        i += 1
    parts.append(expr[last:])
    return [p.strip() for p in parts]

def split_ternary(expr):
    """'cond ? a : b' at depth 0 -> (cond, a, b) or None"""
    depth, i, q = 0, 0, None
    while i < len(expr):
        c = expr[i]
        if is_str_start(expr, i): i = skip_string(expr, i); continue
        if c in '({[': depth += 1
        elif c in ')}]': depth -= 1
        elif depth == 0 and c == '?' and q is None:
            nxt = expr[i + 1] if i + 1 < len(expr) else ''
            prv = expr[i - 1] if i else ''
            if nxt not in '.?' and prv != '?':
                q = i
        elif depth == 0 and c == ':' and q is not None:
            return expr[:q].strip(), expr[q + 1:i].strip(), expr[i + 1:].strip()
        i += 1
    return None

def literal_content(op):
    """an operand that is exactly one string literal -> interpolated template content, else None"""
    op = op.strip()
    if not is_str_start(op, 0): return None
    end = skip_string(op, 0)
    if op[end:].strip(): return None
    j = 0; interp = False; verbatim = False
    while op[j] in '$@':
        if op[j] == '$': interp = True
        else: verbatim = True
        j += 1
    body = op[j + 1:end - 1]
    if verbatim:
        body = body.replace('""', '\\"').replace('\n', '\\n')
    if not interp:
        body = body.replace('{', '{{').replace('}', '}}')
    return body

def to_template(expr):
    """a string expression (concatenation) -> one interpolated template"""
    expr = expr.strip()
    while expr.startswith('(') and expr.endswith(')') and skip_hole(expr.replace('(', '{', 1), 0) == len(expr):
        expr = expr[1:-1].strip()
    out = []
    for op in top_level_split(expr, '+'):
        lit = literal_content(op)
        out.append(lit if lit is not None else '{' + op + '}')
    return ''.join(out)

def method_source(code, name):
    m = re.search(r'override\s+string\s+' + name + r'\s*\(([^)]*)\)\s*(=>|\{)', code)
    if not m: return None
    i = m.end()
    if m.group(2) == '=>':
        depth, j = 0, i
        while True:
            c = code[j]
            if is_str_start(code, j): j = skip_string(code, j); continue
            if c in '({[': depth += 1
            elif c in ')}]': depth -= 1
            elif c == ';' and depth == 0: break
            j += 1
        return ('expr', code[i:j].strip())
    depth, j = 1, i
    while depth:
        c = code[j]
        if is_str_start(code, j): j = skip_string(code, j); continue
        if c == '{': depth += 1
        elif c == '}': depth -= 1
        j += 1
    return ('block', code[i:j - 1])

def statements(block):
    out, depth, i, last = [], 0, 0, 0
    while i < len(block):
        c = block[i]
        if is_str_start(block, i): i = skip_string(block, i); continue
        if block.startswith('//', i):
            k = block.find('\n', i); i = len(block) if k < 0 else k; continue
        if c in '({[': depth += 1
        elif c in ')}]': depth -= 1
        elif c == ';' and depth == 0:
            out.append(block[last:i].strip()); last = i + 1
        i += 1
    return [re.sub(r'^(\s*//[^\n]*\n)+', '', s).strip() for s in out if s.strip()]

# ---------------------------------------------------------------- plant data (element, damage type)
ELEMENTS = ['Fire', 'Water', 'Grass', 'Wind', 'Poison', 'Ice', 'Neutral']
DAMAGE = ['Physical', 'Magic', 'True', 'Environmental']
def enum_names(enum):
    s = open(os.path.join(PROJ, r"Assets\Scripts\Entities\Entity.cs"), encoding='utf-8-sig').read()
    body = re.search(r'enum\s+' + enum + r'\s*\{([^}]*)\}', s).group(1)
    body = re.sub(r'//[^\n]*', '', body)
    return [x.split('=')[0].strip() for x in body.split(',') if x.strip()]
ELEMENTS = enum_names('ElementalType'); DAMAGE = enum_names('DamageType')

guid_to_prefab = {}
for meta in glob.glob(os.path.join(PROJ, r"Assets\Prefabs\Plants\*.prefab.meta")):
    g = re.search(r'^guid: (\w+)', open(meta, encoding='utf-8').read(), re.M).group(1)
    guid_to_prefab[g] = os.path.basename(meta)[:-len('.prefab.meta')]
data_by_prefab = {}
for a in glob.glob(os.path.join(DATA, '*.asset')):
    t = open(a, encoding='utf-8', errors='replace').read()
    pm = re.search(r'plantPrefab: \{fileID: \d+, guid: (\w+)', t)
    if not pm or pm.group(1) not in guid_to_prefab: continue
    def f(k, default=None):
        m = re.search(r'^  ' + k + r': (.*)$', t, re.M)
        return m.group(1).strip() if m else default
    data_by_prefab[guid_to_prefab[pm.group(1)]] = {
        'displayName': f('displayName', ''), 'element': ELEMENTS[int(f('elementalType', '0'))],
        'damageType': DAMAGE[int(f('damageType', '0'))], 'role': ['Tank', 'DPS', 'Support'][int(f('role', '0'))] if f('role') else '',
        'asset': os.path.basename(a)}

DEPRECATED = {'Cattail', 'MorningGlory', 'GhostFungus', 'Aeonium'}

# ---------------------------------------------------------------- per plant
plants, problems = [], []
for path in sorted(glob.glob(os.path.join(PLANTS, '*.cs'))):
    cls = os.path.basename(path)[:-3]
    code = open(path, encoding='utf-8-sig').read()
    if not re.search(r'override\s+string\s+GetPath1Description', code): continue
    getters = {}
    block_getters = set()
    for name in ('GetName', 'GetDescription', 'GetAttackDescription', 'GetPassiveDescription', 'GetSkillDesription'):
        src = method_source(code, name)
        if src and src[0] == 'expr':
            try: getters[name] = to_template(src[1])
            except Exception as e: problems.append((cls, name, str(e)))
        elif src:
            rets = [st[len('return '):] for st in statements(src[1]) if st.startswith('return ')]
            if len(rets) == 1:
                getters[name] = to_template(rets[0]); block_getters.add(name)
            else:
                problems.append((cls, name, 'block bodied with %d returns' % len(rets)))
    sections = []
    for n, key, title, getter in ((1, 'attack', 'Attack', 'GetAttackDescription'), (2, 'passive', 'Passive', 'GetPassiveDescription'),
                                  (3, 'skill', 'Skill', 'GetSkillDesription')):
        src = method_source(code, 'GetPath%dDescription' % n)
        sec = {'key': key, 'title': title, 'method': 'GetPath%dDescription' % n, 'overview': None, 'details': None,
               'upgrades': [], 'maxBonus': None, 'maxBonusDetails': None, 'extra': []}
        if not src or src[0] != 'block':
            problems.append((cls, key, 'path method missing or expression bodied')); sections.append(sec); continue
        locals_ = {}
        ret = None
        for st in statements(src[1]):
            m = re.match(r'(?:string|var)\s+(\w+)\s*=\s*(.*)$', st, re.S)
            if m: locals_[m.group(1)] = m.group(2).strip(); continue
            if st.startswith('return '): ret = st[len('return '):]
        def resolve(expr):
            """returns (overview, details) templates for a string expression"""
            tern = split_ternary(expr)
            if tern and tern[0] == 'details':
                return resolve(tern[2])[0], resolve(tern[1])[0]
            call = re.fullmatch(r'(\w+)\(\)', expr.strip())
            if call and call.group(1) in getters:
                return getters[call.group(1)], None
            if re.fullmatch(r'\w+', expr.strip()) and expr.strip() in locals_:
                return resolve(locals_[expr.strip()])
            t = to_template(expr)
            return t, None
        if 'desc' in locals_:
            sec['overview'], sec['details'] = resolve(locals_['desc'])
            om = re.fullmatch(r'details\s*\?.*:\s*(\w+)\(\)\s*', locals_['desc'], re.S)
            if om: sec['overviewFrom'] = om.group(1)
            if sec['details'] == sec['overview']: sec['details'] = None
        if ret is None:
            problems.append((cls, key, 'no return')); sections.append(sec); continue
        ops = top_level_split(ret, '+')
        if sec['overview'] is None:
            head = literal_content(ops[0]) or ''
            hm = re.search(r'\{(\w+)\(\)\}', head)
            if hm and hm.group(1) in getters:
                sec['overview'] = getters[hm.group(1)]
                sec['overviewFrom'] = hm.group(1)
        for op in ops[1:]:
            lit = literal_content(op)
            if op.startswith('ShiftHint('): continue
            if lit is not None and lit.startswith('Level: ['): continue
            if lit is not None and '{SkillCooldownLine()}' in lit: continue
            if lit is not None and lit.startswith('{Level5Section('):
                hole = lit[1:skip_hole(lit, 0) - 1]
                args = top_level_split(hole[len('Level5Section('):-1], ',')
                if len(args) > 1:
                    ov, de = resolve(','.join(args[1:]))
                    sec['maxBonus'], sec['maxBonusDetails'] = ov, (de if de != ov else None)
                continue
            if lit is not None:
                body = lit[:-4] if lit.endswith('\\n\\n') else lit
                sec['upgrades'].append(body)
            else:
                sec['extra'].append('{' + op + '}')
        sections.append(sec)
    info = data_by_prefab.get(cls, {})
    if not info:
        a = os.path.join(DATA, cls + 'Data.asset')
        if os.path.exists(a):
            t = open(a, encoding='utf-8', errors='replace').read()
            def f(k, default=None):
                m = re.search(r'^  ' + k + r': (.*)$', t, re.M)
                return m.group(1).strip() if m else default
            info = {'displayName': f('displayName', ''), 'element': ELEMENTS[int(f('elementalType', '0'))],
                    'damageType': DAMAGE[int(f('damageType', '0'))], 'role': ['Tank', 'DPS', 'Support'][int(f('role', '0'))] if f('role') else ''}
    plants.append({'id': cls, 'name': info.get('displayName') or cls, 'file': os.path.relpath(path, PROJ).replace('\\', '/'),
                   'element': info.get('element', ''), 'damageType': info.get('damageType', ''), 'role': info.get('role', ''),
                   'deprecated': cls in DEPRECATED, 'nameTemplate': getters.get('GetName', cls),
                   'overview': getters.get('GetDescription'), 'blockGetters': sorted(block_getters), 'sections': sections})

# ---------------------------------------------------------------- palette: every color the game's text uses
# fixed groups first (elements and damage types, from PlantData.ElementalColor/DamageTypeLabel),
# then every other <color=...> in the plant tooltips, then the rest of the scripts. each color is
# named after the text it most often colors, so "#00CED1" shows as "Armor"
from collections import Counter, defaultdict
FIXED = [
    ('Elements', [('orange', 'Fire'), ('#4FC3F7', 'Water'), ('green', 'Grass'), ('#B2EBF2', 'Wind'), ('purple', 'Poison'), ('#00FFFF', 'Ice')]),
    ('Damage types', [('#A0522D', 'Physical'), ('#FFB6C1', 'Magic'), ('white', 'True')]),
]
def color_uses(text, counts, casing, context):
    for m in re.finditer(r'<color=([^>{]+)>(.*?)</color>', text, re.S):
        raw = m.group(1).strip().strip('"')
        key = raw.lower()
        counts[key] += 1
        casing[key][raw] += 1
        inner = re.sub(r'<[^>]*>|\{[^}]*\}', ' ', m.group(2))
        inner = re.sub(r'\s+', ' ', inner).strip(' .,:;[]()+-%')
        if re.search(r'[A-Za-z]{3}', inner) and len(inner) <= 26:
            context[key][inner] += 1
    for m in re.finditer(r'<color=([^>{]+)>', text):
        pass

def templates_of(p):
    yield p['overview'] or ''; yield p['nameTemplate'] or ''
    for s in p['sections']:
        for k in ('overview', 'details', 'maxBonus', 'maxBonusDetails'): yield s[k] or ''
        yield from s['upgrades']

tip_counts, all_counts = Counter(), Counter()
casing, context = defaultdict(Counter), defaultdict(Counter)
for p in plants:
    for t in templates_of(p): color_uses(t, tip_counts, casing, context)
for f in glob.glob(os.path.join(PROJ, r"Assets\Scripts\**\*.cs"), recursive=True):
    color_uses(open(f, encoding='utf-8-sig', errors='replace').read(), all_counts, casing, context)

palette, taken = [], set()
for group, cols in FIXED:
    for value, name in cols:
        palette.append({'group': group, 'value': value, 'name': name, 'uses': tip_counts[value.lower()]})
        taken.add(value.lower())
def best_name(key):
    words = [w for w, _ in context[key].most_common(5)]
    return words[0] if words else ''
for group, counts in (('Used in plant tooltips', tip_counts), ('Used elsewhere in the game', all_counts)):
    for key, n in counts.most_common():
        if key in taken: continue
        taken.add(key)
        palette.append({'group': group, 'value': casing[key].most_common(1)[0][0], 'name': best_name(key), 'uses': n})

json.dump({'plants': plants, 'palette': palette}, open(os.path.join(HERE, 'descriptions.json'), 'w', encoding='utf-8'), indent=1, ensure_ascii=False)
print('palette:', len(palette), 'colors')
print(len(plants), 'plants;', sum(len(s['upgrades']) for p in plants for s in p['sections']), 'upgrade lines')
missing = [(p['id'], s['key']) for p in plants for s in p['sections'] if not s['overview']]
print('sections without overview:', missing)
print('extras:', [(p['id'], s['key'], s['extra']) for p in plants for s in p['sections'] if s['extra']])
print('no data asset:', [p['id'] for p in plants if not p['element']])
print('problems:', problems)
