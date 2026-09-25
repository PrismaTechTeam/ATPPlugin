using System;
using System.Windows.Forms;
using DevExpress.XtraEditors;

namespace ServiceContractPhotocopier.ServiceContract.OperationForms
{
    /// <summary>
    /// The terms on one printed line: the least its copies are billed at, and the deal that gives the
    /// rental back when they print enough.
    ///
    /// <para><b>Why these are not rows in the grid.</b> They were, and they read badly, because they
    /// are not what the grid's columns are for. A charge row answers "how much per unit"; a term
    /// answers "and if the month goes this way, then that" — a sentence with two figures in it. A
    /// grid gives every row the same columns, so the sentence had to be spread across headings that
    /// meant nothing on the other rows ("Price now" over the words "bill at least"). Here there is
    /// room to say it in order, once.</para>
    ///
    /// <para>Both are stored where the engine already reads them: a COMMIT or WAIVE meter on the
    /// line's first machine, scoped to the line. Nothing is invented, so nothing can be written here
    /// and quietly never read.</para>
    /// </summary>
    public partial class LineTerms_Form : XtraForm
    {
        /// <summary>One machine on the line, and the floor it carries on its own.</summary>
        public class MachineMin
        {
            public string Name = "";
            public string Model = "";
            public decimal Min;
            public decimal WaiveAt;
            public decimal WaiveAmount;
            public int WaiveFreeMonths;
        }

        /// <summary>Bill the line's copies at least this much a month. 0 = no minimum.
        /// Only meaningful when <see cref="MinPerMachine"/> is false.</summary>
        public decimal MinCharge;

        /// <summary>Whose copies the floor is measured over, in the engine's own letters:
        /// <c>S</c> each machine against its own, <c>G</c> this line's machines added up,
        /// <c>C</c> every machine on the contract added up.
        ///
        /// <para>All three print ONE row on the invoice. Only the arithmetic differs — which is why
        /// they belong on one screen, as three answers to a single question, rather than scattered
        /// across the screens that happen to know about a machine or a line.</para></summary>
        public string MinScope = "G";

        /// <summary>Shorthand for <see cref="MinScope"/> == "S".</summary>
        public bool MinPerMachine { get { return MinScope == "S"; } }

        /// <summary>The machines on this line and their own floors, filled in and read back when
        /// <see cref="MinPerMachine"/> is chosen.</summary>
        public readonly System.Data.DataTable Machines = NewMachineTable();

        /// <summary>Which copies the minimum is measured over: <c>BKCL</c> both, <c>BK</c> black
        /// only, <c>CL</c> colour only. A deal can floor the black copies and leave colour out of it
        /// (feedback ATP-4). The engine has always read it -- the meter's WaiveScope -- this is the
        /// screen that sets it.</summary>
        public string MinCount = "BKCL";

        /// <summary>"Charge a minimum" was ticked when OK was pressed.</summary>
        public bool MinOn;

        // The Count only choices, by their words: a row of Meters / Pricing offers only its own
        // colour and "black and colour" (see LimitMinCount), so the list is not always all three.
        private const string MIN_BOTH = "black and colour";
        private const string MIN_BK = "black only";
        private const string MIN_CL = "colour only";

        /// <summary>Whose copies decide the waive: <c>G</c> this line's machines added up, <c>S</c>
        /// each machine against its own. A waive credits the RENTAL, so the set is the machines that
        /// share the rental — unlike a minimum, which floors the copies and follows the copies.</summary>
        public string WaiveScopeMode = "G";

        /// <summary>The machines on this line with their own waive deals, when each has one.</summary>
        public readonly System.Data.DataTable WaiveMachines = NewWaiveTable();

        private static System.Data.DataTable NewWaiveTable()
        {
            System.Data.DataTable t = new System.Data.DataTable("WaiveMachines");
            t.Columns.Add("Name", typeof(string));
            t.Columns.Add("Model", typeof(string));
            t.Columns.Add("At", typeof(decimal));
            t.Columns.Add("Amount", typeof(decimal));
            t.Columns.Add("FreeMonths", typeof(int));
            return t;
        }

