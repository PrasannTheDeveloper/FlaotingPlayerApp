using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FloatingPlayer
{
    public partial class Form1 : Form
    {
        private TabControl tabControl;
        private FlowLayoutPanel navPanel;
        private List<CustomWebsite> customWebsites;
        private Dictionary<string, string> defaultWebsiteIcons;
        private string settingsPath;
        private string iconsSettingsPath;

        public Form1()
        {
            InitializeComponent();
            this.Text = "Floating Player";
            this.Size = new Size(1000, 700);
            this.TopMost = true;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.SetStyle(ControlStyles.ResizeRedraw, true);
            this.ShowInTaskbar = true; 

         
            var appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FloatingPlayer");
            Directory.CreateDirectory(appDataPath);

            settingsPath = Path.Combine(appDataPath, "settings.json");
            iconsSettingsPath = Path.Combine(appDataPath, "icons_settings.json");

            LoadCustomWebsites();
            LoadDefaultWebsiteIcons();
            InitializeLayout();
        }

        private async void Form1_Load(object sender, EventArgs e)
        {
            await OpenNewTab("YouTube", "https://www.youtube.com", GetWebsiteIcon("YouTube"));
        }

        private void InitializeLayout()
        {

            var mainPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
            };
            mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); 
            mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 60)); 

         
            tabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                DrawMode = TabDrawMode.OwnerDrawFixed,
                SizeMode = TabSizeMode.Fixed,
                ItemSize = new Size(150, 30)
            };
            tabControl.SelectedIndexChanged += TabControl_SelectedIndexChanged;
            tabControl.DrawItem += TabControl_DrawItem;
            tabControl.MouseClick += TabControl_MouseClick;

      
            navPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                Width = 60,
                BackColor = Color.FromArgb(45, 45, 48),
                Padding = new Padding(2),
                AutoScroll = true
            };

            LoadNavigationButtons();

            mainPanel.Controls.Add(tabControl, 0, 0);
            mainPanel.Controls.Add(navPanel, 1, 0);
            this.Controls.Add(mainPanel);
        }

        private void LoadNavigationButtons()
        {
            navPanel.Controls.Clear();

        
            AddNavButton(navPanel, "YouTube", "https://www.youtube.com", GetWebsiteIcon("YouTube"));
            AddNavButton(navPanel, "ChatGPT", "https://chat.openai.com", GetWebsiteIcon("ChatGPT"));
            AddNavButton(navPanel, "Google", "https://www.google.com", GetWebsiteIcon("Google"));
            AddNavButton(navPanel, "LinkedIn", "https://www.linkedin.com", GetWebsiteIcon("LinkedIn"));
            AddNavButton(navPanel, "Instagram", "https://www.instagram.com", GetWebsiteIcon("Instagram"));
            AddNavButton(navPanel, "WhatsApp", "https://web.whatsapp.com", GetWebsiteIcon("WhatsApp"));

            var separator = new Panel
            {
                Height = 1,
                Width = navPanel.Width - 10,
                BackColor = Color.Gray,
                Margin = new Padding(3, 10, 3, 10)
            };
            navPanel.Controls.Add(separator);

           
            foreach (var website in customWebsites)
            {
                AddNavButton(navPanel, website.Name, website.Url, GetCustomIcon(website.IconPath));
            }

        
            var separator2 = new Panel
            {
                Height = 1,
                Width = navPanel.Width - 10,
                BackColor = Color.Gray,
                Margin = new Padding(3, 10, 3, 10)
            };
            navPanel.Controls.Add(separator2);

            AddSettingsButton(navPanel);
        }

        private void AddNavButton(Control parent, string label, string url, Image icon)
        {
            var btn = new Button
            {
                Text = "",
                Width = 54, 
                Height = 45,
                BackColor = Color.FromArgb(62, 62, 66),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Margin = new Padding(3),
                Image = ResizeImage(icon, 24, 24),
                ImageAlign = ContentAlignment.MiddleCenter,
                TextAlign = ContentAlignment.BottomCenter,
                UseVisualStyleBackColor = false,
                Tag = new { Label = label, Url = url, Icon = icon }
            };

            btn.FlatAppearance.BorderColor = Color.FromArgb(85, 85, 85);
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(75, 75, 78);
            btn.FlatAppearance.MouseDownBackColor = Color.FromArgb(90, 90, 95);

        
            var tooltip = new ToolTip();
            tooltip.SetToolTip(btn, label);

            btn.Click += async (s, e) =>
            {
                var tag = (dynamic)btn.Tag;
                await OpenNewTab(tag.Label, tag.Url, tag.Icon);
            };

            var contextMenu = new ContextMenuStrip();

            var openInNewTabItem = new ToolStripMenuItem("Open in New Tab");
            openInNewTabItem.Click += async (s, e) =>
            {
                var tag = (dynamic)btn.Tag;
                await OpenNewTab(tag.Label, tag.Url, tag.Icon);
            };
            contextMenu.Items.Add(openInNewTabItem);

            var openInNewWindowItem = new ToolStripMenuItem("Open in New Window");
            openInNewWindowItem.Click += async (s, e) =>
            {
                var tag = (dynamic)btn.Tag;
                await OpenInNewWindow(tag.Label, tag.Url, tag.Icon);
            };
            contextMenu.Items.Add(openInNewWindowItem);

            if (IsDefaultWebsite(label))
            {
                var changeIconItem = new ToolStripMenuItem("Change Icon");
                changeIconItem.Click += (s, e) => ChangeDefaultWebsiteIcon(label);
                contextMenu.Items.Add(changeIconItem);

                var resetIconItem = new ToolStripMenuItem("Reset Icon");
                resetIconItem.Click += (s, e) => ResetDefaultWebsiteIcon(label);
                contextMenu.Items.Add(resetIconItem);
            }

            if (customWebsites.Any(w => w.Name == label))
            {
                contextMenu.Items.Add(new ToolStripSeparator());
                var deleteItem = new ToolStripMenuItem("Remove");
                deleteItem.Click += (s, e) => RemoveCustomWebsite(label);
                contextMenu.Items.Add(deleteItem);
            }

            btn.ContextMenuStrip = contextMenu;
            parent.Controls.Add(btn);
        }

        private bool IsDefaultWebsite(string name)
        {
            return name switch
            {
                "YouTube" or "ChatGPT" or "Google" or "LinkedIn" or "Instagram" or "WhatsApp" => true,
                _ => false
            };
        }

        private void ChangeDefaultWebsiteIcon(string websiteName)
        {
            using (var openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "Image files (*.png;*.jpg;*.jpeg;*.gif;*.bmp;*.ico)|*.png;*.jpg;*.jpeg;*.gif;*.bmp;*.ico|All files (*.*)|*.*";
                openFileDialog.Title = $"Select Icon for {websiteName}";

                if (openFileDialog.ShowDialog(this) == DialogResult.OK)
                {
                    defaultWebsiteIcons[websiteName] = openFileDialog.FileName;
                    SaveDefaultWebsiteIcons();
                    LoadNavigationButtons();
                }
            }
        }

        private void ResetDefaultWebsiteIcon(string websiteName)
        {
            if (defaultWebsiteIcons.ContainsKey(websiteName))
            {
                defaultWebsiteIcons.Remove(websiteName);
                SaveDefaultWebsiteIcons();
                LoadNavigationButtons();
            }
        }

        private void AddSettingsButton(Control parent)
        {
            var btn = new Button
            {
                Text = "",
                Width = 54, 
                Height = 45,
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Margin = new Padding(3),
                Image = ResizeImage(GetSettingsIcon(), 24, 24),
                ImageAlign = ContentAlignment.MiddleCenter,
                UseVisualStyleBackColor = false
            };

            btn.FlatAppearance.BorderColor = Color.FromArgb(0, 100, 195);
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(0, 140, 235);

            var tooltip = new ToolTip();
            tooltip.SetToolTip(btn, "Settings");

          
            var contextMenu = new ContextMenuStrip();

            var addWebsiteItem = new ToolStripMenuItem("Add Website");
            addWebsiteItem.Click += (s, e) => ShowAddWebsiteDialog();
            contextMenu.Items.Add(addWebsiteItem);

            var customizeIconsItem = new ToolStripMenuItem("Customize Icons");
            customizeIconsItem.Click += (s, e) => ShowCustomizeIconsDialog();
            contextMenu.Items.Add(customizeIconsItem);

            var resetAllIconsItem = new ToolStripMenuItem("Reset All Icons");
            resetAllIconsItem.Click += (s, e) => ResetAllDefaultIcons();
            contextMenu.Items.Add(resetAllIconsItem);

            var toggleTopMostItem = new ToolStripMenuItem("Toggle Always on Top");
            toggleTopMostItem.Click += (s, e) => ToggleTopMost();
            contextMenu.Items.Add(toggleTopMostItem);

            btn.Click += (s, e) => contextMenu.Show(btn, new Point(0, btn.Height));
            parent.Controls.Add(btn);
        }

        private void ToggleTopMost()
        {
            this.TopMost = !this.TopMost;
            MessageBox.Show($"Always on top: {(this.TopMost ? "Enabled" : "Disabled")}",
                "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ShowCustomizeIconsDialog()
        {
            var dialog = new CustomizeIconsDialog(this, defaultWebsiteIcons);
            dialog.IconChanged += (websiteName, iconPath) =>
            {
                if (string.IsNullOrEmpty(iconPath))
                {
                    ResetDefaultWebsiteIcon(websiteName);
                }
                else
                {
                    defaultWebsiteIcons[websiteName] = iconPath;
                    SaveDefaultWebsiteIcons();
                    LoadNavigationButtons();
                }
            };
            dialog.ShowDialog();
        }

        private void ResetAllDefaultIcons()
        {
            if (MessageBox.Show("Reset all default website icons to their original state?",
                "Confirm Reset", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                defaultWebsiteIcons.Clear();
                SaveDefaultWebsiteIcons();
                LoadNavigationButtons();
            }
        }

        private async Task OpenNewTab(string label, string url, Image icon)
        {
            try
            {
             
                foreach (TabPage tab in tabControl.TabPages)
                {
                    if (tab.Text == label)
                    {
                        tabControl.SelectedTab = tab;
                        return;
                    }
                }

                var tabPage = new TabPage(label);
                tabPage.Tag = new { Icon = icon, Url = url };

                var webView = new WebView2
                {
                    Dock = DockStyle.Fill
                };
                tabPage.Controls.Add(webView);
                tabControl.TabPages.Add(tabPage);
                tabControl.SelectedTab = tabPage;

                var env = await CoreWebView2Environment.CreateAsync(null,
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FloatingPlayer", "WebView2Data"));
                await webView.EnsureCoreWebView2Async(env);
                webView.Source = new Uri(url);


                webView.CoreWebView2.ContextMenuRequested += (s, e) =>
                {
                    var openInNewWindow = webView.CoreWebView2.Environment.CreateContextMenuItem(
                        "Open in New Window", null, CoreWebView2ContextMenuItemKind.Command);
                    openInNewWindow.CustomItemSelected += async (sender, args) =>
                    {
                        await OpenInNewWindow(label, webView.Source.ToString(), icon);
                    };
                    e.MenuItems.Insert(0, openInNewWindow);
                };
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening tab: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task OpenInNewWindow(string title, string url, Image icon)
        {
            var newForm = new FloatingWindow(title, url, icon, this.TopMost);
            newForm.Show();
        }

        private void TabControl_DrawItem(object sender, DrawItemEventArgs e)
        {
            var tabControl = sender as TabControl;
            var tabPage = tabControl.TabPages[e.Index];
            var tabRect = tabControl.GetTabRect(e.Index);

     
            var brush = e.State == DrawItemState.Selected ?
                new SolidBrush(Color.FromArgb(0, 120, 215)) :
                new SolidBrush(Color.FromArgb(45, 45, 48));
            e.Graphics.FillRectangle(brush, tabRect);

    
            if (tabPage.Tag != null)
            {
                var tag = (dynamic)tabPage.Tag;
                if (tag.Icon != null)
                {
                    var iconRect = new Rectangle(tabRect.X + 5, tabRect.Y + 5, 20, 20);
                    e.Graphics.DrawImage(tag.Icon, iconRect);
                }
            }

           
            var textRect = new Rectangle(tabRect.X + 30, tabRect.Y, tabRect.Width - 50, tabRect.Height);
            var textBrush = new SolidBrush(Color.White);
            var stringFormat = new StringFormat
            {
                Alignment = StringAlignment.Near,
                LineAlignment = StringAlignment.Center
            };
            e.Graphics.DrawString(tabPage.Text, Font, textBrush, textRect, stringFormat);


            var closeRect = new Rectangle(tabRect.Right - 20, tabRect.Y + 5, 15, 15);
            e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(200, 100, 100)), closeRect);
            e.Graphics.DrawString("×", new Font(Font.FontFamily, 8, FontStyle.Bold),
                new SolidBrush(Color.White), closeRect, new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                });

            brush.Dispose();
            textBrush.Dispose();
        }

        private void TabControl_MouseClick(object sender, MouseEventArgs e)
        {
            for (int i = 0; i < tabControl.TabPages.Count; i++)
            {
                var tabRect = tabControl.GetTabRect(i);
                var closeRect = new Rectangle(tabRect.Right - 20, tabRect.Y + 5, 15, 15);

                if (closeRect.Contains(e.Location))
                {
                    tabControl.TabPages.RemoveAt(i);
                    break;
                }
            }
        }

        private void ShowAddWebsiteDialog()
        {
            try
            {
                var dialog = new AddWebsiteDialog(this);
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    var website = new CustomWebsite
                    {
                        Name = dialog.WebsiteName,
                        Url = dialog.WebsiteUrl,
                        IconPath = dialog.IconPath
                    };
                    customWebsites.Add(website);
                    SaveCustomWebsites();
                    LoadNavigationButtons();

                    MessageBox.Show($"Website '{website.Name}' added successfully!", "Success",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error showing add website dialog: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RemoveCustomWebsite(string name)
        {
            if (MessageBox.Show($"Are you sure you want to remove '{name}'?",
                "Confirm Removal", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                customWebsites.RemoveAll(w => w.Name == name);
                SaveCustomWebsites();
                LoadNavigationButtons();
            }
        }

        private void LoadCustomWebsites()
        {
            try
            {
                if (File.Exists(settingsPath))
                {
                    var json = File.ReadAllText(settingsPath);
                    customWebsites = JsonSerializer.Deserialize<List<CustomWebsite>>(json) ?? new List<CustomWebsite>();
                }
                else
                {
                    customWebsites = new List<CustomWebsite>();
                }
            }
            catch
            {
                customWebsites = new List<CustomWebsite>();
            }
        }

        private void LoadDefaultWebsiteIcons()
        {
            try
            {
                if (File.Exists(iconsSettingsPath))
                {
                    var json = File.ReadAllText(iconsSettingsPath);
                    defaultWebsiteIcons = JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new Dictionary<string, string>();
                }
                else
                {
                    defaultWebsiteIcons = new Dictionary<string, string>();
                }
            }
            catch
            {
                defaultWebsiteIcons = new Dictionary<string, string>();
            }
        }

        private void SaveCustomWebsites()
        {
            try
            {
                var json = JsonSerializer.Serialize(customWebsites, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(settingsPath, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving settings: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SaveDefaultWebsiteIcons()
        {
            try
            {
                var json = JsonSerializer.Serialize(defaultWebsiteIcons, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(iconsSettingsPath, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving icon settings: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void TabControl_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (tabControl.SelectedTab != null)
            {
                var webView = tabControl.SelectedTab.Controls.OfType<WebView2>().FirstOrDefault();
                webView?.Focus();
            }
        }


        internal Image GetWebsiteIcon(string websiteName)
        {
       
            if (defaultWebsiteIcons.TryGetValue(websiteName, out string customIconPath) && File.Exists(customIconPath))
            {
                try
                {
                    var originalImage = Image.FromFile(customIconPath);
                    return ResizeImage(originalImage, 32, 32);
                }
                catch { }
            }

            return websiteName switch
            {
                "YouTube" => GetYouTubeIcon(),
                "ChatGPT" => GetChatGPTIcon(),
                "Google" => GetGoogleIcon(),
                "LinkedIn" => GetLinkedInIcon(),
                "Instagram" => GetInstagramIcon(),
                "WhatsApp" => GetWhatsAppIcon(),
                _ => GetDefaultIcon()
            };
        }

        private Image GetCustomIcon(string iconPath)
        {
            try
            {
                if (!string.IsNullOrEmpty(iconPath) && File.Exists(iconPath))
                {
                    var originalImage = Image.FromFile(iconPath);
                    return ResizeImage(originalImage, 32, 32);
                }
            }
            catch { }
            return GetDefaultIcon();
        }

        private Image GetYouTubeIcon() => CreateIconWithText("YT", Color.Red);
        private Image GetChatGPTIcon() => CreateIconWithText("AI", Color.Green);
        private Image GetGoogleIcon() => CreateIconWithText("G", Color.Blue);
        private Image GetLinkedInIcon() => CreateIconWithText("in", Color.FromArgb(0, 119, 181));
        private Image GetInstagramIcon() => CreateIconWithText("IG", Color.FromArgb(225, 48, 108));
        private Image GetWhatsAppIcon() => CreateIconWithText("WA", Color.FromArgb(37, 211, 102));
        private Image GetSettingsIcon() => CreateSettingsGearIcon();
        public Image GetDefaultIcon() => CreateIconWithText("?", Color.DarkGray);

        private Image CreateIconWithText(string text, Color backgroundColor)
        {
            var bitmap = new Bitmap(48, 48);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

            
                using (var brush = new SolidBrush(backgroundColor))
                {
                    g.FillEllipse(brush, 2, 2, 44, 44);
                }

                using (var pen = new Pen(Color.White, 2))
                {
                    g.DrawEllipse(pen, 2, 2, 44, 44);
                }

                using (var font = new Font("Arial", text.Length > 1 ? 12 : 16, FontStyle.Bold))
                using (var brush = new SolidBrush(Color.White))
                {
                    var stringFormat = new StringFormat
                    {
                        Alignment = StringAlignment.Center,
                        LineAlignment = StringAlignment.Center
                    };
                    g.DrawString(text, font, brush, new RectangleF(0, 0, 48, 48), stringFormat);
                }
            }
            return bitmap;
        }

        private Image CreateSettingsGearIcon()
        {
            var bitmap = new Bitmap(48, 48);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

               
                using (var brush = new SolidBrush(Color.Gray))
                using (var pen = new Pen(Color.White, 2))
                {
                  
                    g.FillEllipse(brush, 4, 4, 40, 40);
                    g.DrawEllipse(pen, 4, 4, 40, 40);

                    g.FillEllipse(new SolidBrush(Color.White), 18, 18, 12, 12);

                    
                    for (int i = 0; i < 8; i++)
                    {
                        double angle = i * Math.PI / 4;
                        int x1 = (int)(24 + 20 * Math.Cos(angle));
                        int y1 = (int)(24 + 20 * Math.Sin(angle));
                        int x2 = (int)(24 + 24 * Math.Cos(angle));
                        int y2 = (int)(24 + 24 * Math.Sin(angle));
                        g.DrawLine(new Pen(Color.White, 3), x1, y1, x2, y2);
                    }
                }
            }
            return bitmap;
        }

        internal Image ResizeImage(Image originalImage, int width, int height)
        {
            if (originalImage == null) return null;

            var resizedImage = new Bitmap(width, height);
            using (var g = Graphics.FromImage(resizedImage))
            {
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;

                g.DrawImage(originalImage, 0, 0, width, height);
            }
            return resizedImage;
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Left)
            {
                NativeMethods.ReleaseCapture();
                NativeMethods.SendMessage(Handle, NativeMethods.WM_NCLBUTTONDOWN, NativeMethods.HT_CAPTION, 0);
            }
        }

        protected override void WndProc(ref Message m)
        {
            const int HTBOTTOMRIGHT = 17;
            const int HTCLIENT = 1;
            const int WM_NCHITTEST = 0x84;

            base.WndProc(ref m);

            if (m.Msg == WM_NCHITTEST && (int)m.Result == HTCLIENT)
            {
                var cursor = this.PointToClient(Cursor.Position);
                if (cursor.X >= this.ClientSize.Width - 10 && cursor.Y >= this.ClientSize.Height - 10)
                {
                    m.Result = (IntPtr)HTBOTTOMRIGHT;
                }
            }
        }
    }


    public class FloatingWindow : Form
    {
        private WebView2 webView;
        private string url;

        public FloatingWindow(string title, string url, Image icon, bool topMost)
        {
            this.Text = title;
            this.Size = new Size(1000, 700);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.TopMost = topMost;
            this.Icon = SystemIcons.Application;
            this.url = url;

            InitializeWebView();
        }

        private async void InitializeWebView()
        {
            webView = new WebView2
            {
                Dock = DockStyle.Fill
            };
            this.Controls.Add(webView);

            try
            {
                var env = await CoreWebView2Environment.CreateAsync(null,
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FloatingPlayer", "WebView2Data"));
                await webView.EnsureCoreWebView2Async(env);
                webView.Source = new Uri(url);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading web content: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

     
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Left)
            {
                NativeMethods.ReleaseCapture();
                NativeMethods.SendMessage(Handle, NativeMethods.WM_NCLBUTTONDOWN, NativeMethods.HT_CAPTION, 0);
            }
        }

        protected override void WndProc(ref Message m)
        {
            const int HTBOTTOMRIGHT = 17;
            const int HTCLIENT = 1;
            const int WM_NCHITTEST = 0x84;

            base.WndProc(ref m);

            if (m.Msg == WM_NCHITTEST && (int)m.Result == HTCLIENT)
            {
                var cursor = this.PointToClient(Cursor.Position);
                if (cursor.X >= this.ClientSize.Width - 10 && cursor.Y >= this.ClientSize.Height - 10)
                {
                    m.Result = (IntPtr)HTBOTTOMRIGHT;
                }
            }
        }
    }

    public class CustomizeIconsDialog : Form
    {
        private Form1 parentForm;
        private Dictionary<string, string> websiteIcons;
        private ListBox websiteList;
        private PictureBox iconPreview;
        private Button changeIconButton;
        private Button resetIconButton;

        public event Action<string, string> IconChanged;

        public CustomizeIconsDialog(Form1 parent, Dictionary<string, string> icons)
        {
            parentForm = parent;
            websiteIcons = new Dictionary<string, string>(icons);
            InitializeDialog();
            Load += CustomizeIconsDialog_Load;
        }

        private void InitializeDialog()
        {
            this.Text = "Customize Website Icons";
            this.Size = new Size(500, 400);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.TopMost = parentForm.TopMost; 

            var mainPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 3,
                Padding = new Padding(10)
            };
            mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
            mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 70));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 20));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 10));

            websiteList = new ListBox
            {
                Dock = DockStyle.Fill,
                SelectionMode = SelectionMode.One
            };
            websiteList.SelectedIndexChanged += WebsiteList_SelectedIndexChanged;
            mainPanel.Controls.Add(websiteList, 0, 0);

            var previewPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.FixedSingle
            };
            iconPreview = new PictureBox
            {
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.CenterImage
            };
            previewPanel.Controls.Add(iconPreview);
            mainPanel.Controls.Add(previewPanel, 1, 0);

            var buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false
            };
            changeIconButton = new Button
            {
                Text = "Change Icon",
                Enabled = false,
                Width = 100
            };
            changeIconButton.Click += ChangeIconButton_Click;
            buttonPanel.Controls.Add(changeIconButton);

            resetIconButton = new Button
            {
                Text = "Reset Icon",
                Enabled = false,
                Width = 100
            };
            resetIconButton.Click += ResetIconButton_Click;
            buttonPanel.Controls.Add(resetIconButton);
            mainPanel.Controls.Add(buttonPanel, 0, 1);
            mainPanel.SetColumnSpan(buttonPanel, 2);

       
            var okButton = new Button
            {
                Text = "OK",
                Dock = DockStyle.Right,
                Width = 80
            };
            okButton.Click += (s, e) => this.DialogResult = DialogResult.OK;
            mainPanel.Controls.Add(okButton, 1, 2);

            this.Controls.Add(mainPanel);
        }

        private void CustomizeIconsDialog_Load(object sender, EventArgs e)
        {
        
            websiteList.Items.AddRange(new object[] {
                "YouTube",
                "ChatGPT",
                "Google",
                "LinkedIn",
                "Instagram",
                "WhatsApp"
            });

         
            if (websiteList.Items.Count > 0)
            {
                websiteList.SelectedIndex = 0;
            }
        }

        private void WebsiteList_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (websiteList.SelectedItem == null) return;

            string websiteName = websiteList.SelectedItem.ToString();
            bool hasCustomIcon = websiteIcons.TryGetValue(websiteName, out string iconPath);

        
            iconPreview.Image = hasCustomIcon && File.Exists(iconPath)
                ? parentForm.ResizeImage(Image.FromFile(iconPath), 128, 128)
                : parentForm.GetWebsiteIcon(websiteName);

         
            changeIconButton.Enabled = true;
            resetIconButton.Enabled = hasCustomIcon;
        }

        private void ChangeIconButton_Click(object sender, EventArgs e)
        {
            if (websiteList.SelectedItem == null) return;

            string websiteName = websiteList.SelectedItem.ToString();
            using (var openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "Image files (*.png;*.jpg;*.jpeg;*.gif;*.bmp;*.ico)|*.png;*.jpg;*.jpeg;*.gif;*.bmp;*.ico|All files (*.*)|*.*";
                openFileDialog.Title = $"Select Icon for {websiteName}";

                if (openFileDialog.ShowDialog(this) == DialogResult.OK)
                {
                    websiteIcons[websiteName] = openFileDialog.FileName;
                    IconChanged?.Invoke(websiteName, openFileDialog.FileName);
                    WebsiteList_SelectedIndexChanged(null, EventArgs.Empty);
                }
            }
        }

        private void ResetIconButton_Click(object sender, EventArgs e)
        {
            if (websiteList.SelectedItem == null) return;

            string websiteName = websiteList.SelectedItem.ToString();
            websiteIcons.Remove(websiteName);
            IconChanged?.Invoke(websiteName, null);
            WebsiteList_SelectedIndexChanged(null, EventArgs.Empty);
        }
    }

    public class AddWebsiteDialog : Form
    {
        private TextBox nameTextBox;
        private TextBox urlTextBox;
        private PictureBox iconPreview;
        private Button browseButton;
        private Button okButton;
        private Button cancelButton;

        public string WebsiteName => nameTextBox.Text.Trim();
        public string WebsiteUrl => urlTextBox.Text.Trim();
        public string IconPath { get; private set; }

        public AddWebsiteDialog(Form parent)
        {
            this.Text = "Add New Website";
            this.Size = new Size(400, 300);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.TopMost = ((Form1)parent).TopMost; 

            InitializeDialog();
        }

        private void InitializeDialog()
        {
            var mainPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 4,
                Padding = new Padding(10)
            };
            mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
            mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            // Name
            mainPanel.Controls.Add(new Label { Text = "Name:", TextAlign = ContentAlignment.MiddleRight }, 0, 0);
            nameTextBox = new TextBox { Dock = DockStyle.Fill };
            mainPanel.Controls.Add(nameTextBox, 1, 0);

            // URL
            mainPanel.Controls.Add(new Label { Text = "URL:", TextAlign = ContentAlignment.MiddleRight }, 0, 1);
            urlTextBox = new TextBox { Dock = DockStyle.Fill };
            mainPanel.Controls.Add(urlTextBox, 1, 1);

            // Icon
            mainPanel.Controls.Add(new Label { Text = "Icon:", TextAlign = ContentAlignment.MiddleRight }, 0, 2);
            var iconPanel = new Panel { Dock = DockStyle.Fill };
            iconPreview = new PictureBox
            {
                SizeMode = PictureBoxSizeMode.Zoom,
                Dock = DockStyle.Left,
                Width = 32,
                Height = 32,
                Image = new Form1().GetDefaultIcon()
            };
            browseButton = new Button
            {
                Text = "Browse...",
                Dock = DockStyle.Right,
                Width = 80
            };
            browseButton.Click += BrowseButton_Click;
            iconPanel.Controls.Add(browseButton);
            iconPanel.Controls.Add(iconPreview);
            mainPanel.Controls.Add(iconPanel, 1, 2);

            // Buttons
            var buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft
            };
            mainPanel.Controls.Add(buttonPanel, 0, 3);
            mainPanel.SetColumnSpan(buttonPanel, 2);

            okButton = new Button
            {
                Text = "OK",
                DialogResult = DialogResult.OK,
                Width = 80
            };
            okButton.Click += OkButton_Click;
            buttonPanel.Controls.Add(okButton);

            cancelButton = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Width = 80
            };
            buttonPanel.Controls.Add(cancelButton);

            this.Controls.Add(mainPanel);
            this.AcceptButton = okButton;
            this.CancelButton = cancelButton;
        }

        private void BrowseButton_Click(object sender, EventArgs e)
        {
            using (var openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "Image files (*.png;*.jpg;*.jpeg;*.gif;*.bmp;*.ico)|*.png;*.jpg;*.jpeg;*.gif;*.bmp;*.ico|All files (*.*)|*.*";
                openFileDialog.Title = "Select Website Icon";

                if (openFileDialog.ShowDialog(this) == DialogResult.OK)
                {
                    IconPath = openFileDialog.FileName;
                    try
                    {
                        iconPreview.Image = Image.FromFile(IconPath);
                    }
                    catch
                    {
                        MessageBox.Show("Invalid image file", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        IconPath = null;
                        iconPreview.Image = new Form1().GetDefaultIcon();
                    }
                }
            }
        }

        private void OkButton_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(WebsiteName))
            {
                MessageBox.Show("Please enter a website name", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                nameTextBox.Focus();
                this.DialogResult = DialogResult.None;
                return;
            }

            if (string.IsNullOrWhiteSpace(WebsiteUrl) || !Uri.TryCreate(WebsiteUrl, UriKind.Absolute, out _))
            {
                MessageBox.Show("Please enter a valid URL", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                urlTextBox.Focus();
                this.DialogResult = DialogResult.None;
                return;
            }
        }

        
    }

    public class CustomWebsite
    {
        public string Name { get; set; }
        public string Url { get; set; }
        public string IconPath { get; set; }
    }

    internal static class NativeMethods
    {
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HT_CAPTION = 0x2;
    }
}