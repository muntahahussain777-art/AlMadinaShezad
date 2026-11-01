namespace AlMadinaShahzad.View
{
    partial class frmDashBoard
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.Windows.Forms.DataVisualization.Charting.ChartArea chartArea1 = new System.Windows.Forms.DataVisualization.Charting.ChartArea();
            System.Windows.Forms.DataVisualization.Charting.Legend legend1 = new System.Windows.Forms.DataVisualization.Charting.Legend();
            System.Windows.Forms.DataVisualization.Charting.Series series1 = new System.Windows.Forms.DataVisualization.Charting.Series();
            System.Windows.Forms.DataVisualization.Charting.Series series2 = new System.Windows.Forms.DataVisualization.Charting.Series();
            System.Windows.Forms.DataVisualization.Charting.Series series3 = new System.Windows.Forms.DataVisualization.Charting.Series();
            this.label7 = new System.Windows.Forms.Label();
            this.panel5 = new System.Windows.Forms.Panel();
            this.supplierChart = new System.Windows.Forms.DataVisualization.Charting.Chart();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.lblExpenseTotal = new System.Windows.Forms.Label();
            this.lblblbl = new System.Windows.Forms.Label();
            this.txtDealerBalance = new System.Windows.Forms.Label();
            this.txtdealerCredit = new System.Windows.Forms.Label();
            this.label12 = new System.Windows.Forms.Label();
            this.BalanceLbl1 = new System.Windows.Forms.Label();
            this.label10 = new System.Windows.Forms.Label();
            this.txtdealeramount = new System.Windows.Forms.Label();
            this.lblTotalDiesel = new System.Windows.Forms.Label();
            this.label9 = new System.Windows.Forms.Label();
            this.label11 = new System.Windows.Forms.Label();
            this.label14 = new System.Windows.Forms.Label();
            this.lblTotalPetrol = new System.Windows.Forms.Label();
            this.label13 = new System.Windows.Forms.Label();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.txtAmount = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.txtlitter = new System.Windows.Forms.Label();
            this.label15 = new System.Windows.Forms.Label();
            this.txtadvance = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.txtbalance = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.txtcredit = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.txtdebit = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.comboYear = new Guna.UI2.WinForms.Guna2ComboBox();
            this.panel5.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.supplierChart)).BeginInit();
            this.groupBox3.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.SuspendLayout();
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Font = new System.Drawing.Font("Segoe UI Black", 18F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label7.ForeColor = System.Drawing.Color.White;
            this.label7.Location = new System.Drawing.Point(484, 9);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(77, 32);
            this.label7.TabIndex = 10;
            this.label7.Text = "Chart";
            // 
            // panel5
            // 
            this.panel5.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel5.Controls.Add(this.supplierChart);
            this.panel5.Location = new System.Drawing.Point(12, 44);
            this.panel5.Name = "panel5";
            this.panel5.Size = new System.Drawing.Size(1139, 290);
            this.panel5.TabIndex = 9;
            // 
            // supplierChart
            // 
            this.supplierChart.BorderlineColor = System.Drawing.SystemColors.Window;
            chartArea1.Name = "ChartArea1";
            this.supplierChart.ChartAreas.Add(chartArea1);
            this.supplierChart.Dock = System.Windows.Forms.DockStyle.Fill;
            legend1.ForeColor = System.Drawing.Color.White;
            legend1.HeaderSeparatorColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            legend1.ItemColumnSeparatorColor = System.Drawing.Color.Silver;
            legend1.Name = "Legend1";
            this.supplierChart.Legends.Add(legend1);
            this.supplierChart.Location = new System.Drawing.Point(0, 0);
            this.supplierChart.Name = "supplierChart";
            series1.ChartArea = "ChartArea1";
            series1.Legend = "Legend1";
            series1.Name = "SoldLitter";
            series2.ChartArea = "ChartArea1";
            series2.Legend = "Legend1";
            series2.Name = "Cash";
            series3.ChartArea = "ChartArea1";
            series3.Legend = "Legend1";
            series3.Name = "Ledger";
            this.supplierChart.Series.Add(series1);
            this.supplierChart.Series.Add(series2);
            this.supplierChart.Series.Add(series3);
            this.supplierChart.Size = new System.Drawing.Size(1137, 288);
            this.supplierChart.TabIndex = 0;
            this.supplierChart.Text = "chart1";
            this.supplierChart.MouseDown += new System.Windows.Forms.MouseEventHandler(this.SupplierChart_MouseClick);
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.lblExpenseTotal);
            this.groupBox3.Controls.Add(this.lblblbl);
            this.groupBox3.Controls.Add(this.txtDealerBalance);
            this.groupBox3.Controls.Add(this.txtdealerCredit);
            this.groupBox3.Controls.Add(this.label12);
            this.groupBox3.Controls.Add(this.BalanceLbl1);
            this.groupBox3.Controls.Add(this.label10);
            this.groupBox3.Controls.Add(this.txtdealeramount);
            this.groupBox3.Controls.Add(this.lblTotalDiesel);
            this.groupBox3.Controls.Add(this.label9);
            this.groupBox3.Controls.Add(this.label11);
            this.groupBox3.Controls.Add(this.label14);
            this.groupBox3.Controls.Add(this.lblTotalPetrol);
            this.groupBox3.Controls.Add(this.label13);
            this.groupBox3.Font = new System.Drawing.Font("Segoe UI Semibold", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.groupBox3.ForeColor = System.Drawing.Color.White;
            this.groupBox3.Location = new System.Drawing.Point(506, 401);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(644, 316);
            this.groupBox3.TabIndex = 18;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "   Dealer   ";
            // 
            // lblExpenseTotal
            // 
            this.lblExpenseTotal.AutoSize = true;
            this.lblExpenseTotal.Location = new System.Drawing.Point(156, 243);
            this.lblExpenseTotal.Name = "lblExpenseTotal";
            this.lblExpenseTotal.Size = new System.Drawing.Size(23, 25);
            this.lblExpenseTotal.TabIndex = 36;
            this.lblExpenseTotal.Text = "0";
            // 
            // lblblbl
            // 
            this.lblblbl.AutoSize = true;
            this.lblblbl.Location = new System.Drawing.Point(49, 243);
            this.lblblbl.Name = "lblblbl";
            this.lblblbl.Size = new System.Drawing.Size(82, 25);
            this.lblblbl.TabIndex = 35;
            this.lblblbl.Text = "Expense";
            // 
            // txtDealerBalance
            // 
            this.txtDealerBalance.AutoSize = true;
            this.txtDealerBalance.Location = new System.Drawing.Point(156, 129);
            this.txtDealerBalance.Name = "txtDealerBalance";
            this.txtDealerBalance.Size = new System.Drawing.Size(23, 25);
            this.txtDealerBalance.TabIndex = 21;
            this.txtDealerBalance.Text = "0";
            // 
            // txtdealerCredit
            // 
            this.txtdealerCredit.AutoSize = true;
            this.txtdealerCredit.Location = new System.Drawing.Point(156, 93);
            this.txtdealerCredit.Name = "txtdealerCredit";
            this.txtdealerCredit.Size = new System.Drawing.Size(23, 25);
            this.txtdealerCredit.TabIndex = 23;
            this.txtdealerCredit.Text = "0";
            // 
            // label12
            // 
            this.label12.AutoSize = true;
            this.label12.Location = new System.Drawing.Point(49, 129);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(78, 25);
            this.label12.TabIndex = 20;
            this.label12.Text = "Balance";
            // 
            // BalanceLbl1
            // 
            this.BalanceLbl1.AutoSize = true;
            this.BalanceLbl1.Location = new System.Drawing.Point(192, 277);
            this.BalanceLbl1.Name = "BalanceLbl1";
            this.BalanceLbl1.Size = new System.Drawing.Size(23, 25);
            this.BalanceLbl1.TabIndex = 34;
            this.BalanceLbl1.Text = "0";
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(49, 58);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(59, 25);
            this.label10.TabIndex = 20;
            this.label10.Text = "Debit";
            // 
            // txtdealeramount
            // 
            this.txtdealeramount.AutoSize = true;
            this.txtdealeramount.Location = new System.Drawing.Point(156, 58);
            this.txtdealeramount.Name = "txtdealeramount";
            this.txtdealeramount.Size = new System.Drawing.Size(23, 25);
            this.txtdealeramount.TabIndex = 22;
            this.txtdealeramount.Text = "0";
            // 
            // lblTotalDiesel
            // 
            this.lblTotalDiesel.AutoSize = true;
            this.lblTotalDiesel.Location = new System.Drawing.Point(156, 207);
            this.lblTotalDiesel.Name = "lblTotalDiesel";
            this.lblTotalDiesel.Size = new System.Drawing.Size(23, 25);
            this.lblTotalDiesel.TabIndex = 13;
            this.lblTotalDiesel.Text = "0";
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(32, 278);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(162, 25);
            this.label9.TabIndex = 8;
            this.label9.Text = "Expense or Credit";
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Location = new System.Drawing.Point(49, 166);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(89, 25);
            this.label11.TabIndex = 9;
            this.label11.Text = "Sell Disel";
            // 
            // label14
            // 
            this.label14.AutoSize = true;
            this.label14.Location = new System.Drawing.Point(49, 93);
            this.label14.Name = "label14";
            this.label14.Size = new System.Drawing.Size(64, 25);
            this.label14.TabIndex = 21;
            this.label14.Text = "Credit";
            // 
            // lblTotalPetrol
            // 
            this.lblTotalPetrol.AutoSize = true;
            this.lblTotalPetrol.Location = new System.Drawing.Point(156, 166);
            this.lblTotalPetrol.Name = "lblTotalPetrol";
            this.lblTotalPetrol.Size = new System.Drawing.Size(23, 25);
            this.lblTotalPetrol.TabIndex = 12;
            this.lblTotalPetrol.Text = "0";
            // 
            // label13
            // 
            this.label13.AutoSize = true;
            this.label13.Location = new System.Drawing.Point(49, 207);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(105, 25);
            this.label13.TabIndex = 10;
            this.label13.Text = "Stock Disel";
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.txtAmount);
            this.groupBox2.Controls.Add(this.label4);
            this.groupBox2.Controls.Add(this.txtlitter);
            this.groupBox2.Controls.Add(this.label15);
            this.groupBox2.Controls.Add(this.txtadvance);
            this.groupBox2.Controls.Add(this.label3);
            this.groupBox2.Controls.Add(this.txtbalance);
            this.groupBox2.Controls.Add(this.label5);
            this.groupBox2.Controls.Add(this.txtcredit);
            this.groupBox2.Controls.Add(this.label8);
            this.groupBox2.Controls.Add(this.txtdebit);
            this.groupBox2.Controls.Add(this.label6);
            this.groupBox2.Font = new System.Drawing.Font("Segoe UI Semibold", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.groupBox2.ForeColor = System.Drawing.Color.White;
            this.groupBox2.Location = new System.Drawing.Point(13, 412);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(487, 290);
            this.groupBox2.TabIndex = 17;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "    Customer ";
            // 
            // txtAmount
            // 
            this.txtAmount.AutoSize = true;
            this.txtAmount.Location = new System.Drawing.Point(141, 196);
            this.txtAmount.Name = "txtAmount";
            this.txtAmount.Size = new System.Drawing.Size(23, 25);
            this.txtAmount.TabIndex = 19;
            this.txtAmount.Text = "0";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(19, 196);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(123, 25);
            this.label4.TabIndex = 18;
            this.label4.Text = "TotalAmount";
            // 
            // txtlitter
            // 
            this.txtlitter.AutoSize = true;
            this.txtlitter.Location = new System.Drawing.Point(140, 232);
            this.txtlitter.Name = "txtlitter";
            this.txtlitter.Size = new System.Drawing.Size(23, 25);
            this.txtlitter.TabIndex = 17;
            this.txtlitter.Text = "0";
            // 
            // label15
            // 
            this.label15.AutoSize = true;
            this.label15.Location = new System.Drawing.Point(19, 232);
            this.label15.Name = "label15";
            this.label15.Size = new System.Drawing.Size(57, 25);
            this.label15.TabIndex = 16;
            this.label15.Text = "Litter";
            // 
            // txtadvance
            // 
            this.txtadvance.AutoSize = true;
            this.txtadvance.Location = new System.Drawing.Point(140, 155);
            this.txtadvance.Name = "txtadvance";
            this.txtadvance.Size = new System.Drawing.Size(23, 25);
            this.txtadvance.TabIndex = 15;
            this.txtadvance.Text = "0";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(15, 155);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(127, 25);
            this.label3.TabIndex = 14;
            this.label3.Text = "TotalAdvance";
            // 
            // txtbalance
            // 
            this.txtbalance.AutoSize = true;
            this.txtbalance.Location = new System.Drawing.Point(122, 118);
            this.txtbalance.Name = "txtbalance";
            this.txtbalance.Size = new System.Drawing.Size(23, 25);
            this.txtbalance.TabIndex = 8;
            this.txtbalance.Text = "0";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(15, 118);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(78, 25);
            this.label5.TabIndex = 5;
            this.label5.Text = "Balance";
            // 
            // txtcredit
            // 
            this.txtcredit.AutoSize = true;
            this.txtcredit.Location = new System.Drawing.Point(122, 82);
            this.txtcredit.Name = "txtcredit";
            this.txtcredit.Size = new System.Drawing.Size(23, 25);
            this.txtcredit.TabIndex = 7;
            this.txtcredit.Text = "0";
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(15, 47);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(59, 25);
            this.label8.TabIndex = 3;
            this.label8.Text = "Debit";
            // 
            // txtdebit
            // 
            this.txtdebit.AutoSize = true;
            this.txtdebit.Location = new System.Drawing.Point(122, 47);
            this.txtdebit.Name = "txtdebit";
            this.txtdebit.Size = new System.Drawing.Size(23, 25);
            this.txtdebit.TabIndex = 6;
            this.txtdebit.Text = "0";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(15, 82);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(64, 25);
            this.label6.TabIndex = 4;
            this.label6.Text = "Credit";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Segoe UI Black", 18F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.ForeColor = System.Drawing.Color.White;
            this.label2.Location = new System.Drawing.Point(327, 360);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(447, 32);
            this.label2.TabIndex = 16;
            this.label2.Text = "Expense,Petroleum ,CustomerLedger";
            // 
            // comboYear
            // 
            this.comboYear.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.comboYear.AutoRoundedCorners = true;
            this.comboYear.BackColor = System.Drawing.Color.Transparent;
            this.comboYear.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(112)))), ((int)(((byte)(51)))), ((int)(((byte)(255)))));
            this.comboYear.BorderRadius = 17;
            this.comboYear.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            this.comboYear.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboYear.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(32)))), ((int)(((byte)(36)))), ((int)(((byte)(61)))));
            this.comboYear.FocusedColor = System.Drawing.Color.Empty;
            this.comboYear.FocusedState.Parent = this.comboYear;
            this.comboYear.Font = new System.Drawing.Font("Segoe UI", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.comboYear.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(68)))), ((int)(((byte)(88)))), ((int)(((byte)(112)))));
            this.comboYear.FormattingEnabled = true;
            this.comboYear.HoverState.Parent = this.comboYear;
            this.comboYear.ItemHeight = 30;
            this.comboYear.ItemsAppearance.Parent = this.comboYear;
            this.comboYear.Location = new System.Drawing.Point(947, 339);
            this.comboYear.Name = "comboYear";
            this.comboYear.ShadowDecoration.Parent = this.comboYear;
            this.comboYear.Size = new System.Drawing.Size(181, 36);
            this.comboYear.TabIndex = 19;
            this.comboYear.SelectedIndexChanged += new System.EventHandler(this.comboYear_SelectedIndexChanged_1);
            // 
            // frmDashBoard
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1167, 729);
            this.Controls.Add(this.comboYear);
            this.Controls.Add(this.groupBox3);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.panel5);
            this.Name = "frmDashBoard";
            this.Text = "frmDashBoard";
            this.Load += new System.EventHandler(this.frmDashBoard_Load);
            this.panel5.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.supplierChart)).EndInit();
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Panel panel5;
        private System.Windows.Forms.DataVisualization.Charting.Chart supplierChart;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.Label BalanceLbl1;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Label txtbalance;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label txtcredit;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Label txtdebit;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label lblTotalDiesel;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.Label lblTotalPetrol;
        private System.Windows.Forms.Label label13;
        private System.Windows.Forms.Label txtadvance;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label txtAmount;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label txtlitter;
        private System.Windows.Forms.Label label15;
        private System.Windows.Forms.Label txtdealerCredit;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.Label txtdealeramount;
        private System.Windows.Forms.Label label14;
        private System.Windows.Forms.Label txtDealerBalance;
        private System.Windows.Forms.Label label12;
        private Guna.UI2.WinForms.Guna2ComboBox comboYear;
        private System.Windows.Forms.Label lblExpenseTotal;
        private System.Windows.Forms.Label lblblbl;
    }
}