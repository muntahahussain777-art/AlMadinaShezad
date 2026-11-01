using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using AlMadinaShahzad.Services;

namespace AlMadinaShahzad
{
    public partial class frmGeminiChat : Form
    {
        private readonly AiChatService _ai = new AiChatService();

        public frmGeminiChat()
        {
            InitializeComponent();
        }

        private void frmGeminiChat_Load(object sender, EventArgs e)
        {
            if (!AiProviderSettings.HasUserSelectedProvider())
                AiProviderSettings.ShowProviderPicker(this);

            AppendSystem("Assalam o Alaikum! Al Madina Shezad Chhutta Services AI Assistant.");
            AppendSystem("Active AI: " + _ai.CurrentProviderLabel());
            AppendSystem("📌 Commands:");
            AppendSystem("  /software <sawal>  → Software ke bare mein (forms, buttons, kaise use karein)");
            AppendSystem("  /database <sawal>  → Live database numbers (sales, profit, expense, balance)");
            AppendSystem("  Bina prefix         → Koi bhi general sawal");
            AppendSystem("");
            AppendSystem("Misal:");
            AppendSystem("  /software Closing Form kya karta hai?");
            AppendSystem("  /database is mahine kitna profit hua?");
            AppendSystem("  Pakistan ki capital kya hai?");

            if (!AiProviderSettings.IsCurrentProviderReady())
                AppendSystem("ℹ Settings se API key check karein ya AI provider change karein.");
        }

        private async void btnSend_Click(object sender, EventArgs e)
        {
            string raw = (txtQuestion.Text ?? "").Trim();
            if (string.IsNullOrEmpty(raw)) return;

            var (mode, question) = GeminiQueryParser.Parse(raw);
            if (string.IsNullOrEmpty(question))
            {
                AppendSystem("Sawal likhein. Misal: /software Daily Diesel Sales kahan hai?");
                txtQuestion.Clear();
                return;
            }

            AppendUser(raw);
            txtQuestion.Clear();
            SetBusy(true);

            try
            {
                string providerLabel = _ai.CurrentProviderLabel();

                if (mode == GeminiQueryMode.Database)
                {
                    AppendAi(LocalCalculationAssistant.Answer(question));

                    if (AiProviderSettings.IsCurrentProviderReady())
                    {
                        string dbContext = PetroleumDataContext.BuildContext(question);
                        var result = await _ai.AskAsync(question, mode, dbContext).ConfigureAwait(true);
                        if (result.HasApiAnswer)
                            AppendAi("── " + providerLabel + " (" + GeminiQueryParser.ModeLabel(mode) + ") ──" + Environment.NewLine + result.ApiAnswer);
                        else
                            AppendSystem("ℹ " + providerLabel + " detail nahi mili — upar database calculation dekhein.");
                    }
                    else
                        AppendSystem("ℹ Database calculation upar hai. AI detail ke liye Settings se API key add karein.");
                }
                else if (mode == GeminiQueryMode.Software)
                {
                    if (AiProviderSettings.IsCurrentProviderReady())
                    {
                        string swContext = SoftwareKnowledgeContext.BuildContext();
                        var result = await _ai.AskAsync(question, mode, swContext).ConfigureAwait(true);
                        if (result.HasApiAnswer)
                            AppendAi("── " + providerLabel + " (" + GeminiQueryParser.ModeLabel(mode) + ") ──" + Environment.NewLine + result.ApiAnswer);
                        else
                            AppendSystem("ℹ " + providerLabel + " jawab nahi aaya. Settings se API key check karein.");
                    }
                    else
                        AppendSystem("ℹ Software guide ke liye API key chahiye. Settings → AI provider aur key set karein.");
                }
                else
                {
                    if (AiProviderSettings.IsCurrentProviderReady())
                    {
                        var result = await _ai.AskAsync(question, mode).ConfigureAwait(true);
                        if (result.HasApiAnswer)
                            AppendAi("── " + providerLabel + " (" + GeminiQueryParser.ModeLabel(mode) + ") ──" + Environment.NewLine + result.ApiAnswer);
                        else
                            AppendSystem("ℹ " + providerLabel + " jawab nahi aaya. API key ya internet check karein.");
                    }
                    else
                        AppendSystem("ℹ General sawal ke liye API key chahiye. Settings se Groq ya Google key add karein.");
                }
            }
            catch
            {
                AppendSystem("ℹ Error aayi — dobara try karein.");
            }
            finally
            {
                SetBusy(false);
                txtQuestion.Focus();
            }
        }

