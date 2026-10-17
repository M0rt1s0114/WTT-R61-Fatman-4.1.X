import sys

import UnityPy


def main() -> int:
    if len(sys.argv) < 2:
        print("usage: inspect_externals.py <bundle> [bundle ...]")
        return 2

    for bundle_path in sys.argv[1:]:
        print(f"===== {bundle_path} =====")
        env = UnityPy.load(bundle_path)
        for file_name, file_item in env.files.items():
            nested_files = getattr(file_item, "files", None)
            candidates = nested_files.items() if isinstance(nested_files, dict) else [(file_name, file_item)]
            for nested_name, nested_item in candidates:
                print(f"file={nested_name!r} type={type(nested_item).__name__}")
                externals = getattr(nested_item, "externals", None)
                if externals is None:
                    continue
                for index, external in enumerate(externals, 1):
                    print(
                        f"  file_id={index} path={getattr(external, 'path', None)!r} "
                        f"name={getattr(external, 'fileName', None)!r} "
                        f"type={getattr(external, 'type', None)!r}"
                    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