        private static System.Data.DataTable NewMachineTable()
        {
            System.Data.DataTable t = new System.Data.DataTable("Machines");
            t.Columns.Add("Name", typeof(string));
            t.Columns.Add("Model", typeof(string));
            t.Columns.Add("Min", typeof(decimal));
            return t;
        }

        /// <summary>What the line's black and colour have to come to before the waive fires.
        /// 0 = no waive.</summary>
        public decimal WaiveAt;

        /// <summary>How much comes off the rental when they do.</summary>
        public decimal WaiveAmount;

        /// <summary>Waived outright for this many months from the rental's start, whatever the
        /// machines print. 0 = no free window. Where a target is set too, the window runs first and
        /// the target takes over when it ends.</summary>
        public int WaiveFirstN;

        /// <summary>The consolation band: reach only this much and a smaller amount comes off
        /// instead of the whole thing. Both 0 = no band, and falling short means the rental is
        /// charged in full.</summary>
        public decimal WaivePartialAt, WaivePartialOff;

        /// <summary>Which copies count towards the target: <c>BKCL</c> both, <c>BK</c> black only,
        /// <c>CL</c> colour only.</summary>
        public string WaiveCount = "BKCL";

        public LineTerms_Form()
        {
            InitializeComponent();
        }

        /// <summary>Whether the waive half may be used at all. A waive is a credit against the
        /// RENTAL, decided by what the black and colour came to; it only reads as a deal when both
        /// halves land on the same piece of paper. Split the rental onto its own invoice and the
        /// customer gets a credit on one page whose reason is printed on another.</summary>
        private readonly bool _allowWaive;

        /// <summary>Whether the waive may be triggered by COPIES.
        ///
        /// <para>A waive decided by what the black and colour came to needs both halves on one
        /// piece of paper -- split the rental onto its own invoice and the customer reads a credit
        /// on one page whose reason is printed on another.</para>
        ///
        /// <para>A waive decided by the MONTH does not: "free for the first twelve months,
        /// whatever they print" reads no copies at all -- ScpWaiveMeter fires it on the month
        /// number and never enters the usage loop. Refusing the whole dialog because half of it
        /// cannot be judged took away the half that can, and free months are the commonest opening
        /// term in these deals.</para></summary>
        private readonly bool _allowUsageWaive;

        /// <summary>What this line's rental comes to in a month. Offered as the waive amount the
        /// moment the waive is switched on, because "free" means the rent -- asking someone to
        /// re-type a figure printed two inches above is asking them to make a typo.</summary>
        private readonly decimal _lineRental;
        private readonly bool _allowMin;
        /// <summary>This rental goes out on an invoice of its own ("a rental invoice and a meter
        /// invoice", or per machine with the rental apart), so a waive has nothing to be judged by.</summary>
        private readonly bool _rentalApart;
        private bool _oneMachine;

