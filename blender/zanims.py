"""Zombie animation sets (same angle conventions as anims.py)."""

import btk as T
from anims import UPPER, Posture

ZOMBIE_ARMS = dict(upperarm_L=(-80, -5, 0), forearm_L=(-15, 0, 0), upperarm_R=(-75, 8, 0), forearm_R=(-20, 0, 0))


def zombie_clips(arm, posture=None, floating=False, big=False):
    """Shambling set: idle, walk, run, spawn, die (base) and attack, hit, cast (upper body)."""
    P = posture or Posture()
    clips = []
    legs = {} if not floating else dict(thigh_L=(-15, 0, 0), thigh_R=(-15, 0, 0), shin_L=(25, 0, 0), shin_R=(25, 0, 0))
    lift = 0.3 if floating else 0.0

    c = T.Clip(arm, "idle", 48)
    c.pose(0, **ZOMBIE_ARMS, head=(0, 0, 10), chest=(0, 0, 4), **legs)
    c.pose(24, **dict(ZOMBIE_ARMS, upperarm_L=(-70, -5, 0)), head=(10, 0, -10), chest=(4, 0, -4))
    c.move(0, "root", (0, 0, lift)).move(24, "root", (0, 0, lift * 1.4))
    clips.append((P.apply(c), True))

    c = T.Clip(arm, "walk", 32)
    for f, s in ((0, 1), (16, -1)):
        arms = dict(ZOMBIE_ARMS, upperarm_L=(-85 + 10 * s, -5, 0), upperarm_R=(-75 - 10 * s, 8, 0))
        if floating:
            c.pose(f, **legs, **arms, chest=(8, 0, 6 * s), head=(0, 0, -10 * s), hips=(0, 0, 4 * s))
        else:
            c.pose(f, thigh_L=(-22 * s, 0, 0), shin_L=(8 if s > 0 else 30, 0, 0), thigh_R=(22 * s, 0, 0),
                   shin_R=(30 if s > 0 else 8, 0, 0), foot_L=(-8 * s, 0, 0), foot_R=(8 * s, 0, 0),
                   hips=(0, 8 * s, 8 * s), chest=(6, -6 * s, 8 * s), head=(0, 12 * s, -8 * s), **arms)
    if floating:
        c.move(0, "root", (0, 0, lift)).move(16, "root", (0, 0, lift * 1.4))
    else:
        c.move(0, "hips", (0, 0, 0)).move(8, "hips", (0, 0, 0.04)).move(16, "hips", (0, 0, 0)).move(24, "hips", (0, 0, 0.04))
    clips.append((P.apply(c), True))

    c = T.Clip(arm, "run", 16)
    for f, s in ((0, 1), (8, -1)):
        if floating:
            c.pose(f, **legs, upperarm_L=(-100, -30, 0), upperarm_R=(-100, 30, 0), forearm_L=(0, 0, 0), forearm_R=(0, 0, 0),
                   chest=(20, 0, 8 * s), head=(-15, 0, 0))
        else:
            c.pose(f, thigh_L=(-45 * s, 0, 0), shin_L=(15 if s > 0 else 75, 0, 0), thigh_R=(45 * s, 0, 0),
                   shin_R=(75 if s > 0 else 15, 0, 0), spine=(18, 0, 0), chest=(10, 0, 10 * s), head=(-15, 0, 0),
                   upperarm_L=(-95 + 20 * s, -15, 0), upperarm_R=(-95 - 20 * s, 15, 0), forearm_L=(-10, 0, 0), forearm_R=(-10, 0, 0))
    if floating:
        c.move(0, "root", (0, 0, lift * 1.2)).move(8, "root", (0, 0, lift * 1.5))
    else:
        c.move(0, "hips", (0, 0, 0)).move(4, "hips", (0, 0, 0.08)).move(8, "hips", (0, 0, 0)).move(12, "hips", (0, 0, 0.08))
    clips.append((P.apply(c), True))

    # rising out of the ground
    c = T.Clip(arm, "spawn", 36)
    c.pose(0, upperarm_L=(-170, -10, 0), upperarm_R=(-170, 10, 0), head=(-30, 0, 0), chest=(-10, 0, 0), **legs)
    c.pose(18, upperarm_L=(-120, -20, 0), upperarm_R=(-140, 20, 0), head=(20, 0, 20), chest=(10, 0, 0))
    c.pose(36, **ZOMBIE_ARMS, head=(0, 0, 0), chest=(0, 0, 0))
    c.move(0, "root", (0, 0, -2.3 if big else -1.5)).move(24, "root", (0, 0, -0.1)).move(36, "root", (0, 0, lift))
    clips.append((P.apply(c), False))

    # flop forward, face down
    c = T.Clip(arm, "die", 30)
    c.pose(0, **ZOMBIE_ARMS, **legs)
    c.pose(6, chest=(-25, 0, 0), head=(-30, 0, 15), upperarm_L=(-150, -40, 0), upperarm_R=(-150, 40, 0))
    c.pose(18, root=(86, 0, 0), chest=(0, 0, 0), head=(10, 0, 30), upperarm_L=(-160, -60, 0), upperarm_R=(-170, 70, 0),
           thigh_L=(10, 0, 0), thigh_R=(-10, 0, 0), shin_L=(40, 0, 0), shin_R=(20, 0, 0))
    c.pose(24, root=(90, 0, 0), head=(20, 0, 30))
    c.move(0, "root", (0, 0, lift)).move(18, "root", (0, -0.1, 0.18)).move(30, "root", (0, -0.1, 0.12))
    clips.append((P.apply(c), False))

    # both arms clawing, or a double-fist smash for the big ones
    c = T.Clip(arm, "attack", 18, UPPER)
    c.pose(0, **ZOMBIE_ARMS, chest=(0, 0, 0), head=(0, 0, 0), spine=(0, 0, 0))
    if big:
        c.pose(7, upperarm_L=(-175, -15, 0), upperarm_R=(-175, 15, 0), forearm_L=(-40, 0, 0), forearm_R=(-40, 0, 0),
               chest=(-20, 0, 0), spine=(-10, 0, 0), head=(-15, 0, 0))
        c.pose(11, upperarm_L=(-60, -10, 0), upperarm_R=(-60, 10, 0), forearm_L=(0, 0, 0), forearm_R=(0, 0, 0),
               chest=(35, 0, 0), spine=(20, 0, 0), head=(10, 0, 0))
    else:
        c.pose(6, upperarm_L=(-140, -35, 0), upperarm_R=(-120, 35, 0), forearm_L=(-60, 0, 0), forearm_R=(-60, 0, 0),
               chest=(-12, 0, -15), head=(-15, 0, 0))
        c.pose(10, upperarm_L=(-70, 10, 0), upperarm_R=(-60, -10, 0), forearm_L=(-5, 0, 0), forearm_R=(-5, 0, 0),
               chest=(25, 0, 15), spine=(12, 0, 0), head=(15, 0, 0))
    c.pose(18, **ZOMBIE_ARMS, chest=(0, 0, 0), head=(0, 0, 0), spine=(0, 0, 0))
    clips.append((P.apply(c), False))

    c = T.Clip(arm, "hit", 10, UPPER)
    c.pose(0, **ZOMBIE_ARMS, chest=(0, 0, 0), head=(0, 0, 0))
    c.pose(3, chest=(-25, 0, 10), head=(-30, 0, -15), upperarm_L=(-40, -50, 0), upperarm_R=(-50, 50, 0))
    c.pose(10, **ZOMBIE_ARMS, chest=(0, 0, 0), head=(0, 0, 0))
    clips.append((P.apply(c), False))

    # casting (dukun) / screaming (kuntilanak): arms up, head back
    c = T.Clip(arm, "cast", 24, UPPER)
    c.pose(0, **ZOMBIE_ARMS, chest=(0, 0, 0), head=(0, 0, 0))
    c.pose(8, upperarm_L=(-160, -40, 0), upperarm_R=(-170, 30, 0), forearm_L=(-20, 0, 0), forearm_R=(-10, 0, 0),
           chest=(-15, 0, 0), head=(-25, 0, 0))
    c.pose(14, upperarm_L=(-150, -50, 0), upperarm_R=(-90, 10, 0), forearm_R=(0, 0, 0), chest=(15, 0, 0), head=(10, 0, 0))
    c.pose(24, **ZOMBIE_ARMS, chest=(0, 0, 0), head=(0, 0, 0))
    clips.append((P.apply(c), False))
    return clips


