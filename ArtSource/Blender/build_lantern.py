from pathlib import Path
prefix=Path(__file__).with_name('create_bar_props.py').read_text(encoding='utf-8-sig').split("lathe('bottle'")[0]
exec(prefix)
milk=material('Amber lantern glass',(.96,.54,.18),0,.24)
lathe('Opaline globe',[(-.26,.07),(-.24,.105),(-.20,.12),(-.11,.118),(-.035,.092),(0,.075)],milk,40)
lathe('rolled enamel reflector',[(-.047,.248),(-.036,.255),(-.023,.252),(.003,.224),(.065,.145),(.12,.074),(.13,.065)],green,48)
lathe('brass collar',[(.124,.070),(.13,.077),(.14,.077),(.145,.063),(.18,.041)],brass,32)
lathe('dusty cable',[(.17,.009),(2,.009)],black,12)
lathe('lower guard ring',[(-.265,.081),(-.257,.087),(-.249,.08)],brass,32)
for i in range(8):
 t=i*math.tau/8;c,s=math.cos(t),math.sin(t)
 pts=[(.081*c,.081*s,-.264),(.129*c,.129*s,-.197),(.132*c,.132*s,-.116),(.129*c,.129*s,-.045)]
 limb('cage rib',pts,[.004,.004,.004,.004],brass,8)
for o in bpy.context.scene.objects:
 for p in o.data.polygons:p.use_smooth=True
export('CellarLamp')
