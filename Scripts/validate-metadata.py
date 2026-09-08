"""Read-only acceptance checks for exported Stonehill front-nine metadata."""
import json,sys,math
from pathlib import Path
root=Path(sys.argv[1])
g=json.loads((root/'GSPro/Stonehill_9H/Stonehill_9H.GKD').read_text(encoding='utf-8-sig'))
waters=json.loads((root/'Reviews/water-boundaries.json').read_text())['waters']
holes=[h for h in g['Holes'] if h['Enabled']]
assert len(holes)==9 and [h['HoleNumber'] for h in holes]==list(range(1,10))
assert sum(h['Par'] for h in holes)==34
assert len(g['Hazards'])==g['hazardCount']==len(waters)==7
for h in holes:
    playable=[t for t in h['Tees'] if t['Enabled'] and t['Distance']>0 and t['Position'] is not None]
    assert {t['TeeType'] for t in playable}=={'White','Red'}
    assert len(h['Pins'])==4
    assert any(t['TeeType']=='AimPoint1' and t['Position'] for t in h['Tees'])
    for v in [t['Position'] for t in playable]+[p['Position'] for p in h['Pins']]:
        assert all(math.isfinite(v[c]) for c in 'xyz')
        assert 0<=v['x']<=2048 and 0<=v['z']<=2048
for hazard,water in zip(g['Hazards'],waters):
    assert hazard['pointCount']==len(water['coords'])
    assert hazard['coords']==water['coords']
print('PASS: nine holes, par 34, white/red positioned tees, four pins each, aiming data and seven exact shoreline matches.')
print('Legacy Enabled flags on zero-distance/positionless tee placeholders retained for compatibility; only white/red have playable positions.')
