using System;
using System.Collections.Generic;

// Does the Lines & Price screen promise what the invoice delivers?
//
// The engine has its own harness and it passes; what has never been checked is whether the SETUP
// SCREEN agrees with it. That gap is where the last three bugs came from -- the screen said "ALL
// MACHINES, one line" and the paper printed five, and nothing anywhere disagreed out loud.
//
// The screen builds its rows in RentalGroupPrice_Form.Collect(): every machine on the contract,
// bucketed by its group code, falling back to its own name. Two facts about that are worth testing:
//
//   * It walks EVERY machine, whether or not the machine owns the meter the row is about. A machine
//     with no rental meter still shows a rental row; a black-only machine still shows a colour row.
//   * It buckets on the group ALONE. The engine splits a bucket further by price, because a printed
//     line is one Qty x Unit Price and cannot hold two.
//
// Either can make the screen count fewer or more lines than the invoice. This says which, and why.
static class ScreenCheck
{
    class Mach
    {
        public string Name;
        public string RentalGroup = "";
        public string MeterGroup = "";
        public bool HasRental, HasBk, HasCl;
        public decimal RentalMoney;
        // What the line would have to print in its price cell. A ladder has no single rate, so it is
        // the ladder that decides whether two machines can share a line -- comparing rates alone said
        // two tiered machines matched (both reading 0.00) when the engine keeps them apart.
        public string BkPrice = "", ClPrice = "";
    }

    public static void Report(List<ServiceContractPhotocopier.Classes.MeterBillLine> lines,
                              int foldedRental, int foldedBk, int foldedCl)
    {
        Dictionary<long, Mach> byItem = new Dictionary<long, Mach>();
        List<long> order = new List<long>();
        foreach (ServiceContractPhotocopier.Classes.MeterBillLine l in lines)
        {
            Mach m;
            if (!byItem.TryGetValue(l.ItemKey, out m))
            {
                m = new Mach();
                m.Name = l.ItemName;
                m.RentalGroup = (l.MergeGroupCode ?? "").Trim();
                m.MeterGroup = (l.MergeGroupCodeMeter ?? "").Trim();
                byItem[l.ItemKey] = m;
                order.Add(l.ItemKey);
            }
            if (l.IsWaiveMeter || l.IsCommittedMin) continue;
            if (l.IsRental || l.IsFlat) { m.HasRental = true; m.RentalMoney = l.Charge; }
            else if (l.ColorLabel == "Colour") { m.HasCl = true; m.ClPrice = PriceOf(l); }
            else { m.HasBk = true; m.BkPrice = PriceOf(l); }
        }

        int screenRental = Buckets(byItem, order, true).Count;
        int screenMeter = Buckets(byItem, order, false).Count;

        Console.WriteLine("    screen: " + screenRental + " rental row(s), " +
                          screenMeter + " black row(s), " + screenMeter + " colour row(s)");
        Console.WriteLine("    paper : " + foldedRental + " rental line(s), " +
                          foldedBk + " black line(s), " + foldedCl + " colour line(s)");

        List<string> notes = new List<string>();
        Explain(byItem, order, true, screenRental, foldedRental, "rental", notes);
        Explain(byItem, order, false, screenMeter, foldedBk, "black", notes);
        Explain(byItem, order, false, screenMeter, foldedCl, "colour", notes);
        if (notes.Count == 0) Console.WriteLine("    AGREES");
        else foreach (string n in notes) Console.WriteLine("    DIFFERS - " + n);
    }

    static string PriceOf(ServiceContractPhotocopier.Classes.MeterBillLine l)
    {
        string ladder = (l.MultiPriceCode ?? "").Trim();
        return ladder.Length > 0 ? "L:" + ladder.ToUpperInvariant() : "P:" + l.Rate.ToString("0.######");
    }

    static Dictionary<string, List<Mach>> Buckets(Dictionary<long, Mach> byItem, List<long> order, bool rental)
    {
        Dictionary<string, List<Mach>> b = new Dictionary<string, List<Mach>>(StringComparer.OrdinalIgnoreCase);
        foreach (long k in order)
        {
            Mach m = byItem[k];
            string g = rental ? m.RentalGroup : m.MeterGroup;
            string key = g.Length > 0 ? "#" + g.ToUpperInvariant() : "=" + m.Name;
            if (!b.ContainsKey(key)) b[key] = new List<Mach>();
            b[key].Add(m);
        }
        return b;
    }

    static void Explain(Dictionary<long, Mach> byItem, List<long> order, bool rental,
                        int screen, int paper, string what, List<string> notes)
    {
        if (screen == paper) return;

        // Machines the screen counted that own no such meter.
        int missing = 0;
        foreach (long k in order)
        {
            Mach m = byItem[k];
            bool owns = rental ? m.HasRental : (what == "colour" ? m.HasCl : m.HasBk);
            if (!owns) missing++;
        }

        // Buckets holding more than one price -- the screen shows one row, the invoice one per price.
        int split = 0;
        foreach (KeyValuePair<string, List<Mach>> kv in Buckets(byItem, order, rental))
        {
            Dictionary<string, bool> prices = new Dictionary<string, bool>();
            foreach (Mach m in kv.Value)
            {
                bool owns = rental ? m.HasRental : (what == "colour" ? m.HasCl : m.HasBk);
                if (!owns) continue;
                string p = rental ? "P:" + m.RentalMoney.ToString("0.######")
                                  : (what == "colour" ? m.ClPrice : m.BkPrice);
                prices[p] = true;
            }
            if (prices.Count > 1) split += prices.Count - 1;
        }

        string why = "";
        if (missing > 0) why += missing + " machine(s) own no " + what + " meter";
        if (split > 0) why += (why.Length > 0 ? "; " : "") + split +
                              " extra line(s) from mixed prices or ladders";
        if (why.Length == 0) why = "unexplained";
        notes.Add(what + ": screen " + screen + " vs paper " + paper + "  (" + why + ")");
    }
}