        private void btnSettings_Click(object sender, EventArgs e)
        {
            using (var dlg = new Form())
            {
                dlg.Text = "AI Settings";
                dlg.Size = new Size(500, 320);
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.BackColor = Color.FromArgb(32, 36, 61);
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.MaximizeBox = false;
                dlg.MinimizeBox = false;

                var lblProvider = new Label
                {
                    Text = "AI Provider:",
                    ForeColor = Color.White,
                    Location = new Point(12, 14),
                    AutoSize = true,
                    Font = new Font("Segoe UI", 10F, FontStyle.Bold)
                };

                var cboProvider = new ComboBox
                {
                    Location = new Point(110, 10),
                    Size = new Size(200, 28),
                    DropDownStyle = ComboBoxStyle.DropDownList,
                    Font = new Font("Segoe UI", 10F)
                };
                cboProvider.Items.Add("Groq AI");
                cboProvider.Items.Add("Google Gemini");
                cboProvider.SelectedIndex = AiProviderSettings.CurrentProvider == AiProviderType.Groq ? 0 : 1;

                var lblKeyHint = new Label
                {
                    ForeColor = Color.Silver,
                    Location = new Point(12, 48),
                    Size = new Size(460, 36),
                    Font = new Font("Segoe UI", 9F)
                };

                var txtKey = new TextBox
                {
                    Location = new Point(12, 90),
                    Size = new Size(460, 26),
                    Font = new Font("Segoe UI", 10F)
                };

                var btnSave = new Button
                {
                    Text = "Save Key",
                    Location = new Point(12, 130),
                    Size = new Size(100, 32),
                    BackColor = Color.FromArgb(112, 51, 255),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat
                };

                var lblInfo = new Label
                {
                    ForeColor = Color.Silver,
                    Location = new Point(120, 136),
                    AutoSize = true
                };

                Action updateKeyHint = () =>
                {
                    bool isGroq = cboProvider.SelectedIndex == 0;
                    lblKeyHint.Text = isGroq
                        ? "Groq key gsk_... se start honi chahiye (console.groq.com):\nNayi key paste karein — limit khatam ho to nayi add karein."
                        : "Google key AIzaSy... se start honi chahiye (aistudio.google.com/apikey):\nNayi key paste karein — limit khatam ho to nayi add karein.";
                    int keyCount = isGroq ? GroqConfig.GetAllKeys().Count : GeminiConfig.GetAllKeys().Count;
                    lblInfo.Text = $"Active keys: {keyCount}  |  Provider: {(isGroq ? "Groq" : "Google")}";
                };

                var btnChangeProvider = new Button
                {
                    Text = "Change AI",
                    Location = new Point(320, 8),
                    Size = new Size(100, 30),
                    BackColor = Color.FromArgb(80, 80, 100),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat
                };
                btnChangeProvider.Click += (s, ev) =>
                {
                    if (AiProviderSettings.ShowProviderPicker(dlg))
                    {
                        cboProvider.SelectedIndex = AiProviderSettings.CurrentProvider == AiProviderType.Groq ? 0 : 1;
                        updateKeyHint();
                    }
                };

                cboProvider.SelectedIndexChanged += (s, ev) =>
                {
                    AiProviderSettings.SetProvider(cboProvider.SelectedIndex == 0 ? AiProviderType.Groq : AiProviderType.Google);
                    updateKeyHint();
                };

                btnSave.Click += (s, ev) =>
                {
                    if (string.IsNullOrWhiteSpace(txtKey.Text)) return;

                    string key = txtKey.Text.Trim();
                    if (cboProvider.SelectedIndex == 0)
                    {
                        if (!GroqConfig.IsValidKeyFormat(key))
                        {
                            MessageBox.Show("Groq key gsk_... se start honi chahiye.", "Invalid Key", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }
                        GroqConfig.AddExtraKey(key);
                    }
                    else
                    {
                        if (!GeminiModelResolver.IsValidKeyFormat(key))
                        {
                            MessageBox.Show("Google key AIzaSy... se start honi chahiye.", "Invalid Key", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }
                        GeminiConfig.AddExtraKey(key);
                    }

                    txtKey.Clear();
                    updateKeyHint();
                    MessageBox.Show("API key save ho gayi!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                };

                updateKeyHint();
                dlg.Controls.AddRange(new Control[] { lblProvider, cboProvider, btnChangeProvider, lblKeyHint, txtKey, btnSave, lblInfo });
                dlg.ShowDialog(this);
            }
        }

        private void txtQuestion_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter && !e.Shift)
            {
                e.SuppressKeyPress = true;
                btnSend.PerformClick();
            }
        }

        private void AppendUser(string text)
        {
            rtbChat.SelectionStart = rtbChat.TextLength;
            rtbChat.SelectionColor = Color.FromArgb(100, 180, 255);
            rtbChat.AppendText("Aap: " + text + Environment.NewLine + Environment.NewLine);
        }

        private void AppendAi(string text)
        {
            rtbChat.SelectionStart = rtbChat.TextLength;
            rtbChat.SelectionColor = Color.FromArgb(180, 255, 180);
            rtbChat.AppendText("AI: " + text + Environment.NewLine + Environment.NewLine);
            rtbChat.ScrollToCaret();
        }

        private void AppendSystem(string text)
        {
            rtbChat.SelectionStart = rtbChat.TextLength;
            rtbChat.SelectionColor = Color.Silver;
            rtbChat.AppendText(text + Environment.NewLine + Environment.NewLine);
        }

        private void SetBusy(bool busy)
        {
            btnSend.Enabled = !busy;
            txtQuestion.Enabled = !busy;
            btnSend.Text = busy ? "..." : "Send";
            Cursor = busy ? Cursors.WaitCursor : Cursors.Default;
        }
    }
}
