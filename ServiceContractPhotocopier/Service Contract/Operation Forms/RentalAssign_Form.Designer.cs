using System.Drawing;
using System.Windows.Forms;
using DevExpress.XtraEditors;

namespace ServiceContractPhotocopier.ServiceContract.OperationForms
{
    partial class RentalAssign_Form
    {
        private System.ComponentModel.IContainer components = null;
        protected override void Dispose(bool disposing) { if (disposing && (components != null)) components.Dispose(); base.Dispose(disposing); }

        private LabelControl LblItem, LblType, LblAmount, LblMin, LblFreeMonths, LblStart, LblMonths, LblBasis;
        private SearchLookUpEdit SluItem; private DevExpress.XtraGrid.Views.Grid.GridView SluItemView;
        private SearchLookUpEdit SluType; private DevExpress.XtraGrid.Views.Grid.GridView SluTypeView;
        private SpinEdit SpnAmount, SpnMin, SpnFreeMonths, SpnMonths;
        private DateEdit DtStart;
        private ComboBoxEdit CmbBasis;
        private SimpleButton BtnOK, BtnCancel;

        private void InitializeComponent()
        {
            this.LblItem = new LabelControl(); this.SluItem = new SearchLookUpEdit();
            this.SluItemView = new DevExpress.XtraGrid.Views.Grid.GridView();
            this.LblType = new LabelControl(); this.SluType = new SearchLookUpEdit();
            this.SluTypeView = new DevExpress.XtraGrid.Views.Grid.GridView();
            this.LblAmount = new LabelControl(); this.SpnAmount = new SpinEdit();
            this.LblMin = new LabelControl(); this.SpnMin = new SpinEdit();
            this.LblFreeMonths = new LabelControl(); this.SpnFreeMonths = new SpinEdit();
            this.LblStart = new LabelControl(); this.DtStart = new DateEdit();
            this.LblMonths = new LabelControl(); this.SpnMonths = new SpinEdit();
            this.LblBasis = new LabelControl(); this.CmbBasis = new ComboBoxEdit();
            this.BtnOK = new SimpleButton(); this.BtnCancel = new SimpleButton();

            ((System.ComponentModel.ISupportInitialize)(this.SluItem.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.SluItemView)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.SluType.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.SluTypeView)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.SpnAmount.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.SpnMin.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.SpnFreeMonths.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.DtStart.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.DtStart.Properties.CalendarTimeProperties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.SpnMonths.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.CmbBasis.Properties)).BeginInit();
            this.SuspendLayout();

            this.Text = "Assign Rental";
            this.ClientSize = new Size(520, 330);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false; this.MinimizeBox = false;

            int lX = 16, eX = 170, eW = 320; int y = 18, gap = 32;
            this.LblItem.Text = "Service Item *"; this.LblItem.Location = new Point(lX, y + 3);
            this.SluItem.Location = new Point(eX, y); this.SluItem.Size = new Size(eW, 20);
            this.SluItem.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
                new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.SluItem.Properties.NullText = "";
            this.SluItem.Properties.PopupView = this.SluItemView;
            y += gap;
            this.LblType.Text = "Rental Type *"; this.LblType.Location = new Point(lX, y + 3);
            this.SluType.Location = new Point(eX, y); this.SluType.Size = new Size(eW, 20);
            this.SluType.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
                new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.SluType.Properties.NullText = "";
            this.SluType.Properties.PopupView = this.SluTypeView;
            y += gap;
            this.LblAmount.Text = "Monthly Amount"; this.LblAmount.Location = new Point(lX, y + 3);
            Num(this.SpnAmount, eX, y, 140, true); y += gap;
            this.LblMin.Text = "Min Charges"; this.LblMin.Location = new Point(lX, y + 3);
            Num(this.SpnMin, eX, y, 140, true); y += gap;
            this.LblFreeMonths.Text = "Free Months (FOC)"; this.LblFreeMonths.Location = new Point(lX, y + 3);
            Num(this.SpnFreeMonths, eX, y, 140, false); y += gap;
            this.LblStart.Text = "Rental Start Date"; this.LblStart.Location = new Point(lX, y + 3);
            this.DtStart.Location = new Point(eX, y); this.DtStart.Size = new Size(140, 20);
            this.DtStart.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
                new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.DtStart.Properties.DisplayFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
            this.DtStart.Properties.DisplayFormat.FormatString = "dd/MM/yyyy";
            this.DtStart.Properties.EditFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
            this.DtStart.Properties.EditFormat.FormatString = "dd/MM/yyyy";
            this.DtStart.Properties.AllowNullInput = DevExpress.Utils.DefaultBoolean.True;
            y += gap;
            this.LblMonths.Text = "Total Months (N)"; this.LblMonths.Location = new Point(lX, y + 3);
            Num(this.SpnMonths, eX, y, 140, false); y += gap;
            this.LblBasis.Text = "Basis"; this.LblBasis.Location = new Point(lX, y + 3);
            this.CmbBasis.Location = new Point(eX, y); this.CmbBasis.Size = new Size(220, 20);
            this.CmbBasis.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;
            this.CmbBasis.Properties.Items.AddRange(new object[] { "A - Accrual (period n)", "P - Prepayment (period n+1)" });
            this.CmbBasis.SelectedIndex = 0;
            y += gap + 8;
            this.BtnOK.Text = "OK"; this.BtnOK.Location = new Point(320, y); this.BtnOK.Size = new Size(80, 26);
            this.BtnOK.Click += new System.EventHandler(this.BtnOK_Click);
            this.BtnCancel.Text = "Cancel"; this.BtnCancel.Location = new Point(410, y); this.BtnCancel.Size = new Size(80, 26);
            this.BtnCancel.Click += new System.EventHandler(this.BtnCancel_Click);

            this.Controls.Add(this.LblItem); this.Controls.Add(this.SluItem);
            this.Controls.Add(this.LblType); this.Controls.Add(this.SluType);
            this.Controls.Add(this.LblAmount); this.Controls.Add(this.SpnAmount);
            this.Controls.Add(this.LblMin); this.Controls.Add(this.SpnMin);
            this.Controls.Add(this.LblFreeMonths); this.Controls.Add(this.SpnFreeMonths);
            this.Controls.Add(this.LblStart); this.Controls.Add(this.DtStart);
            this.Controls.Add(this.LblMonths); this.Controls.Add(this.SpnMonths);
            this.Controls.Add(this.LblBasis); this.Controls.Add(this.CmbBasis);
            this.Controls.Add(this.BtnOK); this.Controls.Add(this.BtnCancel);

            ((System.ComponentModel.ISupportInitialize)(this.SluItem.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.SluItemView)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.SluType.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.SluTypeView)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.SpnAmount.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.SpnMin.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.SpnFreeMonths.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.DtStart.Properties.CalendarTimeProperties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.DtStart.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.SpnMonths.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.CmbBasis.Properties)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private static void Num(SpinEdit s, int x, int y, int w, bool money)
        {
            s.Location = new Point(x, y); s.Width = w;
            s.Properties.IsFloatValue = money;
            s.Properties.MinValue = 0m;
            s.Properties.MaxValue = 999999999m;
            s.Properties.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            s.Properties.DisplayFormat.FormatString = money ? "n2" : "n0";
        }
    }
}
