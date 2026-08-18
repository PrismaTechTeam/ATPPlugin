using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;

// Creates copier MODELS in AED_ATPTEST so the line-merging scenarios have something real to point
// at. A machine's model (zSCP2_Item.ItemCode) is an AutoCount stock item, so these go in through
// ItemDataAccess rather than an INSERT -- the Item master is four tables and a DocKey, and a
// hand-written row would be missing its UOM.
//
// Re-runnable: an item that already exists is left exactly as it is.
static class SeedItems
{
    const string AC = @"C:\Program Files\AutoCount\Accounting 2.2";
    static int _made = 0, _kept = 0;

    static int Main()
    {
        AppDomain.CurrentDomain.AssemblyResolve += (s, a) =>
        {
            string n = new AssemblyName(a.Name).Name;
            string p = Path.Combine(AC, n + ".dll");
            return File.Exists(p) ? Assembly.LoadFrom(p) : null;
        };
        try { Run(); }
        catch (Exception ex) { Console.WriteLine("FATAL: " + ex); return 2; }
        Console.WriteLine(Environment.NewLine + _made + " created, " + _kept + " already there.");
        return 0;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Run()
    {
        AutoCount.Data.DBSetting db = new AutoCount.Data.DBSetting(
            AutoCount.Data.DBServerType.SQL2000,
            "localhost,1433", "sa", "rs6663", "AED_ATPTEST", false);

        AutoCount.Authentication.UserSession ses = new AutoCount.Authentication.UserSession(db);
        if (!AutoCount.Authentication.UserSession.CurrentUserSession.Login("ADMIN", "ADMIN"))
            throw new Exception("AutoCount login failed.");
        Console.WriteLine("Logged in to AED_ATPTEST.");

        // Six models, deliberately three shapes, so every line rule has something to prove:
        //   the PAIR   two different models a customer wants on ONE rental line
        //   the FOUR   one model with several units, on a line of its own
        //   the ODDS   two more models, for "one line per model" and for leaving ungrouped
        Make(db, ses, "iR-ADV C5335",     "COPIER iR-ADV C5335 - COLOUR A3 MEDIUM DUTY");
        Make(db, ses, "iR-ADV C5665",     "COPIER iR-ADV C5665 - COLOUR A3 MEDIUM DUTY");
        Make(db, ses, "iR-ADV C1234",     "COPIER iR-ADV C1234 - COLOUR A3 LIGHT DUTY");
        Make(db, ses, "imageFORCE C5150", "COPIER imageFORCE C5150 - COLOUR A3 HEAVY DUTY");
        Make(db, ses, "iR-ADV DX 4951i",  "COPIER iR-ADV DX 4951i - MONO A3 HEAVY DUTY");
        Make(db, ses, "iR-ADV DX C3922i", "COPIER iR-ADV DX C3922i - COLOUR A3 LIGHT DUTY");
    }

    // Same shape as the book's existing copiers (iR-ADV C5540i): group C001, type H001, one UNIT,
    // serialised, not stock-controlled -- a rented machine is tracked by its contract, not by a
    // stock balance.
    static void Make(AutoCount.Data.DBSetting db, AutoCount.Authentication.UserSession ses,
        string code, string desc)
    {
        object exists = db.ExecuteScalar(
            "SELECT ItemCode FROM dbo.Item WHERE ItemCode = N'" + code.Replace("'", "''") + "'");
        if (exists != null && exists != DBNull.Value)
        {
            Console.WriteLine("  kept   " + code);
            _kept++;
            return;
        }

        AutoCount.Stock.Item.ItemDataAccess da = AutoCount.Stock.Item.ItemDataAccess.Create(ses, db);
        AutoCount.Stock.Item.ItemEntity it = da.NewItem();
        it.ItemCode = code;
        it.Description = desc;
        it.ItemGroup = "C001";
        it.ItemType = "H001";
        // NewItem() already comes with one blank UOM row -- name THAT one rather than adding a
        // second, or the item ships with a stray '' UOM next to its real one.
        if (it.UomCount > 0) it.GetUom(0).Uom = "UNIT";
        else it.NewUom("UNIT", 1m);
        it.BaseUom = "UNIT";
        it.SalesUom = "UNIT";
        it.PurchaseUom = "UNIT";
        it.ReportUom = "UNIT";
        it.StockControl = false;
        it.HasSerialNo = true;
        it.IsActive = true;
        da.SaveData(it, "ADMIN");
        Console.WriteLine("  new    " + code + "   " + desc);
        _made++;
    }
}
