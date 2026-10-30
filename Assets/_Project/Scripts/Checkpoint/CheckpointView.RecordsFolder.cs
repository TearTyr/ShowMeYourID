using System.Collections.Generic;
using Hollowbrook.Bootstrap;
using Hollowbrook.Core;
using Hollowbrook.Logic;
using Hollowbrook.UI;
using UnityEngine;
using UnityEngine.UI;
using L = Hollowbrook.Checkpoint.CheckpointLayout;

namespace Hollowbrook.Checkpoint
{
    // The STUDENT RECORDS folder: a manila folder icon on the station. Clicking it opens the
    // records panel under the station with three cohort tabs (S2024-2028, S2025-2029,
    // S2026-2030); a tab lists that cohort's students and clicking a name lays that student's
    // record on the desk. No animation: the panel is simply there or not, so a click is
    // answered on its own frame. The panel is built before the papers layer so a record
    // pulled from it always lands on top.
    public partial class CheckpointView
    {
        // Folder tints: idle under the lamp, the open cohort lit, file rows dark until pulled.
        // All multiply over the drawn sprites/panels (uGUI tint).
        private static readonly Color FolderIdle = new Color(0.88f, 0.88f, 0.90f);
        private static readonly Color FolderOpen = new Color(1.18f, 1.15f, 1.05f);
        private static readonly Color FileRowIdle = new Color(0.20f, 0.19f, 0.16f);
        private static readonly Color FileRowSel = new Color(0.42f, 0.38f, 0.26f);
        private static readonly Color StationInk = new Color(0.10f, 0.11f, 0.12f);
        private static readonly Color PanelBodyInk = new Color(0.085f, 0.082f, 0.075f);
        private static readonly Color PanelMouthInk = new Color(0.020f, 0.020f, 0.022f);

        private readonly Button[] _folderTabs = new Button[RecordSearch.CohortCount];
        private readonly Image[] _folderTabInks = new Image[RecordSearch.CohortCount];
        private Image _stationInk;          // the station face (the kit skin paints the cabinet on it)
        private Button _recordsIcon;        // the closed state: one folder you click
        private GameObject _recordsBox;     // the records panel slot, registered on the grid
        private RectTransform _recordsBody; // the panel sheet (the kit skin paints it)
        private Text _recordsHead;
        private RectTransform _recordsRows;
        private readonly List<Image> _fileRowInks = new List<Image>();
        private readonly List<int> _fileRowCanon = new List<int>();

        private int _openCohort = -1;         // folder tab index (0 = oldest), -1 = none selected
        private int _selectedCanon = -1;      // canon index of the pulled file
        private bool _recordFromFile;         // the sheet on the desk came out of the folder
        private bool _recordsOpen;

        /// <summary>True while the records panel is open (headless proofs read this).</summary>
        internal bool RecordsOpen => _recordsOpen;
        /// <summary>The open cohort's tab index (0 = oldest), or -1 when none is selected.</summary>
        internal int RecordsTab => _openCohort;

        // ---------------------------------------------------------------- build
        private void BuildRecordsFolder()
        {
            var station = UIFactory.Panel(_desk, "station", StationInk);
            Reg("station", station.rectTransform, L.Station);
            _stationInk = station;
            var cap = UIFactory.Label(station.rectTransform, "cap", "STUDENT RECORDS", 12,
                UIFactory.NsChalk, TextAnchor.MiddleCenter);
            cap.font = UIFactory.UiFontBold;
            cap.raycastTarget = false;
            L.Place(cap.rectTransform, L.Inner.StationCap);

            _recordsIcon = BuildRecordsIcon(station.rectTransform);
            for (int tab = 0; tab < _folderTabs.Length; tab++)
            {
                int t = tab;   // capture per tab
                var btn = FolderTab(station.rectTransform, "folder" + (t + 1),
                    RecordSearch.CohortLabel(RecordSearch.CohortOfTab(t)), () => OpenRecords(t));
                L.Place(btn.GetComponent<RectTransform>(), new L.Region(L.Inner.FolderTabX,
                    L.Inner.FolderTabY0 + t * L.Inner.FolderTabStep,
                    L.Inner.FolderTabW, L.Inner.FolderTabH));
                _folderTabs[t] = btn;
                _folderTabInks[t] = btn.targetGraphic as Image;
            }

            // the panel: the approved grid rect under the station, clipped to its mouth
            var slot = UIFactory.BorderedPanel(_desk, "filedrawer", PanelMouthInk, UIFactory.Border, 2);
            Reg("filedrawer", slot, L.FileDrawer);
            slot.gameObject.AddComponent<RectMask2D>();
            _recordsBox = slot.gameObject;
            _recordsBody = UIFactory.NewUI(slot, "drawerbody");
            UIFactory.PlaceTL(_recordsBody, 0f, 0f, L.FileDrawer.W, L.Inner.RecordsPanelH);
            var bodyInk = _recordsBody.gameObject.AddComponent<Image>();
            bodyInk.sprite = ProceduralArt.Get("rough_panel_dark");
            bodyInk.color = PanelBodyInk;
            bodyInk.raycastTarget = false;
            _recordsHead = UIFactory.Label(_recordsBody, "drawerhead", "", 11,
                new Color(0.72f, 0.65f, 0.45f), TextAnchor.MiddleLeft);
            _recordsHead.font = UIFactory.UiFontBold;
            _recordsHead.raycastTarget = false;
            L.Place(_recordsHead.rectTransform, L.Inner.RecordsHead);

            _recordsOpen = false;
            ApplyRecordsState();
        }

