"""writes Tooltip Editor edits back into the plant scripts.
usage: python apply.py <folder of edit json files, as ArtifactData list out_dir saves them>
each edit's text is the content of a C# $"..." string. it replaces exactly the code piece the
extractor read it from, written one paragraph per line like the rest of the code. anything this
can't place safely stops with an error instead of guessing"""
import os, re, json, glob, sys
HERE = os.path.dirname(os.path.abspath(__file__))
_src = open(os.path.join(HERE, 'extract.py'), encoding='utf-8').read()
exec(_src.split('# ---------------------------------------------------------------- plant data')[0])

PATH_METHOD = {'attack': 'GetPath1Description', 'passive': 'GetPath2Description', 'skill': 'GetPath3Description'}

def method_span(code, name):
    """(kind, start, end) of a method's expression ('expr') or block body ('block') in code"""
    m = re.search(r'override\s+string\s+' + name + r'\s*\(([^)]*)\)\s*(=>|\{)', code)
    if not m: raise SystemExit('no method ' + name)
    i = m.end()
    if m.group(2) == '=>':
        depth, j = 0, i
        while True:
            c = code[j]
            if is_str_start(code, j): j = skip_string(code, j); continue
            if c in '({[': depth += 1
            elif c in ')}]': depth -= 1
            elif c == ';' and depth == 0: return 'expr', i, j
            j += 1
    depth, j = 1, i
    while depth:
        c = code[j]
        if is_str_start(code, j): j = skip_string(code, j); continue
        if c == '{': depth += 1
        elif c == '}': depth -= 1
        j += 1
    return 'block', i, j - 1

def statement_spans(code, a, b):
    """[(start, end)] of statements between a and b, end at the ';'"""
    out, depth, i, last = [], 0, a, a
    while i < b:
        c = code[i]
        if is_str_start(code, i): i = skip_string(code, i); continue
        if code.startswith('//', i): i = code.find('\n', i); continue
        if c in '({[': depth += 1
        elif c in ')}]': depth -= 1
        elif c == ';' and depth == 0: out.append((last, i)); last = i + 1
        i += 1
    return out

def ternary_spans(code, a, b):
    """for 'cond ? x : y' in code[a:b] -> ((x_start, x_end), (y_start, y_end)) or None"""
    depth, i, q = 0, a, None
    while i < b:
        c = code[i]
        if is_str_start(code, i): i = skip_string(code, i); continue
        if c in '({[': depth += 1
        elif c in ')}]': depth -= 1
        elif depth == 0 and c == '?' and q is None and code[i + 1] not in '.?' and code[i - 1] != '?': q = i
        elif depth == 0 and c == ':' and q is not None: return (q + 1, i), (i + 1, b)
        i += 1
    return None

def strip_span(code, a, b):
    while a < b and code[a].isspace(): a += 1
    while b > a and code[b - 1].isspace(): b -= 1
    return a, b

def is_whole_hole(p):
    return p.startswith('{') and not p.startswith('{{') and skip_hole(p, 0) == len(p)

def fmt_multiline(template, indent):
    parts, rest = [], template
    while rest:
        k = rest.find('\\n\\n')
        if k < 0: parts.append(rest); break
        parts.append(rest[:k + 4]); rest = rest[k + 4:]
    ops = [p[1:-1] if is_whole_hole(p) else '$"' + p + '"' for p in parts]
    return (' +\n' + indent).join(ops)

def fmt_inline(template):
    return '$"' + template + '"' if re.search(r'[{}]', template) else '"' + template + '"'

def continuation_indent(code, a, b, fallback):
    seg = code[a:b]
    m = re.search(r'\n([ \t]*)\S', seg)
    return m.group(1) if m else fallback

def line_indent(code, pos):
    start = code.rfind('\n', 0, pos) + 1
    return re.match(r'[ \t]*', code[start:]).group(0)

def replace(code, a, b, new):
    return code[:a] + new + code[b:]