        public LineTerms_Form(string lineName, int machines, decimal minCharge,
            decimal waiveAt, decimal waiveAmount, bool allowMin, bool allowWaive, string whyNoWaive,
            bool perMachine, System.Collections.Generic.List<MachineMin> onLine,
            bool waivePerMachine, int waiveFirstN,
            decimal waivePartialAt, decimal waivePartialOff, string waiveCount,
            bool allowUsageWaive, decimal lineRental, string minCount) : this()
        {
            MinScope = perMachine ? "S" : "G";
            if (onLine != null)
                foreach (MachineMin m in onLine)
                    Machines.Rows.Add(m.Name, m.Model, m.Min);
            GridMin.DataSource = Machines;
            if (onLine != null)
                foreach (MachineMin m in onLine)
                    WaiveMachines.Rows.Add(m.Name, m.Model, m.WaiveAt, m.WaiveAmount,
                                           m.WaiveFreeMonths);
            GridWaive.DataSource = WaiveMachines;
            WaiveScopeMode = waivePerMachine ? "S" : "G";
            RgWaiveMode.EditValue = WaiveScopeMode;
            RgWaiveMode.EditValueChanged += new EventHandler(Gate);
            RgMinMode.EditValue = MinScope;
            RgMinMode.EditValueChanged += new EventHandler(Gate);
            _allowMin = allowMin;
            _allowWaive = allowWaive;
            _allowUsageWaive = allowUsageWaive;
            _lineRental = lineRental;
            // The caller hands over a reason only when the rental sits on its own invoice.
            _rentalApart = allowWaive && !string.IsNullOrEmpty(whyNoWaive);
            LblWhyNot.Text = allowWaive ? "" : (whyNoWaive ?? "");
            MinCharge = minCharge;
            WaiveAt = waiveAt;
            WaiveAmount = waiveAmount;

            LblLine.Text = lineName + "   (" + machines + (machines == 1 ? " machine)" : " machines)");
            // One machine is not a choice. "Each machine has its own" and "one for the whole line" say
            // the same thing about it, so the question is not asked -- and the figures move up into the
            // space the radios would have taken rather than sitting under a blank.
            _oneMachine = machines < 2;
            if (_oneMachine)
            {
                RgMinMode.Visible = false;
                RgWaiveMode.Visible = false;
                RgMinMode.EditValue = "G";
                RgWaiveMode.EditValue = "G";
                MinScope = MinScope == "C" ? "C" : "G";
                WaiveScopeMode = "G";
                // and the figures move into the row the radios were on, so the group is not a box
                // with a hole where a question used to be.
                Up(LblMinA); Up(SpnMin); Up(LblMinB); Up(LblMinCount); Up(CmbMinCount); Up(LblMinC);
                Up(LblWaiveA); Up(SpnWaiveAt); Up(LblWaiveB); Up(SpnWaiveAmount); Up(LblWaiveC);
                // ...and everything BELOW them, which is the rest of the deal. Only the first two
                // rows used to move, and the box was then cut to the second one -- so a line with
                // ONE machine lost the free window, the consolation band and "count only"
                // altogether. They were not hidden by a rule; they were simply below the bottom
                // edge of a box measured before they existed.
                // ChkFree is NOT in this list any more: the free window now sits above the
                // radios, so nothing under it moved and shifting it would run it into the
                // group title.
                Up(ChkPartial); Up(SpnPartAt); Up(LblPartB); Up(SpnPartOff); Up(LblPartC);
                Up(LblCount); Up(CmbCount); Up(LblWaiveD);
                GrpMin.Height = LblMinC.Bottom + 12 + Chrome(GrpMin);
            }
            SpnMin.Value = minCharge;
            MinCount = (minCount ?? "BKCL").Trim().ToUpperInvariant();
            SelectMinCount(MinCount);
            CmbMinCount.SelectedIndexChanged += new EventHandler(Gate);
            SpnWaiveAt.Value = waiveAt;
            SpnWaiveAmount.Value = waiveAmount;
            WaiveFirstN = waiveFirstN;
            WaivePartialAt = waivePartialAt;
            WaivePartialOff = waivePartialOff;
            WaiveCount = (waiveCount ?? "BKCL").Trim().ToUpperInvariant();
            ChkFree.Checked = waiveFirstN > 0;
            SpnFree.Value = waiveFirstN > 0 ? waiveFirstN : 12;
            ChkPartial.Checked = waivePartialAt > 0m && waivePartialOff > 0m;
            SpnPartAt.Value = waivePartialAt;
            SpnPartOff.Value = waivePartialOff;
            CmbCount.SelectedIndex = WaiveCount == "BK" ? 1 : (WaiveCount == "CL" ? 2 : 0);
            ChkFree.CheckedChanged += new EventHandler(Gate);
            ChkPartial.CheckedChanged += new EventHandler(Gate);
            // Ticked when this line HAS a floor, whichever of the two ways it was written.
            ChkMin.Checked = allowMin && (minCharge > 0m || (perMachine && SumMachineMins() > 0m));
            ChkWaive.Checked = allowWaive &&
                (waiveAt > 0m || waiveAmount > 0m || (waivePerMachine && SumWaiveAmounts() > 0m));
            ChkMin.Enabled = allowMin;
            ChkWaive.Enabled = allowWaive;

            // Only the half this line can actually carry. A minimum floors the copies, so it belongs to
            // a black-and-colour line; a waive credits the rental, so it belongs to a rental line. The
            // other half is not greyed out here -- it is not shown, because it was never this line's
            // question. The one exception is a waive on a split contract: that one IS this line's
            // question, and it is shown disabled with the answer beside it.
            GrpMin.Visible = allowMin;
            GrpWaive.Visible = allowWaive || LblWhyNot.Text.Length > 0;
            OneUp();

            ChkMin.CheckedChanged += new EventHandler(Gate);
            // Refused before it lands, so the box never flickers on and off.
            ChkWaive.EditValueChanging += new DevExpress.XtraEditors.Controls.ChangingEventHandler(RefuseWaiveWhenRentalApart);
            ChkWaive.CheckedChanged += new EventHandler(WaiveSwitchedOn);
            ChkWaive.CheckedChanged += new EventHandler(Gate);
            BtnOK.Click += new EventHandler(BtnOK_Click);
            this.Load += new EventHandler(Gate);
        }