        // The closed state: the kit's manila folder with STUDENT RECORDS on it, one click to open.
        private Button BuildRecordsIcon(Transform parent)
        {
            var rt = UIFactory.NewUI(parent, "recordsicon");
            L.Place(rt, L.Inner.RecordsIcon);
            var img = rt.gameObject.AddComponent<Image>();
            var sprite = ProceduralArt.Get("sec_folder_tab1");
            img.sprite = sprite != null ? sprite : ProceduralArt.Get("folder_tab");
            img.color = FolderIdle;
            img.raycastTarget = true;
            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.transition = Selectable.Transition.ColorTint;
            var cb = btn.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1.30f, 1.28f, 1.20f);   // subtle brighten on hover
            cb.pressedColor = new Color(1.50f, 1.46f, 1.34f);
            cb.fadeDuration = 0.12f;
            btn.colors = cb;
            btn.onClick.AddListener(ToggleRecords);
            rt.gameObject.AddComponent<HoverCursor>();
            var label = UIFactory.Label(rt, "label", "STUDENT RECORDS", 15, UIFactory.NsInk,
                TextAnchor.MiddleCenter);
            label.font = UIFactory.UiFontBold;
            label.raycastTarget = false;
            L.Place(label.rectTransform, L.Inner.RecordsIconLabel);
            return btn;
        }

