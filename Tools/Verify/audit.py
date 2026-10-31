#!/usr/bin/env python3
"""Static audit for Show Me Your ID!

Catches the bug classes that only appear in the real Unity editor - namespace /
using-directive fidelity, nested-type misuse, legacy-only compliance, and the
canvas/scaler/EventSystem wiring the whole UI depends on.

This is a *heuristic* net. Ground truth is the real compile:
    Tools/Verify/Assembly-CSharp.csproj  (see README.md)

Usage:  python3 Tools/Verify/audit.py [path/to/Assets]
"""
import os
import re
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = sys.argv[1] if len(sys.argv) > 1 else os.path.normpath(os.path.join(HERE, "..", "..", "Assets"))

# ---------------------------------------------------------------- rule tables
# Types and the namespace they REALLY live in. Verified against the actual
# UnityEngine.UI.dll / UnityEngine.UIModule.dll / UnityEngine.CoreModule.dll
# assemblies from Unity 2022.3.62f1 (Tools/Verify/fetch_unity_libs.py).
NS_REQUIRED = {
    "UnityEngine.UI": [
        "CanvasScaler", "GraphicRaycaster", "Selectable", "ScrollRect", "MaskableGraphic",
        "LayoutElement", "ContentSizeFitter", "HorizontalLayoutGroup", "VerticalLayoutGroup",
        "GridLayoutGroup", "BaseMeshEffect", "VertexHelper", "SpriteState", "ColorBlock",
        "Outline", "Shadow", "Toggle", "ToggleGroup", "Slider", "Button", "Image", "RawImage",
        "Text", "Graphic", "RectMask2D", "Mask", "Dropdown", "InputField", "AspectRatioFitter",
        "LayoutRebuilder",
    ],
    "UnityEngine.EventSystems": [
        "EventSystem", "StandaloneInputModule", "PointerEventData", "AxisEventData",
        "BaseEventData", "ExecuteEvents", "EventTrigger", "BaseInputModule",
        "IPointerEnterHandler", "IPointerExitHandler", "IPointerClickHandler",
        "IPointerDownHandler", "IPointerUpHandler", "IDragHandler", "IBeginDragHandler",
        "IEndDragHandler", "IScrollHandler", "ISelectHandler",
    ],
    "UnityEngine.SceneManagement": ["SceneManager", "LoadSceneMode"],
}

# Types that must appear in TYPE position (declaration / generic arg / cast /
# typeof / new) before we call them a uGUI usage. This avoids flagging member
# names such as GameEvent.Text or DialogueLine { Text = ... } initializers.
def type_position(t):
    return re.compile(
        r"(?:\bnew\s+" + t + r"\s*[\(\[{])"                                    # new Text(...)
        r"|\btypeof\(\s*" + t + r"\s*\)"                                       # typeof(Text)
        r"|\b(?:Get|Add)Component<\s*" + t + r"\s*>"                           # GetComponent<Text>
        r"|\b" + t + r"\s*\[\s*\]"                                             # Text[]
        r"|\(\s*" + t + r"\s*\)"                                               # (Text) cast
        r"|(?<![\w.>])\b" + t + r"\s+[A-Za-z_]\w*\s*(?:=|;|,|\)|\()"           # Text label;
        r"|<\s*" + t + r"\s*[,>]"                                              # List<Text>
        r"|,\s*" + t + r"\s*>"                                                 # Dict<int, Text>
        r"|\b" + t + r"\s*\.\s*(?:ScaleMode|ScreenMatchMode|Transition|ColorBlock)"
    )


# If the token immediately before a candidate is itself a type name, the candidate
# is a MEMBER named e.g. Text ("public string Text;"), not a uGUI type usage.
PRECEDED_BY_TYPE = re.compile(
    r"(?:\b(?:string|int|uint|float|double|bool|byte|sbyte|char|decimal|long|ulong|short|ushort|"
    r"object|var|Vector2|Vector3|Vector4|Color|Color32|Rect|Quaternion|GameObject|Transform|"
    r"RectTransform|Sprite|AudioClip|Font|Canvas|CanvasGroup|SpriteRenderer|Renderer|Material|"
    r"Texture2D|Texture|AudioSource|Light|Camera|GameEvent|DialogueLine|List<[^>]*>|"
    r"Dictionary<[^>]*>|HashSet<[^>]*>)\s+)$"
)


def real_type_use(code, t, rx):
    """Return the first match of `rx` in `code` that is a genuine type usage."""
    for m in rx.finditer(code):
        prefix = code[:m.start()].rstrip()
        if PRECEDED_BY_TYPE.search(prefix):
            continue
        return m
    return None

