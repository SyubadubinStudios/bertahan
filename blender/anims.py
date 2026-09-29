"""Animation sets. Angles are degrees about the rest pose armature axes:
X +: tip forward for upward bones / swing backward for downward limbs,
Z +: turn to the character's left, Y +: right arm out / left arm in.

Base clips key every bone. Action clips ("upper body") only key the torso and
arms, so the game can play them on top of idle/walk/run.
"""

import btk as T

LOOPING = {"idle", "walk", "run", "cheer", "aim"}

UPPER = ["spine", "chest", "head", "upperarm_L", "forearm_L", "hand_L", "upperarm_R", "forearm_R", "hand_R"]


def _add(a, b):
    return tuple(x + y for x, y in zip(a, b))


class Posture:
    """Constant offsets (elders stoop, zombies hunch) applied on top of every pose."""

    def __init__(self, **offsets):
        self.offsets = offsets

    def apply(self, clip):
        clip.offsets = dict(self.offsets)
        return clip


ARMS_READY = dict(upperarm_L=(-10, -8, 0), forearm_L=(-35, 0, 0), upperarm_R=(-18, 10, 0), forearm_R=(-55, 0, 0))


def family_clips(arm, posture=None, weight=1.0):
    """weight scales the bounciness (kids bounce more, elders less)."""
    P = posture or Posture()
    w = weight
    clips = []

    # ------------------------------------------------------------- idle
    c = T.Clip(arm, "idle", 60)
    c.pose(0, chest=(0, 0, 0), head=(0, 0, 0), **ARMS_READY)
    c.pose(30, chest=(-3, 0, 0), head=(3, 0, 4), upperarm_L=(-12, -10, 0), forearm_R=(-60, 0, 0))
    c.move(0, "hips", (0, 0, 0)).move(30, "hips", (0, 0, -0.015 * w))
    clips.append((P.apply(c), c.name in LOOPING))

    # ------------------------------------------------------------- walk
    c = T.Clip(arm, "walk", 24)
    for f, s in ((0, 1), (12, -1)):
        c.pose(f,
               thigh_L=(-28 * s, 0, 0), shin_L=(10 if s > 0 else 35, 0, 0), foot_L=(-10 * s, 0, 0),
               thigh_R=(28 * s, 0, 0), shin_R=(35 if s > 0 else 10, 0, 0), foot_R=(10 * s, 0, 0),
               upperarm_L=(22 * s, -8, 0), forearm_L=(-30, 0, 0),
               upperarm_R=(-15 - 10 * s, 10, 0), forearm_R=(-55, 0, 0),
               hips=(0, 0, 6 * s), chest=(4, 0, -8 * s), head=(0, 0, 4 * s))
    for f in (6, 18):
        c.move(f, "hips", (0, 0, 0.045 * w))
    c.move(0, "hips", (0, 0, 0)).move(12, "hips", (0, 0, 0))
    clips.append((P.apply(c), c.name in LOOPING))

    # ------------------------------------------------------------- run
    c = T.Clip(arm, "run", 16)
    for f, s in ((0, 1), (8, -1)):
        c.pose(f,
               thigh_L=(-50 * s, 0, 0), shin_L=(20 if s > 0 else 80, 0, 0), foot_L=(-15 * s, 0, 0),
               thigh_R=(50 * s, 0, 0), shin_R=(80 if s > 0 else 20, 0, 0), foot_R=(15 * s, 0, 0),
               upperarm_L=(45 * s, -10, 0), forearm_L=(-80, 0, 0),
               upperarm_R=(-20 - 25 * s, 12, 0), forearm_R=(-70, 0, 0),
               hips=(0, 0, 10 * s), spine=(10, 0, 0), chest=(6, 0, -12 * s), head=(-10, 0, 6 * s))
    for f in (4, 12):
        c.move(f, "hips", (0, 0, 0.09 * w))
    c.move(0, "hips", (0, 0, -0.02)).move(8, "hips", (0, 0, -0.02))
    clips.append((P.apply(c), c.name in LOOPING))

    # ------------------------------------------------------------- dodge (roll-ish dive)
    c = T.Clip(arm, "dodge", 14)
    c.pose(0, **ARMS_READY)
    c.pose(4, spine=(35, 0, 0), chest=(25, 0, 0), head=(-10, 0, 0), thigh_L=(-70, 0, 0), shin_L=(110, 0, 0),
           thigh_R=(-40, 0, 0), shin_R=(90, 0, 0), upperarm_L=(-60, -20, 0), upperarm_R=(-60, 20, 0))
    c.pose(9, spine=(50, 0, 0), chest=(30, 0, 0), thigh_L=(-90, 0, 0), shin_L=(130, 0, 0),
           thigh_R=(-90, 0, 0), shin_R=(130, 0, 0))
    c.pose(14, spine=(0, 0, 0), chest=(0, 0, 0), head=(0, 0, 0), thigh_L=(0, 0, 0), shin_L=(0, 0, 0),
           thigh_R=(0, 0, 0), shin_R=(0, 0, 0), **ARMS_READY)
    c.move(0, "hips", (0, 0, 0)).move(5, "hips", (0, 0, -0.3)).move(9, "hips", (0, 0, -0.35)).move(14, "hips", (0, 0, 0))
    clips.append((P.apply(c), c.name in LOOPING))

    # ------------------------------------------------------------- die
    c = T.Clip(arm, "die", 36)
    c.pose(0, **ARMS_READY)
    c.pose(8, chest=(-20, 0, 10), head=(-20, 0, 0), upperarm_L=(-60, -40, 0), upperarm_R=(-60, 40, 0),
           shin_L=(40, 0, 0), shin_R=(30, 0, 0))
    c.pose(20, root=(-88, 0, 0), chest=(0, 0, 0), head=(-10, 0, 20), upperarm_L=(-150, -70, 0),
           upperarm_R=(-150, 70, 0), forearm_L=(0, 0, 0), forearm_R=(0, 0, 0), thigh_L=(-10, 0, 0),
           thigh_R=(-25, 0, 0), shin_L=(10, 0, 0), shin_R=(25, 0, 0))
    c.pose(36, root=(-90, 0, 0))
    c.move(0, "root", (0, 0, 0)).move(20, "root", (0, 0.1, 0.12)).move(36, "root", (0, 0.1, 0.12))
    c.move(0, "hips", (0, 0, 0)).move(8, "hips", (0, 0, -0.1)).move(20, "hips", (0, 0, 0))
    clips.append((P.apply(c), False))

    # ------------------------------------------------------------- cheer
    c = T.Clip(arm, "cheer", 30)
    c.pose(0, upperarm_L=(-170, -20, 0), upperarm_R=(-170, 20, 0), forearm_L=(-10, 0, 0), forearm_R=(-10, 0, 0),
           head=(-12, 0, 0), thigh_L=(0, 0, 0), shin_L=(0, 0, 0))
    c.pose(15, upperarm_L=(-150, -40, 0), upperarm_R=(-150, 40, 0), head=(-5, 0, 8), thigh_L=(-30, 0, 0), shin_L=(60, 0, 0))
    c.move(0, "hips", (0, 0, 0)).move(8, "hips", (0, 0, 0.16 * w)).move(15, "hips", (0, 0, 0)).move(23, "hips", (0, 0, 0.16 * w))
    clips.append((P.apply(c), c.name in LOOPING))

    # ============================================================ upper body actions
    # ---- swing: big overhead diagonal hit, the default melee attack
    c = T.Clip(arm, "swing", 16, UPPER)
    c.pose(0, **ARMS_READY, chest=(0, 0, 0), spine=(0, 0, 0))
    c.pose(5, chest=(-10, 0, -35), spine=(-5, 0, -10), upperarm_R=(-165, 30, 0), forearm_R=(-40, 0, 0),
           upperarm_L=(-40, -30, 0), forearm_L=(-60, 0, 0), head=(0, 0, 15))
    c.pose(8, chest=(20, 0, 30), spine=(10, 0, 12), upperarm_R=(-50, -15, 0), forearm_R=(-5, 0, 0),
           upperarm_L=(10, -20, 0), forearm_L=(-30, 0, 0), head=(0, 0, -10))
    c.pose(11, chest=(15, 0, 25), spine=(8, 0, 10), upperarm_R=(-30, -10, 0), forearm_R=(-10, 0, 0))
    c.pose(16, chest=(0, 0, 0), spine=(0, 0, 0), head=(0, 0, 0), **ARMS_READY)
    clips.append((P.apply(c), c.name in LOOPING))

    # ---- thrust: spear / crowbar jab
    c = T.Clip(arm, "thrust", 12, UPPER)
    c.pose(0, **ARMS_READY, chest=(0, 0, 0), spine=(0, 0, 0))
    c.pose(3, chest=(-10, 0, -20), upperarm_R=(-20, 10, 0), forearm_R=(-100, 0, 0), upperarm_L=(-50, -10, 0),
           forearm_L=(-70, 0, 0))
    c.pose(6, chest=(20, 0, 15), spine=(10, 0, 0), upperarm_R=(-85, 0, 0), forearm_R=(-5, 0, 0),
           upperarm_L=(-80, 20, 0), forearm_L=(-20, 0, 0))
    c.pose(12, chest=(0, 0, 0), spine=(0, 0, 0), **ARMS_READY)
    clips.append((P.apply(c), c.name in LOOPING))

    # ---- aim / shoot: rifle raised to the shoulder
    aim = dict(upperarm_R=(-78, 5, 0), forearm_R=(-12, 0, 0), hand_R=(0, 0, 0),
               upperarm_L=(-80, 38, 0), forearm_L=(-35, 0, 0), chest=(0, 0, 12), head=(0, 0, 8), spine=(0, 0, 0))
    c = T.Clip(arm, "aim", 30, UPPER)
    c.pose(0, **aim)
    c.pose(15, **dict(aim, chest=(-2, 0, 12)))
    clips.append((P.apply(c), c.name in LOOPING))

    c = T.Clip(arm, "shoot", 8, UPPER)
    c.pose(0, **aim)
    c.pose(2, **dict(aim, chest=(-12, 0, 16), upperarm_R=(-95, 5, 0), upperarm_L=(-95, 38, 0), head=(-8, 0, 8)))
    c.pose(8, **aim)
    clips.append((P.apply(c), c.name in LOOPING))

    # ---- throw: molotov overarm throw
    c = T.Clip(arm, "throw", 20, UPPER)
    c.pose(0, **ARMS_READY, chest=(0, 0, 0))
    c.pose(7, chest=(-15, 0, -40), spine=(-5, 0, -10), upperarm_R=(-150, 40, 0), forearm_R=(-90, 0, 0),
           upperarm_L=(-70, -20, 0), forearm_L=(-20, 0, 0))
    c.pose(11, chest=(20, 0, 30), spine=(10, 0, 10), upperarm_R=(-70, -10, 0), forearm_R=(-10, 0, 0),
           upperarm_L=(20, -20, 0))
    c.pose(20, chest=(0, 0, 0), spine=(0, 0, 0), **ARMS_READY)
    clips.append((P.apply(c), c.name in LOOPING))

    # ---- hit reaction
    c = T.Clip(arm, "hit", 10, UPPER)
    c.pose(0, **ARMS_READY, chest=(0, 0, 0), head=(0, 0, 0))
    c.pose(3, chest=(-18, 0, 8), head=(-20, 0, -10), upperarm_L=(-30, -45, 0), upperarm_R=(-40, 45, 0))
    c.pose(10, chest=(0, 0, 0), head=(0, 0, 0), **ARMS_READY)
    clips.append((P.apply(c), c.name in LOOPING))

    return clips


def build_family(arm, posture=None, weight=1.0):
    """Builds every clip and returns their names."""
    names = []
    for clip, loop in family_clips(arm, posture, weight):
        clip.build(loop=loop)
        names.append(clip.name)
    return names