        // One manila folder tab: drawn sprite front, dark ink label on the body, a subtle
        // brighten on hover/press like every other desk control.
        private static Button FolderTab(Transform parent, string name, string label, System.Action onClick)
        {
            var rt = UIFactory.NewUI(parent, name);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = ProceduralArt.Get("folder_tab");
            img.color = FolderIdle;
            img.raycastTarget = true;
            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.transition = Selectable.Transition.ColorTint;
            var cb = btn.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1.30f, 1.28f, 1.20f);   // subtle brighten on hover
            cb.pressedColor = new Color(1.50f, 1.46f, 1.34f);
            cb.fadeDuration = 0.12f;
            btn.colors = cb;
            if (onClick != null) btn.onClick.AddListener(() => onClick());
            var txt = UIFactory.Label(rt, "label", label, 11, UIFactory.PaperInk, TextAnchor.MiddleLeft);
            UIFactory.AnchorsFull(txt.rectTransform);
            txt.font = UIFactory.UiFont;
            txt.raycastTarget = false;
            txt.rectTransform.offsetMin = new Vector2(10, 0);   // the word sits on the tab
            txt.rectTransform.offsetMax = new Vector2(-140, -6); // knob; the body stays paper
            return btn;
        }

        // ---------------------------------------------------------------- open / close
        /// <summary>The icon click and the TAB key: open the folder (on the cohort last browsed,
        /// else the oldest), or close it when it is already open.</summary>
        internal void ToggleRecords()
        {
            if (_recordsOpen) CloseRecords();
            else OpenRecords(_openCohort >= 0 ? _openCohort : 0);
        }

        /// <summary>Open the panel on one cohort's files. Folders are computed from the live
        /// canon roster, so the panel lists exactly what the registry holds. Asking for the
        /// cohort that is already open closes the panel.</summary>
        internal void OpenRecords(int tab)
        {
            tab = Mathf.Clamp(tab, 0, RecordSearch.CohortCount - 1);
            if (_recordsOpen && _openCohort == tab) { CloseRecords(); return; }
            // switching cohorts retires the sheet the previous cohort pulled: a record on the
            // desk always belongs to the folder it came out of. The admin's District Roster Memo
            // rides the same window and is never "stale".
            bool changed = _openCohort != tab;
            _openCohort = tab;
            if (changed && _recordFromFile && _recordGo != null && _recordGo.activeSelf)
            {
                _recordGo.SetActive(false);
                _selectedCanon = -1;
                _recordFromFile = false;
            }
            PopulateCohort(tab);
            _recordsOpen = true;
            ApplyRecordsState();
            if (_gameRoot != null) _gameRoot.Audio.PlaySfx("sfx_paper", 0.3f);
        }

        /// <summary>Close the panel. The selected file survives (the pulled record is still on
        /// the desk); a fresh visitor clears it via <see cref="ResetRecords"/>.</summary>
        internal void CloseRecords()
        {
            if (!_recordsOpen) return;
            _recordsOpen = false;
            ApplyRecordsState();
            if (_gameRoot != null) _gameRoot.Audio.PlaySfx("sfx_ui_close", 0.25f);
        }

        /// <summary>Clear the station for a fresh desk (new visitor, or leaving the desk): panel
        /// closed, no cohort selected, nothing highlighted, no sound.</summary>
        internal void ResetRecords()
        {
            _openCohort = -1;
            _selectedCanon = -1;
            _recordFromFile = false;
            _recordsOpen = false;
            ApplyRecordsState();
        }

        // Model -> widgets: the icon shows when closed, the tabs and panel when open.
        private void ApplyRecordsState()
        {
            if (_recordsBox != null) _recordsBox.SetActive(_recordsOpen);
            if (_recordsIcon != null) _recordsIcon.gameObject.SetActive(!_recordsOpen);
            for (int i = 0; i < _folderTabs.Length; i++)
            {
                if (_folderTabs[i] != null) _folderTabs[i].gameObject.SetActive(_recordsOpen);
                if (_folderTabInks[i] != null)
                    _folderTabInks[i].color = _recordsOpen && _openCohort == i ? FolderOpen : FolderIdle;
            }
        }

        private void PopulateCohort(int tab)
        {
            var canon = _gameRoot != null && _gameRoot.Roster != null ? _gameRoot.Roster.Canon : null;
            int cohort = RecordSearch.CohortOfTab(tab);
            RecordSearch.CohortFolder folder = null;
            if (canon != null)
                foreach (var f in RecordSearch.BuildCohortFolders(canon))
                    if (f.Cohort == cohort) { folder = f; break; }
            PopulateRecords(RecordSearch.CohortLabel(cohort), canon,
                folder != null ? folder.Indices : new List<int>());
        }

        // Rebuilds the panel's contents: header + one file row per student (name + registry
        // number). An empty cohort still opens — with an explicit NO FILES line. Internal so
        // the headless FlowCheck can drive the empty-folder edge with a synthetic list.
        internal void PopulateRecords(string label, IReadOnlyList<StudentIdentity> canon,
            IReadOnlyList<int> indices)
        {
            _fileRowInks.Clear();
            _fileRowCanon.Clear();
            if (_recordsRows != null) Destroy(_recordsRows.gameObject);
            if (_recordsBody == null) return;
            _recordsRows = UIFactory.NewUI(_recordsBody, "drawerrows");
            UIFactory.AnchorsFull(_recordsRows);
            int count = indices != null ? indices.Count : 0;
            if (_recordsHead != null)
                _recordsHead.text = label + " — " + count + (count == 1 ? " FILE" : " FILES");
            if (count == 0)
            {
                var none = UIFactory.Label(_recordsRows, "nofiles", "NO FILES — NOBODY FILED HERE", 11,
                    UIFactory.InkFaint, TextAnchor.MiddleCenter);
                none.raycastTarget = false;
                L.Place(none.rectTransform, new L.Region(L.Inner.FileRowX, L.Inner.FileRowY0,
                    L.Inner.FileRowW, L.Inner.FileRowH));
                return;
            }
            // a taller roster than the panel holds shows what fits plus an explicit remainder
            // line instead of spilling out of the mouth
            int shown = Mathf.Min(count, RecordsRowCapacity());
            for (int i = 0; i < shown; i++)
            {
                int canonIdx = indices[i];
                var s = canon != null && canonIdx >= 0 && canonIdx < canon.Count ? canon[canonIdx] : null;
                AddFileRow(i, canonIdx, s);
            }
            if (count > shown)
            {
                var more = UIFactory.Label(_recordsRows, "morefiles",
                    "+ " + (count - shown) + " MORE FILES — NOT SHOWN", 10,
                    UIFactory.InkFaint, TextAnchor.MiddleCenter);
                more.raycastTarget = false;
                L.Place(more.rectTransform, new L.Region(L.Inner.FileRowX,
                    L.Inner.FileRowY0 + shown * L.Inner.FileRowStep, L.Inner.FileRowW, 16));
            }
        }

        // One file row: a dark name pill with a manila edge, the registry number beside it.
        private void AddFileRow(int i, int canonIdx, StudentIdentity s)
        {
            var rowRt = UIFactory.NewUI(_recordsRows, "filerow" + i);
            float rowY = L.Inner.FileRowY0 + i * L.Inner.FileRowStep;
            L.Place(rowRt, new L.Region(L.Inner.FileRowX, rowY, L.Inner.FileRowW, L.Inner.FileRowH));
            var pillRt = UIFactory.NewUI(rowRt, "pill");
            UIFactory.PlaceTL(pillRt, 0f, 0f, L.Inner.FileRowPillW, L.Inner.FileRowH);
            var bg = pillRt.gameObject.AddComponent<Image>();
            bg.color = canonIdx == _selectedCanon ? FileRowSel : FileRowIdle;
            // the pill IS the row's click and hover surface (the Button's targetGraphic): with
            // the raycast off no pointer event would ever reach the row in play
            bg.raycastTarget = true;
            var edge = UIFactory.Panel(pillRt, "edge", new Color(0.55f, 0.49f, 0.33f));
            edge.rectTransform.anchorMin = new Vector2(0, 0);   // manila left edge: a file
            edge.rectTransform.anchorMax = new Vector2(0, 1);   // folder peeking from the row
            edge.rectTransform.offsetMin = Vector2.zero;
            edge.rectTransform.offsetMax = new Vector2(5, 0);
            edge.raycastTarget = false;
            var nm = UIFactory.Label(pillRt, "fname", s != null ? s.FullName.ToUpperInvariant() : "—",
                10, UIFactory.Ink, TextAnchor.MiddleLeft);
            UIFactory.PlaceTL(nm.rectTransform, L.Inner.FileRowNameX, 0,
                L.Inner.FileRowNameW, L.Inner.FileRowH);
            nm.font = UIFactory.UiFont;
            nm.raycastTarget = false;
            var idl = UIFactory.Label(rowRt, "fid", s != null ? s.IdNumber : "",
                9, UIFactory.InkDim, TextAnchor.MiddleRight);
            UIFactory.PlaceTL(idl.rectTransform, L.Inner.FileRowIdX, 0,
                L.Inner.FileRowIdW, L.Inner.FileRowH);
            idl.font = UIFactory.UiFont;
            // the registry number is part of the visible row, so it is part of the hit area
            idl.raycastTarget = true;
            var btn = rowRt.gameObject.AddComponent<Button>();
            btn.targetGraphic = bg;
            btn.transition = Selectable.Transition.ColorTint;
            var cb = btn.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1.35f, 1.33f, 1.24f);   // subtle brighten on hover
            cb.pressedColor = new Color(1.60f, 1.56f, 1.44f);
            cb.fadeDuration = 0.10f;
            btn.colors = cb;
            btn.onClick.AddListener(() => OpenCanonFile(canonIdx));
            _fileRowInks.Add(bg);
            _fileRowCanon.Add(canonIdx);
        }

        /// <summary>How many file rows fit in the panel below its header.</summary>
        internal static int RecordsRowCapacity() => Mathf.Max(1, Mathf.FloorToInt(
            (L.Inner.RecordsPanelH - L.Inner.FileRowY0 - L.Inner.FileRowH) / L.Inner.FileRowStep) + 1);

        // Pulls one canon file into the record window — the folder-browsing replacement for the
        // old typed search. The panel STAYS open: browsing several files in a row is the normal
        // use. During the administrator's visit the registry holds no badge, so no file replaces
        // the memo already on the desk.
        internal void OpenCanonFile(int canonIdx)
        {
            if (_gameRoot == null || _gameRoot.Roster == null) return;
            var canon = _gameRoot.Roster.Canon;
            if (canonIdx < 0 || canonIdx >= canon.Count || canon[canonIdx] == null) return;
            if (_current != null && _current.IsAdministrator)
            {
                // refusals ride the HUD toast like every other desk refusal
                _gameRoot.Bus.Publish(GameEvent.Of(EventIds.Notification,
                    "NO RECORD — SEE ATTACHED MEMO"));
                return;
            }
            _selectedCanon = canonIdx;
            _recordFromFile = true;
            FillRecordSheet(canon[canonIdx]);
            if (_recordGo != null)
            {
                _recordGo.SetActive(true);
                _recordGo.transform.SetAsLastSibling();   // the pulled file rises above the panel
            }
            RefreshFileRows();
            _gameRoot.Audio.PlaySfx("sfx_paper", 0.4f);
        }

        // The selected file row keeps its lit manila tint while its folder is open.
        private void RefreshFileRows()
        {
            for (int i = 0; i < _fileRowInks.Count; i++)
                if (_fileRowInks[i] != null)
                    _fileRowInks[i].color = i < _fileRowCanon.Count && _fileRowCanon[i] == _selectedCanon
                        ? FileRowSel
                        : FileRowIdle;
        }
    }
}
