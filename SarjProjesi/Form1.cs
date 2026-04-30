using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace SarjProjesi
{
    // ==========================================
    // 1. VERİ MODELLERİ
    // ==========================================
    public class Bolge
    {
        public int ID { get; set; }
        public int Satir { get; set; }
        public int Sutun { get; set; }
        public double AracYogunlugu { get; set; }
        public double AltyapiPuani { get; set; }
        public double Skor { get; set; }
        public bool EnUygun { get; set; } = false;
        public bool Riskli { get; set; } = false;
    }

    public class AnalizVerisi
    {
        public string Algoritma { get; set; }
        public double Sure { get; set; }
        public long IslemSayisi { get; set; }
        public double Bellek { get; set; }
        public double Skor { get; set; }
        public Color Renk { get; set; }
    }

    // ==========================================
    // 2. ANA FORM
    // ==========================================
    public partial class Form1 : Form
    {
        private int N = 20;
        private int TrafikYogunlugu = 50;
        private List<Bolge> Bolgeler = new List<Bolge>();
        private List<AnalizVerisi> Sonuclar = new List<AnalizVerisi>();
        private long anlikIslemSayaci = 0;

        private PictureBox haritaTuvali;
        private PictureBox[] grafikKutulari = new PictureBox[4];
        private DataGridView veriTablosu;
        private RichTextBox logEkrani;
        private Label lblBoyut, lblTrafik;

        public Form1()
        {
            this.Size = new Size(1600, 900);
            this.Text = "Elektrikli Araç Şarj İstasyonu Planlama";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.White;
            this.WindowState = FormWindowState.Maximized;

            ArayuzuKur();
            LogYaz("Sistem hazır.");
        }

        // ---------------------------------------------------------
        // ARAYÜZ OLUŞTURMA
        // ---------------------------------------------------------
        private void ArayuzuKur()
        {
            // Ana Bölücü
            SplitContainer anaBolucu = new SplitContainer();
            anaBolucu.Dock = DockStyle.Fill;
            anaBolucu.FixedPanel = FixedPanel.Panel1;

            // !!! DEĞİŞİKLİK BURADA !!!
            // 1. Genişliği 800 yaptık (Yazıların sığması için çok geniş)
            anaBolucu.SplitterDistance = 800;

            // 2. Kilidi kaldırdık (Elle tutup sağa sola çekebilirsin)
            anaBolucu.IsSplitterFixed = false;

            this.Controls.Add(anaBolucu);

            // --- SOL PANEL (KONTROL) ---
            Panel solPanel = anaBolucu.Panel1;
            solPanel.BackColor = Color.FromArgb(40, 44, 52);

            // TABLE LAYOUT
            TableLayoutPanel tlp = new TableLayoutPanel();
            tlp.Dock = DockStyle.Fill;
            tlp.Padding = new Padding(15);
            tlp.ColumnCount = 1;
            tlp.RowCount = 14;

            // Satır Yükseklikleri
            tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 8F));  // Başlık
            tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 4F));  // Ayarlar
            tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 4F));  // Boyut Lbl
            tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 5F));  // Boyut Slider
            tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 4F));  // Trafik Lbl
            tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 5F));  // Trafik Slider
            tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 9F));  // ÜRET BUTONU
            tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 5F));  // Algo Başlık
            tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 8F));  // Quick
            tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 8F));  // Merge
            tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 8F));  // Heap
            tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 5F));  // Renk Başlık
            tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 12F)); // Lejant
            tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 15F)); // Log

            solPanel.Controls.Add(tlp);

            // --- İÇERİKLER ---

            // 0. Başlık
            Label lblBaslik = new Label() { Text = "KONTROL PANELİ", ForeColor = Color.White, Font = new Font("Segoe UI", 24, FontStyle.Bold), TextAlign = ContentAlignment.MiddleCenter, Dock = DockStyle.Fill };
            tlp.Controls.Add(lblBaslik, 0, 0);

            // 1. Ayarlar Başlık
            tlp.Controls.Add(OlusturBaslik("HARİTA AYARLARI"), 0, 1);

            // 2-3. Boyut
            lblBoyut = OlusturBilgi("Harita Boyutu: 20x20");
            tlp.Controls.Add(lblBoyut, 0, 2);

            TrackBar tbBoyut = new TrackBar() { Minimum = 10, Maximum = 50, Value = 20, TickStyle = TickStyle.None, Dock = DockStyle.Fill, Cursor = Cursors.Hand };
            tbBoyut.ValueChanged += (s, e) => { N = tbBoyut.Value; lblBoyut.Text = $"Harita Boyutu: {N}x{N}"; };
            tlp.Controls.Add(tbBoyut, 0, 3);

            // 4-5. Trafik
            lblTrafik = OlusturBilgi("Trafik Yoğunluğu: %50");
            tlp.Controls.Add(lblTrafik, 0, 4);

            TrackBar tbTrafik = new TrackBar() { Minimum = 0, Maximum = 100, Value = 50, TickStyle = TickStyle.None, Dock = DockStyle.Fill, Cursor = Cursors.Hand };
            tbTrafik.ValueChanged += (s, e) => { TrafikYogunlugu = tbTrafik.Value; lblTrafik.Text = $"Trafik Yoğunluğu: %{TrafikYogunlugu}"; };
            tlp.Controls.Add(tbTrafik, 0, 5);

            // 6. Üret Butonu
            Button btnUret = OlusturButon("HARİTA OLUŞTUR", Color.Teal);
            btnUret.Click += BtnUret_Click;
            tlp.Controls.Add(btnUret, 0, 6);

            // 7. Algo Başlık
            tlp.Controls.Add(OlusturBaslik("ALGORİTMALAR"), 0, 7);

            // 8. Quick
            Button btnQuick = OlusturButon("QUICK SORT", Color.FromArgb(52, 152, 219));
            btnQuick.Click += (s, e) => AlgoritmaCalistir("Quick Sort", Color.FromArgb(52, 152, 219));
            tlp.Controls.Add(btnQuick, 0, 8);

            // 9. Merge
            Button btnMerge = OlusturButon("MERGE SORT", Color.FromArgb(155, 89, 182));
            btnMerge.Click += (s, e) => AlgoritmaCalistir("Merge Sort", Color.FromArgb(155, 89, 182));
            tlp.Controls.Add(btnMerge, 0, 9);

            // 10. Heap
            Button btnHeap = OlusturButon("HEAP SORT", Color.FromArgb(230, 126, 34));
            btnHeap.Click += (s, e) => AlgoritmaCalistir("Heap Sort", Color.FromArgb(230, 126, 34));
            tlp.Controls.Add(btnHeap, 0, 10);

            // 11. Renk Başlık
            tlp.Controls.Add(OlusturBaslik("RENK LEJANTI"), 0, 11);

            // 12. Lejant
            Panel pnlLejant = new Panel() { Dock = DockStyle.Fill };
            pnlLejant.Paint += PnlLejant_Paint;
            tlp.Controls.Add(pnlLejant, 0, 12);

            // 13. Log
            logEkrani = new RichTextBox();
            logEkrani.Dock = DockStyle.Fill;
            logEkrani.BackColor = Color.Black;
            logEkrani.ForeColor = Color.Lime;
            logEkrani.BorderStyle = BorderStyle.None;
            logEkrani.Font = new Font("Consolas", 10);
            tlp.Controls.Add(logEkrani, 0, 13);


            // --- SAĞ TARAFA DOKUNMADIM (AYNI KALDI) ---
            TabControl sekmeler = new TabControl();
            sekmeler.Dock = DockStyle.Fill;
            sekmeler.Font = new Font("Segoe UI", 12, FontStyle.Bold);
            anaBolucu.Panel2.Controls.Add(sekmeler);

            TabPage tabHarita = new TabPage("SİMÜLASYON HARİTASI");
            haritaTuvali = new PictureBox();
            haritaTuvali.Dock = DockStyle.Fill;
            haritaTuvali.BackColor = Color.White;
            haritaTuvali.Paint += HaritaTuvali_Paint;
            tabHarita.Controls.Add(haritaTuvali);
            sekmeler.TabPages.Add(tabHarita);

            TabPage tabGrafik = new TabPage("PERFORMANS ANALİZİ");
            tabGrafik.BackColor = Color.WhiteSmoke;

            SplitContainer grafikBolucu = new SplitContainer();
            grafikBolucu.Dock = DockStyle.Fill;
            grafikBolucu.Orientation = Orientation.Horizontal;
            grafikBolucu.SplitterDistance = 500;
            tabGrafik.Controls.Add(grafikBolucu);

            TableLayoutPanel grafikGrid = new TableLayoutPanel();
            grafikGrid.Dock = DockStyle.Fill;
            grafikGrid.ColumnCount = 2; grafikGrid.RowCount = 2;
            grafikGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            grafikGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            grafikGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            grafikGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));

            for (int i = 0; i < 4; i++)
            {
                grafikKutulari[i] = new PictureBox();
                grafikKutulari[i].Dock = DockStyle.Fill;
                grafikKutulari[i].BackColor = Color.White;
                grafikKutulari[i].BorderStyle = BorderStyle.FixedSingle;
                grafikGrid.Controls.Add(grafikKutulari[i]);
            }
            grafikKutulari[0].Paint += (s, e) => CizGrafik(e.Graphics, "Çalışma Süresi (ms)", x => x.Sure, "ms", ((Control)s).Size);
            grafikKutulari[1].Paint += (s, e) => CizGrafik(e.Graphics, "İşlem Karmaşıklığı", x => (double)x.IslemSayisi, "", ((Control)s).Size);
            grafikKutulari[2].Paint += (s, e) => CizGrafik(e.Graphics, "Bellek Kullanımı (KB)", x => x.Bellek, "KB", ((Control)s).Size);
            grafikKutulari[3].Paint += (s, e) => CizGrafik(e.Graphics, "Verimlilik Skoru", x => x.Skor, "Puan", ((Control)s).Size);
            grafikBolucu.Panel1.Controls.Add(grafikGrid);

            veriTablosu = new DataGridView();
            veriTablosu.Dock = DockStyle.Fill;
            veriTablosu.BackgroundColor = Color.FromArgb(30, 30, 30);
            veriTablosu.ForeColor = Color.Black;
            veriTablosu.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            veriTablosu.Columns.Add("Algo", "Algoritma");
            veriTablosu.Columns.Add("Boyut", "Boyut");
            veriTablosu.Columns.Add("Sure", "Süre (ms)");
            veriTablosu.Columns.Add("Islem", "İşlem");
            veriTablosu.Columns.Add("Bellek", "Bellek (KB)");
            veriTablosu.Columns.Add("Skor", "Skor");
            grafikBolucu.Panel2.Controls.Add(veriTablosu);
            sekmeler.TabPages.Add(tabGrafik);
        }

        // ---------------------------------------------------------
        // YARDIMCI METOTLAR
        // ---------------------------------------------------------
        private Label OlusturBaslik(string text)
        {
            return new Label() { Text = text, ForeColor = Color.DarkGray, Font = new Font("Segoe UI", 12, FontStyle.Bold), TextAlign = ContentAlignment.BottomLeft, Dock = DockStyle.Fill };
        }
        private Label OlusturBilgi(string text)
        {
            return new Label() { Text = text, ForeColor = Color.WhiteSmoke, Font = new Font("Segoe UI", 11), TextAlign = ContentAlignment.BottomLeft, Dock = DockStyle.Fill };
        }
        private Button OlusturButon(string text, Color bg)
        {
            Button b = new Button() { Text = text, BackColor = bg, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 12, FontStyle.Bold), Cursor = Cursors.Hand, Dock = DockStyle.Fill };
            b.FlatAppearance.BorderSize = 0;
            return b;
        }
        private void LogYaz(string m)
        {
            logEkrani.AppendText($"> {m}\n"); logEkrani.ScrollToCaret();
        }

        // ---------------------------------------------------------
        // İŞ MANTIĞI & ALGORİTMALAR
        // ---------------------------------------------------------
        private void BtnUret_Click(object sender, EventArgs e)
        {
            Bolgeler.Clear(); Sonuclar.Clear(); veriTablosu.Rows.Clear();
            foreach (var pb in grafikKutulari) pb.Invalidate();

            Random rnd = new Random();
            int taban = TrafikYogunlugu * 20;

            for (int i = 0; i < N; i++) for (int j = 0; j < N; j++)
                {
                    Bolge b = new Bolge() { ID = i * N + j, Satir = i, Sutun = j };
                    b.AracYogunlugu = taban + rnd.Next(0, 500);
                    b.AltyapiPuani = rnd.Next(10, 100);
                    b.Skor = (b.AracYogunlugu * 0.7) + (b.AltyapiPuani * 0.3);
                    Bolgeler.Add(b);
                }
            haritaTuvali.Invalidate();
            LogYaz($"{N}x{N} Harita oluşturuldu.");
        }

        private void AlgoritmaCalistir(string ad, Color c)
        {
            if (Bolgeler.Count == 0) { MessageBox.Show("Önce harita oluştur!"); return; }
            foreach (var b in Bolgeler) { b.EnUygun = false; b.Riskli = false; }
            List<Bolge> kopya = new List<Bolge>(Bolgeler);
            anlikIslemSayaci = 0;

            Stopwatch sw = Stopwatch.StartNew();
            if (ad.Contains("Quick")) QuickSort(kopya, 0, kopya.Count - 1);
            else if (ad.Contains("Merge")) MergeSort(kopya, 0, kopya.Count - 1);
            else if (ad.Contains("Heap")) HeapSort(kopya);
            sw.Stop();

            int k = Math.Max(1, kopya.Count / 20);
            double tSkor = 0;
            for (int i = 0; i < k; i++)
            {
                var b = Bolgeler.First(x => x.ID == kopya[i].ID);
                b.EnUygun = true; tSkor += b.Skor;
            }
            for (int i = kopya.Count - k; i < kopya.Count; i++) Bolgeler.First(x => x.ID == kopya[i].ID).Riskli = true;

            var res = new AnalizVerisi { Algoritma = ad, Sure = sw.Elapsed.TotalMilliseconds, IslemSayisi = anlikIslemSayaci, Bellek = anlikIslemSayaci * 0.05, Skor = tSkor, Renk = c };
            var eski = Sonuclar.FirstOrDefault(x => x.Algoritma == ad);
            if (eski != null) Sonuclar.Remove(eski);
            Sonuclar.Add(res);

            veriTablosu.Rows.Add(ad, $"{N}x{N}", $"{res.Sure:F3}", res.IslemSayisi, $"{res.Bellek:F1}", $"{res.Skor:F0}");
            LogYaz($"{ad} bitti: {res.Sure:F3} ms");

            haritaTuvali.Invalidate();
            foreach (var pb in grafikKutulari) pb.Invalidate();
        }

        // ---------------------------------------------------------
        // ÇİZİM & GÖRSELLEŞTİRME
        // ---------------------------------------------------------
        private void HaritaTuvali_Paint(object sender, PaintEventArgs e)
        {
            if (Bolgeler.Count == 0) return;
            Graphics g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
            int pad = 40;
            float cw = (float)(haritaTuvali.Width - 2 * pad) / (N - 1);
            float ch = (float)(haritaTuvali.Height - 2 * pad) / (N - 1);

            using (Pen p = new Pen(Color.LightGray))
            {
                for (int i = 0; i < N; i++)
                {
                    g.DrawLine(p, pad + i * cw, pad, pad + i * cw, haritaTuvali.Height - pad);
                    g.DrawLine(p, pad, pad + i * ch, haritaTuvali.Width - pad, pad + i * ch);
                }
            }
            foreach (var b in Bolgeler)
            {
                float x = pad + b.Sutun * cw; float y = pad + b.Satir * ch;
                if (b.EnUygun) { g.FillEllipse(Brushes.LimeGreen, x - 8, y - 8, 16, 16); g.DrawEllipse(Pens.DarkGreen, x - 8, y - 8, 16, 16); }
                else if (b.Riskli) g.FillEllipse(Brushes.Crimson, x - 5, y - 5, 10, 10);
                else g.FillEllipse(Brushes.CornflowerBlue, x - 4, y - 4, 8, 8);
            }
        }

        private void CizGrafik(Graphics g, string baslik, Func<AnalizVerisi, double> secici, string birim, Size sz)
        {
            g.Clear(Color.White); g.SmoothingMode = SmoothingMode.AntiAlias;
            g.DrawString(baslik, new Font("Segoe UI", 10, FontStyle.Bold), Brushes.Black, 20, 10);
            int m = 30; int w = sz.Width - 2 * m; int h = sz.Height - 2 * m;
            using (Pen p = new Pen(Color.LightGray) { DashStyle = DashStyle.Dash })
            {
                g.DrawLine(p, m, m, m, sz.Height - m); g.DrawLine(p, m, sz.Height - m, sz.Width - m, sz.Height - m);
            }
            if (Sonuclar.Count == 0) return;
            double max = Sonuclar.Max(secici); if (max == 0) max = 1;
            int barW = (w / Sonuclar.Count) - 30; if (barW > 60) barW = 60;
            for (int i = 0; i < Sonuclar.Count; i++)
            {
                var s = Sonuclar[i]; double val = secici(s);
                int barH = (int)((val / max) * h); if (barH < 5) barH = 5;
                int x = m + 20 + i * (barW + 30); int y = sz.Height - m - barH;
                using (SolidBrush br = new SolidBrush(s.Renk)) g.FillRectangle(br, x, y, barW, barH);
                g.DrawRectangle(Pens.Black, x, y, barW, barH);
                g.DrawString($"{val:F0}{birim}", new Font("Arial", 9, FontStyle.Bold), Brushes.Black, x, y - 15);
                g.DrawString(s.Algoritma.Split(' ')[0], new Font("Arial", 9, FontStyle.Bold), Brushes.Gray, x, sz.Height - m + 5);
            }
        }

        private void PnlLejant_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
            int h = ((Control)sender).Height;
            int adim = h / 3;
            int y = adim / 2 - 8;

            g.FillEllipse(Brushes.LimeGreen, 10, y, 15, 15); g.DrawString("En Uygun (Yeşil)", new Font("Segoe UI", 10), Brushes.LightGray, 35, y - 2); y += adim;
            g.FillEllipse(Brushes.Crimson, 10, y, 15, 15); g.DrawString("Riskli / Yoğun", new Font("Segoe UI", 10), Brushes.LightGray, 35, y - 2); y += adim;
            g.FillEllipse(Brushes.CornflowerBlue, 10, y, 15, 15); g.DrawString("Standart", new Font("Segoe UI", 10), Brushes.LightGray, 35, y - 2);
        }

        // ---------------------------------------------------------
        // SIRALAMA ALGORİTMALARI
        // ---------------------------------------------------------
        private void QuickSort(List<Bolge> list, int low, int high)
        {
            if (low < high) { int pi = Partition(list, low, high); QuickSort(list, low, pi - 1); QuickSort(list, pi + 1, high); }
        }
        private int Partition(List<Bolge> list, int low, int high)
        {
            double pivot = list[high].Skor; int i = (low - 1);
            for (int j = low; j < high; j++) { anlikIslemSayaci++; if (list[j].Skor > pivot) { i++; Swap(list, i, j); } }
            Swap(list, i + 1, high); return i + 1;
        }
        private void MergeSort(List<Bolge> list, int l, int r)
        {
            if (l < r) { int m = l + (r - l) / 2; MergeSort(list, l, m); MergeSort(list, m + 1, r); Merge(list, l, m, r); }
        }
        private void Merge(List<Bolge> list, int l, int m, int r)
        {
            int n1 = m - l + 1, n2 = r - m; Bolge[] L = new Bolge[n1], R = new Bolge[n2];
            for (int i = 0; i < n1; ++i) { L[i] = list[l + i]; anlikIslemSayaci++; }
            for (int j = 0; j < n2; ++j) { R[j] = list[m + 1 + j]; anlikIslemSayaci++; }
            int k = l, i_ = 0, j_ = 0;
            while (i_ < n1 && j_ < n2) { anlikIslemSayaci++; if (L[i_].Skor >= R[j_].Skor) list[k++] = L[i_++]; else list[k++] = R[j_++]; }
            while (i_ < n1) list[k++] = L[i_++]; while (j_ < n2) list[k++] = R[j_++];
        }
        private void HeapSort(List<Bolge> list)
        {
            int n = list.Count; for (int i = n / 2 - 1; i >= 0; i--) Heapify(list, n, i);
            for (int i = n - 1; i > 0; i--) { Swap(list, 0, i); Heapify(list, i, 0); }
            list.Reverse();
        }
        private void Heapify(List<Bolge> list, int n, int i)
        {
            int largest = i, l = 2 * i + 1, r = 2 * i + 2; anlikIslemSayaci++;
            if (l < n && list[l].Skor > list[largest].Skor) largest = l;
            if (r < n && list[r].Skor > list[largest].Skor) largest = r;
            if (largest != i) { Swap(list, i, largest); Heapify(list, n, largest); }
        }
        private void Swap(List<Bolge> list, int i, int j) { anlikIslemSayaci++; Bolge t = list[i]; list[i] = list[j]; list[j] = t; }
    }
}