# Nested-type misuse that compiles against bad stubs but not in the editor.
BAD_QUALIFIED = [
    (r"\bCanvas\.RenderMode\b", "RenderMode is a top-level enum -> RenderMode.ScreenSpaceOverlay"),
    (r"\bCanvas\.ScreenSpaceOverlay\b", "use RenderMode.ScreenSpaceOverlay"),
    (r"\bScreen\.Resolution\b", "Resolution is a top-level struct"),
    (r"\bApplication\.Platform\b", "RuntimePlatform is top-level"),
    (r"\bCanvasScaler\.ScreenMatchMode\s*\.\s*\w+\s*\(", "ScreenMatchMode is an enum, not a method"),
]

# APIs removed / deprecated in Unity 6.x (6000.x). Checked with strings intact.
UNITY6_RISK = [
    (r"\bWWW\b", "WWW removed -> UnityWebRequest"),
    (r"\bMovieTexture\b", "MovieTexture removed"),
    (r"\bGUIText\b|\bGUITexture\b", "GUIText/GUITexture removed"),
    (r"\bApplication\.LoadLevel", "removed -> SceneManager.LoadScene"),
    (r'"Arial\.ttf"', "built-in Arial.ttf no longer ships in Unity 6 - must be a FALLBACK only, "
                      "with LegacyRuntime.ttf tried first (otherwise no text renders)"),
    (r"\bFindObjectOfType\b", "obsolete in Unity 6 -> FindFirstObjectByType"),
    (r"\bFindObjectsOfType\b", "obsolete in Unity 6 -> FindObjectsByType"),
    (r"\bHandheld\.", "Handheld removed"),
]

# Legacy-only compliance (Built-in RP + legacy Input Manager + uGUI legacy Text).
FORBIDDEN = [
    (r"\busing\s+TMPro\b|\bTextMeshPro", "TextMeshPro violates the legacy-only restriction"),
    (r"\busing\s+UnityEngine\.InputSystem\b|\bInputAction\b", "new Input System violates the legacy-only restriction"),
    (r"\busing\s+UnityEngine\.Rendering\.Universal\b|\bUniversalRenderPipeline", "URP violates the Built-in RP requirement"),
    (r"\busing\s+UnityEngine\.UIElements\b|\bVisualElement\b", "UIElements is not used by this UI layer"),
    (r"\bGameObject\.Find\s*\(", "master prompt: avoid GameObject.Find in gameplay code"),
]


def read(p):
    with open(p, encoding="utf-8-sig") as f:
        return f.read()


def strip_comments(src):
    """Remove comments, KEEP string literals (needed for font/asset-name rules)."""
    s = re.sub(r"/\*.*?\*/", " ", src, flags=re.S)
    s = re.sub(r"//[^\n]*", " ", s)
    return s


def strip_all(src):
    """Remove comments AND string literals (needed for type-name rules)."""
    return re.sub(r"@?\"(?:\\.|[^\"\\])*\"", '""', strip_comments(src))


