"""Run inside Blender's Text Editor (Alt-P). Writes only the selected asset's FBX."""
import bpy
from pathlib import Path
def export_asset(output=None):
    blend=Path(bpy.data.filepath).resolve()
    root=blend.parents[3]
    if not (root/'Assets'/'Zombar').is_dir():
        raise RuntimeError('Keep the .blend file in ArtSource/Blender/Editable in the Unity project.')
    view=bpy.context.view_layer
    selected=list(bpy.context.selected_objects);active=view.objects.active
    if bpy.context.mode!='OBJECT':bpy.ops.object.mode_set(mode='OBJECT')
    copies=[];anchor=None;pose=None
    try:
        bpy.ops.object.select_all(action='DESELECT')
        if blend.stem in ('Zombie_Male','Zombie_Female'):
            face=bpy.data.collections['Face details']
            rig=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE')
            pose=rig.data.pose_position;rig.data.pose_position='REST';view.update()
            for part in face.objects:
                copy=part.copy();copy.data=part.data.copy();bpy.context.scene.collection.objects.link(copy)
                copies.append(copy)
            sculpt=next(o for o in copies if o.get('portrait_sculpt'))
            view.objects.active=sculpt;sculpt.select_set(True)
            if len(sculpt.data.polygons)>18000:
                m=sculpt.modifiers.new('Runtime silhouette','DECIMATE');m.ratio=.23
                bpy.ops.object.modifier_move_to_index(modifier=m.name,index=0)
                bpy.ops.object.modifier_apply(modifier=m.name)
            for o in copies:o.select_set(True)
            bpy.ops.object.join();copies=[sculpt];sculpt.name='Portrait'
            for o in bpy.context.scene.objects:
                o.select_set(o==rig or o==sculpt or (o.type=='MESH' and o not in face.objects[:]))
            rig.data.pose_position=pose;view.update()
            target=root/'Assets/Zombar/Art/Characters/Adapted'/(blend.stem+'.fbx')
            animated=True
        elif blend.stem=='BarProps':
            if not active:raise RuntimeError('Select a mesh of the prop to export.')
            group=next((c for c in active.users_collection if c.get('prop_id')),None)
            if group is None:raise RuntimeError('Select an object inside a prop collection.')
            anchor=next(o for o in group.objects if o.type=='EMPTY');home=anchor.location.copy();anchor.location=(0,0,0);view.update()
            for o in group.objects:o.select_set(o.type=='MESH')
            target=root/'Assets/Zombar/Resources/PhysicalBar/Models'/(group['prop_id']+'.fbx')
            animated=False
        else:raise RuntimeError('Select one of the supplied editable assets.')
        if output is not None:target=Path(output)
        bpy.ops.export_scene.fbx(filepath=str(target),use_selection=True,object_types={'ARMATURE','MESH'},
            add_leaf_bones=False,bake_anim=animated,bake_anim_use_all_actions=animated,bake_anim_simplify_factor=.2,
            axis_forward='-Z',axis_up='Y',path_mode='STRIP')
        print('Exported:',target)
    finally:
        if anchor:anchor.location=home
        if pose is not None:rig.data.pose_position=pose
        for o in copies:
            if o.name in bpy.data.objects:bpy.data.objects.remove(o,do_unlink=True)
        bpy.ops.object.select_all(action='DESELECT')
        for o in selected:o.select_set(True)
        view.objects.active=active
if __name__=='__main__':export_asset()
