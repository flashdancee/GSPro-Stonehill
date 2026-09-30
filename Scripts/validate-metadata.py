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
assert g['teeTypeCount']==2
totals={t['TeeType']:t for t in g['TeeTypeTotalDistance']}
assert {t['TeeType'] for t in totals.values() if t['Enabled']}=={'Red','Yellow'}
assert all(t['Distance']==0 for t in totals.values() if not t['Enabled'])
distance_totals={'Red':0.0,'Yellow':0.0}
for h in holes:
    playable=[t for t in h['Tees'] if t['Enabled'] and t['Distance']>0 and t['Position'] is not None]
    assert {t['TeeType'] for t in playable}=={'Red','Yellow'}
    by_type={t['TeeType']:t for t in playable}
    tee_types={'Black','White','Green','Blue','Yellow','Red','Junior','Par3'}
    assert {t['TeeType'] for t in h['Tees'] if t['TeeType'] in tee_types and t['Enabled']}=={'Red','Yellow'}
    assert all(t['Distance']==0 and t['Position'] is None for t in h['Tees'] if t['TeeType'] in tee_types and not t['Enabled'])
    for color in distance_totals:
        distance_totals[color]+=by_type[color]['Distance']
    assert len(h['Pins'])==4
    assert any(t['TeeType']=='AimPoint1' and t['Position'] for t in h['Tees'])
    for v in [t['Position'] for t in playable]+[p['Position'] for p in h['Pins']]:
        assert all(math.isfinite(v[c]) for c in 'xyz')
        assert 0<=v['x']<=2048 and 0<=v['z']<=2048
for hazard,water in zip(g['Hazards'],waters):
    assert hazard['pointCount']==len(water['coords'])
    assert hazard['coords']==water['coords']
for color,distance in distance_totals.items():
    assert math.isclose(totals[color]['Distance'],distance,abs_tol=1e-6)
print('PASS: nine holes, par 34, exactly two enabled Red/Yellow tee choices, four pins each, aiming data and seven exact shoreline matches.')
