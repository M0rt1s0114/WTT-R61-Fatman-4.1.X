import os
import sys

import UnityPy


def main() -> int:
    if len(sys.argv) < 2:
        print("usage: find_shader_bundle.py <directory> [directory ...]")
        return 2

    wanted_path_id = -9098473984068178372
    bundle_paths = []

    for root in sys.argv[1:]:
        for current, _, files in os.walk(root):
            for file_name in files:
                if file_name.lower().endswith(".bundle"):
                    bundle_paths.append(os.path.join(current, file_name))

    print(f"scanning {len(bundle_paths)} bundles")

    for index, bundle_path in enumerate(bundle_paths, 1):
        try:
            env = UnityPy.load(bundle_path)
            shaders = []
            for obj in env.objects:
                if obj.type.name != "Shader":
                    continue
                data = obj.read()
                name = getattr(data, "m_Name", None) or getattr(data, "name", None)
                shaders.append((obj.path_id, name))

            if shaders:
                print(f"[{index}/{len(bundle_paths)}] {bundle_path}")
                for path_id, name in shaders:
                    marker = " <== TARGET" if path_id == wanted_path_id else ""
                    print(f"  Shader path_id={path_id} name={name!r}{marker}")
        except Exception as exc:
            print(f"[{index}/{len(bundle_paths)}] ERROR {bundle_path}: {exc}")

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
