import argparse
import os
import traceback

import UnityPy


OLD_SHADER_CAB = "CAB-1dc8d26be8722a766953ce9d8a444e8c"
TARGET_SHADER_PATH_ID = 6009825322293057295
TARGET_SHADER_CAB = "CAB-e977dfd61e828856ad0028c2f6da904e"
TARGET_SHADER_EXTERNAL = f"archive:/{TARGET_SHADER_CAB}/{TARGET_SHADER_CAB}"
EXPECTED_BUNDLE_COUNT = 6
EXPECTED_MATERIALS_PER_BUNDLE = 2


def iter_externals(env):
    for file_item in env.files.values():
        nested_files = getattr(file_item, "files", None)
        candidates = nested_files.values() if isinstance(nested_files, dict) else [file_item]
        for nested_item in candidates:
            for external in getattr(nested_item, "externals", None) or []:
                yield external


def patch_bundle(source_path: str, output_path: str) -> bool:
    env = UnityPy.load(source_path)
    materials = []
    externals_patched = 0
    for external in iter_externals(env):
        current_path = getattr(external, "path", None)
        if current_path and OLD_SHADER_CAB in current_path:
            external.path = TARGET_SHADER_EXTERNAL
            externals_patched += 1

    for obj in env.objects:
        if obj.type.name != "Material":
            continue
        data = obj.read()
        shader = getattr(data, "m_Shader", None)
        if shader is None:
            continue
        old_file_id = getattr(shader, "m_FileID", None)
        old_path_id = getattr(shader, "m_PathID", None)
        shader.m_FileID = 1
        shader.m_PathID = TARGET_SHADER_PATH_ID
        data.save()
        materials.append(
            (
                getattr(data, "m_Name", None) or getattr(data, "name", None),
                old_file_id,
                old_path_id,
            )
        )

    if len(materials) != EXPECTED_MATERIALS_PER_BUNDLE:
        raise RuntimeError(
            f"expected exactly {EXPECTED_MATERIALS_PER_BUNDLE} materials in "
            f"{source_path}, found {len(materials)}"
        )

    if externals_patched != 1:
        raise RuntimeError(
            f"expected exactly 1 external reference in {source_path}, "
            f"patched {externals_patched}"
        )

    output_dir = os.path.dirname(output_path)
    os.makedirs(output_dir, exist_ok=True)
    env.save(out_path=output_dir)
    if not os.path.exists(output_path):
        raise RuntimeError(f"UnityPy did not write expected output: {output_path}")

    verified_materials = 0
    verified_externals = 0
    verify_env = UnityPy.load(output_path)
    for obj in verify_env.objects:
        if obj.type.name != "Material":
            continue
        data = obj.read()
        shader = getattr(data, "m_Shader", None)
        if shader is None:
            continue
        if getattr(shader, "m_FileID", None) != 1:
            raise RuntimeError(f"unexpected shader file_id in {output_path}")
        if getattr(shader, "m_PathID", None) != TARGET_SHADER_PATH_ID:
            raise RuntimeError(f"unexpected shader path_id in {output_path}")
        verified_materials += 1

    for external in iter_externals(verify_env):
        if getattr(external, "path", None) == TARGET_SHADER_EXTERNAL:
            verified_externals += 1

    if verified_materials != len(materials) or verified_externals != 1:
        raise RuntimeError(
            f"output verification failed for {output_path}: "
            f"materials {verified_materials}/{len(materials)}, "
            f"externals {verified_externals}/1"
        )

    print(
        f"  patched {os.path.basename(source_path)}: "
        f"{len(materials)} material(s), {externals_patched} external(s)"
    )
    for name, old_file_id, old_path_id in materials:
        print(
            f"    {name}: file_id {old_file_id} -> 1, "
            f"path_id {old_path_id} -> {TARGET_SHADER_PATH_ID}"
        )
    return True


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("source_dir")
    parser.add_argument("output_dir")
    parser.add_argument("--expect-bundles", type=int, default=EXPECTED_BUNDLE_COUNT)
    args = parser.parse_args()

    patched = 0
    skipped = 0
    for current, _, files in os.walk(args.source_dir):
        for file_name in files:
            if not file_name.lower().endswith(".bundle"):
                continue
            source_path = os.path.join(current, file_name)
            relative_path = os.path.relpath(source_path, args.source_dir)
            output_path = os.path.join(args.output_dir, relative_path)
            try:
                if patch_bundle(source_path, output_path):
                    patched += 1
                else:
                    skipped += 1
            except Exception as exc:
                print(f"  ERROR: {source_path}: {exc}")
                print(traceback.format_exc())
                skipped += 1

    print(
        f"patched={patched} skipped={skipped} expected={args.expect_bundles}"
    )
    if patched != args.expect_bundles or skipped != 0:
        print("ERROR: bundle patch set did not match the expected complete set")
        return 1
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
