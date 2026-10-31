#!/usr/bin/env python3
"""Fetch REAL Unity assemblies so the compile harness matches the Unity editor.

Why this exists
---------------
The project's earlier verification ran against hand-written Unity API *stubs*.
The stubs placed `CanvasScaler` in `UnityEngine` instead of `UnityEngine.UI`, so
`AspectMatchGuard.cs` compiled in the sandbox but failed in the editor with
CS0246. Compiling against the real assemblies makes that class of bug impossible.

Sources (public):
  * RocketModFix.UnityEngine.Redist 2022.3.62.3 (nuget.org) - the actual
    Editor/Data/Managed assemblies from Unity 2022.3.62f1, incl. UnityEngine.UI.dll.
  * (optional) com.unity.ugui source tarball from packages.unity.com, for
    grepping exact uGUI namespaces/member signatures.

Nothing is committed: assemblies are downloaded to a scratch dir (default
/tmp/unitylibs) because Unity's assemblies must not be redistributed.

Usage:  python3 fetch_unity_libs.py [dest_dir] [--all]
        --all  extract every assembly in the package (for the widest-coverage build)
"""
import os
import shutil
import sys
import urllib.request
import zipfile

NUPKG_URL = (
    "https://api.nuget.org/v3-flatcontainer/rocketmodfix.unityengine.redist/"
    "2022.3.62.3/rocketmodfix.unityengine.redist.2022.3.62.3.nupkg"
)
UGUI_TGZ_URL = (
    "https://packages.unity.com/com.unity.ugui/-/com.unity.ugui-3.0.0-exp.4.tgz"
)

# The modules this project actually touches. Everything else is skipped to keep
# the scratch dir small; add more here if a new script needs another module.
NEEDED = [
    "UnityEngine.CoreModule.dll",            # MonoBehaviour, GameObject, RectTransform, Canvas? (no: UIModule)
    "UnityEngine.UIModule.dll",              # Canvas, CanvasRenderer, CanvasGroup
    "UnityEngine.UI.dll",                    # CanvasScaler, GraphicRaycaster, Text, Image, Button, Slider, Toggle
    "UnityEngine.AudioModule.dll",           # AudioSource, AudioClip, AudioListener
    "UnityEngine.TextRenderingModule.dll",   # Font, TextAnchor, FontStyle, HorizontalWrapMode
    "UnityEngine.TextCoreFontEngineModule.dll",
    "UnityEngine.TextCoreTextEngineModule.dll",
    "UnityEngine.InputLegacyModule.dll",     # Input (legacy Input Manager)
    "UnityEngine.ImageConversionModule.dll", # Texture2D.EncodeToPNG
    "UnityEngine.JSONSerializeModule.dll",   # JsonUtility
    "UnityEngine.AnimationModule.dll",       # AnimationCurve usage
    "UnityEngine.ParticleSystemModule.dll",
    "UnityEngine.Physics2DModule.dll",
    "UnityEngine.PhysicsModule.dll",
    "UnityEngine.ScreenCaptureModule.dll",
    "UnityEngine.SpriteMaskModule.dll",
    "UnityEngine.TilemapModule.dll",
    "UnityEngine.IMGUIModule.dll",
    "UnityEngine.SharedInternalsModule.dll",
    "netstandard.dll",
    "mscorlib.dll",
    "System.dll",
    "System.Core.dll",
]


def main(dest=None, want_all=False):
    dest = dest or os.environ.get("UNITY_LIBS") or "/tmp/unitylibs"
    os.makedirs(dest, exist_ok=True)
    tmp = os.path.join(dest, "_nupkg")
    os.makedirs(tmp, exist_ok=True)
    pkg = os.path.join(tmp, "unity.nupkg")

    if not os.path.exists(pkg):
        print("downloading", NUPKG_URL)
        req = urllib.request.Request(NUPKG_URL, headers={"User-Agent": "python"})
        with urllib.request.urlopen(req) as r, open(pkg, "wb") as f:
            shutil.copyfileobj(r, f)
    print("package:", os.path.getsize(pkg) // 1024, "KiB")

    z = zipfile.ZipFile(pkg)
    # The package ships the same assemblies for net40 / netstandard2.0 / netstandard2.1.
    # Prefer netstandard2.1 (matches Unity 2022+'s API compatibility level) and
    # dedupe by file name so each assembly is extracted exactly once.
    pref = ["lib/netstandard2.1/", "lib/netstandard2.0/", "lib/net40/"]
    entries = [n for n in z.namelist() if n.endswith(".dll") and n.startswith("lib/")]
    entries.sort(key=lambda n: (pref.index(next((p for p in pref if n.startswith(p)), pref[-1])),
                                os.path.basename(n)))
    got, seen = [], set()
    for name in entries:
        base = os.path.basename(name)
        if base in seen:
            continue
        if NEEDED and not want_all and base not in NEEDED:
            continue
        seen.add(base)
        with z.open(name) as src, open(os.path.join(dest, base), "wb") as dst:
            shutil.copyfileobj(src, dst)
        got.append(base)
    missing = [] if want_all else [n for n in NEEDED if n not in got]

    print("extracted", len(got), "assemblies to", dest)
    for n in sorted(got):
        print("   ", n)
    if missing:
        print("not present in this package (usually harmless):", ", ".join(missing))
    print("\noptional uGUI source for namespace grepping:", UGUI_TGZ_URL)
    return 0


if __name__ == "__main__":
    args = [a for a in sys.argv[1:] if not a.startswith("--")]
    sys.exit(main(args[0] if args else None, "--all" in sys.argv))