def apply_edit(code, e, section_info):
    sec, part = e['section'], e['part']
    if e.get('isNew') or e.get('deleted'):
        raise SystemExit('new or deleted pieces need a hand placement: ' + str((e['plant'], sec, part, e['index'])))
    text = e['text']
    if sec == 'plant':
        kind, a, b = method_span(code, 'GetDescription')
        if kind != 'expr': raise SystemExit('GetDescription is a block')
        a, b = strip_span(code, a, b)
        return replace(code, a, b, fmt_multiline(text, line_indent(code, a)))
    getter = section_info.get('overviewFrom') if part == 'overview' else None
    if getter:
        kind, a, b = method_span(code, getter)
        if kind == 'block':
            sts = [s for s in statement_spans(code, a, b) if code[s[0]:s[1]].strip().startswith('return ')]
            if len(sts) != 1: raise SystemExit(getter + ' has %d returns' % len(sts))
            a = code.index('return', sts[0][0]) + len('return'); b = sts[0][1]
        a, b = strip_span(code, a, b)
        return replace(code, a, b, fmt_multiline(text, continuation_indent(code, a, b, line_indent(code, a))))
    kind, ma, mb = method_span(code, PATH_METHOD[sec])
    stmts = statement_spans(code, ma, mb)
    if part in ('overview', 'details'):
        desc = [s for s in stmts if re.match(r'\s*(string|var)\s+desc\s*=', code[s[0]:s[1]])]
        if len(desc) != 1: raise SystemExit('no single desc in ' + PATH_METHOD[sec])
        da = code.index('=', desc[0][0]) + 1; db = desc[0][1]
        t = ternary_spans(code, da, db)
        if t is None:
            if part == 'details': raise SystemExit('desc has no details branch')
            a, b = strip_span(code, da, db)
        else:
            a, b = strip_span(code, *(t[0] if part == 'details' else t[1]))
        return replace(code, a, b, fmt_multiline(text, continuation_indent(code, a, b, line_indent(code, a) + '  ')))
    ret = [s for s in stmts if code[s[0]:s[1]].strip().startswith('return ')]
    ra = code.index('return', ret[0][0]) + len('return'); rb = ret[0][1]
    ops, depth, i, last = [], 0, ra, ra
    while i < rb:
        c = code[i]
        if is_str_start(code, i): i = skip_string(code, i); continue
        if c in '({[': depth += 1
        elif c in ')}]': depth -= 1
        elif c == '+' and depth == 0: ops.append(strip_span(code, last, i)); last = i + 1
        i += 1
    ops.append(strip_span(code, last, rb))
    if part == 'upgrade':
        ups = [(a, b) for a, b in ops[1:] if literal_content(code[a:b]) is not None
               and not literal_content(code[a:b]).startswith(('Level: [', '{Level5Section(')) and '{SkillCooldownLine()}' not in code[a:b]]
        a, b = ups[e['index']]
        return replace(code, a, b, '$"' + text + '\\n\\n"')
    if part in ('maxBonus', 'maxBonusDetails'):
        l5 = [(a, b) for a, b in ops if '{Level5Section(' in code[a:b]]
        a, b = l5[0]
        hole_a = code.index('{Level5Section(', a) + 1
        hole_b = skip_hole(code, hole_a - 1) - 1
        call = code[hole_a:hole_b]
        args = top_level_split(call[len('Level5Section('):-1], ',')
        if len(args) < 2: raise SystemExit('Level5Section has no bonus text')
        arg_a = code.index(args[1], hole_a); arg_b = arg_a + len(args[1])
        name = args[1].strip()
        if re.fullmatch(r'\w+', name):
            loc = [s for s in stmts if re.match(r'\s*(string|var)\s+' + name + r'\s*=', code[s[0]:s[1]])]
            arg_a = code.index('=', loc[0][0]) + 1; arg_b = loc[0][1]
        t = ternary_spans(code, arg_a, arg_b)
        if t is None:
            if part == 'maxBonusDetails': raise SystemExit('max bonus has no details branch')
            a2, b2 = strip_span(code, arg_a, arg_b)
        else:
            a2, b2 = strip_span(code, *(t[0] if part == 'maxBonusDetails' else t[1]))
        return replace(code, a2, b2, fmt_inline(text))
    raise SystemExit('unknown part ' + part)

if __name__ == '__main__':
    folder = sys.argv[1]
    data = json.load(open(os.path.join(HERE, 'descriptions.json'), encoding='utf-8'))
    info = {(p['id'], s['key']): s for p in data['plants'] for s in p['sections']}
    edits = []
    for f in glob.glob(os.path.join(folder, '**', '*.json'), recursive=True):
        d = json.load(open(f, encoding='utf-8'))
        d = d.get('data', d)
        if d.get('status') == 'pending': edits.append(d)
    by_plant = {}
    for e in edits: by_plant.setdefault(e['plant'], []).append(e)
    for plant, es in by_plant.items():
        path = os.path.join(PLANTS, plant + '.cs')
        raw = open(path, 'rb').read()
        bom = raw.startswith(b'\xef\xbb\xbf'); crlf = b'\r\n' in raw
        code = raw.decode('utf-8-sig').replace('\r\n', '\n')
        for e in es:
            code = apply_edit(code, e, info.get((plant, e['section']), {}))
            print('applied', plant, e['section'], e['part'], e['index'])
        if crlf: code = code.replace('\n', '\r\n')
        open(path, 'wb').write((b'\xef\xbb\xbf' if bom else b'') + code.encode('utf-8'))
