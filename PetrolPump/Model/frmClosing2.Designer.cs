namespace AlMadinaShahzad.Model
{
    partial class frmClosing2
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
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle3 = new System.Windows.Forms.DataGridViewCellStyle();
            this.dtpStart = new Guna.UI2.WinForms.Guna2DateTimePicker();
            this.dtpEnd = new Guna.UI2.WinForms.Guna2DateTimePicker();
            this.labelFrom = new System.Windows.Forms.Label();
            this.labelTo = new System.Windows.Forms.Label();
            this.btnLoad = new Guna.UI2.WinForms.Guna2Button();
            this.btnExcel = new Guna.UI2.WinForms.Guna2Button();
            this.lblCapTempEntry = new System.Windows.Forms.Label();
            this.txtTempName = new System.Windows.Forms.TextBox();
            this.cbTempMode = new System.Windows.Forms.ComboBox();
            this.txtTempAmount = new System.Windows.Forms.TextBox();
            this.btnAddTemp = new Guna.UI2.WinForms.Guna2Button();
            this.btnClearTemp = new Guna.UI2.WinForms.Guna2Button();
            this.lblCustomerSale = new System.Windows.Forms.Label();
            this.lblDealerPurchase = new System.Windows.Forms.Label();
            this.lblMarginPerLiter = new System.Windows.Forms.Label();
            this.lblTotalLiters = new System.Windows.Forms.Label();
            this.lblGrossProfit = new System.Windows.Forms.Label();
            this.lblExpenseTotal = new System.Windows.Forms.Label();
            this.lblNetProfit = new System.Windows.Forms.Label();
            this.lblCapSale = new System.Windows.Forms.Label();
            this.lblCapDealer = new System.Windows.Forms.Label();
            this.lblCapMargin = new System.Windows.Forms.Label();
            this.lblCapLiter = new System.Windows.Forms.Label();
            this.lblCapGross = new System.Windows.Forms.Label();
            this.lblCapExpense = new System.Windows.Forms.Label();
            this.lblCapNet = new System.Windows.Forms.Label();
            this.lblCustomerResult = new System.Windows.Forms.Label();
            this.lblDealerResult = new System.Windows.Forms.Label();
            this.lblCustomerTotal = new System.Windows.Forms.Label();
            this.lblDealerTotal = new System.Windows.Forms.Label();
            this.lblNetBalance = new System.Windows.Forms.Label();
            this.lblCapCustTotal = new System.Windows.Forms.Label();
            this.lblCapDealerTotal = new System.Windows.Forms.Label();
            this.lblCapBalance = new System.Windows.Forms.Label();
            this.guna2DataGridView1 = new Guna.UI2.WinForms.Guna2DataGridView();
            this.colSr = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dgvRecived = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colGap = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dgvPayable = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dgvNetProfit = new System.Windows.Forms.DataGridViewTextBoxColumn();
            ((System.ComponentModel.ISupportInitialize)(this.guna2DataGridView1)).BeginInit();
            this.SuspendLayout();
            // 
            // dtpStart
            // 
            this.dtpStart.AutoRoundedCorners = true;
            this.dtpStart.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(112)))), ((int)(((byte)(51)))), ((int)(((byte)(255)))));
            this.dtpStart.BorderRadius = 14;
            this.dtpStart.BorderThickness = 1;
            this.dtpStart.CheckedState.Parent = this.dtpStart;
            this.dtpStart.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(37)))), ((int)(((byte)(41)))), ((int)(((byte)(74)))));
            this.dtpStart.ForeColor = System.Drawing.Color.White;
            this.dtpStart.Format = System.Windows.Forms.DateTimePickerFormat.Long;
            this.dtpStart.Location = new System.Drawing.Point(70, 18);
            this.dtpStart.MaxDate = new System.DateTime(9998, 12, 31, 0, 0, 0, 0);
            this.dtpStart.MinDate = new System.DateTime(1753, 1, 1, 0, 0, 0, 0);
            this.dtpStart.Name = "dtpStart";
            this.dtpStart.Size = new System.Drawing.Size(200, 31);
            this.dtpStart.TabIndex = 0;
            this.dtpStart.Value = new System.DateTime(2024, 6, 21, 0, 0, 0, 0);
            // 
            // dtpEnd
            // 
            this.dtpEnd.AutoRoundedCorners = true;
            this.dtpEnd.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(112)))), ((int)(((byte)(51)))), ((int)(((byte)(255)))));
            this.dtpEnd.BorderRadius = 14;
            this.dtpEnd.BorderThickness = 1;
            this.dtpEnd.CheckedState.Parent = this.dtpEnd;
            this.dtpEnd.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(37)))), ((int)(((byte)(41)))), ((int)(((byte)(74)))));
            this.dtpEnd.ForeColor = System.Drawing.Color.White;
            this.dtpEnd.Format = System.Windows.Forms.DateTimePickerFormat.Long;
            this.dtpEnd.Location = new System.Drawing.Point(313, 18);
            this.dtpEnd.MaxDate = new System.DateTime(9998, 12, 31, 0, 0, 0, 0);
            this.dtpEnd.MinDate = new System.DateTime(1753, 1, 1, 0, 0, 0, 0);
            this.dtpEnd.Name = "dtpEnd";
            this.dtpEnd.Size = new System.Drawing.Size(200, 31);
            this.dtpEnd.TabIndex = 1;
            this.dtpEnd.Value = new System.DateTime(2024, 6, 21, 0, 0, 0, 0);
            // 
            // labelFrom
            // 
            this.labelFrom.AutoSize = true;
            this.labelFrom.ForeColor = System.Drawing.Color.White;
            this.labelFrom.Location = new System.Drawing.Point(22, 26);
            this.labelFrom.Name = "labelFrom";
            this.labelFrom.Size = new System.Drawing.Size(30, 13);
            this.labelFrom.TabIndex = 2;
            this.labelFrom.Text = "From";
            // 
            // labelTo
            // 
            this.labelTo.AutoSize = true;
            this.labelTo.ForeColor = System.Drawing.Color.White;
            this.labelTo.Location = new System.Drawing.Point(286, 26);
            this.labelTo.Name = "labelTo";
            this.labelTo.Size = new System.Drawing.Size(20, 13);
            this.labelTo.TabIndex = 3;
            this.labelTo.Text = "To";
            // 
            // btnLoad
            // 
            this.btnLoad.AutoRoundedCorners = true;
            this.btnLoad.BorderRadius = 15;
            this.btnLoad.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(112)))), ((int)(((byte)(51)))), ((int)(((byte)(255)))));
            this.btnLoad.Font = new System.Drawing.Font("Segoe UI Semibold", 12F, System.Drawing.FontStyle.Bold);
            this.btnLoad.ForeColor = System.Drawing.Color.White;
            this.btnLoad.Location = new System.Drawing.Point(530, 16);
            this.btnLoad.Name = "btnLoad";
            this.btnLoad.Size = new System.Drawing.Size(95, 33);
            this.btnLoad.TabIndex = 4;
            this.btnLoad.Text = "Load";
            this.btnLoad.Click += new System.EventHandler(this.btnLoad_Click);
            // 
            // btnExcel
            // 
            this.btnExcel.AutoRoundedCorners = true;
            this.btnExcel.BorderRadius = 15;
            this.btnExcel.FillColor = System.Drawing.Color.Crimson;
            this.btnExcel.Font = new System.Drawing.Font("Segoe UI Semibold", 12F, System.Drawing.FontStyle.Bold);
            this.btnExcel.ForeColor = System.Drawing.Color.White;
            this.btnExcel.Location = new System.Drawing.Point(631, 16);
            this.btnExcel.Name = "btnExcel";
            this.btnExcel.Size = new System.Drawing.Size(95, 33);
            this.btnExcel.TabIndex = 5;
            this.btnExcel.Text = "Excel";
            this.btnExcel.Click += new System.EventHandler(this.btnExcel_Click);
            // 
            // captions and values row 1
            // 
            this.lblCapSale.AutoSize = true;
            this.lblCapSale.Font = new System.Drawing.Font("Segoe UI Semibold", 9.75F, System.Drawing.FontStyle.Bold);
            this.lblCapSale.ForeColor = System.Drawing.Color.Silver;
            this.lblCapSale.Location = new System.Drawing.Point(22, 62);
            this.lblCapSale.Text = "Sale Avg (Customer)";
            this.lblCapSale.Size = new System.Drawing.Size(130, 17);

            this.lblCustomerSale.AutoSize = true;
            this.lblCustomerSale.Font = new System.Drawing.Font("Segoe UI Semibold", 14F, System.Drawing.FontStyle.Bold);
            this.lblCustomerSale.ForeColor = System.Drawing.Color.White;
            this.lblCustomerSale.Location = new System.Drawing.Point(22, 82);
            this.lblCustomerSale.Text = "0.000";

            this.lblCapDealer.AutoSize = true;
            this.lblCapDealer.Font = new System.Drawing.Font("Segoe UI Semibold", 9.75F, System.Drawing.FontStyle.Bold);
            this.lblCapDealer.ForeColor = System.Drawing.Color.Silver;
            this.lblCapDealer.Location = new System.Drawing.Point(170, 62);
            this.lblCapDealer.Text = "Dealer Avg";

            this.lblDealerPurchase.AutoSize = true;
            this.lblDealerPurchase.Font = new System.Drawing.Font("Segoe UI Semibold", 14F, System.Drawing.FontStyle.Bold);
            this.lblDealerPurchase.ForeColor = System.Drawing.Color.White;
            this.lblDealerPurchase.Location = new System.Drawing.Point(170, 82);
            this.lblDealerPurchase.Text = "0.000";

            this.lblCapMargin.AutoSize = true;
            this.lblCapMargin.Font = new System.Drawing.Font("Segoe UI Semibold", 9.75F, System.Drawing.FontStyle.Bold);
            this.lblCapMargin.ForeColor = System.Drawing.Color.Silver;
            this.lblCapMargin.Location = new System.Drawing.Point(318, 62);
            this.lblCapMargin.Text = "Margin / L";

            this.lblMarginPerLiter.AutoSize = true;
            this.lblMarginPerLiter.Font = new System.Drawing.Font("Segoe UI Semibold", 14F, System.Drawing.FontStyle.Bold);
            this.lblMarginPerLiter.ForeColor = System.Drawing.Color.LimeGreen;
            this.lblMarginPerLiter.Location = new System.Drawing.Point(318, 82);
            this.lblMarginPerLiter.Text = "0.000";

            this.lblCapLiter.AutoSize = true;
            this.lblCapLiter.Font = new System.Drawing.Font("Segoe UI Semibold", 9.75F, System.Drawing.FontStyle.Bold);
            this.lblCapLiter.ForeColor = System.Drawing.Color.Silver;
            this.lblCapLiter.Location = new System.Drawing.Point(466, 62);
            this.lblCapLiter.Text = "Liters Sold";

            this.lblTotalLiters.AutoSize = true;
            this.lblTotalLiters.Font = new System.Drawing.Font("Segoe UI Semibold", 14F, System.Drawing.FontStyle.Bold);
            this.lblTotalLiters.ForeColor = System.Drawing.Color.White;
            this.lblTotalLiters.Location = new System.Drawing.Point(466, 82);
            this.lblTotalLiters.Text = "0.000";

            this.lblCapGross.AutoSize = true;
            this.lblCapGross.Font = new System.Drawing.Font("Segoe UI Semibold", 9.75F, System.Drawing.FontStyle.Bold);
            this.lblCapGross.ForeColor = System.Drawing.Color.Silver;
            this.lblCapGross.Location = new System.Drawing.Point(614, 62);
            this.lblCapGross.Text = "Gross (L × Margin)";

            this.lblGrossProfit.AutoSize = true;
            this.lblGrossProfit.Font = new System.Drawing.Font("Segoe UI Semibold", 14F, System.Drawing.FontStyle.Bold);
            this.lblGrossProfit.ForeColor = System.Drawing.Color.White;
            this.lblGrossProfit.Location = new System.Drawing.Point(614, 82);
            this.lblGrossProfit.Text = "0.00";

            this.lblCapExpense.AutoSize = true;
            this.lblCapExpense.Font = new System.Drawing.Font("Segoe UI Semibold", 9.75F, System.Drawing.FontStyle.Bold);
            this.lblCapExpense.ForeColor = System.Drawing.Color.Silver;
            this.lblCapExpense.Location = new System.Drawing.Point(820, 62);
            this.lblCapExpense.Text = "Expense";

            this.lblExpenseTotal.AutoSize = true;
            this.lblExpenseTotal.Font = new System.Drawing.Font("Segoe UI Semibold", 14F, System.Drawing.FontStyle.Bold);
            this.lblExpenseTotal.ForeColor = System.Drawing.Color.Orange;
            this.lblExpenseTotal.Location = new System.Drawing.Point(820, 82);
            this.lblExpenseTotal.Text = "0.00";

            this.lblCapNet.AutoSize = true;
            this.lblCapNet.Font = new System.Drawing.Font("Segoe UI Semibold", 9.75F, System.Drawing.FontStyle.Bold);
            this.lblCapNet.ForeColor = System.Drawing.Color.Silver;
            this.lblCapNet.Location = new System.Drawing.Point(1000, 62);
            this.lblCapNet.Text = "Net Profit";

            this.lblNetProfit.AutoSize = true;
            this.lblNetProfit.Font = new System.Drawing.Font("Segoe UI Semibold", 16F, System.Drawing.FontStyle.Bold);
            this.lblNetProfit.ForeColor = System.Drawing.Color.Gold;
            this.lblNetProfit.Location = new System.Drawing.Point(1000, 78);
            this.lblNetProfit.Text = "0.00";
            // 
            // lblCustomerResult / lblDealerResult
            // 
            this.lblCustomerResult.AutoSize = true;
            this.lblCustomerResult.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.lblCustomerResult.ForeColor = System.Drawing.Color.White;
            this.lblCustomerResult.Location = new System.Drawing.Point(22, 115);
            this.lblCustomerResult.MaximumSize = new System.Drawing.Size(650, 0);
            this.lblCustomerResult.Text = "Customer Average";

            this.lblDealerResult.AutoSize = true;
            this.lblDealerResult.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.lblDealerResult.ForeColor = System.Drawing.Color.White;
            this.lblDealerResult.Location = new System.Drawing.Point(22, 138);
            this.lblDealerResult.MaximumSize = new System.Drawing.Size(1200, 0);
            this.lblDealerResult.Text = "Dealer Average";
            // 
            // totals before grid
            // 
            this.lblCapCustTotal.AutoSize = true;
            this.lblCapCustTotal.Font = new System.Drawing.Font("Segoe UI Semibold", 10F, System.Drawing.FontStyle.Bold);
            this.lblCapCustTotal.ForeColor = System.Drawing.Color.Silver;
            this.lblCapCustTotal.Location = new System.Drawing.Point(22, 168);
            this.lblCapCustTotal.Text = "Customer Total";

            this.lblCustomerTotal.AutoSize = true;
            this.lblCustomerTotal.Font = new System.Drawing.Font("Segoe UI Semibold", 12F, System.Drawing.FontStyle.Bold);
            this.lblCustomerTotal.ForeColor = System.Drawing.Color.White;
            this.lblCustomerTotal.Location = new System.Drawing.Point(22, 188);
            this.lblCustomerTotal.Text = "0.00";

            this.lblCapDealerTotal.AutoSize = true;
            this.lblCapDealerTotal.Font = new System.Drawing.Font("Segoe UI Semibold", 10F, System.Drawing.FontStyle.Bold);
            this.lblCapDealerTotal.ForeColor = System.Drawing.Color.Silver;
            this.lblCapDealerTotal.Location = new System.Drawing.Point(450, 168);
            this.lblCapDealerTotal.Text = "Dealer Total";

            this.lblDealerTotal.AutoSize = true;
            this.lblDealerTotal.Font = new System.Drawing.Font("Segoe UI Semibold", 12F, System.Drawing.FontStyle.Bold);
            this.lblDealerTotal.ForeColor = System.Drawing.Color.White;
            this.lblDealerTotal.Location = new System.Drawing.Point(450, 188);
            this.lblDealerTotal.Text = "0.00";

            this.lblCapBalance.AutoSize = true;
            this.lblCapBalance.Font = new System.Drawing.Font("Segoe UI Semibold", 10F, System.Drawing.FontStyle.Bold);
            this.lblCapBalance.ForeColor = System.Drawing.Color.Silver;
            this.lblCapBalance.Location = new System.Drawing.Point(900, 168);
            this.lblCapBalance.Text = "Balance";

            this.lblNetBalance.AutoSize = true;
            this.lblNetBalance.Font = new System.Drawing.Font("Segoe UI Semibold", 12F, System.Drawing.FontStyle.Bold);
            this.lblNetBalance.ForeColor = System.Drawing.Color.White;
            this.lblNetBalance.Location = new System.Drawing.Point(900, 188);
            this.lblNetBalance.Text = "0.00";
            // 
            // lblCapTempEntry
            // 
            this.lblCapTempEntry.AutoSize = true;
            this.lblCapTempEntry.Font = new System.Drawing.Font("Segoe UI Semibold", 9F, System.Drawing.FontStyle.Bold);
            this.lblCapTempEntry.ForeColor = System.Drawing.Color.Silver;
            this.lblCapTempEntry.Location = new System.Drawing.Point(18, 218);
            this.lblCapTempEntry.Text = "Temp Entry:";
            // 
            // txtTempName
            // 
            this.txtTempName.BackColor = System.Drawing.Color.FromArgb(37, 41, 74);
            this.txtTempName.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtTempName.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.txtTempName.ForeColor = System.Drawing.Color.White;
            this.txtTempName.Location = new System.Drawing.Point(105, 214);
            this.txtTempName.Name = "txtTempName";
            this.txtTempName.Size = new System.Drawing.Size(175, 26);
            this.txtTempName.TabIndex = 20;
            // 
            // cbTempMode
            // 
            this.cbTempMode.BackColor = System.Drawing.Color.FromArgb(37, 41, 74);
            this.cbTempMode.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbTempMode.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.cbTempMode.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.cbTempMode.ForeColor = System.Drawing.Color.White;
            this.cbTempMode.Location = new System.Drawing.Point(290, 214);
            this.cbTempMode.Name = "cbTempMode";
            this.cbTempMode.Size = new System.Drawing.Size(130, 26);
            this.cbTempMode.TabIndex = 21;
            // 
            // txtTempAmount
            // 
            this.txtTempAmount.BackColor = System.Drawing.Color.FromArgb(37, 41, 74);
            this.txtTempAmount.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtTempAmount.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.txtTempAmount.ForeColor = System.Drawing.Color.White;
            this.txtTempAmount.Location = new System.Drawing.Point(430, 214);
            this.txtTempAmount.Name = "txtTempAmount";
            this.txtTempAmount.Size = new System.Drawing.Size(120, 26);
            this.txtTempAmount.TabIndex = 22;
            // 
            // btnAddTemp
            // 
            this.btnAddTemp.AutoRoundedCorners = true;
            this.btnAddTemp.BorderRadius = 12;
            this.btnAddTemp.FillColor = System.Drawing.Color.FromArgb(0, 150, 136);
            this.btnAddTemp.Font = new System.Drawing.Font("Segoe UI Semibold", 10F, System.Drawing.FontStyle.Bold);
            this.btnAddTemp.ForeColor = System.Drawing.Color.White;
            this.btnAddTemp.Location = new System.Drawing.Point(560, 212);
            this.btnAddTemp.Name = "btnAddTemp";
            this.btnAddTemp.Size = new System.Drawing.Size(70, 30);
            this.btnAddTemp.TabIndex = 23;
            this.btnAddTemp.Text = "Add";
            this.btnAddTemp.Click += new System.EventHandler(this.btnAddTemp_Click);
            // 
            // btnClearTemp
            // 
            this.btnClearTemp.AutoRoundedCorners = true;
            this.btnClearTemp.BorderRadius = 12;
            this.btnClearTemp.FillColor = System.Drawing.Color.FromArgb(80, 80, 100);
            this.btnClearTemp.Font = new System.Drawing.Font("Segoe UI Semibold", 10F, System.Drawing.FontStyle.Bold);
            this.btnClearTemp.ForeColor = System.Drawing.Color.White;
            this.btnClearTemp.Location = new System.Drawing.Point(640, 212);
            this.btnClearTemp.Name = "btnClearTemp";
            this.btnClearTemp.Size = new System.Drawing.Size(70, 30);
            this.btnClearTemp.TabIndex = 24;
            this.btnClearTemp.Text = "Clear";
            this.btnClearTemp.Click += new System.EventHandler(this.btnClearTemp_Click);
            // 
            // guna2DataGridView1
            // 
            this.guna2DataGridView1.AllowUserToAddRows = false;
            this.guna2DataGridView1.AllowUserToDeleteRows = false;
            this.guna2DataGridView1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.guna2DataGridView1.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            dataGridViewCellStyle1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(37)))), ((int)(((byte)(41)))), ((int)(((byte)(74)))));
            this.guna2DataGridView1.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle1;
            this.guna2DataGridView1.BackgroundColor = System.Drawing.Color.FromArgb(((int)(((byte)(32)))), ((int)(((byte)(36)))), ((int)(((byte)(66)))));
            this.guna2DataGridView1.BorderStyle = System.Windows.Forms.BorderStyle.None;
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle2.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(88)))), ((int)(((byte)(255)))));
            dataGridViewCellStyle2.ForeColor = System.Drawing.Color.White;
            dataGridViewCellStyle2.Font = new System.Drawing.Font("Segoe UI Semibold", 11.25F, System.Drawing.FontStyle.Bold);
            this.guna2DataGridView1.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle2;
            this.guna2DataGridView1.ColumnHeadersHeight = 40;
            this.guna2DataGridView1.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colSr, this.dgvRecived, this.colGap, this.dgvPayable, this.dgvNetProfit });
            dataGridViewCellStyle3.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(32)))), ((int)(((byte)(36)))), ((int)(((byte)(66)))));
            dataGridViewCellStyle3.ForeColor = System.Drawing.Color.White;
            dataGridViewCellStyle3.Font = new System.Drawing.Font("Segoe UI Semibold", 11.25F, System.Drawing.FontStyle.Bold);
            this.guna2DataGridView1.DefaultCellStyle = dataGridViewCellStyle3;
            this.guna2DataGridView1.EnableHeadersVisualStyles = false;
            this.guna2DataGridView1.GridColor = System.Drawing.Color.FromArgb(((int)(((byte)(32)))), ((int)(((byte)(36)))), ((int)(((byte)(66)))));
            this.guna2DataGridView1.Location = new System.Drawing.Point(18, 258);
            this.guna2DataGridView1.Name = "guna2DataGridView1";
            this.guna2DataGridView1.ReadOnly = true;
            this.guna2DataGridView1.RowHeadersVisible = false;
            this.guna2DataGridView1.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.guna2DataGridView1.Size = new System.Drawing.Size(1350, 512);
            this.guna2DataGridView1.TabIndex = 6;
            // 
            // columns
            // 
            this.colSr.HeaderText = "Sr#";
            this.colSr.Name = "colSr";
            this.colSr.ReadOnly = true;
            this.colSr.Width = 50;
            this.dgvRecived.HeaderText = "Customer (Receivable)";
            this.dgvRecived.Name = "dgvRecived";
            this.dgvRecived.ReadOnly = true;
            this.colGap.HeaderText = "";
            this.colGap.Name = "colGap";
            this.colGap.ReadOnly = true;
            this.colGap.Width = 40;
            this.dgvPayable.HeaderText = "Dealer (Payable)";
            this.dgvPayable.Name = "dgvPayable";
            this.dgvPayable.ReadOnly = true;
            this.dgvNetProfit.HeaderText = "Profit / Loss";
            this.dgvNetProfit.Name = "dgvNetProfit";
            this.dgvNetProfit.ReadOnly = true;
            // 
            // frmClosing2
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(32)))), ((int)(((byte)(36)))), ((int)(((byte)(61)))));
            this.ClientSize = new System.Drawing.Size(1386, 788);
            this.Controls.Add(this.guna2DataGridView1);
            this.Controls.Add(this.btnClearTemp);
            this.Controls.Add(this.btnAddTemp);
            this.Controls.Add(this.txtTempAmount);
            this.Controls.Add(this.cbTempMode);
            this.Controls.Add(this.txtTempName);
            this.Controls.Add(this.lblCapTempEntry);
            this.Controls.Add(this.lblNetBalance);
            this.Controls.Add(this.lblCapBalance);
            this.Controls.Add(this.lblDealerTotal);
            this.Controls.Add(this.lblCapDealerTotal);
            this.Controls.Add(this.lblCustomerTotal);
            this.Controls.Add(this.lblCapCustTotal);
            this.Controls.Add(this.lblDealerResult);
            this.Controls.Add(this.lblCustomerResult);
            this.Controls.Add(this.lblNetProfit);
            this.Controls.Add(this.lblCapNet);
            this.Controls.Add(this.lblExpenseTotal);
            this.Controls.Add(this.lblCapExpense);
            this.Controls.Add(this.lblGrossProfit);
            this.Controls.Add(this.lblCapGross);
            this.Controls.Add(this.lblTotalLiters);
            this.Controls.Add(this.lblCapLiter);
            this.Controls.Add(this.lblMarginPerLiter);
            this.Controls.Add(this.lblCapMargin);
            this.Controls.Add(this.lblDealerPurchase);
            this.Controls.Add(this.lblCapDealer);
            this.Controls.Add(this.lblCustomerSale);
            this.Controls.Add(this.lblCapSale);
            this.Controls.Add(this.btnExcel);
            this.Controls.Add(this.btnLoad);
            this.Controls.Add(this.labelTo);
            this.Controls.Add(this.labelFrom);
            this.Controls.Add(this.dtpEnd);
            this.Controls.Add(this.dtpStart);
            this.Name = "frmClosing2";
            this.Text = "Closing 2 - Profit";
            this.Load += new System.EventHandler(this.frmClosing2_Load);
            ((System.ComponentModel.ISupportInitialize)(this.guna2DataGridView1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private Guna.UI2.WinForms.Guna2DateTimePicker dtpStart;
        private Guna.UI2.WinForms.Guna2DateTimePicker dtpEnd;
        private System.Windows.Forms.Label labelFrom;
        private System.Windows.Forms.Label labelTo;
        private Guna.UI2.WinForms.Guna2Button btnLoad;
        private Guna.UI2.WinForms.Guna2Button btnExcel;
        private System.Windows.Forms.Label lblCustomerSale;
        private System.Windows.Forms.Label lblDealerPurchase;
        private System.Windows.Forms.Label lblMarginPerLiter;
        private System.Windows.Forms.Label lblTotalLiters;
        private System.Windows.Forms.Label lblGrossProfit;
        private System.Windows.Forms.Label lblExpenseTotal;
        private System.Windows.Forms.Label lblNetProfit;
        private System.Windows.Forms.Label lblCapSale;
        private System.Windows.Forms.Label lblCapDealer;
        private System.Windows.Forms.Label lblCapMargin;
        private System.Windows.Forms.Label lblCapLiter;
        private System.Windows.Forms.Label lblCapGross;
        private System.Windows.Forms.Label lblCapExpense;
        private System.Windows.Forms.Label lblCapNet;
        private System.Windows.Forms.Label lblCustomerResult;
        private System.Windows.Forms.Label lblDealerResult;
        private System.Windows.Forms.Label lblCustomerTotal;
        private System.Windows.Forms.Label lblDealerTotal;
        private System.Windows.Forms.Label lblNetBalance;
        private System.Windows.Forms.Label lblCapCustTotal;
        private System.Windows.Forms.Label lblCapDealerTotal;
        private System.Windows.Forms.Label lblCapBalance;
        private Guna.UI2.WinForms.Guna2DataGridView guna2DataGridView1;
        private System.Windows.Forms.DataGridViewTextBoxColumn colSr;
        private System.Windows.Forms.DataGridViewTextBoxColumn dgvRecived;
        private System.Windows.Forms.DataGridViewTextBoxColumn colGap;
        private System.Windows.Forms.DataGridViewTextBoxColumn dgvPayable;
        private System.Windows.Forms.DataGridViewTextBoxColumn dgvNetProfit;
        private System.Windows.Forms.Label lblCapTempEntry;
        private System.Windows.Forms.TextBox txtTempName;
        private System.Windows.Forms.ComboBox cbTempMode;
        private System.Windows.Forms.TextBox txtTempAmount;
        private Guna.UI2.WinForms.Guna2Button btnAddTemp;
        private Guna.UI2.WinForms.Guna2Button btnClearTemp;
    }
}