        /// <summary>One row up — the height of the mode radios, which a single machine is not asked.
        /// </summary>
        /// <summary>Slides a control up by the row the radios were on.
        ///
        /// <para>The distance is the radio row's own height, asked at runtime. It used to be the
        /// number 28, which is what that row measures at 96 DPI and nowhere else: on a scaled
        /// screen every control here is 14% further down than the designer put it, and a fixed
        /// 28 leaves the rows overlapping by the difference.</para></summary>
        private void Up(System.Windows.Forms.Control c)
        {
            c.Location = new System.Drawing.Point(c.Left, c.Top - (RgWaiveMode.Height + 4));
        }

        /// <summary>With one group hidden, close the gap it left rather than leaving the dialog with a
        /// hole in it and the buttons stranded at the bottom.</summary>
        /// <summary>How much of a group's height is caption and border rather than room for
        /// controls. Sizing a box to its contents needs this; leaving it out loses a caption's worth
        /// off the bottom, every time.</summary>
        private static int Chrome(System.Windows.Forms.Control c)
        {
            return c.Height - c.ClientSize.Height;
        }

        private void OneUp()
        {
            // Asked of the FLAGS, never of Control.Visible. A control reports Visible == false until
            // its whole parent chain is shown, so reading it from a constructor answers "no" about
            // something that was set to true two lines earlier -- and the dialog was sized around the
            // wrong group, putting OK inside it.
            bool showMin = _allowMin;
            bool showWaive = _allowWaive || LblWhyNot.Text.Length > 0;
            // Both on screen: the waive box follows the minimum box, wherever that one ended up --
            // it grows and shrinks with its own contents. This used to give up here and leave the
            // form the height the designer drew, so whatever the waive box grew to was simply cut.
            if (showMin && showWaive)
                GrpWaive.Location = new System.Drawing.Point(16, GrpMin.Bottom + 12);
            else
            {
                System.Windows.Forms.Control only = showMin ? (System.Windows.Forms.Control)GrpMin : GrpWaive;
                only.Location = new System.Drawing.Point(16, 44);
            }
            System.Windows.Forms.Control shown = showWaive ? (System.Windows.Forms.Control)GrpWaive : GrpMin;
            int bottom = shown.Bottom + 14;
            BtnOK.Location = new System.Drawing.Point(BtnOK.Left, bottom);
            BtnCancel.Location = new System.Drawing.Point(BtnCancel.Left, bottom);
            this.ClientSize = new System.Drawing.Size(this.ClientSize.Width, bottom + BtnOK.Height + 12);
        }

