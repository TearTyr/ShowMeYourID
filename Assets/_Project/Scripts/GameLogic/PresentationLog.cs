using System.Collections.Generic;

namespace Hollowbrook.Logic
{
    /// <summary>Tonight's checkpoint presentation log. Records the CARD PHOTO each ID number
    /// was first presented with. Duplicate rule (objective + fair):
    ///  • same ID number, same card photo  => returning student (NOT a defect);
    ///  • same ID number, different card photo => duplicate presentation (defect).
    /// In Normal mode the log is cleared at the start of every shift ("tonight");
    /// in Endless/Challenge it spans the run. Because the log stores card photos (never the
    /// face at the window), a legitimate student is never flagged because an anomaly earlier
    /// wore their card — the fairness invariant asserted by the test suite.</summary>
    public class PresentationLog
    {
        private readonly Dictionary<string, Portrait> _firstCardPhoto = new Dictionary<string, Portrait>();
        private readonly Dictionary<string, string> _labels = new Dictionary<string, string>();

        public int Count => _firstCardPhoto.Count;
        public bool WasPresented(string id) => !string.IsNullOrEmpty(id) && _firstCardPhoto.ContainsKey(id);

        public bool WasPresentedWithDifferentCard(string id, Portrait cardPhoto) =>
            !string.IsNullOrEmpty(id) && _firstCardPhoto.TryGetValue(id, out var p) && p != cardPhoto;

        public string LabelFor(string id) => _labels.TryGetValue(id ?? "", out var l) ? l : "";

        /// <summary>Record a presentation. Caller guarantees the ID is registry-valid.</summary>
        public void Record(string id, Portrait cardPhoto, string label)
        {
            if (string.IsNullOrEmpty(id) || _firstCardPhoto.ContainsKey(id)) return;
            _firstCardPhoto[id] = cardPhoto;
            _labels[id] = label ?? "";
        }

        public void RecordFrom(VisitorData v, string label)
        {
            if (v == null || !IdRules.IsFormatValid(v.IdNumber)) return;
            Record(v.IdNumber, v.IdPhoto, label);
        }

        public void Clear()
        {
            _firstCardPhoto.Clear();
            _labels.Clear();
        }

        /// <summary>IDs eligible as DuplicateId evidence (already presented tonight).</summary>
        public List<string> PresentedIds()
        {
            var l = new List<string>(_firstCardPhoto.Keys);
            l.Sort(System.StringComparer.Ordinal);
            return l;
        }

        public Portrait? FirstCardPhotoOf(string id) =>
            _firstCardPhoto.TryGetValue(id ?? "", out var p) ? p : (Portrait?)null;

        // --- save support (id:h,f,a,dl|...) ---
        public string Encode()
        {
            var sb = new System.Text.StringBuilder();
            var ids = PresentedIds();
            for (int i = 0; i < ids.Count; i++)
            {
                var p = _firstCardPhoto[ids[i]];
                if (i > 0) sb.Append('|');
                sb.Append(ids[i]).Append(':')
                  .Append(p.HairStyle).Append(',').Append(p.FaceVariant).Append(',').Append(p.Accessory)
                  .Append(',').Append(_labels.TryGetValue(ids[i], out var lb) ? lb : "");
            }
            return sb.ToString();
        }

        public void Decode(string s)
        {
            Clear();
            if (string.IsNullOrEmpty(s)) return;
            foreach (var entry in s.Split('|'))
            {
                if (entry.Length == 0) continue;
                int colon = entry.IndexOf(':');
                if (colon <= 0) continue;
                string id = entry.Substring(0, colon);
                var parts = entry.Substring(colon + 1).Split(',');
                if (parts.Length < 4) continue;
                if (!int.TryParse(parts[0], out int h)) continue;
                if (!int.TryParse(parts[1], out int f)) continue;
                if (!int.TryParse(parts[2], out int a)) continue;
                Record(id, new Portrait { HairStyle = h, FaceVariant = f, Accessory = a }, parts[3]);
            }
        }
    }
}
