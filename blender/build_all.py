"""Rebuilds every game asset into src/Bertahan/Assets/Models.

Run from the Blender MCP (exec this file) or headless:
    blender -b --python blender/build_all.py
"""

import importlib
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import btk  # noqa: E402
import weapons  # noqa: E402
import anims  # noqa: E402
import characters  # noqa: E402
import zanims  # noqa: E402
import zombies  # noqa: E402
import props  # noqa: E402
import villagers  # noqa: E402
import animals  # noqa: E402
import props_lanjut  # noqa: E402
import zombies_tambahan  # noqa: E402
import bosses  # noqa: E402

for module in (btk, weapons, anims, characters, zanims, zombies, props, villagers, animals, props_lanjut, zombies_tambahan, bosses):
    importlib.reload(module)


def main(groups=("characters", "villagers", "animals", "zombies", "weapons", "props", "bosses")):
    report = {}
    if "characters" in groups:
        report["characters"] = {k: len(v["clips"]) for k, v in characters.build_all().items()}
    if "villagers" in groups:
        report["villagers"] = {k: len(v["clips"]) for k, v in villagers.build_all().items()}
    if "animals" in groups:
        report["animals"] = {k: len(v["clips"]) for k, v in animals.build_all().items()}
    if "zombies" in groups:
        report["zombies"] = {k: len(v["clips"]) for k, v in zombies.build_all().items()}
        report["zombies_tambahan"] = list(zombies_tambahan.build_all())
    if "bosses" in groups:
        report["bosses"] = list(bosses.build_all())
    if "weapons" in groups:
        weapons.export_pickups()
        report["weapons"] = list(weapons.BUILDERS)
    if "props" in groups:
        report["props"] = len(props.build_all()) + len(props_lanjut.build_all())
    btk.clear_scene()
    return report


if __name__ == "__main__":
    print(main())
