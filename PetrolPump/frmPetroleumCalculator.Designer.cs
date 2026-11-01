namespace AlMadinaShahzad
{
    partial class frmPetroleumCalculator
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.pnlHeader = new System.Windows.Forms.Panel();
            this.btnClose = new Guna.UI2.WinForms.Guna2Button();
            this.btnClearAll = new Guna.UI2.WinForms.Guna2Button();
            this.lblHeader = new System.Windows.Forms.Label();
            this.lblSubtitle = new System.Windows.Forms.Label();
            this.tabMain = new System.Windows.Forms.TabControl();
            this.tabSale = new System.Windows.Forms.TabPage();
            this.lblSaleHint = new System.Windows.Forms.Label();
            this.lblSaleBalance = new System.Windows.Forms.Label();
            this.lblSaleAmount = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.txtCredit = new System.Windows.Forms.TextBox();
            this.txtAdvance = new System.Windows.Forms.TextBox();
            this.txtRate = new System.Windows.Forms.TextBox();
            this.txtLiters = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.lblLiters = new System.Windows.Forms.Label();
            this.tabProfit = new System.Windows.Forms.TabPage();
            this.lblMarginPct = new System.Windows.Forms.Label();
            this.lblProfitPct = new System.Windows.Forms.Label();
            this.lblNetProfit = new System.Windows.Forms.Label();
            this.lblGrossProfit = new System.Windows.Forms.Label();
            this.lblRateDiff = new System.Windows.Forms.Label();
            this.label10 = new System.Windows.Forms.Label();
            this.label9 = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.txtExpense = new System.Windows.Forms.TextBox();
            this.txtProfitLiters = new System.Windows.Forms.TextBox();
            this.txtPurchaseRate = new System.Windows.Forms.TextBox();
            this.txtSaleRate = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.tabPercent = new System.Windows.Forms.TabPage();
            this.lblPctHint = new System.Windows.Forms.Label();
            this.lblNetPct = new System.Windows.Forms.Label();
            this.lblNetAmount = new System.Windows.Forms.Label();
            this.label14 = new System.Windows.Forms.Label();
            this.label13 = new System.Windows.Forms.Label();
            this.txtCreditPct = new System.Windows.Forms.TextBox();
            this.txtDebit = new System.Windows.Forms.TextBox();
            this.label12 = new System.Windows.Forms.Label();
            this.label11 = new System.Windows.Forms.Label();
            this.tabAverage = new System.Windows.Forms.TabPage();
            this.lblAvgCombined = new System.Windows.Forms.Label();
            this.btnAvgClear = new Guna.UI2.WinForms.Guna2Button();
            this.btnAvgAdd = new Guna.UI2.WinForms.Guna2Button();
            this.dgvAvg = new System.Windows.Forms.DataGridView();
            this.colLiters = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colRate = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colAmount = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.lblAvgRateResult = new System.Windows.Forms.Label();
            this.lblAvgAmount = new System.Windows.Forms.Label();
            this.label18 = new System.Windows.Forms.Label();
            this.label17 = new System.Windows.Forms.Label();
            this.txtAvgRate = new System.Windows.Forms.TextBox();
            this.txtAvgLiters = new System.Windows.Forms.TextBox();
            this.label16 = new System.Windows.Forms.Label();
            this.label15 = new System.Windows.Forms.Label();
            this.tabFast = new System.Windows.Forms.TabPage();
            this.pnlKeypad = new System.Windows.Forms.FlowLayoutPanel();
            this.lblFastResult = new System.Windows.Forms.Label();
            this.btnFastLxR = new Guna.UI2.WinForms.Guna2Button();
            this.btnFastEquals = new Guna.UI2.WinForms.Guna2Button();
            this.btnFastClear = new Guna.UI2.WinForms.Guna2Button();
            this.txtFastCalc = new System.Windows.Forms.TextBox();
            this.pnlHeader.SuspendLayout();
            this.tabMain.SuspendLayout();
            this.tabSale.SuspendLayout();
            this.tabProfit.SuspendLayout();
            this.tabPercent.SuspendLayout();
            this.tabAverage.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvAvg)).BeginInit();
            this.tabFast.SuspendLayout();
            this.SuspendLayout();
            // 
            // pnlHeader
            // 
            this.pnlHeader.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(25)))), ((int)(((byte)(10)))));
            this.pnlHeader.Controls.Add(this.btnClose);
            this.pnlHeader.Controls.Add(this.btnClearAll);
            this.pnlHeader.Controls.Add(this.lblSubtitle);
            this.pnlHeader.Controls.Add(this.lblHeader);
            this.pnlHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlHeader.Location = new System.Drawing.Point(0, 0);
            this.pnlHeader.Name = "pnlHeader";
            this.pnlHeader.Size = new System.Drawing.Size(720, 72);
            this.pnlHeader.TabIndex = 0;
            // 
            // lblHeader
            // 
            this.lblHeader.AutoSize = true;
            this.lblHeader.Font = new System.Drawing.Font("Segoe UI", 16F, System.Drawing.FontStyle.Bold);
            this.lblHeader.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(215)))), ((int)(((byte)(100)))));
            this.lblHeader.Location = new System.Drawing.Point(14, 10);
            this.lblHeader.Name = "lblHeader";
            this.lblHeader.Size = new System.Drawing.Size(318, 30);
            this.lblHeader.Text = "⛽ VIP Petroleum Calculator";
            // 
            // lblSubtitle
            // 
            this.lblSubtitle.AutoSize = true;
            this.lblSubtitle.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblSubtitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(180)))), ((int)(((byte)(120)))));
            this.lblSubtitle.Location = new System.Drawing.Point(16, 44);
            this.lblSubtitle.Name = "lblSubtitle";
            this.lblSubtitle.Size = new System.Drawing.Size(380, 15);
            this.lblSubtitle.Text = "Liters × Rate • Profit • % • Average • Fast Calc — Enter se agla field";
            // 
            // btnClearAll
            // 
            this.btnClearAll.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            this.btnClearAll.BorderRadius = 8;
            this.btnClearAll.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(60)))), ((int)(((byte)(20)))));
            this.btnClearAll.Font = new System.Drawing.Font("Segoe UI Semibold", 9F, System.Drawing.FontStyle.Bold);
            this.btnClearAll.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(220)))), ((int)(((byte)(120)))));
            this.btnClearAll.Location = new System.Drawing.Point(520, 18);
            this.btnClearAll.Name = "btnClearAll";
            this.btnClearAll.Size = new System.Drawing.Size(90, 36);
            this.btnClearAll.Text = "Clear All";
            this.btnClearAll.Click += new System.EventHandler(this.btnClearAll_Click);
            // 
            // btnClose
            // 
            this.btnClose.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            this.btnClose.BorderRadius = 8;
            this.btnClose.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(140)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
            this.btnClose.Font = new System.Drawing.Font("Segoe UI Semibold", 9F, System.Drawing.FontStyle.Bold);
            this.btnClose.ForeColor = System.Drawing.Color.White;
            this.btnClose.Location = new System.Drawing.Point(618, 18);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(90, 36);
            this.btnClose.Text = "Close";
            this.btnClose.Click += new System.EventHandler(this.btnClose_Click);
            // 
            // tabMain
            // 
            this.tabMain.Controls.Add(this.tabSale);
            this.tabMain.Controls.Add(this.tabProfit);
            this.tabMain.Controls.Add(this.tabPercent);
            this.tabMain.Controls.Add(this.tabAverage);
            this.tabMain.Controls.Add(this.tabFast);
            this.tabMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabMain.Font = new System.Drawing.Font("Segoe UI Semibold", 10F, System.Drawing.FontStyle.Bold);
            this.tabMain.Location = new System.Drawing.Point(0, 72);
            this.tabMain.Name = "tabMain";
            this.tabMain.SelectedIndex = 0;
            this.tabMain.Size = new System.Drawing.Size(720, 508);
            this.tabMain.TabIndex = 1;
            // 
            // tabSale — Sale Bill
            // 
            this.tabSale.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(18)))), ((int)(((byte)(18)))), ((int)(((byte)(32)))));
            this.tabSale.Controls.Add(this.lblSaleHint);
            this.tabSale.Controls.Add(this.lblSaleBalance);
            this.tabSale.Controls.Add(this.lblSaleAmount);
            this.tabSale.Controls.Add(this.label4);
            this.tabSale.Controls.Add(this.label3);
            this.tabSale.Controls.Add(this.txtCredit);
            this.tabSale.Controls.Add(this.txtAdvance);
            this.tabSale.Controls.Add(this.txtRate);
            this.tabSale.Controls.Add(this.txtLiters);
            this.tabSale.Controls.Add(this.label2);
            this.tabSale.Controls.Add(this.label1);
            this.tabSale.Controls.Add(this.lblLiters);
            this.tabSale.Location = new System.Drawing.Point(4, 26);
            this.tabSale.Name = "tabSale";
            this.tabSale.Padding = new System.Windows.Forms.Padding(12);
            this.tabSale.Size = new System.Drawing.Size(712, 478);
            this.tabSale.TabIndex = 0;
            this.tabSale.Text = "  Sale Bill  ";
            // 
            StyleField(this.lblLiters, "Liters (L)", 24, 24);
            StyleField(this.label1, "Rate (Rs/L)", 24, 88);
            StyleField(this.label2, "Advance (Rs)", 24, 152);
            StyleField(this.label3, "Credit (Rs)", 380, 24);
            StyleField(this.label4, "Amount (Rs)", 380, 88);
            StyleInput(this.txtLiters, 24, 48, 320);
            StyleInput(this.txtRate, 24, 112, 320);
            StyleInput(this.txtAdvance, 24, 176, 320);
            StyleInput(this.txtCredit, 380, 48, 300);
            StyleResult(this.lblSaleAmount, 380, 112, 300, 40);
            this.lblSaleAmount.Text = "0.00";
            this.label4.Text = "Total Amount";
            var lblBalTitle = new System.Windows.Forms.Label();
            lblBalTitle.AutoSize = true;
            lblBalTitle.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            lblBalTitle.ForeColor = System.Drawing.Color.FromArgb(200, 180, 120);
            lblBalTitle.Location = new System.Drawing.Point(380, 168);
            lblBalTitle.Text = "Balance (Amount − Credit)";
            this.tabSale.Controls.Add(lblBalTitle);
            StyleResult(this.lblSaleBalance, 380, 192, 300, 48);
            this.lblSaleBalance.Font = new System.Drawing.Font("Segoe UI", 20F, System.Drawing.FontStyle.Bold);
            this.lblSaleBalance.ForeColor = System.Drawing.Color.FromArgb(100, 255, 160);
            this.lblSaleBalance.Text = "0.00";
            this.lblSaleHint.AutoSize = true;
            this.lblSaleHint.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.lblSaleHint.ForeColor = System.Drawing.Color.Silver;
            this.lblSaleHint.Location = new System.Drawing.Point(24, 240);
            this.lblSaleHint.Text = "Liters × Rate + Advance − Credit";
            // 
            // tabProfit
            // 
            this.tabProfit.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(18)))), ((int)(((byte)(18)))), ((int)(((byte)(32)))));
            this.tabProfit.Controls.Add(this.lblMarginPct);
            this.tabProfit.Controls.Add(this.lblProfitPct);
            this.tabProfit.Controls.Add(this.lblNetProfit);
            this.tabProfit.Controls.Add(this.lblGrossProfit);
            this.tabProfit.Controls.Add(this.lblRateDiff);
            this.tabProfit.Controls.Add(this.label10);
            this.tabProfit.Controls.Add(this.label9);
            this.tabProfit.Controls.Add(this.label8);
            this.tabProfit.Controls.Add(this.label7);
            this.tabProfit.Controls.Add(this.label6);
            this.tabProfit.Controls.Add(this.txtExpense);
            this.tabProfit.Controls.Add(this.txtProfitLiters);
            this.tabProfit.Controls.Add(this.txtPurchaseRate);
            this.tabProfit.Controls.Add(this.txtSaleRate);
            this.tabProfit.Controls.Add(this.label5);
            this.tabProfit.Location = new System.Drawing.Point(4, 26);
            this.tabProfit.Name = "tabProfit";
            this.tabProfit.Size = new System.Drawing.Size(712, 478);
            this.tabProfit.TabIndex = 1;
            this.tabProfit.Text = "  Profit  ";
            StyleField(this.label5, "Customer Sale Rate", 20, 20);
            StyleField(this.label6, "Dealer Purchase Rate", 20, 84);
            StyleField(this.label7, "Total Liters", 20, 148);
            StyleField(this.label8, "Expense (Rs)", 20, 212);
            StyleInput(this.txtSaleRate, 20, 44, 300);
            StyleInput(this.txtPurchaseRate, 20, 108, 300);
            StyleInput(this.txtProfitLiters, 20, 172, 300);
            StyleInput(this.txtExpense, 20, 236, 300);
            StyleField(this.label9, "Rate Difference", 380, 20);
            StyleField(this.label10, "Gross Profit", 380, 84);
            var lblNetT = new System.Windows.Forms.Label();
            lblNetT.AutoSize = true;
            lblNetT.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            lblNetT.ForeColor = System.Drawing.Color.FromArgb(200, 180, 120);
            lblNetT.Location = new System.Drawing.Point(380, 148);
            lblNetT.Text = "Net Profit";
            this.tabProfit.Controls.Add(lblNetT);
            StyleResult(this.lblRateDiff, 380, 44, 300, 32);
            StyleResult(this.lblGrossProfit, 380, 108, 300, 32);
            StyleResult(this.lblNetProfit, 380, 172, 300, 40);
            this.lblNetProfit.Font = new System.Drawing.Font("Segoe UI", 18F, System.Drawing.FontStyle.Bold);
            this.lblNetProfit.ForeColor = System.Drawing.Color.FromArgb(255, 215, 100);
            var lblPctT = new System.Windows.Forms.Label();
            lblPctT.AutoSize = true;
            lblPctT.ForeColor = System.Drawing.Color.FromArgb(200, 180, 120);
            lblPctT.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            lblPctT.Location = new System.Drawing.Point(380, 228);
            lblPctT.Text = "Profit %  |  Margin %";
            this.tabProfit.Controls.Add(lblPctT);
            this.lblProfitPct.AutoSize = true;
            this.lblProfitPct.Font = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Bold);
            this.lblProfitPct.ForeColor = System.Drawing.Color.FromArgb(100, 220, 255);
            this.lblProfitPct.Location = new System.Drawing.Point(380, 252);
            this.lblProfitPct.Text = "0.00 %";
            this.lblMarginPct.AutoSize = true;
            this.lblMarginPct.Font = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Bold);
            this.lblMarginPct.ForeColor = System.Drawing.Color.FromArgb(180, 140, 255);
            this.lblMarginPct.Location = new System.Drawing.Point(520, 252);
            this.lblMarginPct.Text = "0.00 %";
            this.lblRateDiff.Text = "0.00";
            this.lblGrossProfit.Text = "0.00";
            this.lblNetProfit.Text = "0.00";
            // 
            // tabPercent
            // 
            this.tabPercent.BackColor = System.Drawing.Color.FromArgb(18, 18, 32);
            this.tabPercent.Controls.Add(this.lblPctHint);
            this.tabPercent.Controls.Add(this.lblNetPct);
            this.tabPercent.Controls.Add(this.lblNetAmount);
            this.tabPercent.Controls.Add(this.label14);
            this.tabPercent.Controls.Add(this.label13);
            this.tabPercent.Controls.Add(this.txtCreditPct);
            this.tabPercent.Controls.Add(this.txtDebit);
            this.tabPercent.Controls.Add(this.label12);
            this.tabPercent.Controls.Add(this.label11);
            this.tabPercent.Location = new System.Drawing.Point(4, 26);
            this.tabPercent.Name = "tabPercent";
            this.tabPercent.Size = new System.Drawing.Size(712, 478);
            this.tabPercent.TabIndex = 2;
            this.tabPercent.Text = "  % Balance  ";
            StyleField(this.label11, "Total Debit / Sale (Rs)", 24, 24);
            StyleField(this.label12, "Total Credit / Payment (Rs)", 24, 88);
            StyleInput(this.txtDebit, 24, 48, 340);
            StyleInput(this.txtCreditPct, 24, 112, 340);
            StyleField(this.label13, "Net (Debit − Credit)", 400, 24);
            StyleField(this.label14, "Net % of Debit", 400, 120);
            StyleResult(this.lblNetAmount, 400, 48, 280, 48);
            this.lblNetAmount.Font = new System.Drawing.Font("Segoe UI", 20F, System.Drawing.FontStyle.Bold);
            this.lblNetAmount.ForeColor = System.Drawing.Color.FromArgb(255, 215, 100);
            StyleResult(this.lblNetPct, 400, 144, 280, 40);
            this.lblNetPct.Font = new System.Drawing.Font("Segoe UI", 18F, System.Drawing.FontStyle.Bold);
            this.lblNetPct.ForeColor = System.Drawing.Color.FromArgb(100, 220, 255);
            this.lblNetAmount.Text = "0.00";
            this.lblNetPct.Text = "0.00 %";
            this.lblPctHint.AutoSize = true;
            this.lblPctHint.ForeColor = System.Drawing.Color.Silver;
            this.lblPctHint.Location = new System.Drawing.Point(24, 180);
            this.lblPctHint.Text = "Receivable / Profit side";
            // 
            // tabAverage
            // 
            this.tabAverage.BackColor = System.Drawing.Color.FromArgb(18, 18, 32);
            this.tabAverage.Controls.Add(this.lblAvgCombined);
            this.tabAverage.Controls.Add(this.btnAvgClear);
            this.tabAverage.Controls.Add(this.btnAvgAdd);
            this.tabAverage.Controls.Add(this.dgvAvg);
            this.tabAverage.Controls.Add(this.lblAvgRateResult);
            this.tabAverage.Controls.Add(this.lblAvgAmount);
            this.tabAverage.Controls.Add(this.label18);
            this.tabAverage.Controls.Add(this.label17);
            this.tabAverage.Controls.Add(this.txtAvgRate);
            this.tabAverage.Controls.Add(this.txtAvgLiters);
            this.tabAverage.Controls.Add(this.label16);
            this.tabAverage.Controls.Add(this.label15);
            this.tabAverage.Location = new System.Drawing.Point(4, 26);
            this.tabAverage.Name = "tabAverage";
            this.tabAverage.Size = new System.Drawing.Size(712, 478);
            this.tabAverage.TabIndex = 3;
            this.tabAverage.Text = "  Average  ";
            StyleField(this.label15, "Liters", 16, 12);
            StyleField(this.label16, "Rate", 180, 12);
            StyleInput(this.txtAvgLiters, 16, 36, 150);
            StyleInput(this.txtAvgRate, 180, 36, 150);
            this.btnAvgAdd.BorderRadius = 8;
            this.btnAvgAdd.FillColor = System.Drawing.Color.FromArgb(180, 140, 40);
            this.btnAvgAdd.Font = new System.Drawing.Font("Segoe UI Semibold", 9F, System.Drawing.FontStyle.Bold);
            this.btnAvgAdd.ForeColor = System.Drawing.Color.White;
            this.btnAvgAdd.Location = new System.Drawing.Point(350, 34);
            this.btnAvgAdd.Size = new System.Drawing.Size(100, 32);
            this.btnAvgAdd.Text = "+ Add Row";
            this.btnAvgAdd.Click += new System.EventHandler(this.btnAvgAdd_Click);
            this.btnAvgClear.BorderRadius = 8;
            this.btnAvgClear.FillColor = System.Drawing.Color.FromArgb(80, 60, 20);
            this.btnAvgClear.ForeColor = System.Drawing.Color.FromArgb(255, 220, 120);
            this.btnAvgClear.Location = new System.Drawing.Point(460, 34);
            this.btnAvgClear.Size = new System.Drawing.Size(80, 32);
            this.btnAvgClear.Text = "Reset";
            this.btnAvgClear.Click += new System.EventHandler(this.btnAvgClear_Click);
            StyleField(this.label17, "Line Amount", 16, 78);
            StyleField(this.label18, "Avg Rate", 280, 78);
            StyleResult(this.lblAvgAmount, 16, 102, 240, 32);
            StyleResult(this.lblAvgRateResult, 280, 102, 200, 32);
            this.lblAvgAmount.Text = "0.00";
            this.lblAvgRateResult.Text = "0.00";
            this.dgvAvg.AllowUserToAddRows = false;
            this.dgvAvg.BackgroundColor = System.Drawing.Color.FromArgb(30, 30, 50);
            this.dgvAvg.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.dgvAvg.ColumnHeadersDefaultCellStyle.BackColor = System.Drawing.Color.FromArgb(50, 40, 20);
            this.dgvAvg.ColumnHeadersDefaultCellStyle.ForeColor = System.Drawing.Color.FromArgb(255, 215, 100);
            this.dgvAvg.DefaultCellStyle.BackColor = System.Drawing.Color.FromArgb(35, 35, 55);
            this.dgvAvg.DefaultCellStyle.ForeColor = System.Drawing.Color.White;
            this.dgvAvg.EnableHeadersVisualStyles = false;
            this.dgvAvg.Location = new System.Drawing.Point(16, 150);
            this.dgvAvg.Size = new System.Drawing.Size(680, 240);
            this.dgvAvg.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] { this.colLiters, this.colRate, this.colAmount });
            this.colLiters.HeaderText = "Liters";
            this.colLiters.Width = 180;
            this.colRate.HeaderText = "Rate";
            this.colRate.Width = 180;
            this.colAmount.HeaderText = "Amount";
            this.colAmount.Width = 200;
            this.lblAvgCombined.ForeColor = System.Drawing.Color.Silver;
            this.lblAvgCombined.Location = new System.Drawing.Point(16, 400);
            this.lblAvgCombined.Size = new System.Drawing.Size(680, 40);
            this.lblAvgCombined.Text = "Add rows for combined weighted average";
            // 
            // tabFast
            // 
            this.tabFast.BackColor = System.Drawing.Color.FromArgb(18, 18, 32);
            this.tabFast.Controls.Add(this.pnlKeypad);
            this.tabFast.Controls.Add(this.lblFastResult);
            this.tabFast.Controls.Add(this.btnFastLxR);
            this.tabFast.Controls.Add(this.btnFastEquals);
            this.tabFast.Controls.Add(this.btnFastClear);
            this.tabFast.Controls.Add(this.txtFastCalc);
            this.tabFast.Location = new System.Drawing.Point(4, 26);
            this.tabFast.Name = "tabFast";
            this.tabFast.Size = new System.Drawing.Size(712, 478);
            this.tabFast.TabIndex = 4;
            this.tabFast.Text = "  Fast Calc  ";
            this.txtFastCalc.Font = new System.Drawing.Font("Consolas", 16F);
            this.txtFastCalc.BackColor = System.Drawing.Color.FromArgb(30, 30, 50);
            this.txtFastCalc.ForeColor = System.Drawing.Color.FromArgb(255, 215, 100);
            this.txtFastCalc.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtFastCalc.Location = new System.Drawing.Point(16, 16);
            this.txtFastCalc.Size = new System.Drawing.Size(680, 32);
            this.txtFastCalc.KeyDown += new System.Windows.Forms.KeyEventHandler(this.txtFastCalc_KeyDown);
            this.lblFastResult.Font = new System.Drawing.Font("Segoe UI", 22F, System.Drawing.FontStyle.Bold);
            this.lblFastResult.ForeColor = System.Drawing.Color.FromArgb(100, 255, 160);
            this.lblFastResult.Location = new System.Drawing.Point(16, 56);
            this.lblFastResult.Size = new System.Drawing.Size(680, 40);
            this.lblFastResult.Text = "0.00";
            this.lblFastResult.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.btnFastLxR.BorderRadius = 8;
            this.btnFastLxR.FillColor = System.Drawing.Color.FromArgb(60, 100, 160);
            this.btnFastLxR.ForeColor = System.Drawing.Color.White;
            this.btnFastLxR.Location = new System.Drawing.Point(16, 104);
            this.btnFastLxR.Size = new System.Drawing.Size(100, 36);
            this.btnFastLxR.Text = "L × R";
            this.btnFastLxR.Click += new System.EventHandler(this.btnFastLxR_Click);
            this.btnFastEquals.BorderRadius = 8;
            this.btnFastEquals.FillColor = System.Drawing.Color.FromArgb(180, 140, 40);
            this.btnFastEquals.ForeColor = System.Drawing.Color.White;
            this.btnFastEquals.Location = new System.Drawing.Point(596, 104);
            this.btnFastEquals.Size = new System.Drawing.Size(100, 36);
            this.btnFastEquals.Text = "=";
            this.btnFastEquals.Click += new System.EventHandler(this.btnFastEquals_Click);
            this.btnFastClear.BorderRadius = 8;
            this.btnFastClear.FillColor = System.Drawing.Color.FromArgb(100, 40, 40);
            this.btnFastClear.ForeColor = System.Drawing.Color.White;
            this.btnFastClear.Location = new System.Drawing.Point(490, 104);
            this.btnFastClear.Size = new System.Drawing.Size(100, 36);
            this.btnFastClear.Text = "C";
            this.btnFastClear.Click += new System.EventHandler(this.btnFastClear_Click);
            this.pnlKeypad.Location = new System.Drawing.Point(16, 150);
            this.pnlKeypad.Size = new System.Drawing.Size(680, 310);
            BuildKeypad();
            // 
            // frmPetroleumCalculator
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(13)))), ((int)(((byte)(13)))), ((int)(((byte)(26)))));
            this.ClientSize = new System.Drawing.Size(720, 580);
            this.Controls.Add(this.tabMain);
            this.Controls.Add(this.pnlHeader);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "frmPetroleumCalculator";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "VIP Petroleum Calculator";
            this.Load += new System.EventHandler(this.frmPetroleumCalculator_Load);
            this.pnlHeader.ResumeLayout(false);
            this.pnlHeader.PerformLayout();
            this.tabMain.ResumeLayout(false);
            this.tabSale.ResumeLayout(false);
            this.tabSale.PerformLayout();
            this.tabProfit.ResumeLayout(false);
            this.tabProfit.PerformLayout();
            this.tabPercent.ResumeLayout(false);
            this.tabPercent.PerformLayout();
            this.tabAverage.ResumeLayout(false);
            this.tabAverage.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvAvg)).EndInit();
            this.tabFast.ResumeLayout(false);
            this.tabFast.PerformLayout();
            this.ResumeLayout(false);
        }

        private void StyleField(System.Windows.Forms.Label lbl, string text, int x, int y)
        {
            lbl.AutoSize = true;
            lbl.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            lbl.ForeColor = System.Drawing.Color.FromArgb(200, 180, 120);
            lbl.Location = new System.Drawing.Point(x, y);
            lbl.Text = text;
        }

        private void StyleInput(System.Windows.Forms.TextBox tb, int x, int y, int w)
        {
            tb.Font = new System.Drawing.Font("Segoe UI", 14F);
            tb.BackColor = System.Drawing.Color.FromArgb(35, 35, 55);
            tb.ForeColor = System.Drawing.Color.White;
            tb.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            tb.Location = new System.Drawing.Point(x, y);
            tb.Size = new System.Drawing.Size(w, 32);
        }

        private void StyleResult(System.Windows.Forms.Label lbl, int x, int y, int w, int h)
        {
            lbl.Font = new System.Drawing.Font("Segoe UI", 16F, System.Drawing.FontStyle.Bold);
            lbl.ForeColor = System.Drawing.Color.FromArgb(255, 215, 100);
            lbl.Location = new System.Drawing.Point(x, y);
            lbl.Size = new System.Drawing.Size(w, h);
            lbl.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        }

        private void BuildKeypad()
        {
            string[] keys = { "7", "8", "9", "/", "4", "5", "6", "*", "1", "2", "3", "-", "0", ".", "⌫", "+" };
            foreach (string k in keys)
            {
                var btn = new Guna.UI2.WinForms.Guna2Button();
                btn.BorderRadius = 8;
                btn.FillColor = System.Drawing.Color.FromArgb(45, 45, 70);
                btn.ForeColor = System.Drawing.Color.White;
                btn.Font = new System.Drawing.Font("Segoe UI Semibold", 14F, System.Drawing.FontStyle.Bold);
                btn.Size = new System.Drawing.Size(158, 52);
                btn.Margin = new System.Windows.Forms.Padding(4);
                btn.Text = k;
                btn.Tag = k;
                btn.Click += new System.EventHandler(this.FastKeypad_Click);
                this.pnlKeypad.Controls.Add(btn);
            }
        }

        private System.Windows.Forms.Panel pnlHeader;
        private System.Windows.Forms.Label lblHeader;
        private System.Windows.Forms.Label lblSubtitle;
        private Guna.UI2.WinForms.Guna2Button btnClearAll;
        private Guna.UI2.WinForms.Guna2Button btnClose;
        private System.Windows.Forms.TabControl tabMain;
        private System.Windows.Forms.TabPage tabSale;
        private System.Windows.Forms.TabPage tabProfit;
        private System.Windows.Forms.TabPage tabPercent;
        private System.Windows.Forms.TabPage tabAverage;
        private System.Windows.Forms.TabPage tabFast;
        private System.Windows.Forms.Label lblLiters;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox txtLiters;
        private System.Windows.Forms.TextBox txtRate;
        private System.Windows.Forms.TextBox txtAdvance;
        private System.Windows.Forms.TextBox txtCredit;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label lblSaleAmount;
        private System.Windows.Forms.Label lblSaleBalance;
        private System.Windows.Forms.Label lblSaleHint;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox txtSaleRate;
        private System.Windows.Forms.TextBox txtPurchaseRate;
        private System.Windows.Forms.TextBox txtProfitLiters;
        private System.Windows.Forms.TextBox txtExpense;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.Label lblRateDiff;
        private System.Windows.Forms.Label lblGrossProfit;
        private System.Windows.Forms.Label lblNetProfit;
        private System.Windows.Forms.Label lblProfitPct;
        private System.Windows.Forms.Label lblMarginPct;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.TextBox txtDebit;
        private System.Windows.Forms.TextBox txtCreditPct;
        private System.Windows.Forms.Label label13;
        private System.Windows.Forms.Label label14;
        private System.Windows.Forms.Label lblNetAmount;
        private System.Windows.Forms.Label lblNetPct;
        private System.Windows.Forms.Label lblPctHint;
        private System.Windows.Forms.Label label15;
        private System.Windows.Forms.Label label16;
        private System.Windows.Forms.TextBox txtAvgLiters;
        private System.Windows.Forms.TextBox txtAvgRate;
        private System.Windows.Forms.Label label17;
        private System.Windows.Forms.Label label18;
        private System.Windows.Forms.Label lblAvgAmount;
        private System.Windows.Forms.Label lblAvgRateResult;
        private System.Windows.Forms.DataGridView dgvAvg;
        private System.Windows.Forms.DataGridViewTextBoxColumn colLiters;
        private System.Windows.Forms.DataGridViewTextBoxColumn colRate;
        private System.Windows.Forms.DataGridViewTextBoxColumn colAmount;
        private Guna.UI2.WinForms.Guna2Button btnAvgAdd;
        private Guna.UI2.WinForms.Guna2Button btnAvgClear;
        private System.Windows.Forms.Label lblAvgCombined;
        private System.Windows.Forms.TextBox txtFastCalc;
        private System.Windows.Forms.Label lblFastResult;
        private Guna.UI2.WinForms.Guna2Button btnFastClear;
        private Guna.UI2.WinForms.Guna2Button btnFastEquals;
        private Guna.UI2.WinForms.Guna2Button btnFastLxR;
        private System.Windows.Forms.FlowLayoutPanel pnlKeypad;
    }
}
