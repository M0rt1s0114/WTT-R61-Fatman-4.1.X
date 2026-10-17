import collections
import sys

import UnityPy


def main() -> int:
    if len(sys.argv) < 2:
        print("usage: inspect_bundle.py <bundle> [bundle ...]")
        return 2

    for bundle_path in sys.argv[1:]:
        print(f"===== {bundle_path} =====")
        env = UnityPy.load(bundle_path)

        counts = collections.Counter()
        materials = []
        shaders = []

        for obj in env.objects:
            counts[obj.type.name] += 1
            if obj.type.name == "Material":
                data = obj.read()
                shader = getattr(data, "m_Shader", None)
                props = getattr(data, "m_SavedProperties", None)
                prop_names = {
                    "textures": [
                        getattr(pair, "first", None)
                        for pair in getattr(props, "m_TexEnvs", [])
                    ],
                    "floats": [
                        getattr(pair, "first", None)
                        for pair in getattr(props, "m_Floats", [])
                    ],
                    "colors": [
                        getattr(pair, "first", None)
                        for pair in getattr(props, "m_Colors", [])
                    ],
                }
                materials.append(
                    (
                        getattr(data, "m_Name", None) or getattr(data, "name", None),
                        getattr(shader, "m_FileID", None),
                        getattr(shader, "m_PathID", None),
                        prop_names,
                    )
                )
            elif obj.type.name == "Shader":
                data = obj.read()
                parsed = getattr(data, "m_ParsedForm", None)
                shaders.append(
                    (
                        obj.path_id,
                        getattr(data, "m_Name", None) or getattr(data, "name", None),
                        getattr(parsed, "m_Name", None) if parsed else None,
                    )
                )

        print("objects:")
        for name, count in sorted(counts.items()):
            print(f"  {name}: {count}")

        print("shaders in bundle:")
        if not shaders:
            print("  (none)")
        for path_id, name, parsed_name in shaders:
            print(f"  path_id={path_id} name={name!r} parsed={parsed_name!r}")

        print("materials:")
        if not materials:
            print("  (none)")
        for name, file_id, path_id, props in materials:
            print(f"  material={name!r} shader_file_id={file_id} shader_path_id={path_id}")
            print(f"    properties={props}")

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