def build_zombie(arm, posture=None, floating=False, big=False):
    names = []
    for clip, loop in zombie_clips(arm, posture, floating, big):
        clip.build(loop=loop)
        names.append(clip.name)
    return names


def build_pocong(arm):
    """Pocong only hops: root, body and head bones."""
    clips = []
    c = T.Clip(arm, "idle", 40)
    c.pose(0, body=(0, 0, 4), head=(0, 0, -6)).pose(20, body=(0, 0, -4), head=(6, 0, 6))
    clips.append((c, True))
    for name, frames, height in (("walk", 20, 0.35), ("run", 14, 0.5)):
        c = T.Clip(arm, name, frames)
        h = frames // 2
        c.pose(0, body=(0, 0, 0), head=(5, 0, 0)).pose(3, body=(-6, 0, 0), head=(-10, 0, 0))
        c.pose(h, body=(4, 0, 0), head=(10, 0, 0)).pose(frames - 3, body=(8, 0, 0), head=(0, 0, 0))
        c.move(0, "root", (0, 0, 0)).move(2, "root", (0, 0, -0.05)).move(h, "root", (0, 0, height)).move(frames - 2, "root", (0, 0, 0.02))
        c.move(0, "body", (0, 0, 0)).move(2, "body", (0, 0, -0.06)).move(h, "body", (0, 0, 0.03))
        clips.append((c, True))
    c = T.Clip(arm, "spawn", 36)
    c.pose(0, body=(-20, 0, 0)).pose(20, body=(10, 0, 10)).pose(36, body=(0, 0, 0))
    c.move(0, "root", (0, 0, -1.6)).move(24, "root", (0, 0, 0.2)).move(36, "root", (0, 0, 0))
    clips.append((c, False))
    c = T.Clip(arm, "die", 24)
    c.pose(0, root=(0, 0, 0)).pose(8, root=(-20, 0, 10), head=(-20, 0, 0)).pose(18, root=(-88, 0, 20)).pose(24, root=(-90, 0, 20))
    c.move(0, "root", (0, 0, 0)).move(8, "root", (0, 0, 0.25)).move(18, "root", (0, 0.1, 0.18)).move(24, "root", (0, 0.1, 0.18))
    clips.append((c, False))
    c = T.Clip(arm, "attack", 16, ["body", "head"])
    c.pose(0, body=(0, 0, 0), head=(0, 0, 0)).pose(6, body=(-20, 0, 0), head=(-20, 0, 0))
    c.pose(9, body=(30, 0, 0), head=(25, 0, 0)).pose(16, body=(0, 0, 0), head=(0, 0, 0))
    clips.append((c, False))
    c = T.Clip(arm, "hit", 10, ["body", "head"])
    c.pose(0, body=(0, 0, 0), head=(0, 0, 0)).pose(3, body=(-20, 0, 12), head=(-25, 0, -10)).pose(10, body=(0, 0, 0), head=(0, 0, 0))
    clips.append((c, False))
    names = []
    for clip, loop in clips:
        clip.build(loop=loop)
        names.append(clip.name)
    return names
