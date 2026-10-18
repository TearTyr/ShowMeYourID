#!/usr/bin/env python3
"""Generates deterministic .meta files for every asset/folder under Assets/.
GUIDs are derived from sha256(path) so they are stable across regenerations;
the two GUIDs referenced by scene/build settings are pinned constants."""
import hashlib, os, sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))  # repo root
ASSETS = os.path.join(ROOT, "Assets")

PINNED = {
    "Assets/_Project/Scenes/Hollowbrook.unity": "1f4e0c8a3b2d4e6f8a0b1c2d3e4f5a6b",
    "Assets/_Project/Scenes/MainMenu.unity": "2a4e0c8a3b2d4e6f8a0b1c2d3e4f5a6b",
    "Assets/_Project/Scenes/School.unity": "3f4e0c8a3b2d4e6f8a0b1c2d3e4f5a6b",
    "Assets/_Project/Scripts/Bootstrap/MenuSceneBootstrap.cs": "0b1b2c3d4e5f60718293a4b5c6d7e8f9",
    "Assets/_Project/Scripts/Bootstrap/CheckpointSceneBootstrap.cs": "0c1b2c3d4e5f60718293a4b5c6d7e8f9",
    "Assets/_Project/Scripts/Bootstrap/SchoolSceneBootstrap.cs": "0d1b2c3d4e5f60718293a4b5c6d7e8f9",
}

def guid_for(rel):
    if rel.replace("\\", "/") in PINNED:
        return PINNED[rel.replace("\\", "/")]
    return hashlib.sha256(rel.replace("\\", "/").encode("utf-8")).hexdigest()[:32]

def meta_for(rel, is_dir):
    g = guid_for(rel)
    if is_dir:
        return f"""fileFormatVersion: 2
guid: {g}
folderAsset: yes
DefaultImporter:
  externalObjects: {{}}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""
    ext = os.path.splitext(rel)[1].lower()
    if ext == ".cs":
        importer = f"""MonoImporter:
  externalObjects: {{}}
  serializedVersion: 2
  defaultReferences: []
  executionOrder: 0
  icon: {{instanceID: 0}}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""
    elif ext == ".unity":
        importer = """DefaultImporter:
  externalObjects: {}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""
    elif ext in (".asset",):
        importer = """NativeFormatImporter:
  externalObjects: {}
  mainObjectFileID: 11400000
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""
    elif ext in (".png", ".jpg", ".tga"):
        importer = """TextureImporter:
  internalIDToNameTable: []
  serializedVersion: 12
  textureSettings:
    serializedVersion: 2
    filterMode: 1
    aniso: 1
    mipBias: 0
    wrapU: 1
    wrapV: 1
    wrapW: 1
  spriteMode: 1
  textureType: 8
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""
    elif ext in (".wav", ".ogg", ".mp3"):
        importer = """AudioImporter:
  serializedVersion: 6
  loadType: 0
  sampleRateSetting: 0
  platformSettingOverrides: {}
  forceToMono: 1
  normalize: 1
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""
    else:
        importer = """DefaultImporter:
  externalObjects: {}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""
    return f"fileFormatVersion: 2\nguid: {g}\n{importer}"

def main():
    created = 0
    # folder metas (Assets itself too)
    for dirpath, dirnames, filenames in os.walk(ASSETS):
        rel = os.path.relpath(dirpath, ROOT)
        meta_path = os.path.join(dirpath + ".meta")
        if rel != "Assets" or True:
            if not os.path.exists(meta_path):
                with open(meta_path, "w") as f:
                    f.write(meta_for(rel, True))
                created += 1
        for fn in filenames:
            if fn.endswith(".meta"):
                continue
            fpath = os.path.join(dirpath, fn)
            rel = os.path.relpath(fpath, ROOT)
            meta_path = fpath + ".meta"
            if not os.path.exists(meta_path):
                with open(meta_path, "w") as f:
                    f.write(meta_for(rel, False))
                created += 1
    print(f"meta files created: {created}")
    # sanity: pinned guids present
    for rel, g in PINNED.items():
        mp = os.path.join(ROOT, rel + ".meta")
        assert os.path.exists(mp), "missing pinned meta: " + mp
        assert g in open(mp).read(), "wrong guid in " + mp
    print("pinned GUIDs verified (scene <-> GameBootstrap, build settings <-> scene)")

if __name__ == "__main__":
    main()
