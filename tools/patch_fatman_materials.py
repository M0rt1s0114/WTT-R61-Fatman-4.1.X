import argparse
import os
import traceback

import UnityPy


OLD_SHADER_CAB = "CAB-1dc8d26be8722a766953ce9d8a444e8c"
OLD_SHADER_PATH_ID = -9098473984068178372
TARGET_SHADER_CAB = "CAB-e977dfd61e828856ad0028c2f6da904e"
TARGET_SHADER_PATH_ID = 6009825322293057295
TARGET_SHADER_NAME = "Tarkov Replacements/Standard SMAP Metallic Replacement"
TARGET_SHADER_EXTERNAL = (
    f"archive:/{TARGET_SHADER_CAB}/{TARGET_SHADER_CAB}"
)
EXPECTED_BUNDLE_COUNT = 6
EXPECTED_MATERIALS_PER_BUNDLE = 2


def iter_externals(env):
    for file_name, file_item in env.files.items():
        nested_files = getattr(file_item, "files", None)
        candidates = (
            nested_files.items()
            if isinstance(nested_files, dict)
            else [(file_name, file_item)]
        )
        for nested_name, nested_item in candidates:
            externals = getattr(nested_item, "externals", None) or []
            for external_index, external in enumerate(externals, 1):
                yield nested_name, external_index, external


def shader_name(data):
    parsed = getattr(data, "m_ParsedForm", None)
    return (
        getattr(parsed, "m_Name", None)
        if parsed
        else None
    ) or getattr(data, "m_Name", None) or getattr(data, "name", None)


def verify_target_shader_bundle(bundle_path):
    env = UnityPy.load(bundle_path)
    cab_names = []
    for file_name, file_item in env.files.items():
        nested_files = getattr(file_item, "files", None)
        candidates = (
            nested_files.items()
            if isinstance(nested_files, dict)
            else [(file_name, file_item)]
        )
        cab_names.extend(name for name, _ in candidates)

    if TARGET_SHADER_CAB not in cab_names:
        raise RuntimeError(
            f"target CAB {TARGET_SHADER_CAB} not found in {bundle_path}; "
            f"found {cab_names}"
        )

    matching_shaders = []
    for obj in env.objects:
        if obj.type.name != "Shader":
            continue
        data = obj.read()
        if obj.path_id != TARGET_SHADER_PATH_ID:
            continue
        matching_shaders.append(shader_name(data))

    if matching_shaders != [TARGET_SHADER_NAME]:
        raise RuntimeError(
            f"target shader verification failed for {bundle_path}: "
            f"PathID {TARGET_SHADER_PATH_ID}, names {matching_shaders}"
        )

    print(
        f"verified shader bundle: CAB={TARGET_SHADER_CAB}, "
        f"PathID={TARGET_SHADER_PATH_ID}, name={TARGET_SHADER_NAME!r}"
    )


def patch_bundle(source_path, output_path):
    env = UnityPy.load(source_path)
    old_externals = [
        (nested_name, external_index, external)
        for nested_name, external_index, external in iter_externals(env)
        if OLD_SHADER_CAB in (getattr(external, "path", None) or "")
    ]
    if len(old_externals) != 1:
        raise RuntimeError(
            f"expected exactly 1 external reference to {OLD_SHADER_CAB} "
            f"in {source_path}, found {len(old_externals)}"
        )

    _, external_index, old_external = old_externals[0]
    old_external.path = TARGET_SHADER_EXTERNAL
    materials = []

    for obj in env.objects:
        if obj.type.name != "Material":
            continue
        data = obj.read()
        shader = getattr(data, "m_Shader", None)
        if shader is None:
            raise RuntimeError(f"material without shader in {source_path}")

        old_pointer = (
            getattr(shader, "m_FileID", None),
            getattr(shader, "m_PathID", None),
        )
        if old_pointer != (external_index, OLD_SHADER_PATH_ID):
            raise RuntimeError(
                f"unexpected source shader pointer in {source_path}: "
                f"found {old_pointer}, expected "
                f"{(external_index, OLD_SHADER_PATH_ID)}"
            )

        shader.m_FileID = external_index
        shader.m_PathID = TARGET_SHADER_PATH_ID
        data.save()
        materials.append(
            (
                getattr(data, "m_Name", None) or getattr(data, "name", None),
                old_pointer,
            )
        )

    if len(materials) != EXPECTED_MATERIALS_PER_BUNDLE:
        raise RuntimeError(
            f"expected exactly {EXPECTED_MATERIALS_PER_BUNDLE} materials in "
            f"{source_path}, found {len(materials)}"
        )

    output_dir = os.path.dirname(output_path)
    os.makedirs(output_dir, exist_ok=True)
    env.save(out_path=output_dir)
    if not os.path.exists(output_path):
        raise RuntimeError(f"UnityPy did not write expected output: {output_path}")

    verify_env = UnityPy.load(output_path)
    target_externals = [
        (nested_name, index, getattr(external, "path", None))
        for nested_name, index, external in iter_externals(verify_env)
        if getattr(external, "path", None) == TARGET_SHADER_EXTERNAL
    ]
    if (
        len(target_externals) != 1
        or target_externals[0][1] != external_index
    ):
        raise RuntimeError(
            f"output external verification failed for {output_path}: "
            f"{target_externals}, expected index {external_index}"
        )

    verified_materials = []
    for obj in verify_env.objects:
        if obj.type.name != "Material":
            continue
        data = obj.read()
        shader = getattr(data, "m_Shader", None)
        pointer = (
            getattr(shader, "m_FileID", None),
            getattr(shader, "m_PathID", None),
        )
        if pointer != (external_index, TARGET_SHADER_PATH_ID):
            raise RuntimeError(
                f"unexpected output shader pointer in {output_path}: "
                f"found {pointer}, expected "
                f"{(external_index, TARGET_SHADER_PATH_ID)}"
            )
        verified_materials.append(pointer)

    if len(verified_materials) != EXPECTED_MATERIALS_PER_BUNDLE:
        raise RuntimeError(
            f"output material verification failed for {output_path}: "
            f"{len(verified_materials)} materials"
        )

    print(
        f"  patched {os.path.basename(source_path)}: "
        f"{len(materials)} material(s), external index {external_index}"
    )
    for name, old_pointer in materials:
        print(
            f"    {name}: shader {old_pointer} -> "
            f"{(external_index, TARGET_SHADER_PATH_ID)}"
        )


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("source_dir")
    parser.add_argument("output_dir")
    parser.add_argument("target_shader_bundle")
    parser.add_argument("--expect-bundles", type=int, default=EXPECTED_BUNDLE_COUNT)
    args = parser.parse_args()

    verify_target_shader_bundle(args.target_shader_bundle)

    patched = 0
    failed = 0
    for current, _, files in os.walk(args.source_dir):
        for file_name in files:
            if not file_name.lower().endswith(".bundle"):
                continue
            source_path = os.path.join(current, file_name)
            relative_path = os.path.relpath(source_path, args.source_dir)
            output_path = os.path.join(args.output_dir, relative_path)
            try:
                patch_bundle(source_path, output_path)
                patched += 1
            except Exception as exc:
                print(f"  ERROR: {source_path}: {exc}")
                print(traceback.format_exc())
                failed += 1

    print(
        f"patched={patched} failed={failed} expected={args.expect_bundles}"
    )
    if patched != args.expect_bundles or failed != 0:
        print("ERROR: bundle patch set did not match the expected complete set")
        return 1
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
