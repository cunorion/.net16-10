using bai1;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ClinicAppointmentManager
{
    public class Appointment
    {
        public string MaLichHen { get; set; } = "";
        public string HoTen { get; set; } = "";
        public string SoDienThoai { get; set; } = "";
        public DateTime NgayGioHen { get; set; } = DateTime.Now;
        public string BacSi { get; set; } = "";
        public string LyDo { get; set; } = "";
        public string TrangThai { get; set; } = "Chờ khám";
    }

    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();
            Application.Run(new MainForm());
        }
    }

    public class MainForm : Form
    {
        BindingList<Appointment> data = new();
        BindingSource source = new();
        ErrorProvider errors = new();
        TextBox txtMa = new(), txtTen = new(), txtSDT = new(), txtLyDo = new(), txtTim = new();
        DateTimePicker dtHen = new();
        ComboBox cbBacSi = new(), cbTrangThai = new(), cbLocTrangThai = new();
        DateTimePicker dtLoc = new();
        CheckBox chkLocNgay = new();
        DataGridView grid = new();
        ToolStripStatusLabel status = new();

        public MainForm()
        {
            Text = "Clinic Appointment Manager";
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(1000, 650);
            Size = new Size(1250, 780);
            errors.ContainerControl = this;
            BuildUI();
            source.DataSource = data;
            grid.DataSource = source;
            RefreshGrid();
        }

        void BuildUI()
        {
            var menu = new MenuStrip();
            var file = new ToolStripMenuItem("Tệp");
            file.DropDownItems.Add("Xuất CSV...", null, (_, __) => ExportCsv());
            file.DropDownItems.Add("Thoát", null, (_, __) => Close());
            menu.Items.Add(file);
            MainMenuStrip = menu;
            Controls.Add(menu);

            var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Padding = new Padding(8) };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 255));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 25));
            Controls.Add(root);
            root.BringToFront();
            root.Controls.Add(BuildInputPanel(), 0, 0);
            root.Controls.Add(BuildListPanel(), 0, 1);

            var strip = new StatusStrip();
            strip.Items.Add(status);
            root.Controls.Add(strip, 0, 2);
        }

        Control BuildInputPanel()
        {
            var outer = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Padding = new Padding(8), BorderStyle = BorderStyle.FixedSingle };
            outer.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            outer.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            var fields = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 3 };
            for (int i = 0; i < 4; i++) fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            for (int i = 0; i < 3; i++) fields.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33f));

            cbBacSi.DropDownStyle = ComboBoxStyle.DropDownList;
            cbBacSi.Items.AddRange(new object[] { "BS. Nguyễn An", "BS. Trần Bình", "BS. Lê Chi", "BS. Phạm Dũng" });
            cbBacSi.SelectedIndex = 0;
            cbTrangThai.DropDownStyle = ComboBoxStyle.DropDownList;
            cbTrangThai.Items.AddRange(new object[] { "Chờ khám", "Đang khám", "Hoàn tất", "Đã hủy" });
            cbTrangThai.SelectedIndex = 0;
            dtHen.Format = DateTimePickerFormat.Custom;
            dtHen.CustomFormat = "dd/MM/yyyy HH:mm";
            dtHen.Width = 220;
            dtHen.Value = DateTime.Now.AddMinutes(30);

            AddField(fields, "Mã lịch hẹn *", txtMa, 0, 0);
            AddField(fields, "Họ tên bệnh nhân *", txtTen, 1, 0);
            AddField(fields, "Số điện thoại *", txtSDT, 2, 0);
            AddField(fields, "Ngày giờ hẹn *", dtHen, 3, 0);
            AddField(fields, "Bác sĩ", cbBacSi, 0, 1);
            AddField(fields, "Trạng thái", cbTrangThai, 1, 1);
            AddField(fields, "Lý do khám *", txtLyDo, 2, 1, 2);

            var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight };
            buttons.Controls.Add(MakeButton("Thêm", (_, __) => AddItem()));
            buttons.Controls.Add(MakeButton("Cập nhật", (_, __) => UpdateItem()));
            buttons.Controls.Add(MakeButton("Xóa", (_, __) => DeleteItem()));
            buttons.Controls.Add(MakeButton("Làm mới", (_, __) => ClearForm()));
            outer.Controls.Add(fields, 0, 0);
            outer.Controls.Add(buttons, 0, 1);
            return outer;
        }

        void AddField(TableLayoutPanel p, string label, Control c, int col, int row, int span = 1)
        {
            var panel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Padding = new Padding(4) };
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            panel.Controls.Add(new Label { Text = label, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
            c.Dock = DockStyle.Fill;
            panel.Controls.Add(c, 0, 1);
            p.Controls.Add(panel, col, row);
            if (span > 1) p.SetColumnSpan(panel, span);
        }

        Control BuildListPanel()
        {
            var panel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Padding = new Padding(0, 8, 0, 0) };
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 76));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            var filters = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = true, Padding = new Padding(3) };
            filters.Controls.Add(new Label { Text = "Tìm tên/SĐT:", AutoSize = true, Margin = new Padding(3, 9, 3, 3) });
            txtTim.Width = 190; txtTim.TextChanged += (_, __) => RefreshGrid(); filters.Controls.Add(txtTim);
            chkLocNgay.Text = "Lọc ngày"; chkLocNgay.AutoSize = true; chkLocNgay.CheckedChanged += (_, __) => RefreshGrid(); filters.Controls.Add(chkLocNgay);
            dtLoc.Format = DateTimePickerFormat.Short; dtLoc.ValueChanged += (_, __) => RefreshGrid(); filters.Controls.Add(dtLoc);
            filters.Controls.Add(new Label { Text = "Trạng thái:", AutoSize = true, Margin = new Padding(3, 9, 3, 3) });
            cbLocTrangThai.DropDownStyle = ComboBoxStyle.DropDownList;
            cbLocTrangThai.Items.AddRange(new object[] { "Tất cả", "Chờ khám", "Đang khám", "Hoàn tất", "Đã hủy" });
            cbLocTrangThai.SelectedIndex = 0; cbLocTrangThai.SelectedIndexChanged += (_, __) => RefreshGrid(); filters.Controls.Add(cbLocTrangThai);
            grid.Dock = DockStyle.Fill; grid.ReadOnly = true; grid.AllowUserToAddRows = false; grid.AllowUserToDeleteRows = false;
            grid.AutoGenerateColumns = false; grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.MultiSelect = false; grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            AddCol("Mã lịch hẹn", "MaLichHen"); AddCol("Bệnh nhân", "HoTen"); AddCol("Số điện thoại", "SoDienThoai");
            AddCol("Ngày giờ hẹn", "NgayGioHen", "dd/MM/yyyy HH:mm"); AddCol("Bác sĩ", "BacSi"); AddCol("Lý do", "LyDo"); AddCol("Trạng thái", "TrangThai");
            grid.SelectionChanged += (_, __) => LoadSelected();
            panel.Controls.Add(filters, 0, 0); panel.Controls.Add(grid, 0, 1);
            return panel;
        }

        void AddCol(string title, string prop, string format = null)
        {
            var c = new DataGridViewTextBoxColumn { HeaderText = title, DataPropertyName = prop, Name = prop };
            if (format != null) c.DefaultCellStyle.Format = format;
            grid.Columns.Add(c);
        }
        Button MakeButton(string text, EventHandler action) { var b = new Button { Text = text, Width = 105, Height = 30 }; b.Click += action; return b; }

        bool ValidateInput()
        {
            errors.Clear(); bool ok = true;
            void Err(Control c, string msg) { errors.SetError(c, msg); ok = false; }
            if (string.IsNullOrWhiteSpace(txtMa.Text)) Err(txtMa, "Nhập mã lịch hẹn.");
            else if (data.Any(x => x.MaLichHen.Equals(txtMa.Text.Trim(), StringComparison.OrdinalIgnoreCase) && !ReferenceEquals(x, Selected()))) Err(txtMa, "Mã lịch hẹn đã tồn tại.");
            if (string.IsNullOrWhiteSpace(txtTen.Text)) Err(txtTen, "Nhập họ tên bệnh nhân.");
            if (!System.Text.RegularExpressions.Regex.IsMatch(txtSDT.Text.Trim(), @"^\+?\d{9,11}$")) Err(txtSDT, "Số điện thoại phải có 9–11 chữ số (có thể bắt đầu bằng +).");
            if (string.IsNullOrWhiteSpace(txtLyDo.Text)) Err(txtLyDo, "Nhập lý do khám.");
            if (dtHen.Value < DateTime.Now.AddMinutes(-1)) Err(dtHen, "Ngày giờ hẹn không được nằm trong quá khứ.");
            return ok;
        }
        Appointment Selected() => grid.CurrentRow?.DataBoundItem as Appointment;
        Appointment ReadForm() => new Appointment { MaLichHen = txtMa.Text.Trim(), HoTen = txtTen.Text.Trim(), SoDienThoai = txtSDT.Text.Trim(), NgayGioHen = dtHen.Value, BacSi = cbBacSi.Text, LyDo = txtLyDo.Text.Trim(), TrangThai = cbTrangThai.Text };
        void AddItem() { if (!ValidateInput()) return; data.Add(ReadForm()); RefreshGrid(); ClearForm(); }
        void UpdateItem() { var old = Selected(); if (old == null) { MessageBox.Show("Chọn lịch hẹn cần cập nhật."); return; } if (!ValidateInput()) return; var v = ReadForm(); old.MaLichHen = v.MaLichHen; old.HoTen = v.HoTen; old.SoDienThoai = v.SoDienThoai; old.NgayGioHen = v.NgayGioHen; old.BacSi = v.BacSi; old.LyDo = v.LyDo; old.TrangThai = v.TrangThai; source.ResetBindings(false); RefreshGrid(); }
        void DeleteItem() { var x = Selected(); if (x == null) return; if (MessageBox.Show($"Xóa lịch hẹn {x.MaLichHen}?", "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes) { data.Remove(x); RefreshGrid(); ClearForm(); } }
        void ClearForm() { errors.Clear(); txtMa.Clear(); txtTen.Clear(); txtSDT.Clear(); txtLyDo.Clear(); dtHen.Value = DateTime.Now.AddMinutes(30); cbBacSi.SelectedIndex = 0; cbTrangThai.SelectedIndex = 0; grid.ClearSelection(); }
        void LoadSelected() { var x = Selected(); if (x == null) return; txtMa.Text = x.MaLichHen; txtTen.Text = x.HoTen; txtSDT.Text = x.SoDienThoai; dtHen.Value = x.NgayGioHen < dtHen.MinDate ? dtHen.MinDate : x.NgayGioHen; cbBacSi.Text = x.BacSi; cbTrangThai.Text = x.TrangThai; txtLyDo.Text = x.LyDo; }
        void RefreshGrid()
        {
            var q = data.AsEnumerable();
            string s = txtTim.Text.Trim();
            if (s.Length > 0) q = q.Where(x => x.HoTen.Contains(s, StringComparison.OrdinalIgnoreCase) || x.SoDienThoai.Contains(s, StringComparison.OrdinalIgnoreCase));
            if (chkLocNgay.Checked) q = q.Where(x => x.NgayGioHen.Date == dtLoc.Value.Date);
            if (cbLocTrangThai.SelectedItem is string st && st != "Tất cả") q = q.Where(x => x.TrangThai == st);
            source.DataSource = new BindingList<Appointment>(q.ToList()); grid.DataSource = source;
            status.Text = $"Số lịch hẹn sau lọc: {q.Count()} / Tổng: {data.Count}";
        }
        void ExportCsv()
        {
            using var dlg = new SaveFileDialog { Filter = "CSV (*.csv)|*.csv", FileName = "LichHen.csv" };
            if (dlg.ShowDialog() != DialogResult.OK) return;
            var q = data.AsEnumerable(); string s = txtTim.Text.Trim();
            if (s.Length > 0) q = q.Where(x => x.HoTen.Contains(s, StringComparison.OrdinalIgnoreCase) || x.SoDienThoai.Contains(s, StringComparison.OrdinalIgnoreCase));
            if (chkLocNgay.Checked) q = q.Where(x => x.NgayGioHen.Date == dtLoc.Value.Date);
            if (cbLocTrangThai.SelectedItem is string st && st != "Tất cả") q = q.Where(x => x.TrangThai == st);
            var sb = new StringBuilder("MaLichHen,HoTen,SoDienThoai,NgayGioHen,BacSi,LyDo,TrangThai\r\n");
            foreach (var x in q) sb.AppendLine(string.Join(",", new[] { x.MaLichHen, x.HoTen, x.SoDienThoai, x.NgayGioHen.ToString("dd/MM/yyyy HH:mm"), x.BacSi, x.LyDo, x.TrangThai }.Select(Csv)));
            File.WriteAllText(dlg.FileName, "\uFEFF" + sb, Encoding.UTF8); MessageBox.Show("Đã xuất CSV.");
        }
        string Csv(string s) => "\"" + (s ?? "").Replace("\"", "\"\"") + "\"";
    }
}