def main():
    files = sorted(os.path.join(dp, f)
                   for dp, dn, fn in os.walk(ROOT) for f in fn if f.endswith(".cs"))
    raw = {os.path.relpath(p, ROOT).replace(os.sep, "/"): read(p) for p in files}
    code = {k: strip_all(v) for k, v in raw.items()}
    nocomment = {k: strip_comments(v) for k, v in raw.items()}

    issues, notes = [], []
    loc = sum(len(v.splitlines()) for v in raw.values())
    print(f"Auditing {len(files)} scripts under {os.path.abspath(ROOT)}\n")

    for rel, src in sorted(code.items()):
        usings = set(re.findall(r"^\s*using\s+([\w.]+)\s*;", raw[rel], re.M))

        # 1. using-directive fidelity - the bug class that broke the editor build
        for ns, types in NS_REQUIRED.items():
            if ns in usings:
                continue
            hit = []
            for t in types:
                m = real_type_use(src, t, type_position(t))
                if m:
                    line = src[:m.start()].count("\n") + 1
                    hit.append(f"{t} @L{line}")
            if hit:
                issues.append(f"{rel}: uses {', '.join(hit)} in a type position but is "
                              f"missing 'using {ns};'  -> CS0246 in the editor")

        # 2. nested-type misuse
        for pat, why in BAD_QUALIFIED:
            if re.search(pat, src):
                issues.append(f"{rel}: {why}")

        # 3. Unity 6.x API risk (strings kept)
        for pat, why in UNITY6_RISK:
            m = re.search(pat, nocomment[rel])
            if m:
                notes.append(f"{rel}: '{m.group(0)}' - {why}")

        # 4. legacy-only compliance
        for pat, why in FORBIDDEN:
            if re.search(pat, nocomment[rel]):
                issues.append(f"{rel}: {why}")

    # ------------------------------------------------------- project-wide wiring
    def find(pat, table=None):
        t = table if table is not None else nocomment
        return sorted(f for f, s in t.items() if re.search(pat, s))

    print("--- project-wide wiring checks (must all be satisfied) ---")
    rows = [
        ("EventSystem created",            r"\bAddComponent<\s*EventSystem\s*>", "uGUI clicks would be dead"),
        ("StandaloneInputModule added",    r"AddComponent<\s*StandaloneInputModule\s*>", "legacy Input Manager module"),
        ("Canvas + CanvasScaler + GraphicRaycaster", r"typeof\(Canvas\).*typeof\(CanvasScaler\).*typeof\(GraphicRaycaster\)", "canvas factory"),
        ("uiScaleMode = ScaleWithScreenSize", r"uiScaleMode\s*=\s*CanvasScaler\.ScaleMode\.ScaleWithScreenSize", "reference-space scaling"),
        ("referenceResolution set",        r"referenceResolution\s*=", "1280x720 design space"),
        ("AspectMatchGuard attached",      r"AddComponent<\s*(?:Core\.)?AspectMatchGuard\s*>", "aspect safety"),
        ("AspectMatchGuard.Init(scaler)",  r"guard\.Init\(", "guard gets its scaler"),
        ("renderMode = RenderMode.*",      r"renderMode\s*=\s*RenderMode\.", "top-level enum (v1.0.1 bug)"),
        ("RuntimeInitialize safety net",   r"\[RuntimeInitializeOnLoadMethod", "bootstrap-less scene routes to menu"),
        ("LegacyRuntime.ttf primary font", r'GetBuiltinResource<Font>\(\s*"LegacyRuntime\.ttf"', "Unity 6 font"),
        ("DontDestroyOnLoad on core",      r"DontDestroyOnLoad", "core survives scene loads"),
    ]
    for label, pat, why in rows:
        hits = find(pat, raw)
        mark = "OK " if hits else "!! "
        print(f"  {mark}{label:38s} {', '.join(h.split('/')[-1] for h in hits) or 'MISSING - ' + why}")

    # scaler default: screenMatchMode is never set explicitly -> Unity's default
    # (MatchWidthOrHeight) is what makes AspectMatchGuard's match switch work.
    if find(r"CreateRootCanvas\s*\(", raw) and not find(r"screenMatchMode\s*=", raw):
        notes.append("UIFactory.CreateRootCanvas never sets scaler.screenMatchMode - it relies on "
                     "Unity's default (MatchWidthOrHeight), which is what AspectMatchGuard assumes. "
                     "Works, but setting it explicitly would be self-documenting.")

    # scene-name constants must match real scene assets
    scenes = sorted(f[:-6] for f in os.listdir(os.path.join(ROOT, "_Project", "Scenes"))
                    if f.endswith(".unity")) if os.path.isdir(os.path.join(ROOT, "_Project", "Scenes")) else []
    consts = sorted(set(re.findall(r'public const string \w+ = "([^"]+)"',
                                   "\n".join(raw.values()))))
    routed = [c for c in consts if c in ("MainMenu", "Hollowbrook", "School")]
    if scenes:
        print("\n--- scene routing ---")
        print(f"  scene assets on disk : {scenes}")
        print(f"  SceneRouter constants: {routed}")
        missing = [s for s in routed if s not in scenes]
        if missing:
            issues.append(f"SceneRouter references scenes that do not exist: {missing}")
        else:
            print("  OK - every routed scene exists (still verify ProjectSettings/EditorBuildSettings.asset)")

    # canvas reference resolution vs AspectMatchGuard's hard-coded 16:9
    m = re.search(r"canvasReferenceWidth\s*=\s*(\d+).*?canvasReferenceHeight\s*=\s*(\d+)",
                  "\n".join(raw.values()), re.S)
    if m:
        w, h = int(m.group(1)), int(m.group(2))
        guard = re.search(r"ReferenceAspect\s*=\s*([\d.f]+)\s*/\s*([\d.f]+)", "\n".join(raw.values()))
        print("\n--- reference resolution consistency ---")
        print(f"  GameConfigSO.canvasReference : {w}x{h}")
        if guard:
            def num(s):
                return float(s.rstrip("fF").rstrip("dD"))
            ga = num(guard.group(1)) / num(guard.group(2))
            print(f"  AspectMatchGuard.ReferenceAspect : {guard.group(1)}/{guard.group(2)} = {ga:.4f}")
            if abs(ga - w / h) > 1e-4:
                issues.append(f"AspectMatchGuard assumes {ga:.4f} but the canvas reference is {w/h:.4f} "
                              f"- the guard will pick the wrong match axis.")
            else:
                print("  OK - guard aspect matches the configured reference")

    print("\n--- findings (would break or misbehave in the editor) ---")
    if issues:
        for i in issues:
            print("  [ERROR-RISK]", i)
    else:
        print("  none - using-directives, qualified types and wiring all match real Unity")

    print("\n--- review notes (not errors) ---")
    for n in notes:
        print("  [note]", n)
    if not notes:
        print("  none")

    print(f"\n{len(files)} scripts, {loc:,} lines of C#.")
    return 1 if issues else 0


if __name__ == "__main__":
    sys.exit(main())