        /// <summary>A term that is switched off greys its figures rather than hiding them: coming back
        /// to a line should show what the deal used to be, not an empty box.</summary>
        /// <summary>Switching the waive on offers the line's own rent as the amount to take off.
        ///
        /// <para>Every waive here is a credit against the rent, and the commonest one takes all of
        /// it -- "free for the first twelve months" means free. Leaving the box at 0.00 made the
        /// screen refuse at OK ("a waive with nothing coming off the rental does nothing") over a
        /// figure it already knew. Typed over the moment it is wrong; never touched again after
        /// that, so a deal that waives half the rent stays as it was keyed.</para></summary>
        /// <summary>
        /// A waive credits the rental because of what the copies came to, so the two have to be on the
        /// same invoice. Where the rental goes out on an invoice of its own the tick is refused here,
        /// before it lands.
        ///
        /// <para>Left alone it did land: the copy thresholds are shut on a split contract, so the waive
        /// went on with no condition at all -- "Nothing set above: the rental is waived EVERY month",
        /// found by the user on the client's book (21/9). Free months stay open: they read a calendar,
        /// not a meter. Switching a contract that already has a waive to a split is guarded from the
        /// other side, in RentalGroupPrice_Form.RgSplit_Changed.</para>
        /// </summary>
        private void RefuseWaiveWhenRentalApart(object sender, DevExpress.XtraEditors.Controls.ChangingEventArgs e)
        {
            if (!_rentalApart) return;
            if (!(e.NewValue is bool) || !(bool)e.NewValue) return;   // only switching it ON is refused
            e.Cancel = true;
            XtraMessageBox.Show(this,
                "This contract sends the rental on an invoice of its own, so the rental cannot be waived here." +
                Environment.NewLine + Environment.NewLine +
                "A waive takes money off the rental because of what the copies came to, and the two have to be " +
                "on the same invoice. To waive the rental, first set \"This contract sends\" to one invoice for " +
                "everything, or one invoice per machine." +
                Environment.NewLine + Environment.NewLine +
                "Free months still work: they read a calendar, not a meter.",
                "Waive the rental", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void WaiveSwitchedOn(object sender, EventArgs e)
        {
            if (!ChkWaive.Checked || _lineRental <= 0m) return;
            if (SpnWaiveAmount.Value == 0m) SpnWaiveAmount.Value = _lineRental;
        }

        /// <summary>
        /// Lays the dialog out again once the window actually exists.
        ///
        /// <para>Everything the constructor does happens at designer scale. The form then scales
        /// itself to the screen when it is shown -- 115% on the machine this was found on -- and
        /// a box measured before that was measured in the wrong units: about a caption short,
        /// which is exactly how much of it went missing. Sizing it again here costs one layout
        /// pass and is the only moment both the contents and their real size are known.</para>
        /// </summary>
        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            Gate(null, EventArgs.Empty);
        }

        private void Gate(object sender, EventArgs e)
        {
            // One deal or the other is on screen, never both -- two live figures for one floor is
            // how a screen ends up saying something the invoice does not do.
            string mode = Convert.ToString(RgMinMode.EditValue);
            bool per = !_oneMachine && mode == "S";
            bool on = _allowMin && ChkMin.Checked;
            RgMinMode.Enabled = on;
            GridMin.Visible = on && per;
            LblMinA.Visible = SpnMin.Visible = LblMinB.Visible = on && !per;
            // Which copies the floor is measured over -- the same question the waive asks below,
            // asked of the minimum (feedback ATP-4). It applies to both modes: per machine, each
            // machine's floor is measured over the copies chosen here.
            LblMinCount.Visible = CmbMinCount.Visible = on;
            CmbMinCount.Enabled = on;
            LblMinA.Text = SelectedMinCount() == "BK"
                ? "Their black copies must come to at least   RM"
                : (SelectedMinCount() == "CL"
                    ? "Their colour copies must come to at least   RM"
                    : "Their black and colour must come to at least   RM");
            // The sentence sits UNDER whatever is on screen. It used to be pinned where the single
            // figure leaves room, so switching to the per-machine sheet drew it straight across the
            // grid's first row -- the top machine simply vanished behind an explanation of itself.
            LblMinC.Text = per
                ? "Each machine is measured against its own copies; the invoice prints the shortfalls added up."
                : (mode == "C"
                    ? "Measured over EVERY machine on this contract, not just this row's."
                    : "Print less and the invoice charges the difference, not the whole amount.");
            int minRowBottom = per ? GridMin.Bottom : SpnMin.Bottom;
            LblMinCount.Location = new System.Drawing.Point(34, minRowBottom + 9);
            CmbMinCount.Location = new System.Drawing.Point(110, minRowBottom + 6);
            LblMinC.Location = new System.Drawing.Point(LblMinA.Left,
                on ? CmbMinCount.Bottom + 8 : minRowBottom + 10);
            SpnMin.Enabled = on && !per;
            // Shown and live are two different questions here. A waive the contract cannot carry -- the
            // rental is on its own invoice -- is still SHOWN, greyed, with the reason beside it, because
            // the deal exists and the person is looking for it. Hiding it answers nothing.
            bool wPer = !_oneMachine && Convert.ToString(RgWaiveMode.EditValue) == "S";
            bool wShown = _allowWaive || LblWhyNot.Text.Length > 0;
            bool wOn = _allowWaive && ChkWaive.Checked;
            RgWaiveMode.Visible = wShown && !_oneMachine;
            RgWaiveMode.Enabled = wOn;
            GridWaive.Visible = wShown && wPer;
            GridWaive.Enabled = wOn;
            LblWaiveA.Visible = SpnWaiveAt.Visible = LblWaiveB.Visible =
                SpnWaiveAmount.Visible = wShown && !wPer;
            // The trigger is the only part that reads copies. The amount is not: a free-months
            // waive still has to say how much comes off.
            SpnWaiveAt.Enabled = wOn && !wPer && _allowUsageWaive;
            SpnWaiveAmount.Enabled = wOn && !wPer;
            LblWaiveC.Visible = wShown;
            LblWaiveC.Text = wPer
                ? "Each machine is judged on its own copies; the invoice prints one credit off the rental."
                : "as a credit on the invoice";
            LblWaiveC.Location = wPer
                ? new System.Drawing.Point(LblWaiveB.Left, GridWaive.Bottom + 6)
                : new System.Drawing.Point(SpnWaiveAmount.Right + 8, SpnWaiveAmount.Top + 3);

            // The rest of the deal. A free window and a partial band are agreed for the LINE, so
            // they stay on screen in both modes -- the per-machine sheet only differs in how many
            // months each machine gets, which is a column on it.
            // FREE MONTHS ARE THEIR OWN DEAL. Some customers get months of free rental and
            // nothing else; some get a credit earned by copies and no free months. Tying the
            // free window to the waive switch meant the first kind could not be set at all
            // without also turning on a waive they were never promised.
            ChkFree.Visible = SpnFree.Visible = LblFreeB.Visible = LblFreeC.Visible = !wPer;
            ChkPartial.Visible = SpnPartAt.Visible = LblPartB.Visible =
                SpnPartOff.Visible = LblPartC.Visible = LblCount.Visible =
                CmbCount.Visible = LblWaiveD.Visible = wShown;
            ChkFree.Enabled = !wPer;
            SpnFree.Enabled = !wPer && ChkFree.Checked;
            ChkPartial.Enabled = wOn && _allowUsageWaive;
            SpnPartAt.Enabled = SpnPartOff.Enabled = wOn && _allowUsageWaive && ChkPartial.Checked;
            CmbCount.Enabled = wOn && _allowUsageWaive;
            // What the deal comes to when nothing else is set. The old screen said this in red at
            // the bottom and it is the most surprising answer here, so it keeps saying it.
            bool anyCondition = (!wPer && ChkFree.Checked)
                             || (wPer && AnyFreeMonths())
                             || (!wPer && SpnWaiveAt.Value > 0m)
                             || (wPer && AnyTarget());
            LblWaiveD.Text = anyCondition
                ? "Nothing reached in a month means the rental is charged in full."
                : "Nothing set above: the rental is waived EVERY month.";
            LblWaiveD.Location = new System.Drawing.Point(34,
                wPer ? GridWaive.Bottom + 26 : CmbCount.Bottom + 10);
            // The box ends under whatever it actually holds. Two different mistakes have cut this
            // dialog off before, and both are worth naming:
            //
            //   * measuring against a named control that is no longer the last one, and
            //   * mixing coordinate systems. A child's Bottom is measured inside its parent's CLIENT
            //     area; the group's own Height also covers the caption and the borders. Subtracting
            //     the group's position on the FORM, as this line used to, answers a question nobody
            //     asked and comes out roughly a caption too short -- which is exactly how much of
            //     the box went missing.
            GrpWaive.Height = LblWaiveD.Bottom + 12 + Chrome(GrpWaive);
            GrpMin.Height = LblMinC.Bottom + 12 + Chrome(GrpMin);
            // ...and the WINDOW ends under the box. OneUp sized it once, in the constructor, before
            // any of the above had happened -- so the form stayed the height the box used to be and
            // clipped it however the box grew afterwards.
            OneUp();
            LblCount.Location = new System.Drawing.Point(34, wPer ? GridWaive.Bottom + 50 : 249);
            CmbCount.Location = new System.Drawing.Point(110, wPer ? GridWaive.Bottom + 47 : 246);
            ChkPartial.Location = new System.Drawing.Point(32, wPer ? GridWaive.Bottom + 74 : 220);
            SpnPartAt.Location = new System.Drawing.Point(176, wPer ? GridWaive.Bottom + 72 : 218);
            LblPartB.Location = new System.Drawing.Point(284, wPer ? GridWaive.Bottom + 75 : 221);
            SpnPartOff.Location = new System.Drawing.Point(360, wPer ? GridWaive.Bottom + 72 : 218);
            LblPartC.Location = new System.Drawing.Point(468, wPer ? GridWaive.Bottom + 75 : 221);
        }

        private bool AnyFreeMonths()
        {
            foreach (System.Data.DataRow r in WaiveMachines.Rows)
                if (r.RowState != System.Data.DataRowState.Deleted && r["FreeMonths"] != DBNull.Value
                    && Convert.ToInt32(r["FreeMonths"]) > 0) return true;
            return false;
        }

        private bool AnyTarget()
        {
            foreach (System.Data.DataRow r in WaiveMachines.Rows)
                if (r.RowState != System.Data.DataRowState.Deleted && r["At"] != DBNull.Value
                    && Convert.ToDecimal(r["At"]) > 0m) return true;
            return false;
        }

        private decimal SumWaiveAmounts()
        {
            decimal sum = 0m;
            foreach (System.Data.DataRow r in WaiveMachines.Rows)
                if (r.RowState != System.Data.DataRowState.Deleted && r["Amount"] != DBNull.Value)
                    sum += Math.Abs(Convert.ToDecimal(r["Amount"]));
            return sum;
        }

        private decimal SumMachineMins()
        {
            decimal sum = 0m;
            foreach (System.Data.DataRow r in Machines.Rows)
                if (r.RowState != System.Data.DataRowState.Deleted && r["Min"] != DBNull.Value)
                    sum += Math.Abs(Convert.ToDecimal(r["Min"]));
            return sum;
        }

        /// <summary>
        /// The minimum is set from the Black or the Colour row of Meters / Pricing, and each row
        /// holds its own (feedback ATP-4: "I only set it for black, then it applies to colour as
        /// well"). So the row's colour is the only one-colour choice it offers, beside "black and
        /// colour" -- a colour minimum is set on the Colour row, where it shows.
        /// </summary>
        public void LimitMinCount(string rowColour)
        {
            string c = (rowColour ?? "").Trim().ToUpperInvariant();
            if (c != "BK" && c != "CL") return;
            string keep = SelectedMinCount();
            CmbMinCount.SelectedIndexChanged -= new EventHandler(Gate);
            CmbMinCount.Properties.Items.Clear();
            CmbMinCount.Properties.Items.Add(c == "BK" ? MIN_BK : MIN_CL);
            CmbMinCount.Properties.Items.Add(MIN_BOTH);
            SelectMinCount(keep == "BKCL" ? "BKCL" : c);
            CmbMinCount.SelectedIndexChanged += new EventHandler(Gate);
        }

        private string SelectedMinCount()
        {
            string t = Convert.ToString(CmbMinCount.EditValue);
            return t == MIN_BK ? "BK" : (t == MIN_CL ? "CL" : "BKCL");
        }

        private void SelectMinCount(string code)
        {
            CmbMinCount.EditValue = code == "BK" ? MIN_BK : (code == "CL" ? MIN_CL : MIN_BOTH);
        }

        private void BtnOK_Click(object sender, EventArgs e)
        {
            ViewMin.PostEditor();
            ViewMin.UpdateCurrentRow();
            MinScope = _allowMin && ChkMin.Checked ? Convert.ToString(RgMinMode.EditValue) : "G";
            MinCharge = _allowMin && ChkMin.Checked && !MinPerMachine ? SpnMin.Value : 0m;
            MinOn = _allowMin && ChkMin.Checked;
            MinCount = !MinOn ? "BKCL" : SelectedMinCount();
            ViewWaive.PostEditor();
            ViewWaive.UpdateCurrentRow();
            WaiveScopeMode = _allowWaive && ChkWaive.Checked
                           ? Convert.ToString(RgWaiveMode.EditValue) : "G";
            bool wPerMachine = WaiveScopeMode == "S";
            WaiveAt = _allowWaive && _allowUsageWaive && ChkWaive.Checked && !wPerMachine
                    ? SpnWaiveAt.Value : 0m;
            WaiveAmount = _allowWaive && ChkWaive.Checked && !wPerMachine ? SpnWaiveAmount.Value : 0m;
            // Free months with no waive: the rent that is being given away is still worth
            // stating -- the summary column says "900.00 a month" and the reading log wants a
            // figure -- so the line rental stands in for it. Nothing is credited either way.
            if (WaiveAmount <= 0m && WaiveFirstN > 0) WaiveAmount = _lineRental;
            bool wLive = _allowWaive && ChkWaive.Checked;
            WaiveFirstN = !wPerMachine && ChkFree.Checked ? Convert.ToInt32(SpnFree.Value) : 0;
            WaivePartialAt = wLive && _allowUsageWaive && ChkPartial.Checked ? SpnPartAt.Value : 0m;
            WaivePartialOff = wLive && _allowUsageWaive && ChkPartial.Checked ? SpnPartOff.Value : 0m;
            WaiveCount = !wLive ? "BKCL"
                       : (CmbCount.SelectedIndex == 1 ? "BK"
                       : (CmbCount.SelectedIndex == 2 ? "CL" : "BKCL"));

            if (_allowMin && ChkMin.Checked && MinPerMachine && SumMachineMins() <= 0m)
            {
                XtraMessageBox.Show(
                    "Nothing has been typed against any machine, so no machine has a floor." +
                    Environment.NewLine +
                    "Give at least one of them a figure, or switch to one minimum for the whole line.",
                    "Minimum / waive", MessageBoxButtons.OK, MessageBoxIcon.Information);
                GridMin.Focus();
                return;
            }

            if (ChkWaive.Checked && _allowWaive && wPerMachine && SumWaiveAmounts() <= 0m)
            {
                XtraMessageBox.Show(
                    "No machine has anything coming off its rental, so the waive does nothing." +
                    Environment.NewLine +
                    "Give at least one of them a figure, or switch to one waive for the whole line.",
                    "Minimum / waive", MessageBoxButtons.OK, MessageBoxIcon.Information);
                GridWaive.Focus();
                return;
            }

            if (wLive && _allowUsageWaive && ChkPartial.Checked
                && (WaivePartialAt <= 0m || WaivePartialOff <= 0m))
            {
                XtraMessageBox.Show(
                    "The consolation band needs both halves: what they have to reach, and what comes " +
                    "off when they do." + Environment.NewLine +
                    "Fill both in, or untick it.",
                    "Minimum / waive", MessageBoxButtons.OK, MessageBoxIcon.Information);
                SpnPartAt.Focus();
                return;
            }

            if (ChkWaive.Checked && _allowWaive && !wPerMachine && WaiveAmount <= 0m)
            {
                XtraMessageBox.Show(
                    "A waive with nothing coming off the rental does nothing." + Environment.NewLine +
                    "Type how much is waived, or untick it.",
                    "Minimum / waive", MessageBoxButtons.OK, MessageBoxIcon.Information);
                SpnWaiveAmount.Focus();
                return;
            }

            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
