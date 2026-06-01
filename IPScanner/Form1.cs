using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace IPScanner
{
    public partial class Form1 : Form
    {
        private CancellationTokenSource _cts;
        private System.Windows.Forms.Timer _autoRefreshTimer;
        private bool _isScanning = false;
        private readonly object _gridLock = new object();

        // ── UI controls ──────────────────────────────────────────
        private Panel   pnlHeader;
        private PictureBox picLogo;
        private Label   lblSubtitle;

        private Panel   pnlInput;
        private Label   lblStartIP;
        private TextBox txtStartIP;
        private Label   lblDash;
        private Label   lblEndIP;
        private TextBox txtEndIP;

        private Panel   pnlButtons;
        private Button  btnScan;
        private Button  btnStop;
        private Button  btnClear;
        private CheckBox chkAutoRefresh;
        private NumericUpDown nudInterval;
        private Label   lblSeconds;

        private Panel   pnlStats;
        private Label   lblOnlineCount;
        private Label   lblOfflineCount;
        private Label   lblTotalCount;
        private Label   lblScanTime;

        private ProgressBar progressBar;
        private Label   lblProgress;

        private DataGridView dgvResults;

        private StatusStrip statusStrip;
        private ToolStripStatusLabel tsslStatus;
        private ToolStripProgressBar tsslProgress;

        // ── Colours ──────────────────────────────────────────────
        private readonly Color C_BG        = Color.FromArgb(18, 22, 30);
        private readonly Color C_SURFACE   = Color.FromArgb(26, 32, 44);
        private readonly Color C_CARD      = Color.FromArgb(34, 42, 58);
        private readonly Color C_ACCENT    = Color.FromArgb(56, 189, 248);
        private readonly Color C_ONLINE    = Color.FromArgb(52, 211, 153);
        private readonly Color C_OFFLINE   = Color.FromArgb(248, 113, 113);
        private readonly Color C_TEXT      = Color.FromArgb(226, 232, 240);
        private readonly Color C_TEXTDIM   = Color.FromArgb(100, 116, 139);
        private readonly Color C_BORDER    = Color.FromArgb(51, 65, 85);
        private readonly Color C_BTN_SCAN  = Color.FromArgb(56, 189, 248);
        private readonly Color C_BTN_STOP  = Color.FromArgb(248, 113, 113);
        private readonly Color C_BTN_CLR   = Color.FromArgb(71, 85, 105);

        public Form1()
        {
            InitializeComponent();
            BuildUI();
            SetupAutoRefreshTimer();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // Form1
            // 
            this.ClientSize = new System.Drawing.Size(939, 720);
            this.Name = "Form1";
            this.Text = "IP Range Scanner";
            this.ResumeLayout(false);

        }

        // ════════════════════════════════════════════════════════
        //  UI CONSTRUCTION
        // ════════════════════════════════════════════════════════
        private void BuildUI()
        {
            this.SuspendLayout();

            // ── Form ─────────────────────────────────────────────
            this.Text            = "KLASSMATE";
            this.Size            = new Size(900, 720);
            this.MinimumSize     = new Size(780, 600);
            this.BackColor       = C_BG;
            this.ForeColor       = C_TEXT;
            this.Font            = new Font("Segoe UI", 9f, FontStyle.Regular);
            this.StartPosition   = FormStartPosition.CenterScreen;

            // ── Header ───────────────────────────────────────────
            pnlHeader = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 72,
                BackColor = C_SURFACE,
                Padding   = new Padding(20, 0, 20, 0)
            };

            string logoPath = System.IO.Path.GetFullPath(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\logo.png"));
            if (!System.IO.File.Exists(logoPath)) {
                logoPath = @"d:\Projects\2026\IPScanner\logo.png";
            }

            picLogo = new PictureBox
            {
                SizeMode  = PictureBoxSizeMode.Zoom,
                Size      = new Size(180, 40),
                Location  = new Point(20, 8)
            };
            try { picLogo.Image = Image.FromFile(logoPath); } catch { }

            lblSubtitle = new Label
            {
                Text      = "Monitor IP addresses across a subnet rang",
                Font      = new Font("Segoe UI", 8.5f),
                ForeColor = C_TEXTDIM,
                AutoSize  = true,
                Location  = new Point(22, 46)
            };

            pnlHeader.Controls.Add(picLogo);
            pnlHeader.Controls.Add(lblSubtitle);

            // Accent bar at bottom of header
            var accentBar = new Panel
            {
                Dock      = DockStyle.Bottom,
                Height    = 2,
                BackColor = C_ACCENT
            };
            pnlHeader.Controls.Add(accentBar);

            // ── Input Row ────────────────────────────────────────
            pnlInput = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 64,
                BackColor = C_CARD,
                Padding   = new Padding(20, 12, 20, 12)
            };

            lblStartIP = CreateLabel("Start IP:", 20, 22);
            txtStartIP = CreateTextBox("192.168.1.1",  100, 16, 130);

            lblDash    = CreateLabel("—", 242, 22);
            lblEndIP   = CreateLabel("End IP:",  265, 22);
            txtEndIP   = CreateTextBox("192.168.1.100", 330, 16, 130);

            pnlInput.Controls.AddRange(new Control[]
                { lblStartIP, txtStartIP, lblDash, lblEndIP, txtEndIP });

            // ── Button Row ───────────────────────────────────────
            pnlButtons = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 56,
                BackColor = C_SURFACE,
                Padding   = new Padding(20, 10, 20, 10)
            };

            btnScan  = CreateButton("▶  Scan",  0,   10, 110, C_BTN_SCAN,  Color.FromArgb(14,  20,  30));
            btnStop  = CreateButton("■  Stop",  120, 10, 110, C_BTN_STOP,  Color.White);
            btnClear = CreateButton("✕  Clear", 240, 10, 110, C_BTN_CLR,   C_TEXT);

            btnStop.Enabled = false;

            chkAutoRefresh = new CheckBox
            {
                Text      = "Auto-Refresh",
                ForeColor = C_TEXT,
                Location  = new Point(370, 15),
                AutoSize  = true,
                Checked   = false
            };
            chkAutoRefresh.CheckedChanged += ChkAutoRefresh_CheckedChanged;

            nudInterval = new NumericUpDown
            {
                Minimum   = 5,
                Maximum   = 300,
                Value     = 30,
                Location  = new Point(478, 12),
                Width     = 55,
                BackColor = C_CARD,
                ForeColor = C_TEXT,
                BorderStyle = BorderStyle.FixedSingle
            };

            lblSeconds = CreateLabel("sec", 538, 15);

            pnlButtons.Controls.AddRange(new Control[]
                { btnScan, btnStop, btnClear, chkAutoRefresh, nudInterval, lblSeconds });

            btnScan.Click  += BtnScan_Click;
            btnStop.Click  += BtnStop_Click;
            btnClear.Click += BtnClear_Click;

            // ── Stats Strip ──────────────────────────────────────
            pnlStats = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 44,
                BackColor = C_BG,
                Padding   = new Padding(20, 8, 20, 8)
            };

            lblTotalCount   = CreateStatLabel("Total: 0",    0);
            lblOnlineCount  = CreateStatLabel("Online: 0",   120);
            lblOfflineCount = CreateStatLabel("Offline: 0",  240);
            lblScanTime     = CreateStatLabel("",            360);

            lblOnlineCount.ForeColor  = C_ONLINE;
            lblOfflineCount.ForeColor = C_OFFLINE;
            lblScanTime.ForeColor     = C_TEXTDIM;

            pnlStats.Controls.AddRange(new Control[]
                { lblTotalCount, lblOnlineCount, lblOfflineCount, lblScanTime });

            // ── Progress ─────────────────────────────────────────
            progressBar = new ProgressBar
            {
                Dock      = DockStyle.Top,
                Height    = 4,
                Style     = ProgressBarStyle.Blocks,
                ForeColor = C_ACCENT,
                BackColor = C_BORDER,
                Minimum   = 0,
                Maximum   = 100,
                Value     = 0
            };

            // ── Grid ─────────────────────────────────────────────
            dgvResults = new DataGridView
            {
                Dock                    = DockStyle.Fill,
                BackgroundColor         = C_SURFACE,
                ForeColor               = C_TEXT,
                GridColor               = C_BORDER,
                BorderStyle             = BorderStyle.None,
                RowHeadersVisible       = false,
                AllowUserToAddRows      = false,
                AllowUserToDeleteRows   = false,
                ReadOnly                = true,
                SelectionMode           = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect             = false,
                AutoSizeColumnsMode     = DataGridViewAutoSizeColumnsMode.Fill,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                ColumnHeadersHeight     = 36,
                RowTemplate             = { Height = 32 },
                Font                    = new Font("Segoe UI", 9f)
            };

            StyleGrid();
            BuildColumns();

            dgvResults.CellFormatting += DgvResults_CellFormatting;

            // ── Status strip ─────────────────────────────────────
            statusStrip = new StatusStrip
            {
                BackColor = C_SURFACE,
                ForeColor = C_TEXTDIM,
                SizingGrip = false
            };

            tsslStatus   = new ToolStripStatusLabel("Ready") { Spring = true, TextAlign = ContentAlignment.MiddleLeft };
            tsslProgress = new ToolStripProgressBar { Width = 120, Visible = false };
            statusStrip.Items.AddRange(new ToolStripItem[] { tsslStatus, tsslProgress });

            // ── Compose form ─────────────────────────────────────
            // Top-docked panels added in reverse (last added = top)
            this.Controls.Add(dgvResults);
            this.Controls.Add(progressBar);
            this.Controls.Add(pnlStats);
            this.Controls.Add(pnlButtons);
            this.Controls.Add(pnlInput);
            this.Controls.Add(pnlHeader);
            this.Controls.Add(statusStrip);

            this.ResumeLayout(true);
        }

        // ════════════════════════════════════════════════════════
        //  HELPER BUILDERS
        // ════════════════════════════════════════════════════════
        private Label CreateLabel(string text, int x, int y)
        {
            return new Label
            {
                Text      = text,
                Location  = new Point(x, y),
                AutoSize  = true,
                ForeColor = C_TEXTDIM
            };
        }

        private Label CreateStatLabel(string text, int x)
        {
            return new Label
            {
                Text      = text,
                Location  = new Point(x, 8),
                AutoSize  = true,
                Font      = new Font("Segoe UI Semibold", 9f, FontStyle.Bold),
                ForeColor = C_TEXT
            };
        }

        private TextBox CreateTextBox(string text, int x, int y, int width)
        {
            return new TextBox
            {
                Text        = text,
                Location    = new Point(x, y),
                Width       = width,
                BackColor   = C_BG,
                ForeColor   = C_TEXT,
                BorderStyle = BorderStyle.FixedSingle,
                Font        = new Font("Consolas", 10f)
            };
        }

        private Button CreateButton(string text, int x, int y, int width, Color backColor, Color foreColor)
        {
            var btn = new Button
            {
                Text      = text,
                Location  = new Point(x, y),
                Size      = new Size(width, 34),
                BackColor = backColor,
                ForeColor = foreColor,
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Segoe UI Semibold", 9f, FontStyle.Bold),
                Cursor    = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize  = 0;
            btn.FlatAppearance.MouseOverBackColor  = ControlPaint.Light(backColor, 0.2f);
            btn.FlatAppearance.MouseDownBackColor  = ControlPaint.Dark(backColor, 0.1f);
            return btn;
        }

        private void StyleGrid()
        {
            var hdrStyle = dgvResults.ColumnHeadersDefaultCellStyle;
            hdrStyle.BackColor   = C_CARD;
            hdrStyle.ForeColor   = C_ACCENT;
            hdrStyle.Font        = new Font("Segoe UI Semibold", 9f, FontStyle.Bold);
            hdrStyle.Alignment   = DataGridViewContentAlignment.MiddleLeft;
            hdrStyle.Padding     = new Padding(8, 0, 0, 0);

            var defStyle = dgvResults.DefaultCellStyle;
            defStyle.BackColor   = C_SURFACE;
            defStyle.ForeColor   = C_TEXT;
            defStyle.SelectionBackColor = Color.FromArgb(45, 56, 189, 248);
            defStyle.SelectionForeColor = C_TEXT;
            defStyle.Padding     = new Padding(8, 0, 0, 0);

            var altStyle = dgvResults.AlternatingRowsDefaultCellStyle;
            altStyle.BackColor   = C_CARD;
            altStyle.ForeColor   = C_TEXT;
            altStyle.SelectionBackColor = Color.FromArgb(45, 56, 189, 248);
            altStyle.SelectionForeColor = C_TEXT;
        }

        private void BuildColumns()
        {
            dgvResults.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name        = "colIndex",
                HeaderText  = "#",
                FillWeight  = 5,
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter }
            });
            dgvResults.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name        = "colIP",
                HeaderText  = "IP Address",
                FillWeight  = 22,
                DefaultCellStyle = { Font = new Font("Consolas", 9.5f) }
            });
            dgvResults.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name        = "colStatus",
                HeaderText  = "Status",
                FillWeight  = 13,
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter, Font = new Font("Segoe UI Semibold", 9f, FontStyle.Bold) }
            });
            dgvResults.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name        = "colResponseTime",
                HeaderText  = "Response (ms)",
                FillWeight  = 18,
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter }
            });
            dgvResults.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name        = "colTTL",
                HeaderText  = "TTL",
                FillWeight  = 10,
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter }
            });
            dgvResults.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name        = "colHostname",
                HeaderText  = "Hostname",
                FillWeight  = 32
            });
            dgvResults.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name        = "colLastChecked",
                HeaderText  = "Last Checked",
                FillWeight  = 22,
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter }
            });
        }

        // ════════════════════════════════════════════════════════
        //  AUTO-REFRESH TIMER
        // ════════════════════════════════════════════════════════
        private void SetupAutoRefreshTimer()
        {
            _autoRefreshTimer = new System.Windows.Forms.Timer();
            _autoRefreshTimer.Tick += (s, e) =>
            {
                if (!_isScanning)
                    StartScan();
            };
        }

        private void ChkAutoRefresh_CheckedChanged(object sender, EventArgs e)
        {
            if (chkAutoRefresh.Checked)
            {
                _autoRefreshTimer.Interval = (int)(nudInterval.Value * 1000);
                _autoRefreshTimer.Start();
                tsslStatus.Text = $"Auto-refresh every {nudInterval.Value}s — next scan will begin shortly.";
            }
            else
            {
                _autoRefreshTimer.Stop();
                tsslStatus.Text = "Auto-refresh disabled.";
            }
        }

        // ════════════════════════════════════════════════════════
        //  BUTTON HANDLERS
        // ════════════════════════════════════════════════════════
        private void BtnScan_Click(object sender, EventArgs e)  => StartScan();
        private void BtnStop_Click(object sender, EventArgs e)  => StopScan();
        private void BtnClear_Click(object sender, EventArgs e) => ClearResults();

        private void ClearResults()
        {
            dgvResults.Rows.Clear();
            progressBar.Value = 0;
            UpdateStats();
            tsslStatus.Text = "Cleared.";
        }

        private void StopScan()
        {
            _cts?.Cancel();
            tsslStatus.Text = "Scan stopped by user.";
        }

        // ════════════════════════════════════════════════════════
        //  SCAN LOGIC
        // ════════════════════════════════════════════════════════
        private void StartScan()
        {
            if (_isScanning) return;

            if (!TryParseRange(out var startBytes, out var endBytes))
            {
                MessageBox.Show("Please enter valid IP addresses.\nExample: 192.168.1.1 — 192.168.1.100",
                    "Invalid Input", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var ips = BuildIPList(startBytes, endBytes);
            if (ips.Count == 0)
            {
                MessageBox.Show("Start IP must be less than or equal to End IP.", "Invalid Range",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (ips.Count > 65536)
            {
                MessageBox.Show("Range too large (max 65 536 IPs).", "Range Too Large",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _cts = new CancellationTokenSource();
            SetScanningState(true);

            // Seed rows (keep existing rows for IPs we already know about)
            SeedRows(ips);
            UpdateStats();

            var token    = _cts.Token;
            var startTime = DateTime.Now;

            Task.Run(() => RunScan(ips, token, startTime), token)
                .ContinueWith(t => SafeInvoke(() =>
                {
                    SetScanningState(false);
                    if (token.IsCancellationRequested || t.IsCanceled)
                        tsslStatus.Text = "Scan stopped by user.";
                    else if (t.IsFaulted)
                        tsslStatus.Text = "Scan error: " + t.Exception?.InnerException?.Message;
                    else
                        tsslStatus.Text = "Scan completed.";
                    
                    tsslProgress.Visible = false;
                    progressBar.Value = 1;
                    lblScanTime.Text = $"Completed in {(DateTime.Now - startTime).TotalSeconds:F1}s";
                }));
        }

        private void RunScan(List<string> ips, CancellationToken token, DateTime startTime)
        {
            int done    = 0;
            int total   = ips.Count;
            int threads = Math.Min(64, total);   // parallel pings

            SafeInvoke(() =>
            {
                progressBar.Maximum = total;
                progressBar.Value   = 0;
                tsslProgress.Maximum = total;
                tsslProgress.Value   = 0;
                tsslProgress.Visible = true;
            });

            try
            {
                Parallel.ForEach(ips,
                    new ParallelOptions { MaxDegreeOfParallelism = threads, CancellationToken = token },
                    ip =>
                    {
                        if (token.IsCancellationRequested) return;

                        var result = PingHost(ip);
                        int current = Interlocked.Increment(ref done);

                        SafeInvoke(() =>
                        {
                            UpdateRow(ip, result);
                            progressBar.Value = Math.Min(current, total);
                            tsslProgress.Value = Math.Min(current, total);
                            tsslStatus.Text = $"Scanning… {current}/{total}  |  {ip}";
                            UpdateStats();
                        });
                    });
            }
            catch (OperationCanceledException)
            {
                // The scan was stopped by the user. 
            }
        }

        private PingResult PingHost(string ip)
        {
            try
            {
                using (var ping = new Ping())
                {
                    var reply = ping.Send(ip, 1000);   // 1 s timeout

                    if (reply?.Status == IPStatus.Success)
                    {
                        string hostname = "";
                        try
                        {
                            var entry = System.Net.Dns.GetHostEntry(ip);
                            hostname  = entry.HostName;
                        }
                        catch { }

                        return new PingResult
                        {
                            IsOnline     = true,
                            ResponseTime = reply.RoundtripTime,
                            TTL          = reply.Options?.Ttl ?? 0,
                            Hostname     = hostname
                        };
                    }
                }
            }
            catch { }

            return new PingResult { IsOnline = false };
        }

        // ════════════════════════════════════════════════════════
        //  GRID HELPERS
        // ════════════════════════════════════════════════════════
        private void SeedRows(List<string> ips)
        {
            // Build a quick-lookup of existing rows
            var existing = new HashSet<string>();
            foreach (DataGridViewRow r in dgvResults.Rows)
                existing.Add(r.Cells["colIP"].Value?.ToString() ?? "");

            int idx = dgvResults.Rows.Count + 1;
            foreach (var ip in ips)
            {
                if (existing.Contains(ip)) continue;
                dgvResults.Rows.Add(idx++, ip, "—", "—", "—", "—", "—");
            }
        }

        private void UpdateRow(string ip, PingResult result)
        {
            foreach (DataGridViewRow row in dgvResults.Rows)
            {
                if (row.Cells["colIP"].Value?.ToString() != ip) continue;

                row.Cells["colStatus"].Value      = result.IsOnline ? "ONLINE"  : "OFFLINE";
                row.Cells["colResponseTime"].Value = result.IsOnline ? result.ResponseTime.ToString() + " ms" : "—";
                row.Cells["colTTL"].Value          = result.IsOnline ? result.TTL.ToString() : "—";
                row.Cells["colHostname"].Value     = result.IsOnline ? result.Hostname       : "—";
                row.Cells["colLastChecked"].Value  = DateTime.Now.ToString("HH:mm:ss");
                return;
            }
        }

        private void DgvResults_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var row = dgvResults.Rows[e.RowIndex];
            var status = row.Cells["colStatus"].Value?.ToString();

            if (dgvResults.Columns[e.ColumnIndex].Name == "colStatus")
            {
                if (status == "ONLINE")
                {
                    e.CellStyle.ForeColor = C_ONLINE;
                    // Use a solid colour instead of transparent to prevent scrolling smudges
                    e.CellStyle.BackColor = Color.FromArgb(29, 53, 57);
                }
                else if (status == "OFFLINE")
                {
                    e.CellStyle.ForeColor = C_OFFLINE;
                    // Use a solid colour instead of transparent to prevent scrolling smudges
                    e.CellStyle.BackColor = Color.FromArgb(44, 38, 49);
                }
            }
        }

        // ════════════════════════════════════════════════════════
        //  STATS
        // ════════════════════════════════════════════════════════
        private void UpdateStats()
        {
            int total   = dgvResults.Rows.Count;
            int online  = 0;
            int offline = 0;

            foreach (DataGridViewRow row in dgvResults.Rows)
            {
                var s = row.Cells["colStatus"].Value?.ToString();
                if (s == "ONLINE")  online++;
                else if (s == "OFFLINE") offline++;
            }

            lblTotalCount.Text   = $"Total: {total}";
            lblOnlineCount.Text  = $"● Online: {online}";
            lblOfflineCount.Text = $"● Offline: {offline}";
        }

        // ════════════════════════════════════════════════════════
        //  STATE
        // ════════════════════════════════════════════════════════
        private void SetScanningState(bool scanning)
        {
            _isScanning      = scanning;
            btnScan.Enabled  = !scanning;
            btnStop.Enabled  = scanning;
            btnClear.Enabled = !scanning;
            txtStartIP.Enabled = !scanning;
            txtEndIP.Enabled   = !scanning;
        }

        // ════════════════════════════════════════════════════════
        //  IP PARSING / BUILDING
        // ════════════════════════════════════════════════════════
        private bool TryParseRange(out byte[] start, out byte[] end)
        {
            start = end = null;
            try
            {
                start = System.Net.IPAddress.Parse(txtStartIP.Text.Trim()).GetAddressBytes();
                end   = System.Net.IPAddress.Parse(txtEndIP.Text.Trim()).GetAddressBytes();
                return true;
            }
            catch { return false; }
        }

        private List<string> BuildIPList(byte[] start, byte[] end)
        {
            var list = new List<string>();
            uint s = ToUInt(start);
            uint e = ToUInt(end);
            if (s > e) return list;
            for (uint i = s; i <= e; i++)
                list.Add(FromUInt(i));
            return list;
        }

        private static uint ToUInt(byte[] b) =>
            ((uint)b[0] << 24) | ((uint)b[1] << 16) | ((uint)b[2] << 8) | b[3];

        private static string FromUInt(uint v) =>
            $"{(v >> 24) & 0xFF}.{(v >> 16) & 0xFF}.{(v >> 8) & 0xFF}.{v & 0xFF}";

        // ════════════════════════════════════════════════════════
        //  THREAD-SAFE INVOKE
        // ════════════════════════════════════════════════════════
        private void SafeInvoke(Action action)
        {
            if (IsDisposed) return;
            if (InvokeRequired)
                Invoke(action);
            else
                action();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _cts?.Cancel();
            _autoRefreshTimer?.Stop();
            base.OnFormClosing(e);
        }
    }

    // ════════════════════════════════════════════════════════
    //  PING RESULT
    // ════════════════════════════════════════════════════════
    public class PingResult
    {
        public bool   IsOnline     { get; set; }
        public long   ResponseTime { get; set; }
        public int    TTL          { get; set; }
        public string Hostname     { get; set; } = "";
    }
}
